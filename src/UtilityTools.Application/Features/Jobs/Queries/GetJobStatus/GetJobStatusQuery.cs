using MediatR;

namespace UtilityTools.Application.Features.Jobs.Queries.GetJobStatus;

public class GetJobStatusQuery : IRequest<GetJobStatusResponse>
{
    public Guid JobId { get; set; }
}

public class GetJobStatusResponse
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ProgressPercentage { get; set; }
    public string? OutputFileKey { get; set; }
    public string? DownloadUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? SignedUrlExpiresAt { get; set; }
}

