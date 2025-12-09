using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Domain.ValueObjects;

namespace UtilityTools.Infrastructure.FileStorage;

/// <summary>
/// MinIO (S3-compatible) file storage implementation
/// </summary>
public class MinIOFileStorage : IFileStorage
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private readonly ILogger<MinIOFileStorage> _logger;
    private readonly string _baseUrl;

    public MinIOFileStorage(
        IMinioClient minioClient,
        IConfiguration configuration,
        ILogger<MinIOFileStorage> logger)
    {
        _minioClient = minioClient;
        _logger = logger;
        _bucketName = configuration["MinIO:BucketName"] 
            ?? throw new InvalidOperationException("MinIO BucketName is required");
        _baseUrl = configuration["BaseUrl"] ?? "http://localhost:5000";

        // Ensure bucket exists
        EnsureBucketExistsAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureBucketExistsAsync()
    {
        var exists = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_bucketName));
        
        if (!exists)
        {
            await _minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_bucketName));
            _logger.LogInformation("Created MinIO bucket: {BucketName}", _bucketName);
        }
    }

    public async Task<string> UploadAsync(
        Stream fileStream, 
        string fileName, 
        string contentType, 
        string? folder = null, 
        CancellationToken cancellationToken = default)
    {
        var fileKey = GenerateFileKey(fileName, folder);

        await _minioClient.PutObjectAsync(
            new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileKey)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType),
            cancellationToken);

        _logger.LogInformation("File uploaded to MinIO: {FileKey}", fileKey);
        return fileKey;
    }

    public async Task<Stream> DownloadAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        var memoryStream = new MemoryStream();

        await _minioClient.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileKey)
                .WithCallbackStream(stream =>
                {
                    stream.CopyTo(memoryStream);
                }),
            cancellationToken);

        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task<string> GeneratePresignedUrlAsync(
        string fileKey, 
        TimeSpan expiration, 
        CancellationToken cancellationToken = default)
    {
        var url = await _minioClient.PresignedGetObjectAsync(
            new PresignedGetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileKey)
                .WithExpiry((int)expiration.TotalSeconds));

        return url;
    }

    public async Task DeleteAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        await _minioClient.RemoveObjectAsync(
            new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileKey),
            cancellationToken);

        _logger.LogInformation("File deleted from MinIO: {FileKey}", fileKey);
    }

    public async Task<bool> ExistsAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await _minioClient.StatObjectAsync(
                new StatObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileKey),
                cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<FileMetadata> GetMetadataAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        var stat = await _minioClient.StatObjectAsync(
            new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileKey),
            cancellationToken);

        var fileName = Path.GetFileName(fileKey);

        return new FileMetadata
        {
            FileName = fileName,
            ContentType = stat.ContentType,
            SizeBytes = stat.Size,
            StorageKey = fileKey,
            UploadedAt = stat.LastModified
        };
    }

    public async Task CopyAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default)
    {
        await _minioClient.CopyObjectAsync(
            new CopyObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(destinationKey)
                .WithCopyObjectSource(
                    new CopySourceObjectArgs()
                        .WithBucket(_bucketName)
                        .WithObject(sourceKey)),
            cancellationToken);

        _logger.LogInformation("File copied in MinIO: {SourceKey} -> {DestinationKey}", sourceKey, destinationKey);
    }

    private string GenerateFileKey(string fileName, string? folder)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Guid.NewGuid().ToString("N")[..8];
        var extension = Path.GetExtension(fileName);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var sanitized = SanitizeFileName(nameWithoutExt);

        var key = $"{timestamp}_{random}_{sanitized}{extension}";
        return folder != null ? $"{folder}/{key}" : key;
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }
}

