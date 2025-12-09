using MediatR;

namespace UtilityTools.Application.Features.Users.Queries.GetUsageStatistics;

public class GetUsageStatisticsQuery : IRequest<GetUsageStatisticsResponse>
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class GetUsageStatisticsResponse
{
    public int TotalOperations { get; set; }
    public long TotalFileSizeBytes { get; set; }
    public Dictionary<string, int> OperationsByTool { get; set; } = new();
    public Dictionary<string, long> FileSizeByTool { get; set; } = new();
    public List<DailyUsage> DailyUsage { get; set; } = new();
}

public class DailyUsage
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public long TotalFileSizeBytes { get; set; }
}

