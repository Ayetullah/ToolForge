using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Shared.Extensions;

namespace UtilityTools.Application.Features.Payments.Commands.CancelSubscription;

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, CancelSubscriptionResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentService _paymentService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CancelSubscriptionCommandHandler> _logger;

    public CancelSubscriptionCommandHandler(
        IUnitOfWork unitOfWork,
        IPaymentService paymentService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CancelSubscriptionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CancelSubscriptionResponse> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var userId = _httpContextAccessor.HttpContext?.User.GetUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var userRepository = _unitOfWork.Repository<Domain.Entities.User>();
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found");

        if (string.IsNullOrEmpty(user.StripeSubscriptionId))
        {
            return new CancelSubscriptionResponse
            {
                Success = false,
                Message = "No active subscription found"
            };
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            await _paymentService.CancelSubscriptionAsync(
                user.StripeSubscriptionId,
                cancellationToken);

            // Update user to Free tier
            user.UpdateSubscription(SubscriptionTier.Free, null, null, null);
            await userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Subscription cancelled for user {UserId}", userId);

            return new CancelSubscriptionResponse
            {
                Success = true,
                Message = "Subscription cancelled successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription for user {UserId}", userId);
            return new CancelSubscriptionResponse
            {
                Success = false,
                Message = $"Error cancelling subscription: {ex.Message}"
            };
        }
    }
}

