using MediatR;
using Microsoft.AspNetCore.Http;

namespace UtilityTools.Application.Features.Tools.Video.Commands.CompressVideo;

public class CompressVideoCommand : IRequest<CompressVideoResponse>
{
    public IFormFile File { get; set; } = null!;
    public int Quality { get; set; } = 23; // CRF value (18-28, lower = better quality)
    public string? Preset { get; set; } = "medium"; // ultrafast, superfast, veryfast, faster, fast, medium, slow, slower, veryslow
    public int? MaxWidth { get; set; }
    public int? MaxHeight { get; set; }
    public int? BitrateKbps { get; set; } // Alternative to CRF
    public string? Codec { get; set; } = "libx264"; // libx264, libx265, libvpx-vp9
}

public class CompressVideoResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? FileKey { get; set; }
    public string? DownloadUrl { get; set; }
    public long OriginalSizeBytes { get; set; }
    public long? CompressedSizeBytes { get; set; }
    public double? CompressionRatio { get; set; }
    public string? ErrorMessage { get; set; }
}

