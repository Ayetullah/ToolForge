using MediatR;
using Microsoft.AspNetCore.Http;

namespace UtilityTools.Application.Features.Tools.Pdf.Commands.MergePdfs;

public class MergePdfsCommand : IRequest<MergePdfsResponse>
{
    public List<IFormFile> Files { get; set; } = new();
    public Dictionary<string, string>? Metadata { get; set; }
}

public class MergePdfsResponse
{
    public string FileKey { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int PagesCount { get; set; }
    public bool IsBackgroundJob { get; set; }
    public string? JobId { get; set; }
}

