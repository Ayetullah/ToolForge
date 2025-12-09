using MediatR;

namespace UtilityTools.Application.Features.Auth.Commands.Logout;

public class LogoutCommand : IRequest<Unit>
{
    public string RefreshToken { get; set; } = string.Empty;
}

