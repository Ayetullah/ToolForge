using MediatR;
using Microsoft.AspNetCore.Http;

namespace UtilityTools.Application.Features.Tools.Pdf.Commands.MergePdf;

public class MergePdfCommand : IRequest<MergePdfResponse>
{
    public List<IFormFile> Files { get; set; } = new();
    public Dictionary<string, string>? Metadata { get; set; }
}

public class MergePdfResponse
{
    public string? FileKey { get; set; }
    public string? DownloadUrl { get; set; }
    public long FileSizeBytes { get; set; }
    public string? JobId { get; set; }
    public bool IsBackgroundJob { get; set; }
}

