using UtilityTools.Domain.Enums;

namespace UtilityTools.Application.Common.Interfaces;

/// <summary>
/// Subscription and tier management service
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Check if user has required subscription tier for a feature
    /// </summary>
    Task<bool> HasRequiredTierAsync(
        Guid userId, 
        SubscriptionTier requiredTier, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get required tier for a tool/feature
    /// </summary>
    SubscriptionTier GetRequiredTierForTool(ToolType toolType);
    
    /// <summary>
    /// Check if tool is a premium feature that requires daily limit
    /// </summary>
    bool IsPremiumTool(ToolType toolType);
    
    /// <summary>
    /// Check if user has exceeded daily usage limit for a premium tool
    /// Returns (hasLimit, currentUsage, dailyLimit, canUse)
    /// </summary>
    Task<(bool HasLimit, int CurrentUsage, int DailyLimit, bool CanUse)> CheckDailyUsageLimitAsync(
        Guid userId,
        ToolType toolType,
        CancellationToken cancellationToken = default);
}

