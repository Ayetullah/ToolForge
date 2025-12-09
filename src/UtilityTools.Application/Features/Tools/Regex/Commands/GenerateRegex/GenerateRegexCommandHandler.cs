using System.Text.RegularExpressions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UtilityTools.Domain.Entities;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Shared.Extensions;
using SysRegex = System.Text.RegularExpressions.Regex;

namespace UtilityTools.Application.Features.Tools.Regex.Commands.GenerateRegex;

public class GenerateRegexCommandHandler : IRequestHandler<GenerateRegexCommand, GenerateRegexResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GenerateRegexCommandHandler> _logger;

    public GenerateRegexCommandHandler(
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GenerateRegexCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GenerateRegexResponse> Handle(GenerateRegexCommand request, CancellationToken cancellationToken)
    {
        var userId = _httpContextAccessor.HttpContext?.User.GetUserId();
        var startTime = DateTime.UtcNow;

        try
        {
            // Simple regex generation based on common patterns
            // In production, this could use AI to generate more complex patterns
            var pattern = GeneratePatternFromDescription(request.Description, request.SampleText);
            var explanation = GenerateExplanation(pattern);
            
            // Test the pattern
            var tests = new List<RegexTest>();
            
            if (!string.IsNullOrWhiteSpace(request.SampleText))
            {
                var match = SysRegex.IsMatch(request.SampleText, pattern);
                tests.Add(new RegexTest
                {
                    TestString = request.SampleText,
                    ShouldMatch = true,
                    ActualMatch = match,
                    Explanation = match ? "Matches the sample text" : "Does not match the sample text"
                });
            }

            if (request.Examples != null)
            {
                foreach (var example in request.Examples)
                {
                    var match = SysRegex.IsMatch(example, pattern);
                    tests.Add(new RegexTest
                    {
                        TestString = example,
                        ShouldMatch = true,
                        ActualMatch = match
                    });
                }
            }

            // Add some common test cases
            AddCommonTestCases(pattern, tests);

            var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            // Record usage if user is authenticated
            if (userId.HasValue)
            {
                var usageRepository = _unitOfWork.Repository<UsageRecord>();
                var usageRecord = new UsageRecord(
                    userId.Value,
                    ToolType.RegexGenerate,
                    fileSizeBytes: request.Description.Length,
                    processingTimeMs: processingTime,
                    cost: 0m);

                await usageRepository.AddAsync(usageRecord, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Regex generated. Pattern: {Pattern}, Tests: {Tests}, Time: {Time}ms",
                pattern, tests.Count, processingTime);

            return new GenerateRegexResponse
            {
                Pattern = pattern,
                Explanation = explanation,
                Tests = tests
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating regex");
            return new GenerateRegexResponse
            {
                Pattern = string.Empty,
                Explanation = string.Empty,
                ErrorMessage = $"Error generating regex: {ex.Message}"
            };
        }
    }

    private string GeneratePatternFromDescription(string description, string? sampleText)
    {
        var lowerDesc = description.ToLower();

        // Common patterns
        if (lowerDesc.Contains("email"))
        {
            return @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        }
        
        if (lowerDesc.Contains("phone") || lowerDesc.Contains("telephone"))
        {
            return @"^\+?[\d\s\-\(\)]{10,}$";
        }
        
        if (lowerDesc.Contains("url") || lowerDesc.Contains("website"))
        {
            return @"^https?://[^\s/$.?#].[^\s]*$";
        }
        
        if (lowerDesc.Contains("ip address"))
        {
            return @"^(\d{1,3}\.){3}\d{1,3}$";
        }
        
        if (lowerDesc.Contains("date"))
        {
            return @"^\d{4}-\d{2}-\d{2}$";
        }
        
        if (lowerDesc.Contains("number") || lowerDesc.Contains("digit"))
        {
            return @"^\d+$";
        }
        
        if (lowerDesc.Contains("alphanumeric"))
        {
            return @"^[a-zA-Z0-9]+$";
        }
        
        if (lowerDesc.Contains("whitespace") || lowerDesc.Contains("space"))
        {
            return @"\s+";
        }

        // If sample text provided, try to infer pattern
        if (!string.IsNullOrWhiteSpace(sampleText))
        {
            return InferPatternFromSample(sampleText);
        }

        // Default: match the description as literal (escaped)
        return SysRegex.Escape(description);
    }

    private string InferPatternFromSample(string sample)
    {
        // Simple pattern inference
        if (SysRegex.IsMatch(sample, @"^\d+$"))
            return @"^\d+$";
        
        if (SysRegex.IsMatch(sample, @"^[a-zA-Z]+$"))
            return @"^[a-zA-Z]+$";
        
        if (SysRegex.IsMatch(sample, @"^[a-zA-Z0-9]+$"))
            return @"^[a-zA-Z0-9]+$";
        
        if (SysRegex.IsMatch(sample, @"^\d{4}-\d{2}-\d{2}$"))
            return @"^\d{4}-\d{2}-\d{2}$";

        // Default: match the sample exactly (escaped)
        return $"^{SysRegex.Escape(sample)}$";
    }

    private string GenerateExplanation(string pattern)
    {
        var explanation = new System.Text.StringBuilder();
        explanation.AppendLine($"Pattern: `{pattern}`");
        explanation.AppendLine();
        explanation.AppendLine("Explanation:");
        
        // Basic explanation of common regex elements
        if (pattern.Contains("^"))
            explanation.AppendLine("- `^` matches the start of the string");
        if (pattern.Contains("$"))
            explanation.AppendLine("- `$` matches the end of the string");
        if (pattern.Contains("\\d"))
            explanation.AppendLine("- `\\d` matches any digit (0-9)");
        if (pattern.Contains("\\w"))
            explanation.AppendLine("- `\\w` matches any word character (a-z, A-Z, 0-9, _)");
        if (pattern.Contains("\\s"))
            explanation.AppendLine("- `\\s` matches any whitespace character");
        if (pattern.Contains("+"))
            explanation.AppendLine("- `+` means one or more of the preceding element");
        if (pattern.Contains("*"))
            explanation.AppendLine("- `*` means zero or more of the preceding element");
        if (pattern.Contains("?"))
            explanation.AppendLine("- `?` means zero or one of the preceding element");
        if (pattern.Contains("[]"))
            explanation.AppendLine("- `[]` defines a character class");
        if (pattern.Contains("()"))
            explanation.AppendLine("- `()` groups elements together");

        return explanation.ToString();
    }

    private void AddCommonTestCases(string pattern, List<RegexTest> tests)
    {
        // Add a few test cases
        var testCases = new[]
        {
            ("test", false),
            ("123", false),
            ("", false)
        };

        foreach (var (testString, shouldMatch) in testCases)
        {
            try
            {
                var match = SysRegex.IsMatch(testString, pattern);
                tests.Add(new RegexTest
                {
                    TestString = testString,
                    ShouldMatch = shouldMatch,
                    ActualMatch = match
                });
            }
            catch
            {
                // Invalid pattern
            }
        }
    }
}

