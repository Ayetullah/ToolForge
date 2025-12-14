using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using UtilityTools.Application.Common.Options;
using UtilityTools.Domain.Entities;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Shared.Extensions;

namespace UtilityTools.Application.Features.Tools.Pdf.Commands.MergePdf;

public class MergePdfCommandHandler : IRequestHandler<MergePdfCommand, MergePdfResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly IOptions<FileLimitsSettings> _fileLimits;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<MergePdfCommandHandler> _logger;
    private const long MaxSyncFileSize = 20_971_520; // 20MB

    public MergePdfCommandHandler(
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        IOptions<FileLimitsSettings> fileLimits,
        IHttpContextAccessor httpContextAccessor,
        ILogger<MergePdfCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _fileLimits = fileLimits ?? throw new ArgumentNullException(nameof(fileLimits));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MergePdfResponse> Handle(MergePdfCommand request, CancellationToken cancellationToken)
    {
        // User context is optional for free tools
        var userId = _httpContextAccessor.HttpContext?.User.GetUserId();
        var isAuthenticated = userId.HasValue;

        var startTime = DateTime.UtcNow;
        var totalSize = request.Files.Sum(f => f.Length);

        // Check if should process as background job (only for authenticated users)
        if (isAuthenticated && totalSize > MaxSyncFileSize)
        {
            return await ProcessAsBackgroundJob(request, userId!.Value, cancellationToken);
        }

        // Process synchronously
        Stream? mergedPdf = null;
        try
        {
            mergedPdf = MergePdfFiles(request.Files);
            var mergedPdfLength = mergedPdf.Length;
            
            // Upload merged PDF (use anonymous path if not authenticated)
            var storagePath = isAuthenticated ? $"pdf/merge/{userId}" : "pdf/merge/anonymous";
            var fileKey = await _fileStorage.UploadAsync(
                mergedPdf,
                "merged.pdf",
                "application/pdf",
                storagePath,
                cancellationToken);

            // Generate download URL
            var downloadUrl = await _fileStorage.GeneratePresignedUrlAsync(
                fileKey,
                TimeSpan.FromHours(24),
                cancellationToken);

            var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            // Record usage only for authenticated users
            if (isAuthenticated)
            {
                var usageRepository = _unitOfWork.Repository<UsageRecord>();
                var usageRecord = new UsageRecord(
                    userId!.Value,
                    ToolType.PdfMerge,
                    fileSizeBytes: totalSize,
                    processingTimeMs: processingTime,
                    cost: 0m);

                await usageRepository.AddAsync(usageRecord, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "PDF merge completed. User: {UserId}, Files: {Count}, Size: {Size} bytes, Time: {Time}ms",
                userId?.ToString() ?? "anonymous", request.Files.Count, totalSize, processingTime);

            return new MergePdfResponse
            {
                FileKey = fileKey,
                DownloadUrl = downloadUrl,
                FileSizeBytes = mergedPdfLength,
                IsBackgroundJob = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging PDF files");
            throw;
        }
        finally
        {
            // ✅ Dispose merged PDF stream
            mergedPdf?.Dispose();
        }
    }

    private Stream MergePdfFiles(List<IFormFile> files)
    {
        var outputDocument = new PdfDocument();

        try
        {
            foreach (var file in files)
            {
                // ✅ Copy file to memory stream first to avoid stream disposal issues
                using var inputStream = new MemoryStream();
                file.CopyTo(inputStream);
                inputStream.Position = 0;

                // ✅ Open PDF from memory stream
                var inputDocument = PdfReader.Open(inputStream, PdfDocumentOpenMode.Import);

                // ✅ Copy all pages to output document
                foreach (PdfPage page in inputDocument.Pages)
                {
                    outputDocument.AddPage(page);
                }
            }

            // ✅ Save to memory stream
            var outputStream = new MemoryStream();
            outputDocument.Save(outputStream);
            outputStream.Position = 0;

            return outputStream;
        }
        finally
        {
            // ✅ Dispose output document
            outputDocument.Dispose();
        }
    }

    private Task<MergePdfResponse> ProcessAsBackgroundJob(
        MergePdfCommand request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        // TODO: Implement background job processing with Hangfire
        // For now, return a job ID placeholder
        var jobId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            "PDF merge queued as background job. User: {UserId}, JobId: {JobId}, Files: {Count}",
            userId, jobId, request.Files.Count);

        return Task.FromResult(new MergePdfResponse
        {
            JobId = jobId,
            IsBackgroundJob = true
        });
    }
}

