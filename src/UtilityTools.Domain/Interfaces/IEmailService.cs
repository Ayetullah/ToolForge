namespace UtilityTools.Domain.Interfaces;

/// <summary>
/// Email service abstraction for sending emails
/// </summary>
public interface IEmailService
{
    Task SendEmailAsync(
        string to,
        string subject,
        string body,
        string? htmlBody = null,
        CancellationToken cancellationToken = default);

    Task SendEmailVerificationAsync(
        string to,
        string name,
        string verificationToken,
        string verificationUrl,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(
        string to,
        string name,
        string resetToken,
        string resetUrl,
        CancellationToken cancellationToken = default);

    Task SendWelcomeEmailAsync(
        string to,
        string name,
        CancellationToken cancellationToken = default);
}

