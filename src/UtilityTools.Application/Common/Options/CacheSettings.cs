namespace UtilityTools.Application.Common.Options;

/// <summary>
/// Cache configuration settings
/// </summary>
public class CacheSettings
{
    public const string SectionName = "Cache";
    
    public int DefaultExpirationMinutes { get; set; } = 30;
    public int SizeLimit { get; set; } = 1024;
}

