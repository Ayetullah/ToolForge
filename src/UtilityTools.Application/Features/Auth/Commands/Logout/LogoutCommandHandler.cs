using MediatR;
using Microsoft.Extensions.Logging;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;

namespace UtilityTools.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<LogoutCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<Domain.Entities.User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);

        if (user != null)
        {
            user.ClearRefreshToken();
            await userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("User logged out: {Email}", user.Email);
        }

        return Unit.Value;
    }
}

