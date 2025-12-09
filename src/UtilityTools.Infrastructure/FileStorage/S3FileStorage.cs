using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Domain.ValueObjects;

namespace UtilityTools.Infrastructure.FileStorage;

/// <summary>
/// AWS S3 file storage implementation
/// </summary>
public class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<S3FileStorage> _logger;
    private readonly string _baseUrl;

    public S3FileStorage(
        IAmazonS3 s3Client,
        IConfiguration configuration,
        ILogger<S3FileStorage> logger)
    {
        _s3Client = s3Client;
        _logger = logger;
        _bucketName = configuration["FileStorage:S3BucketName"] 
            ?? throw new InvalidOperationException("S3BucketName is required");
        _baseUrl = configuration["BaseUrl"] ?? "http://localhost:5000";
    }

    public async Task<string> UploadAsync(
        Stream fileStream, 
        string fileName, 
        string contentType, 
        string? folder = null, 
        CancellationToken cancellationToken = default)
    {
        var fileKey = GenerateFileKey(fileName, folder);

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = fileKey,
            InputStream = fileStream,
            ContentType = contentType,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);

        _logger.LogInformation("File uploaded to S3: {FileKey}", fileKey);
        return fileKey;
    }

    public async Task<Stream> DownloadAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = fileKey
        };

        var response = await _s3Client.GetObjectAsync(request, cancellationToken);
        var memoryStream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public async Task<string> GeneratePresignedUrlAsync(
        string fileKey, 
        TimeSpan expiration, 
        CancellationToken cancellationToken = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = fileKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiration)
        };

        var url = await _s3Client.GetPreSignedURLAsync(request);
        return url;
    }

    public async Task DeleteAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = fileKey
        };

        await _s3Client.DeleteObjectAsync(request, cancellationToken);
        _logger.LogInformation("File deleted from S3: {FileKey}", fileKey);
    }

    public async Task<bool> ExistsAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = fileKey
            };

            await _s3Client.GetObjectMetadataAsync(request, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<FileMetadata> GetMetadataAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        var request = new GetObjectMetadataRequest
        {
            BucketName = _bucketName,
            Key = fileKey
        };

        var response = await _s3Client.GetObjectMetadataAsync(request, cancellationToken);
        var fileName = Path.GetFileName(fileKey);

        return new FileMetadata
        {
            FileName = fileName,
            ContentType = response.Headers.ContentType ?? "application/octet-stream",
            SizeBytes = response.ContentLength,
            StorageKey = fileKey,
            UploadedAt = response.LastModified
        };
    }

    public async Task CopyAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default)
    {
        var request = new CopyObjectRequest
        {
            SourceBucket = _bucketName,
            SourceKey = sourceKey,
            DestinationBucket = _bucketName,
            DestinationKey = destinationKey
        };

        await _s3Client.CopyObjectAsync(request, cancellationToken);
        _logger.LogInformation("File copied in S3: {SourceKey} -> {DestinationKey}", sourceKey, destinationKey);
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

