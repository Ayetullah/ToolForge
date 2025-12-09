using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;

namespace UtilityTools.Application.Features.Payments.Commands.HandleWebhook;

public class HandleWebhookCommandHandler : IRequestHandler<HandleWebhookCommand, HandleWebhookResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<HandleWebhookCommandHandler> _logger;

    public HandleWebhookCommandHandler(
        IUnitOfWork unitOfWork,
        IPaymentService paymentService,
        ILogger<HandleWebhookCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HandleWebhookResponse> Handle(HandleWebhookCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify webhook signature
            var isValid = await _paymentService.VerifyWebhookSignatureAsync(
                request.Payload,
                request.Signature,
                cancellationToken);

            if (!isValid)
            {
                _logger.LogWarning("Invalid Stripe webhook signature");
                return new HandleWebhookResponse
                {
                    Success = false,
                    Message = "Invalid webhook signature"
                };
            }

            // Parse webhook event
            // Note: Event parsing should be handled by IPaymentService or a dedicated webhook parser
            // For now, we'll use a simplified approach
            JsonElement stripeEvent;
            try
            {
                stripeEvent = JsonSerializer.Deserialize<JsonElement>(request.Payload);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Stripe webhook event");
                return new HandleWebhookResponse
                {
                    Success = false,
                    Message = "Failed to parse webhook event"
                };
            }
            {
                return new HandleWebhookResponse
                {
                    Success = false,
                    Message = "Failed to parse webhook event"
                };
            }

            // Handle different event types
            var eventType = stripeEvent.GetProperty("type").GetString();
            
            switch (eventType)
            {
                case "customer.subscription.created":
                case "customer.subscription.updated":
                    await HandleSubscriptionUpdate(stripeEvent, cancellationToken);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionDeleted(stripeEvent, cancellationToken);
                    break;

                case "invoice.payment_succeeded":
                    await HandlePaymentSucceeded(stripeEvent, cancellationToken);
                    break;

                case "invoice.payment_failed":
                    await HandlePaymentFailed(stripeEvent, cancellationToken);
                    break;

                default:
                    _logger.LogInformation("Unhandled webhook event type: {EventType}", eventType);
                    break;
            }

            return new HandleWebhookResponse
            {
                Success = true,
                Message = "Webhook processed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return new HandleWebhookResponse
            {
                Success = false,
                Message = $"Error processing webhook: {ex.Message}"
            };
        }
    }

    private async Task HandleSubscriptionUpdate(JsonElement stripeEvent, CancellationToken cancellationToken)
    {
        try
        {
            var data = stripeEvent.GetProperty("data");
            var subscriptionObj = data.GetProperty("object");
            var subscriptionId = subscriptionObj.GetProperty("id").GetString();
            var customerId = subscriptionObj.GetProperty("customer").GetString();
            
            if (string.IsNullOrEmpty(subscriptionId)) return;

            var userRepository = _unitOfWork.Repository<Domain.Entities.User>();
            var user = await userRepository.FirstOrDefaultAsync(u => u.StripeSubscriptionId == subscriptionId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found for subscription {SubscriptionId}", subscriptionId);
                return;
            }

            // Get price ID from subscription items
            var items = subscriptionObj.GetProperty("items").GetProperty("data");
            if (items.GetArrayLength() > 0)
            {
                var priceId = items[0].GetProperty("price").GetProperty("id").GetString();
                var tier = MapStripePriceToTier(priceId ?? string.Empty);
                
                // Get expiration date
                var currentPeriodEnd = subscriptionObj.GetProperty("current_period_end").GetInt64();
                var expiresAt = DateTimeOffset.FromUnixTimeSeconds(currentPeriodEnd).DateTime;

                user.UpdateSubscription(tier, customerId, subscriptionId, expiresAt);
                await userRepository.UpdateAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Subscription updated for user {UserId}: {Tier}", user.Id, tier);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling subscription update webhook");
        }
    }

    private async Task HandleSubscriptionDeleted(JsonElement stripeEvent, CancellationToken cancellationToken)
    {
        try
        {
            var data = stripeEvent.GetProperty("data");
            var subscriptionObj = data.GetProperty("object");
            var subscriptionId = subscriptionObj.GetProperty("id").GetString();
            
            if (string.IsNullOrEmpty(subscriptionId)) return;

            var userRepository = _unitOfWork.Repository<Domain.Entities.User>();
            var user = await userRepository.FirstOrDefaultAsync(u => u.StripeSubscriptionId == subscriptionId, cancellationToken);

            if (user == null) return;

            user.UpdateSubscription(SubscriptionTier.Free, null, null, null);
            await userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Subscription deleted for user {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling subscription deleted webhook");
        }
    }

    private async Task HandlePaymentSucceeded(JsonElement stripeEvent, CancellationToken cancellationToken)
    {
        try
        {
            var data = stripeEvent.GetProperty("data");
            var invoiceObj = data.GetProperty("object");
            var subscriptionId = invoiceObj.TryGetProperty("subscription", out var sub) 
                ? sub.GetString() 
                : null;
            
            if (string.IsNullOrEmpty(subscriptionId)) return;

            var userRepository = _unitOfWork.Repository<Domain.Entities.User>();
            var user = await userRepository.FirstOrDefaultAsync(u => u.StripeSubscriptionId == subscriptionId, cancellationToken);

            if (user == null) return;

            _logger.LogInformation("Payment succeeded for user {UserId}, subscription {SubscriptionId}",
                user.Id, subscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment succeeded webhook");
        }
    }

    private async Task HandlePaymentFailed(JsonElement stripeEvent, CancellationToken cancellationToken)
    {
        try
        {
            var data = stripeEvent.GetProperty("data");
            var invoiceObj = data.GetProperty("object");
            var subscriptionId = invoiceObj.TryGetProperty("subscription", out var sub) 
                ? sub.GetString() 
                : null;
            
            if (string.IsNullOrEmpty(subscriptionId)) return;

            var userRepository = _unitOfWork.Repository<Domain.Entities.User>();
            var user = await userRepository.FirstOrDefaultAsync(u => u.StripeSubscriptionId == subscriptionId, cancellationToken);

            if (user == null) return;

            _logger.LogWarning("Payment failed for user {UserId}, subscription {SubscriptionId}",
                user.Id, subscriptionId);

            // Optionally: Send notification to user, downgrade subscription, etc.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment failed webhook");
        }
    }

    private SubscriptionTier MapStripePriceToTier(string priceId)
    {
        // This should match the price IDs in configuration
        // For now, return Pro as default - should be improved with configuration lookup
        // TODO: Implement proper price ID to tier mapping from configuration
        if (string.IsNullOrEmpty(priceId))
        {
            return SubscriptionTier.Free;
        }
        
        // In production, this should check against configured price IDs
        return SubscriptionTier.Pro;
    }
}

