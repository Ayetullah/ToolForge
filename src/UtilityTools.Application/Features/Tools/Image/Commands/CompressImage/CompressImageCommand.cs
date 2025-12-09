using MediatR;
using Microsoft.AspNetCore.Http;

namespace UtilityTools.Application.Features.Tools.Image.Commands.CompressImage;

public class CompressImageCommand : IRequest<CompressImageResponse>
{
    public IFormFile File { get; set; } = null!;
    public int Quality { get; set; } = 80; // 1-100
    public string? TargetFormat { get; set; } // jpg, png, webp
    public int? MaxWidth { get; set; }
    public int? MaxHeight { get; set; }
}

public class CompressImageResponse
{
    public string FileKey { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public long OriginalSizeBytes { get; set; }
    public long CompressedSizeBytes { get; set; }
    public double CompressionRatio { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
}
