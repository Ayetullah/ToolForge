using UtilityTools.Domain.Enums;

namespace UtilityTools.Domain.Interfaces;

/// <summary>
/// Payment service abstraction for Stripe
/// </summary>
public interface IPaymentService
{
    Task<string> CreateCustomerAsync(string email, string name, CancellationToken cancellationToken = default);
    Task<string> CreateSubscriptionAsync(string customerId, SubscriptionTier tier, CancellationToken cancellationToken = default);
    Task CancelSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<bool> VerifyWebhookSignatureAsync(string payload, string signature, CancellationToken cancellationToken = default);
    Task UpdateSubscriptionAsync(string subscriptionId, SubscriptionTier tier, CancellationToken cancellationToken = default);
}

