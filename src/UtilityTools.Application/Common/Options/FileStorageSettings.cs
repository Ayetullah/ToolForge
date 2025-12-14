namespace UtilityTools.Application.Common.Options;

/// <summary>
/// File storage configuration settings (Local storage only)
/// </summary>
public class FileStorageSettings
{
    public const string SectionName = "FileStorage";
    
    public string LocalPath { get; set; } = "./storage";
}

