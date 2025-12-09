using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Shared.Extensions;

namespace UtilityTools.Application.Features.Admin.Queries.GetAllUsers;

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, GetAllUsersResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GetAllUsersQueryHandler> _logger;

    public GetAllUsersQueryHandler(
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GetAllUsersQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetAllUsersResponse> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var userId = _httpContextAccessor.HttpContext?.User.GetUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        // Check if user is admin - use repository with includes
        var userRepository = _unitOfWork.Repository<Domain.Entities.User>();
        var currentUser = await userRepository.GetByIdWithIncludesAsync(
            userId,
            cancellationToken,
            u => u.Roles)
            ?? throw new KeyNotFoundException("User not found");

        var isAdmin = currentUser.Roles.Any(r => r.Name == "Admin") || currentUser.SubscriptionTier == SubscriptionTier.Admin;
        if (!isAdmin)
        {
            // ✅ Security: Log unauthorized admin access attempts
            _logger.LogWarning("Unauthorized admin access attempt. UserId: {UserId}, Email: {Email}", 
                userId, currentUser.Email);
            throw new UnauthorizedAccessException("Admin access required");
        }

        // Use repository GetQueryable for complex queries with Include
        var query = userRepository.GetQueryable()
            .Include(u => u.UsageRecords)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(u => 
                u.Email.ToLower().Contains(searchTerm) ||
                u.FirstName.ToLower().Contains(searchTerm) ||
                u.LastName.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrEmpty(request.SubscriptionTier) && 
            Enum.TryParse<SubscriptionTier>(request.SubscriptionTier, true, out var tier))
        {
            query = query.Where(u => u.SubscriptionTier == tier);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                SubscriptionTier = u.SubscriptionTier.ToString(),
                SubscriptionExpiresAt = u.SubscriptionExpiresAt,
                IsEmailVerified = u.IsEmailVerified,
                TotalUsageCount = u.UsageRecords.Count,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new GetAllUsersResponse
        {
            Users = users,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

