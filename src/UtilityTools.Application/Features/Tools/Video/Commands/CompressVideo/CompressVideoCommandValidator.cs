using FluentValidation;

namespace UtilityTools.Application.Features.Tools.Video.Commands.CompressVideo;

public class CompressVideoCommandValidator : AbstractValidator<CompressVideoCommand>
{
    private static readonly string[] AllowedFormats = 
    {
        "video/mp4",
        "video/quicktime",
        "video/x-msvideo", // AVI
        "video/webm",
        "video/x-matroska" // MKV
    };

    private static readonly string[] AllowedExtensions = { ".mp4", ".mov", ".avi", ".webm", ".mkv" };

    public CompressVideoCommandValidator()
    {
        RuleFor(v => v.File)
            .NotNull().WithMessage("Video file is required.")
            .Must(file => file.Length > 0).WithMessage("File cannot be empty.")
            .Must(file => file.Length <= 500_000_000) // 500MB
            .WithMessage("File must not exceed 500MB.")
            .Must(file => AllowedFormats.Contains(file.ContentType) ||
                         AllowedExtensions.Any(ext => file.FileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .WithMessage("File must be a valid video format (MP4, MOV, AVI, WEBM, MKV).");

        RuleFor(v => v.Quality)
            .InclusiveBetween(18, 28)
            .WithMessage("Quality (CRF) must be between 18 and 28.");

        When(v => !string.IsNullOrEmpty(v.Preset), () =>
        {
            RuleFor(v => v.Preset)
                .Must(p => new[] { "ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow" }
                    .Contains(p!.ToLower()))
                .WithMessage("Preset must be one of: ultrafast, superfast, veryfast, faster, fast, medium, slow, slower, veryslow.");
        });

        RuleFor(v => v.MaxWidth)
            .GreaterThan(0).When(v => v.MaxWidth.HasValue)
            .WithMessage("Max width must be greater than 0.");

        RuleFor(v => v.MaxHeight)
            .GreaterThan(0).When(v => v.MaxHeight.HasValue)
            .WithMessage("Max height must be greater than 0.");

        RuleFor(v => v.BitrateKbps)
            .GreaterThan(0).When(v => v.BitrateKbps.HasValue)
            .WithMessage("Bitrate must be greater than 0.");

        When(v => !string.IsNullOrEmpty(v.Codec), () =>
        {
            RuleFor(v => v.Codec)
                .Must(c => new[] { "libx264", "libx265", "libvpx-vp9" }.Contains(c!.ToLower()))
                .WithMessage("Codec must be libx264, libx265, or libvpx-vp9.");
        });
    }
}

