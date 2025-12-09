using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Application.Common.Options;
using UtilityTools.Domain.Interfaces;

namespace UtilityTools.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOptions<JwtSettings> _jwtSettings;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _jwtSettings = jwtSettings ?? throw new ArgumentNullException(nameof(jwtSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }
        
        var userRepository = _unitOfWork.Repository<Domain.Entities.User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);

        if (user == null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        // ✅ Use JWT service instead of inline logic
        var token = _jwtTokenService.GenerateToken(user);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.Value.ExpirationMinutes);

        // Update refresh token
        user.SetRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(_jwtSettings.Value.RefreshTokenExpirationDays));
        await userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Token refreshed for user: {Email}", user.Email);

        return new RefreshTokenResponse
        {
            AccessToken = token,
            RefreshToken = newRefreshToken,
            ExpiresAt = expiresAt
        };
    }
}

