using Microsoft.Extensions.Logging;
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
            ToolType.VideoCompress => SubscriptionTier.Basic,
            ToolType.DocToPdf => SubscriptionTier.Basic,
            _ => SubscriptionTier.Free
        };
    }
}

