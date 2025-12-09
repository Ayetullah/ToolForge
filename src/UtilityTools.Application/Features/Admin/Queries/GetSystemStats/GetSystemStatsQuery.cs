using MediatR;

namespace UtilityTools.Application.Features.Admin.Queries.GetSystemStats;

public class GetSystemStatsQuery : IRequest<GetSystemStatsResponse>
{
}

public class GetSystemStatsResponse
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalOperations { get; set; }
    public long TotalFileSizeProcessed { get; set; }
    public Dictionary<string, int> UsersByTier { get; set; } = new();
    public Dictionary<string, int> OperationsByTool { get; set; } = new();
    public List<DailyStats> DailyStats { get; set; } = new();
}

public class DailyStats
{
    public DateTime Date { get; set; }
    public int NewUsers { get; set; }
    public int Operations { get; set; }
    public long FileSizeBytes { get; set; }
}

