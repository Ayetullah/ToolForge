using MediatR;
using Microsoft.AspNetCore.Http;

namespace UtilityTools.Application.Features.Tools.Excel.Commands.CleanExcel;

public class CleanExcelCommand : IRequest<CleanExcelResponse>
{
    public IFormFile File { get; set; } = null!;
    public CleanOptions Options { get; set; } = new();
}

public class CleanOptions
{
    public bool RemoveEmptyRows { get; set; } = true;
    public bool RemoveEmptyColumns { get; set; } = true;
    public bool TrimWhitespace { get; set; } = true;
    public bool RemoveDuplicates { get; set; } = false;
    public bool StandardizeFormats { get; set; } = true;
    public string? OutputFormat { get; set; } // xlsx, csv
}

public class CleanExcelResponse
{
    public string FileKey { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public long OriginalSizeBytes { get; set; }
    public long CleanedSizeBytes { get; set; }
    public int RowsRemoved { get; set; }
    public int ColumnsRemoved { get; set; }
    public string ContentType { get; set; } = string.Empty;
}

