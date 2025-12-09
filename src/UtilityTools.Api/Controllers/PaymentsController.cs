using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityTools.Application.Features.Payments.Commands.CancelSubscription;
using UtilityTools.Application.Features.Payments.Commands.CreateSubscription;
using UtilityTools.Application.Features.Payments.Commands.HandleWebhook;
using UtilityTools.Domain.Enums;

namespace UtilityTools.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Route("api/v{version:apiVersion}/payments")] // ✅ Support both versioned and non-versioned routes
[ApiVersion("1.0")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IMediator mediator, ILogger<PaymentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("subscribe")]
    [Authorize]
    public async Task<ActionResult<CreateSubscriptionResponse>> Subscribe([FromBody] Dictionary<string, object> body)
    {
        if (body == null || !body.TryGetValue("tier", out var tierValue) || tierValue == null)
        {
            return BadRequest(new { error = "Tier is required" });
        }

        if (!Enum.TryParse<SubscriptionTier>(tierValue.ToString(), true, out var tier))
        {
            return BadRequest(new { error = "Invalid tier. Must be Basic, Pro, or Enterprise" });
        }

        var command = new CreateSubscriptionCommand
        {
            Tier = tier,
            PaymentMethodId = body.TryGetValue("paymentMethodId", out var pmId) ? pmId?.ToString() ?? string.Empty : string.Empty
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("cancel")]
    [Authorize]
    public async Task<ActionResult<CancelSubscriptionResponse>> Cancel([FromBody] CancelSubscriptionCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    [Consumes("application/json")]
    public async Task<ActionResult<HandleWebhookResponse>> Webhook([FromBody] object payload, [FromHeader(Name = "Stripe-Signature")] string? signature)
    {
        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("Stripe webhook called without signature");
            return BadRequest(new { error = "Missing Stripe-Signature header" });
        }

        // Convert payload to string
        var payloadString = payload?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(payloadString))
        {
            // Try to read from request body directly
            using var reader = new StreamReader(Request.Body);
            payloadString = await reader.ReadToEndAsync();
        }

        var command = new HandleWebhookCommand
        {
            Payload = payloadString,
            Signature = signature
        };

        var result = await _mediator.Send(command);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}

