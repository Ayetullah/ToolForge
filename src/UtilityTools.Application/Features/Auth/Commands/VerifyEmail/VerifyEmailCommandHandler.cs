using MediatR;
using Microsoft.Extensions.Logging;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;

namespace UtilityTools.Application.Features.Auth.Commands.VerifyEmail;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, VerifyEmailResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<VerifyEmailResponse> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var userRepository = _unitOfWork.Repository<Domain.Entities.User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Email verification attempted for non-existent email: {Email}", request.Email);
            return new VerifyEmailResponse
            {
                Success = false,
                Message = "Invalid verification token or email."
            };
        }

        if (user.IsEmailVerified)
        {
            return new VerifyEmailResponse
            {
                Success = true,
                Message = "Email is already verified."
            };
        }

        // Validate token
        if (user.EmailVerificationToken != request.Token)
        {
            _logger.LogWarning("Invalid email verification token for user {Email}", request.Email);
            return new VerifyEmailResponse
            {
                Success = false,
                Message = "Invalid or expired verification token."
            };
        }

        try
        {
            // Verify email
            user.VerifyEmail();
            await userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Send welcome email
            try
            {
                await _emailService.SendWelcomeEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    cancellationToken);
            }
            catch (Exception emailEx)
            {
                _logger.LogWarning(emailEx, "Failed to send welcome email to {Email}, but email verification succeeded", user.Email);
            }

            _logger.LogInformation("Email verified for user {Email}", user.Email);

            return new VerifyEmailResponse
            {
                Success = true,
                Message = "Email verified successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email for user {Email}", request.Email);
            return new VerifyEmailResponse
            {
                Success = false,
                Message = "An error occurred while verifying your email."
            };
        }
    }
}

