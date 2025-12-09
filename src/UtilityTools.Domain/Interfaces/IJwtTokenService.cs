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
    string GenerateToken(User user);
    
    /// <summary>
    /// Validate JWT token and return claims principal
    /// </summary>
    System.Security.Claims.ClaimsPrincipal? ValidateToken(string token);
    
    /// <summary>
    /// Generate refresh token
    /// </summary>
    string GenerateRefreshToken();
}

