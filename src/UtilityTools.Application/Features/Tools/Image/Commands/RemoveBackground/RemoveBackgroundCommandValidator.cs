using FluentValidation;
using System.Text.RegularExpressions;
using SysRegex = System.Text.RegularExpressions.Regex;

namespace UtilityTools.Application.Features.Tools.Image.Commands.RemoveBackground;

public class RemoveBackgroundCommandValidator : AbstractValidator<RemoveBackgroundCommand>
{
    private static readonly string[] AllowedFormats = { "image/jpeg", "image/jpg", "image/png", "image/webp" };
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    public RemoveBackgroundCommandValidator()
    {
        RuleFor(v => v.File)
            .NotNull().WithMessage("Image file is required.")
            .Must(file => file.Length > 0).WithMessage("File cannot be empty.")
            .Must(file => file.Length <= 10_485_760) // 10MB
            .WithMessage("File must not exceed 10MB.")
            .Must(file => AllowedFormats.Contains(file.ContentType.ToLower()) ||
                         AllowedExtensions.Any(ext => file.FileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .WithMessage("File must be a valid image format (JPG, PNG, WEBP).");

        When(v => !string.IsNullOrEmpty(v.BackgroundColor), () =>
        {
            RuleFor(v => v.BackgroundColor)
                .Must(color => SysRegex.IsMatch(color!, @"^#[0-9A-Fa-f]{6}$"))
                .WithMessage("Background color must be a valid hex color (e.g., #FFFFFF).");
        });
    }
}

