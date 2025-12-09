using FluentValidation;

namespace UtilityTools.Application.Features.Tools.Ai.Commands.Summarize;

public class SummarizeCommandValidator : AbstractValidator<SummarizeCommand>
{
    public SummarizeCommandValidator()
    {
        RuleFor(v => v)
            .Must(c => !string.IsNullOrWhiteSpace(c.Text) || !string.IsNullOrWhiteSpace(c.Url))
            .WithMessage("Either text or URL must be provided.");

        When(v => !string.IsNullOrWhiteSpace(v.Text), () =>
        {
            RuleFor(v => v.Text)
                .MaximumLength(100000).WithMessage("Text must not exceed 100,000 characters.");
        });

        When(v => !string.IsNullOrWhiteSpace(v.Url), () =>
        {
            RuleFor(v => v.Url)
                .Must(BeValidUrl).WithMessage("URL must be a valid URL.");
        });

        RuleFor(v => v.MaxLength)
            .InclusiveBetween(50, 2000)
            .WithMessage("Max length must be between 50 and 2000 characters.");
    }

    private bool BeValidUrl(string? url)
    {
        return !string.IsNullOrWhiteSpace(url) &&
               Uri.TryCreate(url, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

