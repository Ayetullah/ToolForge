using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using UtilityTools.Domain.Entities;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;

namespace UtilityTools.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<RegisterCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<User>();
        
        // Check if user exists
        var existingUser = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (existingUser != null)
        {
            throw new Exception("User with this email already exists.");
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User(request.Email, passwordHash, request.FirstName, request.LastName);
        
        // Generate email verification token
        var verificationToken = GenerateSecureToken();
        user.SetEmailVerificationToken(verificationToken);
        
        await userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send verification email
        try
        {
            var baseUrl = _configuration["BaseUrl"] ?? "http://localhost:5000";
            var verificationUrl = $"{baseUrl}/api/auth/verify-email?token={verificationToken}&email={Uri.EscapeDataString(user.Email)}";

            await _emailService.SendEmailVerificationAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}",
                verificationToken,
                verificationUrl,
                cancellationToken);

            _logger.LogInformation("Verification email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending verification email to {Email}", user.Email);
            // Continue even if email fails - user can request resend
        }

        _logger.LogInformation("User registered: {Email}", request.Email);

        return new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Message = "User registered successfully. Please check your email to verify your account."
        };
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

