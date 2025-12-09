using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using UtilityTools.Application.Common.Options;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.Interfaces;

namespace UtilityTools.Infrastructure.Services;

/// <summary>
/// Stripe payment service implementation
/// </summary>
public class StripePaymentService : IPaymentService
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripePaymentService> _logger;
    private readonly string _webhookSecret;

    public StripePaymentService(IOptions<StripeSettings> stripeSettings, ILogger<StripePaymentService> logger)
    {
        _settings = stripeSettings?.Value ?? throw new ArgumentNullException(nameof(stripeSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        if (string.IsNullOrEmpty(_settings.SecretKey))
        {
            throw new InvalidOperationException("Stripe SecretKey is required");
        }

        StripeConfiguration.ApiKey = _settings.SecretKey;
        _webhookSecret = _settings.WebhookSecret ?? string.Empty;
    }

    public async Task<string> CreateCustomerAsync(string email, string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new CustomerCreateOptions
            {
                Email = email,
                Name = name
            };

            var service = new CustomerService();
            var customer = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation("Stripe customer created: {CustomerId} for {Email}", customer.Id, email);
            return customer.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error creating Stripe customer for {Email}", email);
            throw;
        }
    }

    public async Task<string> CreateSubscriptionAsync(string customerId, SubscriptionTier tier, CancellationToken cancellationToken = default)
    {
        try
        {
            var priceId = GetPriceIdForTier(tier);
            if (string.IsNullOrEmpty(priceId))
            {
                throw new InvalidOperationException($"No price ID configured for tier {tier}");
            }

            var options = new SubscriptionCreateOptions
            {
                Customer = customerId,
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions
                    {
                        Price = priceId
                    }
                },
                PaymentBehavior = "default_incomplete",
                PaymentSettings = new SubscriptionPaymentSettingsOptions
                {
                    PaymentMethodTypes = new List<string> { "card" }
                },
                Expand = new List<string> { "latest_invoice.payment_intent" }
            };

            var service = new SubscriptionService();
            var subscription = await service.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation("Stripe subscription created: {SubscriptionId} for customer {CustomerId}, tier {Tier}",
                subscription.Id, customerId, tier);

            return subscription.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error creating Stripe subscription for customer {CustomerId}, tier {Tier}", customerId, tier);
            throw;
        }
    }

    public async Task CancelSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new SubscriptionCancelOptions
            {
                InvoiceNow = false,
                Prorate = false
            };

            var service = new SubscriptionService();
            await service.CancelAsync(subscriptionId, options, cancellationToken: cancellationToken);

            _logger.LogInformation("Stripe subscription cancelled: {SubscriptionId}", subscriptionId);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error cancelling Stripe subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public Task<bool> VerifyWebhookSignatureAsync(string payload, string signature, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_webhookSecret))
            {
                _logger.LogWarning("Stripe webhook secret not configured, skipping signature verification");
                return Task.FromResult(true); // In development, allow without verification
            }

            var stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                _webhookSecret);

            return Task.FromResult(stripeEvent != null);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error verifying Stripe webhook signature");
            return Task.FromResult(false);
        }
    }

    public async Task UpdateSubscriptionAsync(string subscriptionId, SubscriptionTier tier, CancellationToken cancellationToken = default)
    {
        try
        {
            var priceId = GetPriceIdForTier(tier);
            if (string.IsNullOrEmpty(priceId))
            {
                throw new InvalidOperationException($"No price ID configured for tier {tier}");
            }

            // Get current subscription
            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.GetAsync(subscriptionId, cancellationToken: cancellationToken);

            // Update subscription items
            var updateOptions = new SubscriptionUpdateOptions
            {
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions
                    {
                        Id = subscription.Items.Data[0].Id,
                        Price = priceId
                    }
                },
                ProrationBehavior = "create_prorations"
            };

            await subscriptionService.UpdateAsync(subscriptionId, updateOptions, cancellationToken: cancellationToken);

            _logger.LogInformation("Stripe subscription updated: {SubscriptionId} to tier {Tier}", subscriptionId, tier);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error updating Stripe subscription {SubscriptionId} to tier {Tier}", subscriptionId, tier);
            throw;
        }
    }

    private string? GetPriceIdForTier(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.Basic => _settings.PriceIds.Basic,
            SubscriptionTier.Pro => _settings.PriceIds.Pro,
            SubscriptionTier.Enterprise => _settings.PriceIds.Enterprise,
            _ => null
        };
    }
}

