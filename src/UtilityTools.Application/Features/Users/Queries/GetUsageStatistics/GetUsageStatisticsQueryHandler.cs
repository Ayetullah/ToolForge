using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Shared.Extensions;

namespace UtilityTools.Application.Features.Users.Queries.GetUsageStatistics;

public class GetUsageStatisticsQueryHandler : IRequestHandler<GetUsageStatisticsQuery, GetUsageStatisticsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GetUsageStatisticsQueryHandler> _logger;

    public GetUsageStatisticsQueryHandler(
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GetUsageStatisticsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetUsageStatisticsResponse> Handle(GetUsageStatisticsQuery request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var userId = _httpContextAccessor.HttpContext?.User.GetUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var startDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30);
        var endDate = request.EndDate ?? DateTime.UtcNow;

        var usageRepository = _unitOfWork.Repository<Domain.Entities.UsageRecord>();
        var usageRecords = await usageRepository.FindAsync(
            ur => ur.UserId == userId && ur.CreatedAt >= startDate && ur.CreatedAt <= endDate,
            cancellationToken);

        var usageRecordsList = usageRecords.ToList();
        var operationsByTool = usageRecordsList
            .GroupBy(ur => ur.ToolType.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var fileSizeByTool = usageRecordsList
            .GroupBy(ur => ur.ToolType.ToString())
            .ToDictionary(g => g.Key, g => g.Sum(ur => ur.FileSizeBytes));

        var dailyUsage = usageRecordsList
            .GroupBy(ur => ur.CreatedAt.Date)
            .Select(g => new DailyUsage
            {
                Date = g.Key,
                Count = g.Count(),
                TotalFileSizeBytes = g.Sum(ur => ur.FileSizeBytes)
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new GetUsageStatisticsResponse
        {
            TotalOperations = usageRecordsList.Count,
            TotalFileSizeBytes = usageRecordsList.Sum(ur => ur.FileSizeBytes),
            OperationsByTool = operationsByTool,
            FileSizeByTool = fileSizeByTool,
            DailyUsage = dailyUsage
        };
    }
}

