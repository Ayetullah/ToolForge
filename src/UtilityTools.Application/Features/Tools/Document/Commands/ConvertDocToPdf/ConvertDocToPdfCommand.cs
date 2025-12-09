using MediatR;
using Microsoft.AspNetCore.Http;

namespace UtilityTools.Application.Features.Tools.Document.Commands.ConvertDocToPdf;

public class ConvertDocToPdfCommand : IRequest<ConvertDocToPdfResponse>
{
    public IFormFile File { get; set; } = null!;
}

public class ConvertDocToPdfResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? FileKey { get; set; }
    public string? DownloadUrl { get; set; }
    public long OriginalSizeBytes { get; set; }
    public long? ConvertedSizeBytes { get; set; }
    public string? ErrorMessage { get; set; }
}

