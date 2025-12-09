using FluentValidation;

namespace UtilityTools.Application.Features.Tools.Pdf.Commands.MergePdf;

public class MergePdfCommandValidator : AbstractValidator<MergePdfCommand>
{
    public MergePdfCommandValidator()
    {
        RuleFor(v => v.Files)
            .NotEmpty().WithMessage("At least one PDF file is required.")
            .Must(files => files.Count >= 2).WithMessage("At least 2 PDF files are required for merging.")
            .Must(files => files.Count <= 20).WithMessage("Maximum 20 PDF files can be merged at once.");

        RuleForEach(v => v.Files)
            .Must(file => file.ContentType == "application/pdf" || 
                         file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .WithMessage("All files must be PDF format.")
            .Must(file => file.Length > 0)
            .WithMessage("File cannot be empty.")
            .Must(file => file.Length <= 20_971_520) // 20MB
            .WithMessage("Each file must not exceed 20MB.");
    }
}

