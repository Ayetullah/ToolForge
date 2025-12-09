using MediatR;

namespace UtilityTools.Application.Features.Payments.Commands.HandleWebhook;

public class HandleWebhookCommand : IRequest<HandleWebhookResponse>
{
    public string Payload { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}

public class HandleWebhookResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

