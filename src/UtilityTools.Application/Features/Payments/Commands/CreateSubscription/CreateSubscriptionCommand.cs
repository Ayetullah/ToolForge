using MediatR;
using UtilityTools.Domain.Enums;

namespace UtilityTools.Application.Features.Payments.Commands.CreateSubscription;

public class CreateSubscriptionCommand : IRequest<CreateSubscriptionResponse>
{
    public SubscriptionTier Tier { get; set; }
    public string PaymentMethodId { get; set; } = string.Empty; // Stripe PaymentMethod ID
}

public class CreateSubscriptionResponse
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty; // For payment confirmation
    public string Status { get; set; } = string.Empty;
    public SubscriptionTier Tier { get; set; }
}

