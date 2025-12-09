using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Shared.Extensions;

namespace UtilityTools.Application.Features.Users.Queries.GetUserProfile;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, GetUserProfileResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GetUserProfileQueryHandler> _logger;

    public GetUserProfileQueryHandler(
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GetUserProfileQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<GetUserProfileResponse> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = _httpContextAccessor.HttpContext?.User.GetUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var userRepository = _unitOfWork.Repository<Domain.Entities.User>();
        
        // ✅ Optimized: Use GetByIdWithIncludesAsync to avoid N+1 query
        var user = await userRepository.GetByIdWithIncludesAsync(
            userId,
            cancellationToken,
            u => u.UsageRecords)
            ?? throw new KeyNotFoundException("User not found");
        
        var totalUsageCount = user.UsageRecords?.Count ?? 0;
        var totalFileSizeProcessed = user.UsageRecords?.Sum(ur => ur.FileSizeBytes) ?? 0;

        return new GetUserProfileResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            SubscriptionTier = user.SubscriptionTier.ToString(),
            SubscriptionExpiresAt = user.SubscriptionExpiresAt,
            IsEmailVerified = user.IsEmailVerified,
            TotalUsageCount = totalUsageCount,
            TotalFileSizeProcessed = totalFileSizeProcessed,
            CreatedAt = user.CreatedAt
        };
    }
}

