using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Shared.Extensions;

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
            throw new KeyNotFoundException($"Job with ID {request.JobId} not found");
        }

        // Verify user owns the job
        if (userId.HasValue && job.UserId != userId.Value)
        {
            throw new UnauthorizedAccessException("You do not have access to this job");
        }

        return new GetJobStatusResponse
        {
            JobId = job.Id,
            Status = job.Status.ToString().ToLower(),
            ProgressPercentage = job.ProgressPercentage,
            OutputFileKey = job.OutputFileKey,
            DownloadUrl = job.SignedDownloadUrl,
            ErrorMessage = job.ErrorMessage,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            SignedUrlExpiresAt = job.SignedUrlExpiresAt
        };
    }
}

