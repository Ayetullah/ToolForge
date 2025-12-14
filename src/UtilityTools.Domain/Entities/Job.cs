using UtilityTools.Domain.Common;
using UtilityTools.Domain.Enums;

namespace UtilityTools.Domain.Entities;

/// <summary>
/// Background job entity for tracking async processing
/// </summary>
public class Job : BaseEntity
{
    public Guid? UserId { get; private set; }
    public User? User { get; private set; }
    public ToolType ToolType { get; private set; }
    public JobStatus Status { get; private set; } = JobStatus.Pending;
    public string? InputFileKey { get; private set; }
    public string? OutputFileKey { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int ProgressPercentage { get; private set; }
    public Dictionary<string, object> Parameters { get; private set; } = new();
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? SignedDownloadUrl { get; private set; }
    public DateTime? SignedUrlExpiresAt { get; private set; }

    private Job() { }

    public Job(Guid? userId, ToolType toolType, string? inputFileKey, Dictionary<string, object>? parameters = null)
    {
        UserId = userId;
        ToolType = toolType;
        InputFileKey = inputFileKey;
        Parameters = parameters ?? new Dictionary<string, object>();
        UpdateTimestamp();
    }

    public void Start()
    {
        Status = JobStatus.Processing;
        StartedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void Complete(string outputFileKey, string? signedDownloadUrl = null, DateTime? urlExpiresAt = null)
    {
        Status = JobStatus.Completed;
        OutputFileKey = outputFileKey;
        CompletedAt = DateTime.UtcNow;
        ProgressPercentage = 100;
        SignedDownloadUrl = signedDownloadUrl;
        SignedUrlExpiresAt = urlExpiresAt;
        UpdateTimestamp();
    }

    public void Fail(string errorMessage)
    {
        Status = JobStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void UpdateProgress(int percentage)
    {
        ProgressPercentage = Math.Clamp(percentage, 0, 100);
        UpdateTimestamp();
    }
}

