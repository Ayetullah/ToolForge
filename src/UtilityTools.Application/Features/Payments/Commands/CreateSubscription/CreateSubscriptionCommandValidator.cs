using FluentValidation;
using UtilityTools.Domain.Enums;

namespace UtilityTools.Application.Features.Payments.Commands.CreateSubscription;

public class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionCommandValidator()
    {
        RuleFor(v => v.Tier)
            .IsInEnum()
            .Must(tier => tier != SubscriptionTier.Free && tier != SubscriptionTier.Admin)
            .WithMessage("Subscription tier must be Basic, Pro, or Enterprise.");

        // PaymentMethodId is optional for initial subscription creation
        // It can be attached later via Stripe payment intent
    }
}

