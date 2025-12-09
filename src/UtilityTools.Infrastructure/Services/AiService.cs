using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UtilityTools.Application.Common.Options;
using UtilityTools.Domain.Interfaces;

namespace UtilityTools.Infrastructure.Services;

/// <summary>
/// AI service implementation using Google Gemini with retry and circuit breaker
/// </summary>
public class AiService : IAiService
{
    private readonly AiSettings _settings;
    private readonly ILogger<AiService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _apiUrl;
    private readonly string _model;

    public AiService(IOptions<AiSettings> aiSettings, ILogger<AiService> logger, HttpClient httpClient)
    {
        _settings = aiSettings?.Value ?? throw new ArgumentNullException(nameof(aiSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        var provider = _settings.Provider ?? "Gemini";
        
        if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
        {
            _apiKey = _settings.Gemini.ApiKey 
                ?? throw new InvalidOperationException("Gemini API key not configured.");
            _model = _settings.Gemini.Model ?? "gemini-2.5-flash";
            
            // Use ApiUrl from settings, or construct it from model if not provided
            if (!string.IsNullOrWhiteSpace(_settings.Gemini.ApiUrl))
            {
                _apiUrl = _settings.Gemini.ApiUrl;
            }
            else
            {
                // Construct URL from model name
                _apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";
            }
            
            _logger.LogInformation("Gemini AI Service initialized with Model: {Model}, ApiUrl: {ApiUrl}", _model, _apiUrl);
        }
        else
        {
            // Fallback to OpenAI if configured
            _apiKey = _settings.OpenAI.ApiKey 
                ?? throw new InvalidOperationException("AI API key not configured.");
            _apiUrl = "https://api.openai.com/v1/chat/completions";
            _model = _settings.OpenAI.Model ?? "gpt-3.5-turbo";
        }
    }

    public async Task<string> SummarizeTextAsync(string text, int maxLength, string? tone = null, CancellationToken cancellationToken = default)
    {
        var provider = _settings.Provider ?? "Gemini";
        
        if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
        {
            return await SummarizeWithGeminiAsync(text, maxLength, tone, cancellationToken);
        }
        else
        {
            // OpenAI fallback (not implemented)
            _logger.LogWarning("OpenAI provider selected but not fully implemented. Please use Gemini.");
            throw new InvalidOperationException("OpenAI provider is not implemented. Please use Gemini.");
        }
    }

    public async Task<string> SummarizeUrlAsync(string url, int maxLength, string? tone = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // First, fetch the content from the URL
            var content = await _httpClient.GetStringAsync(url, cancellationToken);
            
            // Extract text content (simple HTML stripping - for production, use HtmlAgilityPack or similar)
            var textContent = ExtractTextFromHtml(content);
            
            // Then summarize the content
            return await SummarizeTextAsync(textContent, maxLength, tone, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching content from URL: {Url}", url);
            throw new InvalidOperationException($"Failed to fetch content from URL: {url}", ex);
        }
    }

    public Task<int> GetTokenCountAsync(string text, CancellationToken cancellationToken = default)
    {
        // For Gemini, rough approximation: 1 token ≈ 4 characters
        // Gemini uses a different tokenization, but this is a reasonable approximation
        return Task.FromResult(text.Length / 4);
    }

    private async Task<string> SummarizeWithGeminiAsync(string text, int maxLength, string? tone, CancellationToken cancellationToken)
    {
        try
        {
            // ✅ Check text length and handle very long texts
            // Gemini 2.0 Flash has ~1M token context window, but we'll be conservative
            // Approximate: 1 token ≈ 4 characters, so 1M tokens ≈ 4M characters
            // For safety, limit to ~500K characters (~125K tokens) to leave room for prompt and response
            const int maxInputChars = 500_000;
            
            if (text.Length > maxInputChars)
            {
                _logger.LogWarning("Text is very long ({Length} chars), truncating to {MaxChars} chars for processing", 
                    text.Length, maxInputChars);
                // Truncate but keep the beginning and end for better context
                var start = text.Substring(0, maxInputChars / 2);
                var end = text.Substring(text.Length - maxInputChars / 2);
                text = $"{start}\n\n[... {text.Length - maxInputChars} characters omitted ...]\n\n{end}";
                _logger.LogInformation("Text truncated to {Length} chars (start + end)", text.Length);
            }
            
            var prompt = BuildSummarizationPrompt(text, maxLength, tone);
            
            // ✅ Log input size for debugging
            var estimatedInputTokens = prompt.Length / 4; // Rough estimate
            _logger.LogInformation("Sending text to Gemini. Text length: {TextLength} chars, Estimated tokens: {Tokens}", 
                text.Length, estimatedInputTokens);
            
            // Build Gemini API request
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    // ✅ Calculate tokens: For words, approximately 1.3 tokens per word
                    // For 200 words, we need ~260 tokens, but add significant buffer for complete sentences
                    // maxLength is in words, so multiply by 4-5 to ensure complete responses
                    // Also set a minimum of 2000 tokens to ensure summaries are never cut off
                    maxOutputTokens = Math.Max(2000, maxLength * 5), 
                    temperature = 0.3, // ✅ Lower temperature for more focused summaries
                    topP = 0.95,
                    topK = 40,
                    stopSequences = Array.Empty<string>() // ✅ Don't stop early
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Build URL with API key
            var url = $"{_apiUrl}?key={_apiKey}";

            _logger.LogDebug("Calling Gemini API with model: {Model}", _model);
            
            // ✅ HttpClient is configured with Polly policies via AddHttpClient
            // Timeout is handled by HttpClient.Timeout or CancellationToken
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30)); // Timeout after 30 seconds
            var response = await _httpClient.PostAsync(url, content, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                
                // Handle specific error codes
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    throw new InvalidOperationException("Rate limit exceeded. Please try again later.");
                }
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new InvalidOperationException("Invalid API key. Please check your configuration.");
                }
                
                throw new InvalidOperationException($"Gemini API error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            // ✅ Log full response for debugging
            _logger.LogDebug("Gemini API response: {Response}", responseContent);
            
            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);

            // Extract text from Gemini response
            if (responseJson.TryGetProperty("candidates", out var candidates) && 
                candidates.GetArrayLength() > 0)
            {
                var candidate = candidates[0];
                
                // ✅ Check for finishReason to see if response was cut off
                if (candidate.TryGetProperty("finishReason", out var finishReason))
                {
                    var reason = finishReason.GetString();
                    _logger.LogInformation("Gemini finishReason: {Reason}", reason);
                    
                    if (reason == "MAX_TOKENS")
                    {
                        _logger.LogWarning("Gemini response was truncated due to token limit. Consider increasing maxOutputTokens.");
                    }
                    else if (reason != "STOP")
                    {
                        _logger.LogWarning("Gemini response finished with reason: {Reason}", reason);
                    }
                }
                
                if (candidate.TryGetProperty("content", out var contentElement) &&
                    contentElement.TryGetProperty("parts", out var parts))
                {
                    var partsArray = parts.EnumerateArray();
                    var summaryBuilder = new StringBuilder();
                    
                    // ✅ Collect all text parts
                    foreach (var part in partsArray)
                    {
                        if (part.TryGetProperty("text", out var textElement))
                        {
                            var p = textElement.GetString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(p))
                            {
                                summaryBuilder.Append(p);
                                _logger.LogDebug("Added text part: {Length} chars", p.Length);
                            }
                        }
                    }
                    
                    var summary = summaryBuilder.ToString().Trim();
                    
                    // ✅ Log summary length for debugging
                    _logger.LogInformation("Extracted summary length: {Length} chars, Parts count: {Parts}", 
                        summary.Length, partsArray.Count());
                    
                    // ✅ Check if summary appears to be cut off (ends with incomplete word or sentence)
                    if (summary.Length > 0)
                    {
                        var lastChar = summary[summary.Length - 1];
                        var incompleteEndings = new[] { '.', '!', '?', '。', '！', '？' };
                        
                        // If summary doesn't end with proper punctuation and is not very short, it might be cut off
                        if (summary.Length > 50 && !incompleteEndings.Contains(lastChar) && !char.IsWhiteSpace(lastChar))
                        {
                            _logger.LogWarning("Summary may be incomplete - doesn't end with proper punctuation. Last 50 chars: {LastChars}", 
                                summary.Substring(Math.Max(0, summary.Length - 50)));
                        }
                    }
                    
                    // ✅ Clean up common prefixes that AI might add
                    if (summary.StartsWith("Summary:", StringComparison.OrdinalIgnoreCase))
                    {
                        summary = summary.Substring("Summary:".Length).Trim();
                    }
                    if (summary.StartsWith("In summary:", StringComparison.OrdinalIgnoreCase))
                    {
                        summary = summary.Substring("In summary:".Length).Trim();
                    }
                    
                    if (string.IsNullOrEmpty(summary))
                    {
                        _logger.LogError("Summary is empty after parsing. Response: {Response}", responseContent);
                        throw new InvalidOperationException("AI service returned an empty summary.");
                    }
                    
                    _logger.LogInformation("Successfully generated summary with Gemini. Final length: {Length} chars", summary.Length);
                    return summary;
                }
                else
                {
                    _logger.LogError("No content.parts found in Gemini response. Response: {Response}", responseContent);
                }
            }
            else
            {
                _logger.LogError("No candidates found in Gemini response. Response: {Response}", responseContent);
            }

            _logger.LogWarning("Unexpected Gemini API response format: {Response}", responseContent);
            throw new InvalidOperationException("Unexpected response format from Gemini API");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout calling Gemini API");
            throw new InvalidOperationException("AI service request timed out. Please try again.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Gemini API");
            throw new InvalidOperationException("Error communicating with AI service. Please try again later.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            throw;
        }
    }

    private string BuildSummarizationPrompt(string text, int maxLength, string? tone)
    {
        var toneInstruction = tone switch
        {
            "professional" => "Use a professional and formal tone.",
            "casual" => "Use a casual and friendly tone.",
            "technical" => "Use a technical and precise tone.",
            _ => "Use a neutral tone."
        };

        // ✅ Improved prompt with clear instructions - emphasize reading the ENTIRE text
        return $@"You are an expert text summarizer. Please carefully read the ENTIRE text below and provide a comprehensive, complete summary.

CRITICAL REQUIREMENTS:
- Read the COMPLETE text from beginning to end - do NOT skip any part
- Do NOT summarize only the beginning or first part
- Analyze the entire content to capture all key points
- Ensure your summary reflects the full scope of the text
- Write COMPLETE sentences - never cut off mid-sentence
- Finish all sentences properly with proper punctuation
- The summary must be a complete, coherent text that can stand alone

Summary Requirements:
- Summary should be approximately {maxLength} words (you can go slightly over if needed for completeness)
- Include all key points and main ideas from throughout the entire text
- Maintain the original meaning and context
- Cover all major topics and conclusions mentioned in the text
- {toneInstruction}
- Write in complete, well-formed sentences
- Do not include phrases like 'Summary:' or 'In summary:' - just provide the summary directly
- Make sure every sentence is complete and properly ended
- Do not truncate or cut off your response - provide the full summary

Text to summarize (READ THE ENTIRE TEXT CAREFULLY):
{text}

Now provide a complete, well-formed summary with all sentences properly finished:";
    }

    private string ExtractTextFromHtml(string html)
    {
        // Simple HTML tag removal - for production, use HtmlAgilityPack
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        // Decode HTML entities
        text = text.Replace("&nbsp;", " ")
                   .Replace("&amp;", "&")
                   .Replace("&lt;", "<")
                   .Replace("&gt;", ">")
                   .Replace("&quot;", "\"")
                   .Replace("&#39;", "'");
        return text.Trim();
    }
}
