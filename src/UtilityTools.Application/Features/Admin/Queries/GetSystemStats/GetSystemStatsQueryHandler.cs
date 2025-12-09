using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Shared.Extensions;

namespace UtilityTools.Application.Features.Admin.Queries.GetSystemStats;

public class GetSystemStatsQueryHandler : IRequestHandler<GetSystemStatsQuery, GetSystemStatsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GetSystemStatsQueryHandler> _logger;

    public GetSystemStatsQueryHandler(
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GetSystemStatsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetSystemStatsResponse> Handle(GetSystemStatsQuery request, CancellationToken cancellationToken)
    {
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
            _logger.LogWarning("Non-admin user {UserId} attempted to access admin stats", userId);
            throw new UnauthorizedAccessException("Admin access required");
        }

        // ✅ Optimized: Single queries instead of N+1
        var startDate = DateTime.UtcNow.AddDays(-30).Date;
        var userQueryable = userRepository.GetQueryable();
        
        var totalUsers = await userRepository.CountAsync(null, cancellationToken);
        
        // ✅ Optimized: Single query for active users
        var activeUsers = await userQueryable
            .Where(u => u.UsageRecords.Any(ur => ur.CreatedAt >= startDate))
            .CountAsync(cancellationToken);

        // ✅ Optimized: Aggregation in database instead of loading all records
        var usageRepository = _unitOfWork.Repository<Domain.Entities.UsageRecord>();
        var usageStats = await usageRepository.GetQueryable()
            .Where(ur => ur.CreatedAt >= startDate)
            .GroupBy(ur => ur.ToolType)
            .Select(g => new
            {
                ToolType = g.Key,
                Count = g.Count(),
                TotalFileSize = g.Sum(ur => ur.FileSizeBytes)
            })
            .ToListAsync(cancellationToken);

        var totalOperations = usageStats.Sum(s => s.Count);
        var totalFileSizeProcessed = usageStats.Sum(s => s.TotalFileSize);

        var usersByTier = await userQueryable
            .GroupBy(u => u.SubscriptionTier)
            .Select(g => new { Tier = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Tier, x => x.Count, cancellationToken);

        var operationsByTool = usageStats
            .ToDictionary(s => s.ToolType.ToString(), s => s.Count);

        // ✅ Optimized: Single queries for daily stats instead of loop
        var last30Days = Enumerable.Range(0, 30)
            .Select(i => DateTime.UtcNow.AddDays(-i).Date)
            .OrderBy(d => d)
            .ToList();

        var dailyUsersData = await userQueryable
            .Where(u => u.CreatedAt >= startDate)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { Date = g.Key, NewUsers = g.Count() })
            .ToListAsync(cancellationToken);

        var dailyUsageData = await usageRepository.GetQueryable()
            .Where(ur => ur.CreatedAt >= startDate)
            .GroupBy(ur => ur.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Operations = g.Count(),
                FileSizeBytes = g.Sum(ur => ur.FileSizeBytes)
            })
            .ToListAsync(cancellationToken);

        var dailyStats = last30Days.Select(date => new DailyStats
        {
            Date = date,
            NewUsers = dailyUsersData.FirstOrDefault(d => d.Date == date)?.NewUsers ?? 0,
            Operations = dailyUsageData.FirstOrDefault(d => d.Date == date)?.Operations ?? 0,
            FileSizeBytes = dailyUsageData.FirstOrDefault(d => d.Date == date)?.FileSizeBytes ?? 0
        }).ToList();

        return new GetSystemStatsResponse
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            TotalOperations = totalOperations,
            TotalFileSizeProcessed = totalFileSizeProcessed,
            UsersByTier = usersByTier,
            OperationsByTool = operationsByTool,
            DailyStats = dailyStats
        };
    }
}

