namespace UtilityTools.Application.Common.Options;

/// <summary>
/// File storage configuration settings
/// </summary>
public class FileStorageSettings
{
    public const string SectionName = "FileStorage";
    
    public string Type { get; set; } = "Local";
    public string LocalPath { get; set; } = "./storage";
    public S3Settings S3 { get; set; } = new();
}

public class S3Settings
{
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
}

