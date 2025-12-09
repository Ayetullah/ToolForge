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
}

