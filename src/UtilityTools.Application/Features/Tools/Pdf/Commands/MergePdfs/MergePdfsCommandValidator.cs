using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace UtilityTools.Application.Features.Tools.Pdf.Commands.MergePdfs;

public class MergePdfsCommandValidator : AbstractValidator<MergePdfsCommand>
{
    public MergePdfsCommandValidator()
    {
        RuleFor(v => v.Files)
            .NotEmpty().WithMessage("At least one PDF file is required.")
            .Must(files => files.Count >= 2).WithMessage("At least 2 PDF files are required for merging.")
            .Must(files => files.Count <= 20).WithMessage("Maximum 20 PDF files can be merged at once.");

        RuleForEach(v => v.Files)
            .Must(BePdfFile).WithMessage("All files must be PDF files.")
            .Must(BeWithinSizeLimit).WithMessage("Each file must be less than 20MB.");
    }

    private bool BePdfFile(IFormFile file)
    {
        if (file == null || file.Length == 0) return false;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return extension == ".pdf" && file.ContentType == "application/pdf";
    }

    private bool BeWithinSizeLimit(IFormFile file)
    {
        const long maxSize = 20 * 1024 * 1024; // 20MB
        return file.Length <= maxSize;
    }
}

