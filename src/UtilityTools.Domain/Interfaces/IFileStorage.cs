using UtilityTools.Domain.ValueObjects;

namespace UtilityTools.Domain.Interfaces;

/// <summary>
/// File storage abstraction for local file storage
/// </summary>
public interface IFileStorage
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string? folder = null, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string fileKey, CancellationToken cancellationToken = default);
    Task<string> GeneratePresignedUrlAsync(string fileKey, TimeSpan expiration, CancellationToken cancellationToken = default);
    Task DeleteAsync(string fileKey, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string fileKey, CancellationToken cancellationToken = default);
    Task<FileMetadata> GetMetadataAsync(string fileKey, CancellationToken cancellationToken = default);
    Task CopyAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default);
}

