using MediatR;
using Microsoft.EntityFrameworkCore;
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
        try
        {
            var userRepository = _unitOfWork.Repository<User>();
            
            // Check if user exists
            var existingUser = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (existingUser != null)
            {
                _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
                throw new InvalidOperationException("User with this email already exists.");
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User(request.Email, passwordHash, request.FirstName, request.LastName);
            
            // Generate email verification token
            var verificationToken = GenerateSecureToken();
            user.SetEmailVerificationToken(verificationToken);
            
            await userRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("User registered successfully: {Email}, UserId: {UserId}", request.Email, user.Id);

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

            return new RegisterResponse
            {
                UserId = user.Id,
                Email = user.Email,
                Message = "User registered successfully. Please check your email to verify your account."
            };
        }
        catch (InvalidOperationException)
        {
            // Re-throw InvalidOperationException (e.g., user already exists)
            throw;
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database error while registering user with email: {Email}. Inner exception: {InnerException}", 
                request.Email, dbEx.InnerException?.Message);
            
            // Check for unique constraint violation (email already exists)
            if (dbEx.InnerException?.Message?.Contains("duplicate key") == true || 
                dbEx.InnerException?.Message?.Contains("unique constraint") == true ||
                dbEx.InnerException?.Message?.Contains("UNIQUE constraint") == true)
            {
                throw new InvalidOperationException("User with this email already exists.", dbEx);
            }
            
            throw new InvalidOperationException($"Database error occurred while registering the user: {dbEx.InnerException?.Message ?? dbEx.Message}", dbEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error registering user with email: {Email}. Exception type: {ExceptionType}, Message: {Message}, Inner: {InnerException}", 
                request.Email, ex.GetType().Name, ex.Message, ex.InnerException?.Message);
            throw new InvalidOperationException($"An error occurred while registering the user: {ex.Message}", ex);
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

