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

        // ✅ Use JWT service instead of inline logic
        var token = _jwtTokenService.GenerateToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.Value.ExpirationMinutes);

        // Save refresh token
        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(_jwtSettings.Value.RefreshTokenExpirationDays));
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

