namespace UtilityTools.Domain.Interfaces;

/// <summary>
/// AI service abstraction for OpenAI/Gemini
/// </summary>
public interface IAiService
{
    Task<string> SummarizeTextAsync(string text, int maxLength, string? tone = null, CancellationToken cancellationToken = default);
    Task<string> SummarizeUrlAsync(string url, int maxLength, string? tone = null, CancellationToken cancellationToken = default);
    Task<int> GetTokenCountAsync(string text, CancellationToken cancellationToken = default);
}

