using MediatR;

namespace UtilityTools.Application.Features.Tools.Regex.Commands.GenerateRegex;

public class GenerateRegexCommand : IRequest<GenerateRegexResponse>
{
    public string Description { get; set; } = string.Empty;
    public string? SampleText { get; set; }
    public List<string>? Examples { get; set; }
}

public class GenerateRegexResponse
{
    public string Pattern { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public List<RegexTest> Tests { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class RegexTest
{
    public string TestString { get; set; } = string.Empty;
    public bool ShouldMatch { get; set; }
    public bool ActualMatch { get; set; }
    public string? Explanation { get; set; }
}

