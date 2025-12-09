using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Application.Common.Options;
using UtilityTools.Domain.Interfaces;

namespace UtilityTools.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOptions<JwtSettings> _jwtSettings;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<LoginCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _jwtSettings = jwtSettings ?? throw new ArgumentNullException(nameof(jwtSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }
        
        var userRepository = _unitOfWork.Repository<Domain.Entities.User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            // ✅ Security: Log failed attempts but don't reveal if email exists
            _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
            // Always throw same exception to prevent user enumeration
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsEmailVerified)
        {
            throw new UnauthorizedAccessException("Please verify your email before logging in.");
        }

        // ✅ Adjust expiration based on RememberMe
        // If RememberMe is true, use longer expiration (30 days), otherwise use default
        var expirationMinutes = request.RememberMe 
            ? 60 * 24 * 30 // 30 days
            : _jwtSettings.Value.ExpirationMinutes;
        
        // ✅ Use JWT service with custom expiration
        var token = _jwtTokenService.GenerateToken(user, expirationMinutes);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

        // Save refresh token - adjust expiration based on RememberMe
        var refreshTokenExpirationDays = request.RememberMe 
            ? 30 // 30 days for remember me
            : _jwtSettings.Value.RefreshTokenExpirationDays;
        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(refreshTokenExpirationDays));
        await userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User logged in: {Email}, UserId: {UserId}", request.Email, user.Id);

        return new LoginResponse
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email,
            SubscriptionTier = user.SubscriptionTier.ToString()
        };
    }
}

