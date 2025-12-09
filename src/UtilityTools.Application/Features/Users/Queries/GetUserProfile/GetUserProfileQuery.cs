using MediatR;

namespace UtilityTools.Application.Features.Users.Queries.GetUserProfile;

public class GetUserProfileQuery : IRequest<GetUserProfileResponse>
{
}

public class GetUserProfileResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = string.Empty;
    public DateTime? SubscriptionExpiresAt { get; set; }
    public bool IsEmailVerified { get; set; }
    public int TotalUsageCount { get; set; }
    public long TotalFileSizeProcessed { get; set; }
    public DateTime CreatedAt { get; set; }
}

