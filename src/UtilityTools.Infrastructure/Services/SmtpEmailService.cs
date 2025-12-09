using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UtilityTools.Domain.Interfaces;

namespace UtilityTools.Infrastructure.Services;

/// <summary>
/// SMTP email service implementation
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly bool _enableSsl;
    private readonly string _fromAddress;
    private readonly string _fromName;
    private readonly string _baseUrl;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _smtpHost = configuration["Email:SMTP:Host"] 
            ?? throw new InvalidOperationException("Email:SMTP:Host is required");
        _smtpPort = configuration.GetValue<int>("Email:SMTP:Port", 587);
        _smtpUsername = configuration["Email:SMTP:Username"] 
            ?? throw new InvalidOperationException("Email:SMTP:Username is required");
        _smtpPassword = configuration["Email:SMTP:Password"] 
            ?? throw new InvalidOperationException("Email:SMTP:Password is required");
        _enableSsl = configuration.GetValue<bool>("Email:SMTP:EnableSSL", true);
        _fromAddress = configuration["Email:FromAddress"] 
            ?? throw new InvalidOperationException("Email:FromAddress is required");
        _fromName = configuration["Email:FromName"] ?? "UtilityTools";
        _baseUrl = configuration["BaseUrl"] ?? "http://localhost:5000";
    }

    public async Task SendEmailAsync(
        string to,
        string subject,
        string body,
        string? htmlBody = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = _enableSsl
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_fromAddress, _fromName),
                Subject = subject,
                Body = htmlBody ?? body,
                IsBodyHtml = !string.IsNullOrEmpty(htmlBody)
            };

            message.To.Add(to);

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {To}", to);
            throw;
        }
    }

    public async Task SendEmailVerificationAsync(
        string to,
        string name,
        string verificationToken,
        string verificationUrl,
        CancellationToken cancellationToken = default)
    {
        var subject = "Verify Your Email - UtilityTools";
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .button:hover {{ background-color: #0056b3; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h2>Welcome to UtilityTools, {name}!</h2>
        <p>Thank you for registering. Please verify your email address by clicking the button below:</p>
        <a href=""{verificationUrl}"" class=""button"">Verify Email</a>
        <p>Or copy and paste this link into your browser:</p>
        <p>{verificationUrl}</p>
        <p>This link will expire in 24 hours.</p>
        <p>If you didn't create an account, please ignore this email.</p>
    </div>
</body>
</html>";

        var textBody = $"Welcome to UtilityTools, {name}!\n\n" +
                      $"Please verify your email by visiting: {verificationUrl}\n\n" +
                      "This link will expire in 24 hours.";

        await SendEmailAsync(to, subject, textBody, htmlBody, cancellationToken);
    }

    public async Task SendPasswordResetAsync(
        string to,
        string name,
        string resetToken,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        var subject = "Reset Your Password - UtilityTools";
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #dc3545; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .button:hover {{ background-color: #c82333; }}
        .warning {{ color: #856404; background-color: #fff3cd; padding: 10px; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h2>Password Reset Request</h2>
        <p>Hello {name},</p>
        <p>We received a request to reset your password. Click the button below to reset it:</p>
        <a href=""{resetUrl}"" class=""button"">Reset Password</a>
        <p>Or copy and paste this link into your browser:</p>
        <p>{resetUrl}</p>
        <div class=""warning"">
            <strong>⚠️ Security Notice:</strong> This link will expire in 1 hour. If you didn't request a password reset, please ignore this email.
        </div>
    </div>
</body>
</html>";

        var textBody = $"Hello {name},\n\n" +
                      $"We received a request to reset your password. Visit: {resetUrl}\n\n" +
                      "This link will expire in 1 hour.\n\n" +
                      "If you didn't request this, please ignore this email.";

        await SendEmailAsync(to, subject, textBody, htmlBody, cancellationToken);
    }

    public async Task SendWelcomeEmailAsync(
        string to,
        string name,
        CancellationToken cancellationToken = default)
    {
        var subject = "Welcome to UtilityTools!";
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h2>Welcome to UtilityTools, {name}!</h2>
        <p>Your account has been successfully verified. You can now use all our utility tools.</p>
        <p>Get started by exploring our tools:</p>
        <ul>
            <li>PDF Merge & Split</li>
            <li>Image Compression</li>
            <li>Document Conversion</li>
            <li>Excel Cleaning</li>
            <li>AI Text Summarization</li>
            <li>And more!</li>
        </ul>
        <p>Happy tooling!</p>
    </div>
</body>
</html>";

        var textBody = $"Welcome to UtilityTools, {name}!\n\n" +
                      "Your account has been successfully verified. You can now use all our utility tools.";

        await SendEmailAsync(to, subject, textBody, htmlBody, cancellationToken);
    }
}

