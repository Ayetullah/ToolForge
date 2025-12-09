# File Storage Adapters Setup

## ✅ Completed

### Storage Implementations
- ✅ **LocalFileStorage** - Complete implementation
  - File upload/download
  - Presigned URL generation (token-based)
  - File metadata
  - File deletion
  - File existence check
  - File copying

- ✅ **S3FileStorage** - AWS S3 implementation
  - Full S3 API integration
  - Presigned URL generation
  - Server-side encryption (AES256)
  - Custom endpoint support (for S3-compatible services)
  - All IFileStorage methods implemented

- ✅ **MinIOFileStorage** - MinIO (S3-compatible) implementation
  - MinIO client integration
  - Automatic bucket creation
  - Presigned URL generation
  - All IFileStorage methods implemented

### Configuration
- ✅ Storage type selection via configuration
- ✅ Automatic adapter registration based on `FileStorage:Type`
- ✅ Support for Local, S3, and MinIO

## 🔧 Configuration

### Local Storage
```json
{
  "FileStorage": {
    "Type": "Local",
    "LocalPath": "./storage"
  },
  "BaseUrl": "http://localhost:5000"
}
```

### AWS S3
```json
{
  "FileStorage": {
    "Type": "S3",
    "S3BucketName": "my-bucket",
    "S3Region": "us-east-1",
    "S3AccessKey": "your-access-key",
    "S3SecretKey": "your-secret-key",
    "S3Endpoint": "" // Optional: for S3-compatible services
  },
  "BaseUrl": "https://my-api.com"
}
```

### MinIO
```json
{
  "FileStorage": {
    "Type": "MinIO"
  },
  "MinIO": {
    "Endpoint": "http://localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin123",
    "UseSSL": false,
    "BucketName": "utilitytools"
  },
  "BaseUrl": "http://localhost:5000"
}
```

## 📝 Usage

### Dependency Injection
The storage adapter is automatically registered based on configuration:

```csharp
// In DependencyInjection.cs
var storageType = configuration["FileStorage:Type"] ?? "Local";

if (storageType.Equals("S3", StringComparison.OrdinalIgnoreCase))
{
    // Register S3
}
else if (storageType.Equals("MinIO", StringComparison.OrdinalIgnoreCase))
{
    // Register MinIO
}
else
{
    // Default to Local
    services.AddScoped<IFileStorage, LocalFileStorage>();
}
```

### Using in Code
```csharp
public class MyService
{
    private readonly IFileStorage _fileStorage;

    public MyService(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public async Task<string> UploadFile(Stream fileStream, string fileName)
    {
        var fileKey = await _fileStorage.UploadAsync(
            fileStream,
            fileName,
            "application/pdf",
            "my-folder",
            cancellationToken);

        var downloadUrl = await _fileStorage.GeneratePresignedUrlAsync(
            fileKey,
            TimeSpan.FromHours(24),
            cancellationToken);

        return downloadUrl;
    }
}
```

## 🔒 Security Features

### S3
- Server-side encryption (AES256)
- Presigned URLs with expiration
- IAM-based access control (via AWS)

### MinIO
- Presigned URLs with expiration
- Access key/secret key authentication
- Bucket-level access control

### Local
- Token-based presigned URLs
- Secure token generation (SHA256)
- File path sanitization

## 🚀 Docker Setup

### MinIO in docker-compose.yml
```yaml
minio:
  image: minio/minio:latest
  ports:
    - "9000:9000"
    - "9001:9001"
  environment:
    MINIO_ROOT_USER: minioadmin
    MINIO_ROOT_PASSWORD: minioadmin123
  command: server /data --console-address ":9001"
  volumes:
    - minio_data:/data
  healthcheck:
    test: ["CMD", "curl", "-f", "http://localhost:9000/minio/health/live"]
    interval: 30s
    timeout: 20s
    retries: 3
```

## 📚 API Methods

All storage adapters implement `IFileStorage` interface:

- `UploadAsync(Stream, string, string, string?, CancellationToken)` - Upload file
- `DownloadAsync(string, CancellationToken)` - Download file
- `GeneratePresignedUrlAsync(string, TimeSpan, CancellationToken)` - Generate presigned URL
- `DeleteAsync(string, CancellationToken)` - Delete file
- `ExistsAsync(string, CancellationToken)` - Check if file exists
- `GetMetadataAsync(string, CancellationToken)` - Get file metadata
- `CopyAsync(string, string, CancellationToken)` - Copy file

## 🔍 Testing

### Local Storage
- Files stored in `./storage` directory
- Accessible via presigned URLs with tokens

### S3
- Requires AWS credentials
- Bucket must exist
- IAM permissions required

### MinIO
- Use docker-compose for local development
- Access MinIO Console at `http://localhost:9001`
- Default credentials: `minioadmin` / `minioadmin123`

## 📝 Notes

- All adapters use the same `IFileStorage` interface
- Switching storage types requires only configuration change
- Presigned URLs are generated differently:
  - **Local**: Token-based URLs (`/api/files/download/{key}?token=...`)
  - **S3**: AWS presigned URLs
  - **MinIO**: MinIO presigned URLs
- File keys are generated consistently across all adapters
- Folder structure is preserved in file keys

