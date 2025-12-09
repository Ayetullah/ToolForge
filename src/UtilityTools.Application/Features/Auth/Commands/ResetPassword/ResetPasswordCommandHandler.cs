using MediatR;
using Microsoft.Extensions.Logging;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;

namespace UtilityTools.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var userRepository = _unitOfWork.Repository<Domain.Entities.User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Password reset attempted for non-existent email: {Email}", request.Email);
            return new ResetPasswordResponse
            {
                Success = false,
                Message = "Invalid reset token or email."
            };
        }

        // Validate token
        if (user.PasswordResetToken != request.Token)
        {
            _logger.LogWarning("Invalid password reset token for user {Email}", request.Email);
            return new ResetPasswordResponse
            {
                Success = false,
                Message = "Invalid or expired reset token."
            };
        }

        // Check if token is expired
        if (!user.PasswordResetTokenExpiresAt.HasValue || user.PasswordResetTokenExpiresAt.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("Expired password reset token for user {Email}", request.Email);
            user.ClearPasswordResetToken();
            await userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new ResetPasswordResponse
            {
                Success = false,
                Message = "Reset token has expired. Please request a new one."
            };
        }

        try
        {
            // Hash new password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            // Update password
            user.UpdatePassword(passwordHash);

            // Clear reset token
            user.ClearPasswordResetToken();

            await userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Password reset successful for user {Email}", request.Email);

            return new ResetPasswordResponse
            {
                Success = true,
                Message = "Password has been reset successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {Email}", request.Email);
            return new ResetPasswordResponse
            {
                Success = false,
                Message = "An error occurred while resetting your password."
            };
        }
    }
}

