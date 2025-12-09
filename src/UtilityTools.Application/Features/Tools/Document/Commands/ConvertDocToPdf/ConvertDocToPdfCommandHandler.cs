using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UtilityTools.Application.Jobs;
using UtilityTools.Domain.Entities;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Shared.Extensions;

namespace UtilityTools.Application.Features.Tools.Document.Commands.ConvertDocToPdf;

public class ConvertDocToPdfCommandHandler : IRequestHandler<ConvertDocToPdfCommand, ConvertDocToPdfResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ConvertDocToPdfCommandHandler> _logger;

    public ConvertDocToPdfCommandHandler(
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ConvertDocToPdfCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ConvertDocToPdfResponse> Handle(ConvertDocToPdfCommand request, CancellationToken cancellationToken)
    {
        var userId = _httpContextAccessor.HttpContext?.User.GetUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var originalSize = request.File.Length;

        try
        {
            // Document conversion requires LibreOffice/unoconv, which should run in a background job
            // For now, we'll create a job record and return immediately
            // The actual conversion will be handled by a worker process

            // Save the uploaded file temporarily
            using var inputStream = request.File.OpenReadStream();
            var tempFileName = $"temp_{Guid.NewGuid()}{Path.GetExtension(request.File.FileName)}";
            var tempFileKey = await _fileStorage.UploadAsync(
                inputStream,
                tempFileName,
                request.File.ContentType,
                $"document/temp/{userId}",
                cancellationToken);

            // Create a job record
            var parameters = new Dictionary<string, object>
            {
                { "OriginalFileName", request.File.FileName },
                { "OriginalSize", originalSize },
                { "ContentType", request.File.ContentType }
            };

            var job = new Job(userId, ToolType.DocToPdf, tempFileKey, parameters);

            var jobRepository = _unitOfWork.Repository<Job>();
            await jobRepository.AddAsync(job, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Document to PDF conversion job created. User: {UserId}, JobId: {JobId}, File: {FileName}, Size: {Size} bytes",
                userId, job.Id, request.File.FileName, originalSize);

              // Enqueue job to Hangfire
              BackgroundJob.Enqueue<JobProcessors>(
                  x => x.ProcessDocumentConversion(job.Id, CancellationToken.None));

            return new ConvertDocToPdfResponse
            {
                JobId = job.Id.ToString(),
                Status = "pending",
                OriginalSizeBytes = originalSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document conversion job");
            return new ConvertDocToPdfResponse
            {
                Status = "failed",
                OriginalSizeBytes = originalSize,
                ErrorMessage = $"Error creating conversion job: {ex.Message}"
            };
        }
    }
}

