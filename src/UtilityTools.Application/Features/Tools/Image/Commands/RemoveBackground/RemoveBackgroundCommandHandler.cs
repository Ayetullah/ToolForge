using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Application.Jobs;
using UtilityTools.Domain.Entities;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Shared.Extensions;

namespace UtilityTools.Application.Features.Tools.Image.Commands.RemoveBackground;

public class RemoveBackgroundCommandHandler : IRequestHandler<RemoveBackgroundCommand, RemoveBackgroundResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RemoveBackgroundCommandHandler> _logger;

    public RemoveBackgroundCommandHandler(
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RemoveBackgroundCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RemoveBackgroundResponse> Handle(RemoveBackgroundCommand request, CancellationToken cancellationToken)
    {
        var userId = _httpContextAccessor.HttpContext?.User.GetUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var originalSize = request.File.Length;

        try
        {
            // Background removal requires AI service (e.g., remove.bg API) or image processing
            // For premium users, this should use an AI service
            // For now, we'll create a job record and return immediately
            // The actual processing will be handled by a worker process or AI service

            // Save the uploaded file temporarily
            using var inputStream = request.File.OpenReadStream();
            var tempFileName = $"temp_{Guid.NewGuid()}{Path.GetExtension(request.File.FileName)}";
            var tempFileKey = await _fileStorage.UploadAsync(
                inputStream,
                tempFileName,
                request.File.ContentType,
                $"image/background-removal/temp/{userId}",
                cancellationToken);

            // Create a job record
            var parameters = new Dictionary<string, object>
            {
                { "OriginalFileName", request.File.FileName },
                { "OriginalSize", originalSize },
                { "Transparent", request.Transparent }
            };
            
            if (!string.IsNullOrEmpty(request.BackgroundColor))
            {
                parameters["BackgroundColor"] = request.BackgroundColor;
            }

            var job = new Job(userId, ToolType.ImageRemoveBackground, tempFileKey, parameters);

            var jobRepository = _unitOfWork.Repository<Job>();
            await jobRepository.AddAsync(job, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Background removal job created. User: {UserId}, JobId: {JobId}, File: {FileName}, Size: {Size} bytes",
                userId, job.Id, request.File.FileName, originalSize);

            // Check if user has premium subscription
            var subscriptionService = _httpContextAccessor.HttpContext?.RequestServices
                .GetRequiredService<Application.Common.Interfaces.ISubscriptionService>();
            
            if (subscriptionService == null)
            {
                throw new InvalidOperationException("Subscription service not available");
            }
            
            var requiredTier = subscriptionService.GetRequiredTierForTool(ToolType.ImageRemoveBackground);
            var hasAccess = await subscriptionService.HasRequiredTierAsync(userId, requiredTier, cancellationToken);
            
            if (!hasAccess)
            {
                job.Fail("Premium subscription required for background removal. Please upgrade to Pro tier.");
                await jobRepository.UpdateAsync(job, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                return new RemoveBackgroundResponse
                {
                    Status = "failed",
                    OriginalSizeBytes = originalSize,
                    ErrorMessage = "Premium subscription required. Please upgrade to Pro tier."
                };
            }

              // Enqueue job to Hangfire
              BackgroundJob.Enqueue<JobProcessors>(
                  x => x.ProcessBackgroundRemoval(job.Id, CancellationToken.None));

            return new RemoveBackgroundResponse
            {
                JobId = job.Id.ToString(),
                Status = "pending",
                OriginalSizeBytes = originalSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating background removal job");
            return new RemoveBackgroundResponse
            {
                Status = "failed",
                OriginalSizeBytes = originalSize,
                ErrorMessage = $"Error creating background removal job: {ex.Message}"
            };
        }
    }
}

