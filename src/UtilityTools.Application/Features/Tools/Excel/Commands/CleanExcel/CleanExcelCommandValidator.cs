using FluentValidation;

namespace UtilityTools.Application.Features.Tools.Excel.Commands.CleanExcel;

public class CleanExcelCommandValidator : AbstractValidator<CleanExcelCommand>
{
    private static readonly string[] AllowedFormats = 
    {
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", // .xlsx
        "application/vnd.ms-excel", // .xls
        "text/csv", // .csv
        "application/csv"
    };

    private static readonly string[] AllowedExtensions = { ".xlsx", ".xls", ".csv" };

    public CleanExcelCommandValidator()
    {
        RuleFor(v => v.File)
            .NotNull().WithMessage("Excel file is required.")
            .Must(file => file.Length > 0).WithMessage("File cannot be empty.")
            .Must(file => file.Length <= 10_485_760) // 10MB
            .WithMessage("File must not exceed 10MB.")
            .Must(file => AllowedFormats.Contains(file.ContentType) ||
                         AllowedExtensions.Any(ext => file.FileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .WithMessage("File must be Excel format (.xlsx, .xls) or CSV.");

        When(v => !string.IsNullOrEmpty(v.Options.OutputFormat), () =>
        {
            RuleFor(v => v.Options.OutputFormat)
                .Must(f => new[] { "xlsx", "csv" }.Contains(f!.ToLower()))
                .WithMessage("Output format must be xlsx or csv.");
        });
    }
}

