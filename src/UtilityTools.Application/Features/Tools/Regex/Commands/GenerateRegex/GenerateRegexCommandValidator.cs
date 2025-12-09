using FluentValidation;

namespace UtilityTools.Application.Features.Tools.Regex.Commands.GenerateRegex;

public class GenerateRegexCommandValidator : AbstractValidator<GenerateRegexCommand>
{
    public GenerateRegexCommandValidator()
    {
        RuleFor(v => v.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        When(v => !string.IsNullOrWhiteSpace(v.SampleText), () =>
        {
            RuleFor(v => v.SampleText)
                .MaximumLength(10000).WithMessage("Sample text must not exceed 10,000 characters.");
        });
    }
}

