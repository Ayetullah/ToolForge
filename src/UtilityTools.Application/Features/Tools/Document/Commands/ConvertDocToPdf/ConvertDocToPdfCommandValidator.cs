using FluentValidation;

namespace UtilityTools.Application.Features.Tools.Document.Commands.ConvertDocToPdf;

public class ConvertDocToPdfCommandValidator : AbstractValidator<ConvertDocToPdfCommand>
{
    private static readonly string[] AllowedFormats = 
    {
        "application/msword", // .doc
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // .docx
        "application/vnd.ms-excel", // .xls
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", // .xlsx
        "application/vnd.ms-powerpoint", // .ppt
        "application/vnd.openxmlformats-officedocument.presentationml.presentation", // .pptx
        "application/rtf", // .rtf
        "text/plain", // .txt
        "text/html", // .html
        "application/vnd.oasis.opendocument.text", // .odt
    };

    private static readonly string[] AllowedExtensions = 
    { 
        ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", 
        ".rtf", ".txt", ".html", ".odt" 
    };

    public ConvertDocToPdfCommandValidator()
    {
        RuleFor(v => v.File)
            .NotNull().WithMessage("Document file is required.")
            .Must(file => file.Length > 0).WithMessage("File cannot be empty.")
            .Must(file => file.Length <= 50_000_000) // 50MB
            .WithMessage("File must not exceed 50MB.")
            .Must(file => AllowedFormats.Contains(file.ContentType) ||
                         AllowedExtensions.Any(ext => file.FileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .WithMessage("File must be a supported document format (DOC, DOCX, XLS, XLSX, PPT, PPTX, RTF, TXT, HTML, ODT).");
    }
}

