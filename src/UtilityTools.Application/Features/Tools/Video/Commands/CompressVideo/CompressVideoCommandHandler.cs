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

namespace UtilityTools.Application.Features.Tools.Video.Commands.CompressVideo;

public class CompressVideoCommandHandler : IRequestHandler<CompressVideoCommand, CompressVideoResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CompressVideoCommandHandler> _logger;

    public CompressVideoCommandHandler(
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CompressVideoCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CompressVideoResponse> Handle(CompressVideoCommand request, CancellationToken cancellationToken)
    {
        var userId = _httpContextAccessor.HttpContext?.User.GetUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var originalSize = request.File.Length;

        try
        {
            // Video compression requires FFmpeg, which should run in a background job
            // For now, we'll create a job record and return immediately
            // The actual compression will be handled by a worker process

            // Save the uploaded file temporarily
            using var inputStream = request.File.OpenReadStream();
            var tempFileName = $"temp_{Guid.NewGuid()}{Path.GetExtension(request.File.FileName)}";
            var tempFileKey = await _fileStorage.UploadAsync(
                inputStream,
                tempFileName,
                request.File.ContentType,
                $"video/temp/{userId}",
                cancellationToken);

            // Create a job record
            var parameters = new Dictionary<string, object>
            {
                { "Quality", request.Quality },
                { "Preset", request.Preset ?? "medium" },
                { "Codec", request.Codec ?? "libx264" },
                { "OriginalFileName", request.File.FileName },
                { "OriginalSize", originalSize }
            };
            
            if (request.MaxWidth.HasValue) parameters["MaxWidth"] = request.MaxWidth.Value;
            if (request.MaxHeight.HasValue) parameters["MaxHeight"] = request.MaxHeight.Value;
            if (request.BitrateKbps.HasValue) parameters["BitrateKbps"] = request.BitrateKbps.Value;

            var job = new Job(userId, ToolType.VideoCompress, tempFileKey, parameters);

            var jobRepository = _unitOfWork.Repository<Job>();
            await jobRepository.AddAsync(job, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Video compression job created. User: {UserId}, JobId: {JobId}, File: {FileName}, Size: {Size} bytes",
                userId, job.Id, request.File.FileName, originalSize);

              // Enqueue job to Hangfire
              BackgroundJob.Enqueue<JobProcessors>(
                  x => x.ProcessVideoCompression(job.Id, CancellationToken.None));

            return new CompressVideoResponse
            {
                JobId = job.Id.ToString(),
                Status = "pending",
                OriginalSizeBytes = originalSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating video compression job");
            return new CompressVideoResponse
            {
                Status = "failed",
                OriginalSizeBytes = originalSize,
                ErrorMessage = $"Error creating compression job: {ex.Message}"
            };
        }
    }
}

