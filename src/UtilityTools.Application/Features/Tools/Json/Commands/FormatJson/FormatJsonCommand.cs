using MediatR;

namespace UtilityTools.Application.Features.Tools.Json.Commands.FormatJson;

public class FormatJsonCommand : IRequest<FormatJsonResponse>
{
    public string Text { get; set; } = string.Empty;
    public string? Json { get; set; } // Alias for Text (frontend compatibility)
    public bool Indent { get; set; } = true;
    public int IndentSize { get; set; } = 2;
}

public class FormatJsonResponse
{
    public string FormattedJson { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

