using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;

namespace UtilityTools.Application.Features.Auth.Commands.ResendVerification;

public class ResendVerificationCommandHandler : IRequestHandler<ResendVerificationCommand, ResendVerificationResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResendVerificationCommandHandler> _logger;

    public ResendVerificationCommandHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<ResendVerificationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ResendVerificationResponse> Handle(ResendVerificationCommand request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var userRepository = _unitOfWork.Repository<Domain.Entities.User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        // Always return success to prevent email enumeration
        if (user == null)
        {
            _logger.LogWarning("Verification email requested for non-existent email: {Email}", request.Email);
            return new ResendVerificationResponse
            {
                Success = true,
                Message = "If an account with that email exists and is not verified, a verification email has been sent."
            };
        }

        if (user.IsEmailVerified)
        {
            return new ResendVerificationResponse
            {
                Success = true,
                Message = "Email is already verified."
            };
        }

        try
        {
            // Generate new verification token
            var verificationToken = GenerateSecureToken();
            user.SetEmailVerificationToken(verificationToken);
            await userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Generate verification URL
            var baseUrl = _configuration["BaseUrl"] ?? "http://localhost:5000";
            var verificationUrl = $"{baseUrl}/api/auth/verify-email?token={verificationToken}&email={Uri.EscapeDataString(user.Email)}";

            // Send email
            await _emailService.SendEmailVerificationAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}",
                verificationToken,
                verificationUrl,
                cancellationToken);

            _logger.LogInformation("Verification email resent to {Email}", user.Email);

            return new ResendVerificationResponse
            {
                Success = true,
                Message = "If an account with that email exists and is not verified, a verification email has been sent."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending verification email to {Email}", request.Email);
            // Still return success to prevent information leakage
            return new ResendVerificationResponse
            {
                Success = true,
                Message = "If an account with that email exists and is not verified, a verification email has been sent."
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

