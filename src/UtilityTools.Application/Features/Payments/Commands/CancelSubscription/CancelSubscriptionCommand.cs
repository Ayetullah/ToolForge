using MediatR;

namespace UtilityTools.Application.Features.Payments.Commands.CancelSubscription;

public class CancelSubscriptionCommand : IRequest<CancelSubscriptionResponse>
{
}

public class CancelSubscriptionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

