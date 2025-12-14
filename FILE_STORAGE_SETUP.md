# File Storage Setup

## ✅ Completed

### Storage Implementation
- ✅ **LocalFileStorage** - Complete implementation
  - File upload/download
  - Presigned URL generation (token-based)
  - File metadata
  - File deletion
  - File existence check
  - File copying

## 🔧 Configuration

### Local Storage
```json
{
  "FileStorage": {
    "LocalPath": "./storage"
  },
  "BaseUrl": "http://localhost:5000"
}
```

## 📝 Usage

### Dependency Injection
Local storage is automatically registered:

```csharp
// In DependencyInjection.cs
services.AddScoped<IFileStorage, LocalFileStorage>();
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

### Local Storage
- Token-based presigned URLs
- Secure token generation (SHA256)
- File path sanitization
- File access via secure tokens with expiration

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
- Files stored in `./storage` directory (configurable via `FileStorage:LocalPath`)
- Accessible via presigned URLs with tokens: `/api/files/download/{key}?token=...`
- Files are organized by folder structure in the storage directory

## 📝 Notes

- Files are stored locally on the server
- Presigned URLs use token-based authentication
- File keys are generated consistently with timestamp and random GUID
- Folder structure is preserved in file keys
- All file operations are async and support cancellation tokens
