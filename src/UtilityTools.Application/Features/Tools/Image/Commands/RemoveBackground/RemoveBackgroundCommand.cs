using MediatR;
using Microsoft.AspNetCore.Http;

namespace UtilityTools.Application.Features.Tools.Image.Commands.RemoveBackground;

public class RemoveBackgroundCommand : IRequest<RemoveBackgroundResponse>
{
    public IFormFile File { get; set; } = null!;
    public string? BackgroundColor { get; set; } // Hex color for replacement (e.g., "#FFFFFF" for white)
    public bool Transparent { get; set; } = true; // Use transparent background
}

public class RemoveBackgroundResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? FileKey { get; set; }
    public string? DownloadUrl { get; set; }
    public long OriginalSizeBytes { get; set; }
    public long? ProcessedSizeBytes { get; set; }
    public string? ErrorMessage { get; set; }
}

