using MediatR;

namespace UtilityTools.Application.Features.Jobs.Queries.GetJobStatus;

public class GetJobStatusQuery : IRequest<GetJobStatusResponse>
{
    public Guid JobId { get; set; }
}

public class GetJobStatusResponse
{
    // Status name constants for error cases
    public const string StatusNotFound = "not_found";
    public const string StatusUnauthorized = "unauthorized";
    
    public Guid JobId { get; set; }
    public int Status { get; set; } // JobStatus enum as int (0=Pending, 1=Processing, 2=Completed, 3=Failed, 4=Cancelled)
    public string StatusName { get; set; } = string.Empty; // Human-readable status name
    public int ProgressPercentage { get; set; }
    public string? OutputFileKey { get; set; }
    public string? DownloadUrl { get; set; }
    public string? SignedDownloadUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? SignedUrlExpiresAt { get; set; }
}

