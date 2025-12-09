namespace UtilityTools.Application.Common.Interfaces;

/// <summary>
/// Service to get current authenticated user information
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}

