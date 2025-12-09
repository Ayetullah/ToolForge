using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Domain.Entities;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.Interfaces;
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
            
            // Check if file is CSV
            var isCsv = request.File.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ||
                       request.File.ContentType == "text/csv" ||
                       request.File.ContentType == "application/csv";

            IWorkbook workbook;
            ISheet sheet;

            if (isCsv)
            {
                // Load CSV into workbook
                workbook = new XSSFWorkbook();
                sheet = workbook.CreateSheet("Sheet1");
                await LoadCsvIntoSheet(inputStream, sheet, cancellationToken);
            }
            else
            {
                // Determine if it's .xls or .xlsx
                var isXls = request.File.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase);
                
                // Load Excel file
                workbook = isXls 
                    ? new HSSFWorkbook(inputStream) 
                    : new XSSFWorkbook(inputStream);
                
                sheet = workbook.GetSheetAt(0) 
                    ?? throw new InvalidOperationException("Excel file must contain at least one worksheet");
            }

            try
            {
                var originalRowCount = sheet.LastRowNum + 1;
                var originalColCount = GetMaxColumnCount(sheet);
                var rowsRemoved = 0;
                var columnsRemoved = 0;

                // Clean operations - order matters!
                // 1. First trim whitespace to clean data
                if (request.Options.TrimWhitespace)
                {
                    TrimWhitespace(sheet);
                }

                // 2. Standardize formats before duplicate detection
                if (request.Options.StandardizeFormats)
                {
                    StandardizeFormats(sheet);
                }

                // 3. Remove duplicates after formatting is standardized
                if (request.Options.RemoveDuplicates)
                {
                    RemoveDuplicates(sheet);
                }

                // 4. Remove empty rows and columns last
                if (request.Options.RemoveEmptyRows)
                {
                    rowsRemoved = RemoveEmptyRows(sheet);
                }

                if (request.Options.RemoveEmptyColumns)
                {
                    columnsRemoved = RemoveEmptyColumns(sheet);
                }

                // Determine output format
                var outputFormat = request.Options.OutputFormat?.ToLower() 
                    ?? (isCsv ? "csv" : "xlsx");

                MemoryStream outputStream;
                long cleanedSize;

                if (outputFormat == "csv")
                {
                    outputStream = new MemoryStream();
                    await SaveAsCsv(sheet, outputStream, cancellationToken);
                    outputStream.Position = 0;
                    cleanedSize = outputStream.Length;
                }
                else
                {
                    // NPOI's Write method closes the stream, so we need to use a non-closing wrapper
                    using var tempStream = new MemoryStream();
                    using var nonClosingStream = new NonClosingStreamWrapper(tempStream);
                    workbook.Write(nonClosingStream);
                    
                    // Now we can safely access the stream
                    tempStream.Position = 0;
                    cleanedSize = tempStream.Length;
                    
                    // Copy to a new stream for upload
                    outputStream = new MemoryStream();
                    await tempStream.CopyToAsync(outputStream, cancellationToken);
                    outputStream.Position = 0;
                }

                // Upload cleaned file
                var extension = outputFormat == "csv" ? ".csv" : ".xlsx";
                var fileName = $"cleaned_{Guid.NewGuid()}{extension}";
                var contentType = outputFormat == "csv" ? "text/csv" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                
                string fileKey;
                using (outputStream)
                {
                    fileKey = await _fileStorage.UploadAsync(
                        outputStream,
                        fileName,
                        contentType,
                        $"excel/clean/{userId}",
                        cancellationToken);
                }

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
            finally
            {
                // Dispose workbook
                workbook?.Close();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error cleaning Excel file. FileName: {FileName}, ContentType: {ContentType}, Size: {Size} bytes, Options: {Options}, ExceptionType: {ExceptionType}, InnerException: {InnerException}",
                request.File.FileName, 
                request.File.ContentType, 
                request.File.Length, 
                System.Text.Json.JsonSerializer.Serialize(request.Options),
                ex.GetType().Name,
                ex.InnerException?.Message ?? "None");
            
            // Re-throw with more context for better error messages
            throw new InvalidOperationException(
                $"Failed to clean Excel file: {ex.Message}. File: {request.File.FileName}, Type: {ex.GetType().Name}",
                ex);
        }
    }

    private int GetMaxColumnCount(ISheet sheet)
    {
        int maxCol = 0;
        for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
        {
            var row = sheet.GetRow(rowIndex);
            if (row != null && row.LastCellNum > maxCol)
            {
                maxCol = row.LastCellNum;
            }
        }
        return maxCol;
    }

    private int RemoveEmptyRows(ISheet sheet)
    {
        var removed = 0;
        var rowsToRemove = new List<int>();
        
        // First, identify empty rows (skip header row at index 0)
        for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
        {
            var row = sheet.GetRow(rowIndex);
            if (row == null || IsRowEmpty(row))
            {
                rowsToRemove.Add(rowIndex);
            }
        }
        
        
        // Remove rows from bottom to top - must be careful with indices
        var lastRowNum = sheet.LastRowNum;
        foreach (var rowIndex in rowsToRemove.OrderByDescending(x => x))
        {
            if (rowIndex <= lastRowNum)
            {
                var row = sheet.GetRow(rowIndex);
                if (row != null)
                {
                    sheet.RemoveRow(row);
                }
                
                // Shift remaining rows up
                if (rowIndex < lastRowNum)
                {
                    sheet.ShiftRows(rowIndex + 1, lastRowNum, -1);
                    lastRowNum--; // Update last row number after shift
                }
                removed++;
            }
        }
        
        return removed;
    }

    private bool IsRowEmpty(IRow row)
    {
        if (row == null) return true;
        for (int col = 0; col < row.LastCellNum; col++)
        {
            var cell = row.GetCell(col);
            if (cell != null && !string.IsNullOrWhiteSpace(GetCellValueAsString(cell)))
            {
                return false;
            }
        }
        return true;
    }

    private int RemoveEmptyColumns(ISheet sheet)
    {
        var removed = 0;
        var maxCol = GetMaxColumnCount(sheet);
        
        for (int colIndex = maxCol - 1; colIndex >= 0; colIndex--)
        {
            if (IsColumnEmpty(sheet, colIndex))
            {
                RemoveColumn(sheet, colIndex);
                removed++;
            }
        }
        return removed;
    }

    private bool IsColumnEmpty(ISheet sheet, int colIndex)
    {
        for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
        {
            var row = sheet.GetRow(rowIndex);
            if (row != null)
            {
                var cell = row.GetCell(colIndex);
                if (cell != null && !string.IsNullOrWhiteSpace(GetCellValueAsString(cell)))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private void RemoveColumn(ISheet sheet, int colIndex)
    {
        var maxCol = GetMaxColumnCount(sheet);
        
        // Shift cells left for each row
        for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
        {
            var row = sheet.GetRow(rowIndex);
            if (row != null)
            {
                // Remove the cell at colIndex
                var cellToRemove = row.GetCell(colIndex);
                if (cellToRemove != null)
                {
                    row.RemoveCell(cellToRemove);
                }
                
                // Shift remaining cells left
                for (int shiftCol = colIndex + 1; shiftCol < maxCol; shiftCol++)
                {
                    var sourceCell = row.GetCell(shiftCol);
                    if (sourceCell != null)
                    {
                        var targetCell = row.GetCell(shiftCol - 1) ?? row.CreateCell(shiftCol - 1);
                        
                        // Copy cell value
                        targetCell.SetCellType(sourceCell.CellType);
                        switch (sourceCell.CellType)
                        {
                            case CellType.Numeric:
                                targetCell.SetCellValue(sourceCell.NumericCellValue);
                                break;
                            case CellType.String:
                                targetCell.SetCellValue(sourceCell.StringCellValue ?? string.Empty);
                                break;
                            case CellType.Boolean:
                                targetCell.SetCellValue(sourceCell.BooleanCellValue);
                                break;
                            case CellType.Formula:
                                targetCell.SetCellFormula(sourceCell.CellFormula ?? string.Empty);
                                break;
                            case CellType.Blank:
                                targetCell.SetCellType(CellType.Blank);
                                break;
                        }
                        
                        // Copy cell style if exists
                        if (sourceCell.CellStyle != null)
                        {
                            targetCell.CellStyle = sourceCell.CellStyle;
                        }
                        
                        // Remove the source cell
                        row.RemoveCell(sourceCell);
                    }
                }
            }
        }
    }

    private void TrimWhitespace(ISheet sheet)
    {
        for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
        {
            var row = sheet.GetRow(rowIndex);
            if (row != null)
            {
                for (int colIndex = 0; colIndex < row.LastCellNum; colIndex++)
                {
                    var cell = row.GetCell(colIndex);
                    if (cell != null && cell.CellType == CellType.String)
                    {
                        var value = cell.StringCellValue;
                        if (value != null)
                        {
                            var trimmed = value.Trim();
                            if (trimmed != value)
                            {
                                cell.SetCellValue(trimmed);
                            }
                        }
                    }
                }
            }
        }
    }

    private void RemoveDuplicates(ISheet sheet)
    {
        var seenRows = new HashSet<string>();
        var rowsToRemove = new List<int>();

        for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
        {
            var row = sheet.GetRow(rowIndex);
            if (row != null && !IsRowEmpty(row)) // Skip empty rows
            {
                var rowData = new List<string>();
                for (int colIndex = 0; colIndex < row.LastCellNum; colIndex++)
                {
                    var cell = row.GetCell(colIndex);
                    var cellValue = GetCellValueAsString(cell);
                    
                    // Normalize date values for comparison
                    if (cell != null && (cell.CellType == CellType.String || 
                        (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))))
                    {
                        // Try to parse as date and normalize
                        if (DateTime.TryParse(cellValue, out var parsedDate))
                        {
                            cellValue = parsedDate.ToString("yyyy-MM-dd");
                        }
                    }
                    
                    // Normalize whitespace and case for comparison
                    cellValue = cellValue?.Trim().ToLowerInvariant() ?? string.Empty;
                    rowData.Add(cellValue);
                }
                var rowKey = string.Join("|", rowData);
                
                if (seenRows.Contains(rowKey))
                {
                    rowsToRemove.Add(rowIndex);
                }
                else
                {
                    seenRows.Add(rowKey);
                }
            }
        }

        // Remove duplicate rows in reverse order using ShiftRows
        foreach (var rowIndex in rowsToRemove.OrderByDescending(x => x))
        {
            var row = sheet.GetRow(rowIndex);
            if (row != null)
            {
                sheet.RemoveRow(row);
            }
            
            // Shift remaining rows up
            if (rowIndex < sheet.LastRowNum)
            {
                sheet.ShiftRows(rowIndex + 1, sheet.LastRowNum, -1);
            }
        }
        
    }

    private void StandardizeFormats(ISheet sheet)
    {
        var workbook = sheet.Workbook;
        var dataFormat = workbook.CreateDataFormat();
        var standardDateStyle = workbook.CreateCellStyle();
        standardDateStyle.DataFormat = dataFormat.GetFormat("yyyy-mm-dd");
        
        // Standardize date and number formats
        for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
        {
            var row = sheet.GetRow(rowIndex);
            if (row != null)
            {
                for (int colIndex = 0; colIndex < row.LastCellNum; colIndex++)
                {
                    var cell = row.GetCell(colIndex);
                    if (cell != null)
                    {
                        // Convert formula results to values if needed
                        if (cell.CellType == CellType.Formula)
                        {
                            // Keep formulas as-is, but ensure proper formatting
                            continue;
                        }
                        
                        // Try to parse and standardize date strings
                        if (cell.CellType == CellType.String)
                        {
                            var stringValue = cell.StringCellValue?.Trim() ?? string.Empty;
                            if (!string.IsNullOrEmpty(stringValue))
                            {
                                // Try multiple date parsing strategies
                                DateTime? parsedDate = null;
                                
                                // Try parsing common date formats manually first (more reliable)
                                if (TryParseDate(stringValue, out var dt2))
                                {
                                    parsedDate = dt2;
                                }
                                // Fallback to standard DateTime.TryParse
                                else if (DateTime.TryParse(stringValue, out var dt))
                                {
                                    parsedDate = dt;
                                }
                                
                                if (parsedDate.HasValue)
                                {
                                    // Convert string date to numeric date with standardized format
                                    cell.SetCellValue(parsedDate.Value);
                                    var dateStyle = workbook.CreateCellStyle();
                                    // Use explicit format string - Excel format code 14 is yyyy-mm-dd
                                    // But we'll use custom format to ensure consistency
                                    dateStyle.DataFormat = dataFormat.GetFormat("yyyy-mm-dd");
                                    cell.CellStyle = dateStyle;
                                    continue;
                                }
                                else
                                {
                                    // If date parsing failed, try to fix common malformed dates
                                    var fixedDate = TryFixMalformedDate(stringValue);
                                    if (fixedDate.HasValue)
                                    {
                                        cell.SetCellValue(fixedDate.Value);
                                        var dateStyle = workbook.CreateCellStyle();
                                        dateStyle.DataFormat = dataFormat.GetFormat("yyyy-mm-dd");
                                        cell.CellStyle = dateStyle;
                                        continue;
                                    }
                                    
                                    // If date parsing failed, check if it looks like a date but is malformed
                                    if (LooksLikeDate(stringValue) && !IsValidDate(stringValue))
                                    {
                                        // Keep as string but don't try to format it
                                        continue;
                                    }
                                }
                            }
                        }
                        
                        // Standardize existing date cells (numeric dates) - ALL must use same format
                        if (cell.CellType == CellType.Numeric)
                        {
                            if (DateUtil.IsCellDateFormatted(cell))
                            {
                                // Apply standard date format - use same format string for consistency
                                // Create new style to ensure format is applied correctly
                                var dateStyle = workbook.CreateCellStyle();
                                // Copy existing style properties if any
                                if (cell.CellStyle != null)
                                {
                                    dateStyle.CloneStyleFrom(cell.CellStyle);
                                }
                                // Apply standard date format
                                dateStyle.DataFormat = dataFormat.GetFormat("yyyy-mm-dd");
                                cell.CellStyle = dateStyle;
                            }
                            // Check if numeric value could be a date (Excel date serial number)
                            else if (cell.NumericCellValue > 0 && cell.NumericCellValue < 1000000)
                            {
                                // Try to interpret as Excel date serial number
                                try
                                {
                                    var excelDate = DateUtil.GetJavaDate(cell.NumericCellValue);
                                    // If it's a reasonable date (between 1900 and 2100)
                                    if (excelDate.Year >= 1900 && excelDate.Year <= 2100)
                                    {
                                        cell.SetCellValue(excelDate);
                                        var dateStyle = workbook.CreateCellStyle();
                                        dateStyle.DataFormat = dataFormat.GetFormat("yyyy-mm-dd");
                                        cell.CellStyle = dateStyle;
                                        continue;
                                    }
                                }
                                catch
                                {
                                    // Not a date, continue with number formatting
                                }
                            }
                            
                            // Ensure numeric cells have proper formatting (only if not a date)
                            if (!DateUtil.IsCellDateFormatted(cell))
                            {
                                if (cell.CellStyle == null || cell.CellStyle.DataFormat == 0)
                                {
                                    var style = workbook.CreateCellStyle();
                                    style.DataFormat = dataFormat.GetFormat("General");
                                    cell.CellStyle = style;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Check if a string looks like a date (contains date-like patterns)
    /// </summary>
    private bool LooksLikeDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        // Check for common date patterns
        return value.Contains("/") || value.Contains("-") || 
               value.Contains("Jan") || value.Contains("Feb") || value.Contains("Mar") ||
               value.Contains("Apr") || value.Contains("May") || value.Contains("Jun") ||
               value.Contains("Jul") || value.Contains("Aug") || value.Contains("Sep") ||
               value.Contains("Oct") || value.Contains("Nov") || value.Contains("Dec");
    }
    
    /// <summary>
    /// Check if a string is a valid date
    /// </summary>
    private bool IsValidDate(string value)
    {
        return TryParseDate(value, out _) || DateTime.TryParse(value, out _);
    }

    private string GetCellValueAsString(ICell? cell)
    {
        if (cell == null) return string.Empty;
        
        return cell.CellType switch
        {
            CellType.String => cell.StringCellValue ?? string.Empty,
            CellType.Numeric => DateUtil.IsCellDateFormatted(cell) 
                ? cell.DateCellValue.ToString() 
                : cell.NumericCellValue.ToString(),
            CellType.Boolean => cell.BooleanCellValue.ToString(),
            CellType.Formula => cell.CellFormula ?? string.Empty,
            CellType.Blank => string.Empty,
            _ => string.Empty
        };
    }

    private async Task SaveAsCsv(ISheet sheet, Stream outputStream, CancellationToken cancellationToken)
    {
        using var writer = new StreamWriter(outputStream, leaveOpen: true);
        
        for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
        {
            var row = sheet.GetRow(rowIndex);
            if (row == null) continue;

            var values = new List<string>();
            for (int colIndex = 0; colIndex < row.LastCellNum; colIndex++)
            {
                var cell = row.GetCell(colIndex);
                var value = GetCellValueAsString(cell);
                
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

    private async Task LoadCsvIntoSheet(Stream csvStream, ISheet sheet, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(csvStream);
        int rowIndex = 0;
        string? line;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                // Simple CSV parsing (handles quoted values)
                var values = ParseCsvLine(line);
                if (values.Count > 0)
                {
                    var row = sheet.CreateRow(rowIndex);
                    for (int colIndex = 0; colIndex < values.Count; colIndex++)
                    {
                        var value = values[colIndex];
                        var cell = row.CreateCell(colIndex);
                        cell.SetCellValue(string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim());
                    }
                    rowIndex++;
                }
            }
            catch (Exception ex)
            {
                // Skip malformed CSV line
                // Continue with next line instead of failing completely
            }
        }
    }

    private List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var currentValue = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    currentValue.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    // Toggle quote state
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // End of value
                values.Add(currentValue.ToString());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        // Add last value
        values.Add(currentValue.ToString());

        return values;
    }

    /// <summary>
    /// Try to parse date from various formats
    /// </summary>
    private bool TryParseDate(string dateString, out DateTime result)
    {
        result = default;
        
        if (string.IsNullOrWhiteSpace(dateString))
            return false;
        
        // Common date formats
        var formats = new[]
        {
            "yyyy/MM/dd",
            "dd-MM-yyyy",
            "yyyy-MM-dd",
            "d MMM yyyy",
            "dd/MM/yyyy",
            "MM/dd/yyyy",
            "yyyy.MM.dd",
            "dd.MM.yyyy"
        };
        
        // Try each format
        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(dateString, format, null, System.Globalization.DateTimeStyles.None, out result))
            {
                return true;
            }
        }
        
        // Try parsing with culture-specific formats
        var cultures = new[] { "en-US", "tr-TR", "en-GB" };
        foreach (var culture in cultures)
        {
            var cultureInfo = new System.Globalization.CultureInfo(culture);
            if (DateTime.TryParse(dateString, cultureInfo, System.Globalization.DateTimeStyles.None, out result))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Try to fix common malformed date patterns
    /// </summary>
    private DateTime? TryFixMalformedDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        
        // Common patterns to fix:
        // "2024/01/C" -> "2024/01/01" (replace last character with 1)
        // "02-01-202" -> "02-01-2024" (add missing year digit)
        // "2024/01/" -> "2024/01/01" (add missing day)
        
        var trimmed = value.Trim();
        
        // Pattern: "2024/01/C" or "2024/01/X" or "2024/01/0" - replace last char(s) with 01
        if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d{4}/\d{2}/[A-Z0]$"))
        {
            var fixedValue = trimmed.Substring(0, trimmed.LastIndexOf('/') + 1) + "01";
            if (DateTime.TryParse(fixedValue, out var dt))
            {
                return dt;
            }
        }
        
        // Pattern: "2024/01/0" - complete to 01
        if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d{4}/\d{2}/0$"))
        {
            var fixedValue = trimmed + "1";
            if (DateTime.TryParse(fixedValue, out var dt))
            {
                return dt;
            }
        }
        
        // Pattern: "02-01-202" - add missing year digit (assume 2024)
        if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d{2}-\d{2}-\d{3}$"))
        {
            var fixedValue = trimmed + "4"; // Assume 2024
            if (DateTime.TryParse(fixedValue, out var dt))
            {
                return dt;
            }
        }
        
        // Pattern: "2024/01/" - add missing day
        if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d{4}/\d{2}/$"))
        {
            var fixedValue = trimmed + "01";
            if (DateTime.TryParse(fixedValue, out var dt))
            {
                return dt;
            }
        }
        
        // Pattern: "2024/01" - add missing day
        if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d{4}/\d{2}$"))
        {
            var fixedValue = trimmed + "/01";
            if (DateTime.TryParse(fixedValue, out var dt))
            {
                return dt;
            }
        }
        
        // Pattern: "02-01-202" without trailing digit - try to complete
        if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d{2}-\d{2}-\d{3}$"))
        {
            // Try adding 4 for 2024
            var fixedValue = trimmed + "4";
            if (DateTime.TryParse(fixedValue, out var dt))
            {
                return dt;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Wrapper class that prevents NPOI from closing the underlying stream
    /// </summary>
    private class NonClosingStreamWrapper : Stream
    {
        private readonly Stream _baseStream;

        public NonClosingStreamWrapper(Stream baseStream)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
        }

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => _baseStream.CanWrite;
        public override long Length => _baseStream.Length;
        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }

        public override void Flush() => _baseStream.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _baseStream.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => _baseStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);
        public override void SetLength(long value) => _baseStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _baseStream.Write(buffer, offset, count);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => _baseStream.WriteAsync(buffer, offset, count, cancellationToken);

        // Override Close/Dispose to NOT close the underlying stream
        protected override void Dispose(bool disposing)
        {
            // Don't dispose the base stream - we want to keep it open
            // The base stream will be disposed by the using statement in the caller
        }
    }
}
