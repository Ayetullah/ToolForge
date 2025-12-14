namespace UtilityTools.Domain.Enums;

/// <summary>
/// Background job status enumeration
/// </summary>
public enum JobStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    NotFound = -1,      // Special value for API responses when job is not found
    Unauthorized = -2   // Special value for API responses when user doesn't have access
}

