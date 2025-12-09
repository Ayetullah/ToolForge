using MediatR;

namespace UtilityTools.Application.Features.Tools.Ai.Commands.Summarize;

public class SummarizeCommand : IRequest<SummarizeResponse>
{
    public string? Text { get; set; }
    public string? Url { get; set; }
    public int MaxLength { get; set; } = 200;
    public string? Tone { get; set; } // professional, casual, technical, etc.
}

public class SummarizeResponse
{
    public string Summary { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public decimal Cost { get; set; }
    public int OriginalLength { get; set; }
    public int SummaryLength { get; set; }
}

