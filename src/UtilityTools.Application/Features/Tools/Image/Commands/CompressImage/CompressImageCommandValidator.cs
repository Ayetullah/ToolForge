using FluentValidation;

namespace UtilityTools.Application.Features.Tools.Image.Commands.CompressImage;

public class CompressImageCommandValidator : AbstractValidator<CompressImageCommand>
{
    private static readonly string[] AllowedFormats = { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp", "image/bmp" };
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };

    public CompressImageCommandValidator()
    {
        RuleFor(v => v.File)
            .NotNull().WithMessage("Image file is required.")
            .Must(file => file.Length > 0).WithMessage("File cannot be empty.")
            .Must(file => file.Length <= 10_485_760) // 10MB
            .WithMessage("File must not exceed 10MB.")
            .Must(file => AllowedFormats.Contains(file.ContentType.ToLower()) ||
                         AllowedExtensions.Any(ext => file.FileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .WithMessage("File must be a valid image format (JPG, PNG, GIF, WEBP, BMP).");

        RuleFor(v => v.Quality)
            .InclusiveBetween(1, 100)
            .WithMessage("Quality must be between 1 and 100.");

        When(v => !string.IsNullOrEmpty(v.TargetFormat), () =>
        {
            RuleFor(v => v.TargetFormat)
                .Must(f => new[] { "jpg", "jpeg", "png", "webp" }.Contains(f!.ToLower()))
                .WithMessage("Target format must be jpg, png, or webp.");
        });

        RuleFor(v => v.MaxWidth)
            .GreaterThan(0).When(v => v.MaxWidth.HasValue)
            .WithMessage("Max width must be greater than 0.");

        RuleFor(v => v.MaxHeight)
            .GreaterThan(0).When(v => v.MaxHeight.HasValue)
            .WithMessage("Max height must be greater than 0.");
    }
}
