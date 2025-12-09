using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using UtilityTools.Domain.Entities;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;

namespace UtilityTools.Application.Features.Tools.Pdf.Commands.MergePdfs;

public class MergePdfsCommandHandler : IRequestHandler<MergePdfsCommand, MergePdfsResponse>
{
    private readonly IFileStorage _fileStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MergePdfsCommandHandler> _logger;

    public MergePdfsCommandHandler(
        IFileStorage fileStorage,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IConfiguration configuration,
        ILogger<MergePdfsCommandHandler> logger)
    {
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MergePdfsResponse> Handle(MergePdfsCommand request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var totalSize = request.Files.Sum(f => f.Length);
        const long syncThreshold = 20 * 1024 * 1024; // 20MB

        // For large files, create background job (TODO: implement)
        if (totalSize > syncThreshold)
        {
            _logger.LogInformation("Large file detected ({Size} bytes), creating background job", totalSize);
            // TODO: Create background job
            throw new NotImplementedException("Background jobs not yet implemented");
        }

        // Synchronous processing for small files
        var mergedPdf = new PdfDocument();
        int totalPages = 0;

        try
        {
            foreach (var file in request.Files)
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream, cancellationToken);
                stream.Position = 0;

                var sourcePdf = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
                totalPages += sourcePdf.PageCount;

                foreach (PdfPage page in sourcePdf.Pages)
                {
                    mergedPdf.AddPage(page);
                }
            }

            // Save merged PDF to memory stream
            using var outputStream = new MemoryStream();
            mergedPdf.Save(outputStream);
            outputStream.Position = 0;

            // Upload to storage
            var fileName = $"merged_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            var fileKey = await _fileStorage.UploadAsync(
                outputStream,
                fileName,
                "application/pdf",
                "pdf/merged",
                cancellationToken);

            // Generate presigned URL
            var downloadUrl = await _fileStorage.GeneratePresignedUrlAsync(
                fileKey,
                TimeSpan.FromHours(24),
                cancellationToken);

            var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            // Record usage
            if (_currentUserService.UserId.HasValue)
            {
                var usageRepository = _unitOfWork.Repository<UsageRecord>();
                var usageRecord = new UsageRecord(
                    _currentUserService.UserId.Value,
                    ToolType.PdfMerge,
                    fileSizeBytes: totalSize,
                    processingTimeMs: processingTime);

                await usageRepository.AddAsync(usageRecord, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "PDFs merged successfully. Files: {Count}, Pages: {Pages}, Size: {Size} bytes, Time: {Time}ms",
                request.Files.Count, totalPages, outputStream.Length, processingTime);

            return new MergePdfsResponse
            {
                FileKey = fileKey,
                DownloadUrl = downloadUrl,
                FileSizeBytes = outputStream.Length,
                PagesCount = totalPages,
                IsBackgroundJob = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging PDFs");
            throw;
        }
    }
}

