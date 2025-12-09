using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;

namespace UtilityTools.Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<Domain.Entities.User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        // Always return success to prevent email enumeration
        if (user == null)
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
            return new ForgotPasswordResponse
            {
                Success = true,
                Message = "If an account with that email exists, a password reset link has been sent."
            };
        }

        try
        {
            // Generate password reset token
            var resetToken = GenerateSecureToken();
            var expiresAt = DateTime.UtcNow.AddHours(1); // Token expires in 1 hour

            // Store token in user entity
            user.SetPasswordResetToken(resetToken, expiresAt);

            await userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Generate reset URL
            var baseUrl = _configuration["BaseUrl"] ?? "http://localhost:5000";
            var resetUrl = $"{baseUrl}/api/auth/reset-password?token={resetToken}&email={Uri.EscapeDataString(user.Email)}";

            // Send email
            await _emailService.SendPasswordResetAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}",
                resetToken,
                resetUrl,
                cancellationToken);

            _logger.LogInformation("Password reset email sent to {Email}", user.Email);

            return new ForgotPasswordResponse
            {
                Success = true,
                Message = "If an account with that email exists, a password reset link has been sent."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing password reset request for {Email}", request.Email);
            // Still return success to prevent information leakage
            return new ForgotPasswordResponse
            {
                Success = true,
                Message = "If an account with that email exists, a password reset link has been sent."
            };
        }
    }

    private string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

