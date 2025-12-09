using FluentValidation;

namespace UtilityTools.Application.Features.Tools.Json.Commands.FormatJson;

public class FormatJsonCommandValidator : AbstractValidator<FormatJsonCommand>
{
    public FormatJsonCommandValidator()
    {
        // Support both 'Text' and 'Json' properties
        RuleFor(v => v.Text)
            .NotEmpty()
            .When(v => string.IsNullOrEmpty(v.Json))
            .WithMessage("JSON text is required.");

        RuleFor(v => v.Json)
            .NotEmpty()
            .When(v => string.IsNullOrEmpty(v.Text))
            .WithMessage("JSON text is required.");

        RuleFor(v => !string.IsNullOrEmpty(v.Text) ? v.Text : v.Json ?? string.Empty)
            .MaximumLength(1000000).WithMessage("JSON text must not exceed 1MB.");

        RuleFor(v => v.IndentSize)
            .InclusiveBetween(1, 8)
            .WithMessage("Indent size must be between 1 and 8.");
    }
}

