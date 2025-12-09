using System.IO.Compression;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using UtilityTools.Domain.Entities;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Shared.Extensions;

namespace UtilityTools.Application.Features.Tools.Pdf.Commands.SplitPdf;

public class SplitPdfCommandHandler : IRequestHandler<SplitPdfCommand, SplitPdfResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SplitPdfCommandHandler> _logger;

    public SplitPdfCommandHandler(
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SplitPdfCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SplitPdfResponse> Handle(SplitPdfCommand request, CancellationToken cancellationToken)
    {
        var userId = _httpContextAccessor.HttpContext?.User.GetUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var startTime = DateTime.UtcNow;

        try
        {
            // ✅ Copy file to memory stream first to avoid stream disposal issues
            using var inputMemoryStream = new MemoryStream();
            await request.File.CopyToAsync(inputMemoryStream, cancellationToken);
            inputMemoryStream.Position = 0;

            var sourceDocument = PdfReader.Open(inputMemoryStream, PdfDocumentOpenMode.Import);
            var totalPages = sourceDocument.PageCount;

            try
            {
                var pageRanges = ParsePagesSpec(request.PagesSpec, totalPages);
                var filesCreated = 0;

                using var zipStream = new MemoryStream();
                using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (var (startPage, endPage) in pageRanges)
                    {
                        var outputDocument = new PdfDocument();

                        try
                        {
                            for (int i = startPage - 1; i < endPage && i < totalPages; i++)
                            {
                                outputDocument.AddPage(sourceDocument.Pages[i]);
                            }

                            // ✅ Save PDF to MemoryStream first (ZipArchiveEntry stream doesn't support seeking)
                            using var pdfMemoryStream = new MemoryStream();
                            outputDocument.Save(pdfMemoryStream);
                            pdfMemoryStream.Position = 0;

                            // ✅ Copy from MemoryStream to ZIP entry
                            var entryName = $"page_{startPage}-{endPage}.pdf";
                            var entry = zipArchive.CreateEntry(entryName);

                            using var entryStream = entry.Open();
                            await pdfMemoryStream.CopyToAsync(entryStream, cancellationToken);
                            filesCreated++;
                        }
                        finally
                        {
                            // ✅ Dispose output document
                            outputDocument.Dispose();
                        }
                    }
                }

                zipStream.Position = 0;
                var zipStreamLength = zipStream.Length;

                // Upload ZIP file
                var fileName = $"split_{Guid.NewGuid()}.zip";
                var fileKey = await _fileStorage.UploadAsync(
                    zipStream,
                    fileName,
                    "application/zip",
                    $"pdf/split/{userId}",
                    cancellationToken);

                // Generate download URL
                var downloadUrl = await _fileStorage.GeneratePresignedUrlAsync(
                    fileKey,
                    TimeSpan.FromHours(24),
                    cancellationToken);

                var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

                // Record usage
                var usageRepository = _unitOfWork.Repository<UsageRecord>();
                var usageRecord = new UsageRecord(
                    userId,
                    ToolType.PdfSplit,
                    fileSizeBytes: request.File.Length,
                    processingTimeMs: processingTime,
                    cost: 0m);

                await usageRepository.AddAsync(usageRecord, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "PDF split completed. User: {UserId}, Pages: {Pages}, Files: {Files}, Time: {Time}ms",
                    userId, totalPages, filesCreated, processingTime);

                return new SplitPdfResponse
                {
                    FileKey = fileKey,
                    DownloadUrl = downloadUrl,
                    FileSizeBytes = zipStreamLength,
                    PagesCount = totalPages,
                    FilesCreated = filesCreated
                };
            }
            finally
            {
                // ✅ Dispose source document
                sourceDocument.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error splitting PDF");
            throw;
        }
    }

    private List<(int Start, int End)> ParsePagesSpec(string pagesSpec, int totalPages)
    {
        var ranges = new List<(int, int)>();

        if (pagesSpec.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return new List<(int, int)> { (1, totalPages) };
        }

        var parts = pagesSpec.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            if (part.Contains('-'))
            {
                var range = part.Split('-');
                if (range.Length == 2 &&
                    int.TryParse(range[0], out var start) &&
                    int.TryParse(range[1], out var end))
                {
                    ranges.Add((Math.Max(1, start), Math.Min(end, totalPages)));
                }
            }
            else if (int.TryParse(part, out var page))
            {
                var validPage = Math.Clamp(page, 1, totalPages);
                ranges.Add((validPage, validPage));
            }
        }

        return ranges.OrderBy(r => r.Item1).ToList();
    }
}

