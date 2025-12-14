using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Shared.Extensions;
using UtilityTools.Domain.Enums;

namespace UtilityTools.Application.Features.Jobs.Queries.GetJobStatus;

public class GetJobStatusQueryHandler : IRequestHandler<GetJobStatusQuery, GetJobStatusResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GetJobStatusQueryHandler> _logger;

    public GetJobStatusQueryHandler(
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GetJobStatusQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<GetJobStatusResponse> Handle(GetJobStatusQuery request, CancellationToken cancellationToken)
    {
        var userId = _httpContextAccessor.HttpContext?.User.GetUserId();

        var jobRepository = _unitOfWork.Repository<Domain.Entities.Job>();
        var job = await jobRepository.GetByIdAsync(request.JobId, cancellationToken);

        if (job == null)
        {
            return new GetJobStatusResponse
            {
                JobId = request.JobId,
                Status = (int)JobStatus.NotFound,
                StatusName = GetJobStatusResponse.StatusNotFound,
                ErrorMessage = $"Job with ID {request.JobId} not found"
            };
        }

        // Verify user owns the job
        if (userId.HasValue && job.UserId != userId.Value)
        {
            return new GetJobStatusResponse
            {
                JobId = request.JobId,
                Status = (int)JobStatus.Unauthorized,
                StatusName = GetJobStatusResponse.StatusUnauthorized,
                ErrorMessage = "You do not have access to this job"
            };
        }

        var response = new GetJobStatusResponse
        {
            JobId = job.Id,
            Status = (int)job.Status, // Return enum as int
            StatusName = job.Status.ToString().ToLower(), // Human-readable name for backward compatibility
            ProgressPercentage = job.ProgressPercentage,
            OutputFileKey = job.OutputFileKey,
            DownloadUrl = job.SignedDownloadUrl,
            SignedDownloadUrl = job.SignedDownloadUrl,
            ErrorMessage = job.ErrorMessage,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            SignedUrlExpiresAt = job.SignedUrlExpiresAt
        };
        
        _logger.LogDebug("Job status query for {JobId}: Status={Status} ({StatusName}), HasDownloadUrl={HasUrl}", 
            request.JobId, response.Status, response.StatusName, !string.IsNullOrEmpty(response.SignedDownloadUrl));
        
        return response;
    }
}

