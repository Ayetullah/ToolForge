using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.Interfaces;

namespace UtilityTools.Application.Common.Services;

/// <summary>
/// Subscription service implementation
/// </summary>
public class SubscriptionService : ISubscriptionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubscriptionService> _logger;
    private const int PremiumDailyLimit = 1; // Premium features: 1 operation per day
    
    public SubscriptionService(
        IUnitOfWork unitOfWork,
        ILogger<SubscriptionService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<bool> HasRequiredTierAsync(
        Guid userId, 
        SubscriptionTier requiredTier, 
        CancellationToken cancellationToken = default)
    {
        var userRepository = _unitOfWork.Repository<Domain.Entities.User>();
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for tier check", userId);
            return false;
        }
        
        // Admin has access to everything
        if (user.SubscriptionTier == SubscriptionTier.Admin)
            return true;
        
        // Check if subscription is expired
        if (user.SubscriptionExpiresAt.HasValue && 
            user.SubscriptionExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogInformation(
                "User {UserId} subscription expired at {ExpiresAt}", 
                userId, user.SubscriptionExpiresAt);
            return false;
        }
        
        return (int)user.SubscriptionTier >= (int)requiredTier;
    }
    
    public SubscriptionTier GetRequiredTierForTool(ToolType toolType)
    {
        return toolType switch
        {
            ToolType.ImageRemoveBackground => SubscriptionTier.Pro,
            _ => SubscriptionTier.Free
        };
    }
    
    public bool IsPremiumTool(ToolType toolType)
    {
        // Premium tools that have daily usage limits
        return toolType switch
        {
            ToolType.ImageRemoveBackground => true,
            _ => false
        };
    }
    
    public async Task<(bool HasLimit, int CurrentUsage, int DailyLimit, bool CanUse)> CheckDailyUsageLimitAsync(
        Guid userId,
        ToolType toolType,
        CancellationToken cancellationToken = default)
    {
        // If not a premium tool, no limit
        if (!IsPremiumTool(toolType))
        {
            return (HasLimit: false, CurrentUsage: 0, DailyLimit: 0, CanUse: true);
        }
        
        // Get user subscription tier
        var userRepository = _unitOfWork.Repository<Domain.Entities.User>();
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for daily usage limit check", userId);
            return (HasLimit: false, CurrentUsage: 0, DailyLimit: 0, CanUse: false);
        }
        
        // Premium users (Basic, Pro, Enterprise, Admin) have unlimited access
        // Only Free tier users have daily limit (1 operation per day for trial)
        if (user.SubscriptionTier != SubscriptionTier.Free)
        {
            _logger.LogDebug(
                "User {UserId} has premium tier {Tier}, unlimited access granted",
                userId, user.SubscriptionTier);
            return (HasLimit: false, CurrentUsage: 0, DailyLimit: 0, CanUse: true);
        }
        
        // Check if subscription is expired (treat as Free)
        if (user.SubscriptionExpiresAt.HasValue && 
            user.SubscriptionExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogDebug(
                "User {UserId} subscription expired, treating as Free tier for limit check",
                userId);
        }
        
        // For Free tier users: Check today's usage for this tool (1 operation per day limit)
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        
        var usageRepository = _unitOfWork.Repository<Domain.Entities.UsageRecord>();
        var todayUsageCount = await usageRepository.GetQueryable()
            .Where(ur => ur.UserId == userId 
                && ur.ToolType == toolType
                && ur.CreatedAt >= today 
                && ur.CreatedAt < tomorrow)
            .CountAsync(cancellationToken);
        
        var canUse = todayUsageCount < PremiumDailyLimit;
        
        _logger.LogDebug(
            "Daily usage check for Free tier user {UserId}, tool {ToolType}: {CurrentUsage}/{DailyLimit}, CanUse: {CanUse}",
            userId, toolType, todayUsageCount, PremiumDailyLimit, canUse);
        
        return (HasLimit: true, CurrentUsage: todayUsageCount, DailyLimit: PremiumDailyLimit, CanUse: canUse);
    }
}

