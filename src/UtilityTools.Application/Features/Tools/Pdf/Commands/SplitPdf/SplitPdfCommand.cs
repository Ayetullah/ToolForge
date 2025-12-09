using MediatR;
using Microsoft.AspNetCore.Http;

namespace UtilityTools.Application.Features.Tools.Pdf.Commands.SplitPdf;

public class SplitPdfCommand : IRequest<SplitPdfResponse>
{
    public IFormFile File { get; set; } = null!;
    public string PagesSpec { get; set; } = string.Empty; // e.g., "1-5,10,15-20" or "all"
}

public class SplitPdfResponse
{
    public string FileKey { get; set; } = string.Empty; // ZIP file containing split PDFs
    public string DownloadUrl { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int PagesCount { get; set; }
    public int FilesCreated { get; set; }
}

