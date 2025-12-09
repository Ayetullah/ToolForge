using UtilityTools.Domain.Entities;

namespace UtilityTools.Domain.Interfaces;

/// <summary>
/// JWT token generation and validation service
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generate JWT access token for user
    /// </summary>
    /// <param name="user">User entity</param>
    /// <param name="expirationMinutes">Token expiration in minutes. If null, uses default from settings.</param>
    string GenerateToken(User user, int? expirationMinutes = null);
    
    /// <summary>
    /// Validate JWT token and return claims principal
    /// </summary>
    System.Security.Claims.ClaimsPrincipal? ValidateToken(string token);
    
    /// <summary>
    /// Generate refresh token
    /// </summary>
    string GenerateRefreshToken();
}

