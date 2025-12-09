namespace UtilityTools.Application.Common.Options;

/// <summary>
/// File size limits configuration
/// </summary>
public class FileLimitsSettings
{
    public const string SectionName = "FileLimits";
    
    public long MaxFileSize { get; set; } = 104857600; // 100MB
    public long MaxPdfSize { get; set; } = 20971520; // 20MB
    public long MaxImageSize { get; set; } = 10485760; // 10MB
    public long MaxVideoSize { get; set; } = 524288000; // 500MB
}

