using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Domain.ValueObjects;

namespace UtilityTools.Infrastructure.FileStorage;

/// <summary>
/// Local file storage implementation
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorage> _logger;
    private readonly IConfiguration _configuration;

    public LocalFileStorage(IConfiguration configuration, ILogger<LocalFileStorage> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _basePath = configuration["FileStorage:LocalPath"] ?? "./storage";
        
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string? folder = null, CancellationToken cancellationToken = default)
    {
        var folderPath = folder != null ? Path.Combine(_basePath, folder) : _basePath;
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // ✅ Generate file key without folder (folder is already in folderPath)
        var fileKey = GenerateFileKey(fileName, null);
        var fullPath = Path.Combine(folderPath, fileKey);
        
        // ✅ Return full key path relative to base path
        var returnKey = folder != null ? Path.Combine(folder, fileKey) : fileKey;

        using var fileStreamWriter = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await fileStream.CopyToAsync(fileStreamWriter, cancellationToken);

        _logger.LogInformation("File uploaded: {FileKey}", returnKey);
        return returnKey;
    }

    public async Task<Stream> DownloadAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(fileKey);
        
        if (!File.Exists(fullPath))
        {
            // Log additional information for debugging
            _logger.LogWarning(
                "File not found. FileKey: {FileKey}, FullPath: {FullPath}, BasePath: {BasePath}, Exists: {Exists}",
                fileKey, fullPath, _basePath, File.Exists(fullPath));
            
            // Try to find the file with different path separators
            var normalizedKey = fileKey.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            var alternativePath = Path.Combine(_basePath, normalizedKey);
            
            if (File.Exists(alternativePath))
            {
                _logger.LogInformation("Found file using normalized path: {Path}", alternativePath);
                fullPath = alternativePath;
            }
            else
            {
                throw new FileNotFoundException($"File not found: {fileKey}. Searched paths: {fullPath}, {alternativePath}");
            }
        }

        var memoryStream = new MemoryStream();
        using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public Task<string> GeneratePresignedUrlAsync(string fileKey, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        // For local storage, generate a token-based URL
        var token = GenerateSecureToken(fileKey, expiration);
        var baseUrl = _configuration["BaseUrl"] ?? "http://localhost:5000";
        // ✅ URL encode the fileKey to handle slashes in the path
        var encodedFileKey = Uri.EscapeDataString(fileKey);
        var url = $"{baseUrl}/api/files/download/{encodedFileKey}?token={token}&expires={DateTime.UtcNow.Add(expiration).ToBinary()}";
        
        return Task.FromResult(url);
    }

    public Task DeleteAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(fileKey);
        
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("File deleted: {FileKey}", fileKey);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(fileKey);
        return Task.FromResult(File.Exists(fullPath));
    }

    public async Task<FileMetadata> GetMetadataAsync(string fileKey, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(fileKey);
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {fileKey}");
        }

        var fileInfo = new FileInfo(fullPath);
        var fileName = Path.GetFileName(fileKey);

        return new FileMetadata
        {
            FileName = fileName,
            ContentType = GetContentType(fileName),
            SizeBytes = fileInfo.Length,
            StorageKey = fileKey,
            UploadedAt = fileInfo.CreationTimeUtc
        };
    }

    public async Task CopyAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default)
    {
        var sourcePath = GetFullPath(sourceKey);
        var destPath = GetFullPath(destinationKey);

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Source file not found: {sourceKey}");
        }

        var destDirectory = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(destDirectory) && !Directory.Exists(destDirectory))
        {
            Directory.CreateDirectory(destDirectory);
        }

        File.Copy(sourcePath, destPath, overwrite: true);
        await Task.CompletedTask;
    }

    private string GetFullPath(string fileKey)
    {
        // Normalize path separators
        var normalizedKey = fileKey.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        // Remove leading separators to avoid issues with Path.Combine
        normalizedKey = normalizedKey.TrimStart(Path.DirectorySeparatorChar);
        return Path.Combine(_basePath, normalizedKey);
    }

    private string GenerateFileKey(string fileName, string? folder)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Guid.NewGuid().ToString("N")[..8];
        var extension = Path.GetExtension(fileName);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var sanitized = SanitizeFileName(nameWithoutExt);
        
        var key = $"{timestamp}_{random}_{sanitized}{extension}";
        return folder != null ? Path.Combine(folder, key) : key;
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries))
            .Replace(" ", "_");
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".zip" => "application/zip",
            ".json" => "application/json",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }

    private string GenerateSecureToken(string fileKey, TimeSpan expiration)
    {
        var secret = _configuration["Jwt:SecretKey"] ?? "default-secret-key";
        var data = $"{fileKey}:{DateTime.UtcNow.Add(expiration).ToBinary()}:{secret}";
        
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}

