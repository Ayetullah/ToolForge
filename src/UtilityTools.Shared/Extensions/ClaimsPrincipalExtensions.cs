using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace UtilityTools.Shared.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal to extract user information
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst("user_id")?.Value 
                       ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public static string? GetUserEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
    }

    public static string? GetSubscriptionTier(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("subscription_tier")?.Value;
    }

    public static bool IsAuthenticated(this ClaimsPrincipal principal)
    {
        return principal?.Identity?.IsAuthenticated ?? false;
    }
}
