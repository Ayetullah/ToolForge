namespace UtilityTools.Domain.ValueObjects;

/// <summary>
/// Value object representing file metadata
/// </summary>
public record FileMetadata
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public string? StorageKey { get; init; }
    public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
    public Dictionary<string, string> Tags { get; init; } = new();

    public FileMetadata() { }

    public FileMetadata(string fileName, string contentType, long sizeBytes, string? storageKey = null)
    {
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StorageKey = storageKey;
    }
}

