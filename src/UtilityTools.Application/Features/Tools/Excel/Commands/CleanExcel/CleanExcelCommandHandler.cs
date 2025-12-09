using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using UtilityTools.Domain.Entities;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Shared.Extensions;

namespace UtilityTools.Application.Features.Tools.Excel.Commands.CleanExcel;

public class CleanExcelCommandHandler : IRequestHandler<CleanExcelCommand, CleanExcelResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CleanExcelCommandHandler> _logger;

    public CleanExcelCommandHandler(
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CleanExcelCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // EPPlus license context (free for non-commercial use)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<CleanExcelResponse> Handle(CleanExcelCommand request, CancellationToken cancellationToken)
    {
        var userId = _httpContextAccessor.HttpContext?.User.GetUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var startTime = DateTime.UtcNow;
        var originalSize = request.File.Length;

        try
        {
            using var inputStream = request.File.OpenReadStream();
            using var package = new ExcelPackage(inputStream);
            
            var worksheet = package.Workbook.Worksheets.FirstOrDefault() 
                ?? throw new InvalidOperationException("Excel file must contain at least one worksheet");

            var originalRowCount = worksheet.Dimension?.Rows ?? 0;
            var originalColCount = worksheet.Dimension?.Columns ?? 0;
            var rowsRemoved = 0;
            var columnsRemoved = 0;

            // Clean operations
            if (request.Options.RemoveEmptyRows)
            {
                rowsRemoved = RemoveEmptyRows(worksheet);
            }

            if (request.Options.RemoveEmptyColumns)
            {
                columnsRemoved = RemoveEmptyColumns(worksheet);
            }

            if (request.Options.TrimWhitespace)
            {
                TrimWhitespace(worksheet);
            }

            if (request.Options.RemoveDuplicates)
            {
                RemoveDuplicates(worksheet);
            }

            if (request.Options.StandardizeFormats)
            {
                StandardizeFormats(worksheet);
            }

            // Determine output format
            var outputFormat = request.Options.OutputFormat?.ToLower() 
                ?? (request.File.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? "csv" : "xlsx");

            using var outputStream = new MemoryStream();

            if (outputFormat == "csv")
            {
                await SaveAsCsv(worksheet, outputStream, cancellationToken);
            }
            else
            {
                package.SaveAs(outputStream);
            }

            outputStream.Position = 0;
            var cleanedSize = outputStream.Length;

            // Upload cleaned file
            var extension = outputFormat == "csv" ? ".csv" : ".xlsx";
            var fileName = $"cleaned_{Guid.NewGuid()}{extension}";
            var contentType = outputFormat == "csv" ? "text/csv" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            
            var fileKey = await _fileStorage.UploadAsync(
                outputStream,
                fileName,
                contentType,
                $"excel/clean/{userId}",
                cancellationToken);

            // Generate download URL
            var downloadUrl = await _fileStorage.GeneratePresignedUrlAsync(
                fileKey,
                TimeSpan.FromHours(24),
                cancellationToken);

            var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            // Record usage
            var usageRepository = _unitOfWork.Repository<UsageRecord>();
            var usageRecord = new UsageRecord(
                userId,
                ToolType.ExcelClean,
                fileSizeBytes: originalSize,
                processingTimeMs: processingTime,
                cost: 0m);

            await usageRepository.AddAsync(usageRecord, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Excel cleaned. User: {UserId}, Original: {Original} bytes, Cleaned: {Cleaned} bytes, Rows: -{Rows}, Cols: -{Cols}, Time: {Time}ms",
                userId, originalSize, cleanedSize, rowsRemoved, columnsRemoved, processingTime);

            return new CleanExcelResponse
            {
                FileKey = fileKey,
                DownloadUrl = downloadUrl,
                OriginalSizeBytes = originalSize,
                CleanedSizeBytes = cleanedSize,
                RowsRemoved = rowsRemoved,
                ColumnsRemoved = columnsRemoved,
                ContentType = contentType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning Excel file");
            throw;
        }
    }

    private int RemoveEmptyRows(OfficeOpenXml.ExcelWorksheet worksheet)
    {
        var removed = 0;
        var dimension = worksheet.Dimension;
        if (dimension == null) return 0;

        for (int row = dimension.End.Row; row >= dimension.Start.Row; row--)
        {
            var isEmpty = true;
            for (int col = dimension.Start.Column; col <= dimension.End.Column; col++)
            {
                if (worksheet.Cells[row, col].Value != null)
                {
                    isEmpty = false;
                    break;
                }
            }
            if (isEmpty)
            {
                worksheet.DeleteRow(row);
                removed++;
            }
        }
        return removed;
    }

    private int RemoveEmptyColumns(OfficeOpenXml.ExcelWorksheet worksheet)
    {
        var removed = 0;
        var dimension = worksheet.Dimension;
        if (dimension == null) return 0;

        for (int col = dimension.End.Column; col >= dimension.Start.Column; col--)
        {
            var isEmpty = true;
            for (int row = dimension.Start.Row; row <= dimension.End.Row; row++)
            {
                if (worksheet.Cells[row, col].Value != null)
                {
                    isEmpty = false;
                    break;
                }
            }
            if (isEmpty)
            {
                worksheet.DeleteColumn(col);
                removed++;
            }
        }
        return removed;
    }

    private void TrimWhitespace(OfficeOpenXml.ExcelWorksheet worksheet)
    {
        var dimension = worksheet.Dimension;
        if (dimension == null) return;

        for (int row = dimension.Start.Row; row <= dimension.End.Row; row++)
        {
            for (int col = dimension.Start.Column; col <= dimension.End.Column; col++)
            {
                var cell = worksheet.Cells[row, col];
                if (cell.Value is string strValue)
                {
                    cell.Value = strValue.Trim();
                }
            }
        }
    }

    private void RemoveDuplicates(OfficeOpenXml.ExcelWorksheet worksheet)
    {
        // Simple duplicate removal - remove rows that are completely identical
        var dimension = worksheet.Dimension;
        if (dimension == null) return;

        var seenRows = new HashSet<string>();

        for (int row = dimension.End.Row; row >= dimension.Start.Row; row--)
        {
            var rowData = new List<object?>();
            for (int col = dimension.Start.Column; col <= dimension.End.Column; col++)
            {
                rowData.Add(worksheet.Cells[row, col].Value);
            }
            var rowKey = string.Join("|", rowData);
            
            if (seenRows.Contains(rowKey))
            {
                worksheet.DeleteRow(row);
            }
            else
            {
                seenRows.Add(rowKey);
            }
        }
    }

    private void StandardizeFormats(OfficeOpenXml.ExcelWorksheet worksheet)
    {
        var dimension = worksheet.Dimension;
        if (dimension == null) return;

        for (int row = dimension.Start.Row; row <= dimension.End.Row; row++)
        {
            for (int col = dimension.Start.Column; col <= dimension.End.Column; col++)
            {
                var cell = worksheet.Cells[row, col];
                // Standardize number formats, dates, etc.
                // This is a simplified version
            }
        }
    }

    private async Task SaveAsCsv(OfficeOpenXml.ExcelWorksheet worksheet, Stream outputStream, CancellationToken cancellationToken)
    {
        using var writer = new StreamWriter(outputStream, leaveOpen: true);
        var dimension = worksheet.Dimension;
        if (dimension == null) return;

        for (int row = dimension.Start.Row; row <= dimension.End.Row; row++)
        {
            var values = new List<string>();
            for (int col = dimension.Start.Column; col <= dimension.End.Column; col++)
            {
                var value = worksheet.Cells[row, col].Value?.ToString() ?? "";
                // Escape CSV values
                if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                {
                    value = $"\"{value.Replace("\"", "\"\"")}\"";
                }
                values.Add(value);
            }
            await writer.WriteLineAsync(string.Join(",", values));
        }
        await writer.FlushAsync();
    }
}

