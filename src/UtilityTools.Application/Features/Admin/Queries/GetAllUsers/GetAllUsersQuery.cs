using MediatR;

namespace UtilityTools.Application.Features.Admin.Queries.GetAllUsers;

public class GetAllUsersQuery : IRequest<GetAllUsersResponse>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public string? SubscriptionTier { get; set; }
}

public class GetAllUsersResponse
{
    public List<UserDto> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string SubscriptionTier { get; set; } = string.Empty;
    public DateTime? SubscriptionExpiresAt { get; set; }
    public bool IsEmailVerified { get; set; }
    public int TotalUsageCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

