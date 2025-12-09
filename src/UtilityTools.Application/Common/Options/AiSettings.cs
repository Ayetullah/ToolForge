namespace UtilityTools.Application.Common.Options;

/// <summary>
/// AI service configuration settings
/// </summary>
public class AiSettings
{
    public const string SectionName = "AI";
    
    public string Provider { get; set; } = "Gemini";
    public GeminiSettings Gemini { get; set; } = new();
    public OpenAISettings OpenAI { get; set; } = new();
}

public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.5-flash";
}

public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4";
    public int MaxTokens { get; set; } = 2000;
}

