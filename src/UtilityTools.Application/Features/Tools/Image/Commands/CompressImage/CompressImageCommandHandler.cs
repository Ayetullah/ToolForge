using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using UtilityTools.Domain.Entities;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Shared.Extensions;

namespace UtilityTools.Application.Features.Tools.Image.Commands.CompressImage;

public class CompressImageCommandHandler : IRequestHandler<CompressImageCommand, CompressImageResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CompressImageCommandHandler> _logger;

    public CompressImageCommandHandler(
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CompressImageCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CompressImageResponse> Handle(CompressImageCommand request, CancellationToken cancellationToken)
    {
        var userId = _httpContextAccessor.HttpContext?.User.GetUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var startTime = DateTime.UtcNow;
        var originalSize = request.File.Length;

        try
        {
            using var inputStream = request.File.OpenReadStream();
            using var image = await SixLabors.ImageSharp.Image.LoadAsync(inputStream, cancellationToken);

            var originalWidth = image.Width;
            var originalHeight = image.Height;

            // Resize if needed
            if (request.MaxWidth.HasValue || request.MaxHeight.HasValue)
            {
                var maxWidth = request.MaxWidth ?? image.Width;
                var maxHeight = request.MaxHeight ?? image.Height;

                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(maxWidth, maxHeight),
                    Mode = ResizeMode.Max
                }));
            }

            // Determine output format
            var targetFormat = DetermineOutputFormat(request.TargetFormat, request.File.ContentType);
            var contentType = GetContentType(targetFormat);
            var extension = GetExtension(targetFormat);

            // Compress and save
            using var outputStream = new MemoryStream();
            
            switch (targetFormat.ToLower())
            {
                case "jpeg":
                case "jpg":
                    await image.SaveAsJpegAsync(outputStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                    {
                        Quality = request.Quality
                    }, cancellationToken);
                    break;

                case "png":
                    await image.SaveAsPngAsync(outputStream, cancellationToken);
                    break;

                case "webp":
                    await image.SaveAsWebpAsync(outputStream, new SixLabors.ImageSharp.Formats.Webp.WebpEncoder
                    {
                        Quality = request.Quality
                    }, cancellationToken);
                    break;

                default:
                    await image.SaveAsJpegAsync(outputStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                    {
                        Quality = request.Quality
                    }, cancellationToken);
                    break;
            }

            outputStream.Position = 0;
            var compressedSize = outputStream.Length;

            // Upload compressed image
            var fileName = $"compressed_{Guid.NewGuid()}{extension}";
            var fileKey = await _fileStorage.UploadAsync(
                outputStream,
                fileName,
                contentType,
                $"image/compress/{userId}",
                cancellationToken);

            // Generate download URL
            var downloadUrl = await _fileStorage.GeneratePresignedUrlAsync(
                fileKey,
                TimeSpan.FromHours(24),
                cancellationToken);

            var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            var compressionRatio = originalSize > 0 
                ? (1.0 - (double)compressedSize / originalSize) * 100 
                : 0;

            // Record usage
            var usageRepository = _unitOfWork.Repository<UsageRecord>();
            var usageRecord = new UsageRecord(
                userId,
                ToolType.ImageCompress,
                fileSizeBytes: originalSize,
                processingTimeMs: processingTime,
                cost: 0m);

            await usageRepository.AddAsync(usageRecord, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Image compressed. User: {UserId}, Original: {Original} bytes, Compressed: {Compressed} bytes, Ratio: {Ratio}%, Time: {Time}ms",
                userId, originalSize, compressedSize, compressionRatio, processingTime);

            return new CompressImageResponse
            {
                FileKey = fileKey,
                DownloadUrl = downloadUrl,
                OriginalSizeBytes = originalSize,
                CompressedSizeBytes = compressedSize,
                CompressionRatio = compressionRatio,
                ContentType = contentType,
                Width = image.Width,
                Height = image.Height
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compressing image");
            throw;
        }
    }

    private string DetermineOutputFormat(string? targetFormat, string contentType)
    {
        if (!string.IsNullOrEmpty(targetFormat))
        {
            return targetFormat.ToLower();
        }

        return contentType.ToLower() switch
        {
            "image/jpeg" or "image/jpg" => "jpeg",
            "image/png" => "png",
            "image/webp" => "webp",
            "image/gif" => "jpeg", // Convert GIF to JPEG
            _ => "jpeg"
        };
    }

    private string GetContentType(string format)
    {
        return format.ToLower() switch
        {
            "jpeg" or "jpg" => "image/jpeg",
            "png" => "image/png",
            "webp" => "image/webp",
            _ => "image/jpeg"
        };
    }

    private string GetExtension(string format)
    {
        return format.ToLower() switch
        {
            "jpeg" or "jpg" => ".jpg",
            "png" => ".png",
            "webp" => ".webp",
            _ => ".jpg"
        };
    }
}
