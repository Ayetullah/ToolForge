using MediatR;

namespace UtilityTools.Application.Features.Auth.Commands.ResendVerification;

public class ResendVerificationCommand : IRequest<ResendVerificationResponse>
{
    public string Email { get; set; } = string.Empty;
}

public class ResendVerificationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

