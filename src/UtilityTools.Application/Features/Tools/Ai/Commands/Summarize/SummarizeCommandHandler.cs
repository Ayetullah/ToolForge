using MediatR;
using Microsoft.Extensions.Logging;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;

namespace UtilityTools.Application.Features.Tools.Ai.Commands.Summarize;

public class SummarizeCommandHandler : IRequestHandler<SummarizeCommand, SummarizeResponse>
{
    private readonly IAiService _aiService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SummarizeCommandHandler> _logger;

    public SummarizeCommandHandler(
        IAiService aiService,
        IUnitOfWork unitOfWork,
        ILogger<SummarizeCommandHandler> logger)
    {
        _aiService = aiService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SummarizeResponse> Handle(SummarizeCommand request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        string summary;
        int tokensUsed = 0;
        int originalLength = 0;

        try
        {
            if (!string.IsNullOrWhiteSpace(request.Url))
            {
                originalLength = request.Url.Length; // Approximate
                summary = await _aiService.SummarizeUrlAsync(
                    request.Url,
                    request.MaxLength,
                    request.Tone,
                    cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(request.Text))
            {
                originalLength = request.Text.Length;
                summary = await _aiService.SummarizeTextAsync(
                    request.Text,
                    request.MaxLength,
                    request.Tone,
                    cancellationToken);
                
                // Estimate tokens (rough approximation: 1 token ≈ 4 characters)
                tokensUsed = await _aiService.GetTokenCountAsync(request.Text, cancellationToken);
            }
            else
            {
                throw new ArgumentException("Either text or URL must be provided.");
            }

            var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            var cost = CalculateCost(tokensUsed);

            // Record usage (TODO: Get user ID from context)
            // var usageRecord = new UsageRecord(
            //     userId: currentUserId,
            //     ToolType.AiSummarize,
            //     fileSizeBytes: originalLength,
            //     processingTimeMs: processingTime,
            //     tokensUsed: tokensUsed,
            //     cost: cost);

            // _context.UsageRecords.Add(usageRecord);
            // await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Text summarized. Original: {Original} chars, Summary: {Summary} chars, Tokens: {Tokens}, Cost: ${Cost}",
                originalLength, summary.Length, tokensUsed, cost);

            return new SummarizeResponse
            {
                Summary = summary,
                TokensUsed = tokensUsed,
                Cost = cost,
                OriginalLength = originalLength,
                SummaryLength = summary.Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error summarizing text");
            throw;
        }
    }

    private decimal CalculateCost(int tokens)
    {
        // OpenAI pricing (approximate): $0.03 per 1K tokens for GPT-4 input
        // This is a simplified calculation - adjust based on actual pricing
        return (tokens / 1000m) * 0.03m;
    }
}

