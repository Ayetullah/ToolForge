using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Shared.Extensions;

namespace UtilityTools.Application.Features.Payments.Commands.CreateSubscription;

public class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, CreateSubscriptionResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentService _paymentService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CreateSubscriptionCommandHandler> _logger;

    public CreateSubscriptionCommandHandler(
        IUnitOfWork unitOfWork,
        IPaymentService paymentService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CreateSubscriptionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CreateSubscriptionResponse> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
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

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // Create or get Stripe customer
            string customerId;
            if (string.IsNullOrEmpty(user.StripeCustomerId))
            {
                customerId = await _paymentService.CreateCustomerAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    cancellationToken);
            }
            else
            {
                customerId = user.StripeCustomerId;
            }

            // Create subscription
            var subscriptionId = await _paymentService.CreateSubscriptionAsync(
                customerId,
                request.Tier,
                cancellationToken);

            // TODO: Get client secret from payment service or implement in IPaymentService
            var clientSecret = string.Empty;

            // Update user subscription
            // TODO: Get subscription expiration from payment service
            var expiresAt = DateTime.UtcNow.AddMonths(1); // Default to 1 month, should come from Stripe
            user.UpdateSubscription(request.Tier, customerId, subscriptionId, expiresAt);
            await userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Subscription created for user {UserId}: {SubscriptionId}, Tier: {Tier}",
                userId, subscriptionId, request.Tier);

            return new CreateSubscriptionResponse
            {
                SubscriptionId = subscriptionId,
                ClientSecret = clientSecret,
                Status = "pending", // Should be obtained from payment service
                Tier = request.Tier
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription for user {UserId}", userId);
            throw;
        }
    }
}

