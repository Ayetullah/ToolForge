using UtilityTools.Domain.Common;
using UtilityTools.Domain.Enums;

namespace UtilityTools.Domain.Entities;

/// <summary>
/// Tracks usage of tools for billing and rate limiting
/// </summary>
public class UsageRecord : BaseEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public ToolType ToolType { get; private set; }
    public long FileSizeBytes { get; private set; }
    public int ProcessingTimeMs { get; private set; }
    public int TokensUsed { get; private set; } // For AI tools
    public decimal Cost { get; private set; }
    public string? JobId { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; } = new();

    private UsageRecord() { }

    public UsageRecord(Guid userId, ToolType toolType, long fileSizeBytes, int processingTimeMs, int tokensUsed = 0, decimal cost = 0, string? jobId = null)
    {
        UserId = userId;
        ToolType = toolType;
        FileSizeBytes = fileSizeBytes;
        ProcessingTimeMs = processingTimeMs;
        TokensUsed = tokensUsed;
        Cost = cost;
        JobId = jobId;
        UpdateTimestamp();
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdateTimestamp();
    }
}

