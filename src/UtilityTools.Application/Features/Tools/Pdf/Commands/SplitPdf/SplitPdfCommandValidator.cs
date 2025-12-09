using FluentValidation;
using System.Text.RegularExpressions;
using SysRegex = System.Text.RegularExpressions.Regex;

namespace UtilityTools.Application.Features.Tools.Pdf.Commands.SplitPdf;

public class SplitPdfCommandValidator : AbstractValidator<SplitPdfCommand>
{
    public SplitPdfCommandValidator()
    {
        RuleFor(v => v.File)
            .NotNull().WithMessage("PDF file is required.")
            .Must(file => file.Length > 0).WithMessage("File cannot be empty.")
            .Must(file => file.ContentType == "application/pdf" || 
                         file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .WithMessage("File must be a PDF.")
            .Must(file => file.Length <= 50_000_000) // 50MB
            .WithMessage("File must not exceed 50MB.");

        RuleFor(v => v.PagesSpec)
            .NotEmpty().WithMessage("Pages specification is required.")
            .Must(BeValidPagesSpec).WithMessage("Invalid pages specification. Use format: '1-5,10,15-20' or 'all'.");
    }

    private bool BeValidPagesSpec(string pagesSpec)
    {
        if (pagesSpec.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Pattern: number or number-number, separated by commas
        var pattern = @"^(\d+(-\d+)?)(,\d+(-\d+)?)*$";
        return SysRegex.IsMatch(pagesSpec, pattern);
    }
}

