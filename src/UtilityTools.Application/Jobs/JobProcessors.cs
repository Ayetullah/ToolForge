using DocumentFormat.OpenXml.Packaging;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using System.Text.Json;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Domain.Entities;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.Interfaces;
using Image = SixLabors.ImageSharp.Image;

namespace UtilityTools.Application.Jobs;

public class JobProcessors
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobProcessors> _logger;

    public JobProcessors(IServiceProvider serviceProvider, ILogger<JobProcessors> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 120, 300 })]
    public async Task ProcessDocumentConversion(Guid jobId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        var jobRepository = unitOfWork.Repository<Job>();
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);

        if (job == null)
        {
            _logger.LogError("Job {JobId} not found", jobId);
            return;
        }

        try
        {
            job.Start();
            await jobRepository.UpdateAsync(job, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Processing document conversion job {JobId}", jobId);

            // Extract parameters
            var originalFileName = job.Parameters.GetValueOrDefault("OriginalFileName")?.ToString() ?? "document";
            var contentType = job.Parameters.GetValueOrDefault("ContentType")?.ToString() ?? "";
            var originalSize = job.Parameters.GetValueOrDefault("OriginalSize") is long os ? os : 0L;

            // Determine file type
            var fileExtension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var isExcel = fileExtension == ".xlsx" || fileExtension == ".xls" || fileExtension == ".csv";
            var isWord = fileExtension == ".docx" || fileExtension == ".doc";

            if (!isExcel && !isWord)
            {
                job.Fail($"Unsupported file format: {fileExtension}. Currently supported: Excel (.xlsx, .xls, .csv) and Word (.docx, .doc). PowerPoint conversion requires LibreOffice/unoconv.");
                await jobRepository.UpdateAsync(job, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            // 1. Download input file from storage
            var tempInputPath = Path.Combine(Path.GetTempPath(), $"input_{job.Id}{Path.GetExtension(originalFileName)}");
            var tempOutputPath = Path.Combine(Path.GetTempPath(), $"output_{job.Id}.pdf");

            try
            {
                using (var inputStream = await fileStorage.DownloadAsync(job.InputFileKey!))
                using (var fileStream = File.Create(tempInputPath))
                {
                    await inputStream.CopyToAsync(fileStream, cancellationToken);
                }

                // 2. Convert to PDF based on file type
                if (isExcel)
                {
                    await ConvertExcelToPdf(tempInputPath, tempOutputPath, cancellationToken);
                }
                else if (isWord)
                {
                    await ConvertWordToPdf(tempInputPath, tempOutputPath, cancellationToken);
                }

                if (!File.Exists(tempOutputPath))
                {
                    throw new FileNotFoundException($"PDF output file not found: {tempOutputPath}");
                }

                var outputFileInfo = new FileInfo(tempOutputPath);

                // 3. Upload output PDF
                using var outputStream = File.OpenRead(tempOutputPath);
                var outputFileName = $"converted_{Guid.NewGuid()}.pdf";
                var outputFileKey = await fileStorage.UploadAsync(
                    outputStream,
                    outputFileName,
                    "application/pdf",
                    $"document/pdf/{job.UserId}",
                    cancellationToken);

                // 4. Generate presigned URL
                var downloadUrl = await fileStorage.GeneratePresignedUrlAsync(
                    outputFileKey,
                    TimeSpan.FromHours(24),
                    cancellationToken);

                // 5. Update job status
                job.Complete(outputFileKey, downloadUrl, DateTime.UtcNow.Add(TimeSpan.FromHours(24)));
                await jobRepository.UpdateAsync(job, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Job {JobId} status updated to Completed. SignedDownloadUrl: {Url}", 
                    jobId, downloadUrl);

                // Record usage only for authenticated users
                if (job.UserId.HasValue)
                {
                    var usageRepository = unitOfWork.Repository<UsageRecord>();
                    var processingTime = (int)(job.CompletedAt!.Value - job.StartedAt!.Value).TotalMilliseconds;
                    var usageRecord = new UsageRecord(
                        job.UserId.Value,
                        ToolType.DocToPdf,
                        fileSizeBytes: originalSize,
                        processingTimeMs: processingTime,
                        cost: 0m);

                    await usageRepository.AddAsync(usageRecord, cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }

                _logger.LogInformation("Document conversion job {JobId} completed successfully. Output file: {OutputFileKey}, Size: {Size} bytes",
                    jobId, outputFileKey, outputFileInfo.Length);
            }
            finally
            {
                // Clean up temporary files
                try
                {
                    if (File.Exists(tempInputPath))
                        File.Delete(tempInputPath);
                    if (File.Exists(tempOutputPath))
                        File.Delete(tempOutputPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up temporary files for job {JobId}", jobId);
                }

                // Clean up temporary input file from storage
                if (!string.IsNullOrEmpty(job.InputFileKey))
                {
                    try
                    {
                        await fileStorage.DeleteAsync(job.InputFileKey, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary input file {FileKey} for job {JobId}", job.InputFileKey, jobId);
                    }
                }
            }

            await jobRepository.UpdateAsync(job, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document conversion job {JobId}. Error: {ErrorMessage}, StackTrace: {StackTrace}", 
                jobId, ex.Message, ex.StackTrace);
            
            try
            {
                job.Fail(ex.Message);
                await jobRepository.UpdateAsync(job, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to update job status to Failed for job {JobId}", jobId);
            }
            
            throw; // Re-throw for Hangfire retry mechanism
        }
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 120, 300 })]
    public async Task ProcessBackgroundRemoval(Guid jobId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        var jobRepository = unitOfWork.Repository<Job>();
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);

        if (job == null)
        {
            _logger.LogError("Job {JobId} not found", jobId);
            return;
        }

        try
        {
            job.Start();
            await jobRepository.UpdateAsync(job, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Processing background removal job {JobId}", jobId);

            // 1. Download input file from storage
            if (string.IsNullOrEmpty(job.InputFileKey))
            {
                job.Fail("Input file key is missing");
                await jobRepository.UpdateAsync(job, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            using var inputStream = await fileStorage.DownloadAsync(job.InputFileKey, cancellationToken);
            if (inputStream == null)
            {
                job.Fail("Failed to download input file");
                await jobRepository.UpdateAsync(job, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            // 2. Process image (remove background)
            // Read original size before processing
            var originalSize = 0L;
            if (inputStream.CanSeek)
            {
                originalSize = inputStream.Length;
            }
            else
            {
                // If stream doesn't support Length, we'll use file size from job metadata if available
                if (job.Parameters.ContainsKey("OriginalSize") && 
                    long.TryParse(job.Parameters["OriginalSize"].ToString(), out var size))
                {
                    originalSize = size;
                }
            }

            using var image = await Image.LoadAsync<Rgba32>(inputStream, cancellationToken);

            // Get parameters from job
            var transparent = job.Parameters.ContainsKey("Transparent") && 
                            bool.TryParse(job.Parameters["Transparent"].ToString(), out var trans) && trans;
            var backgroundColor = job.Parameters.ContainsKey("BackgroundColor") 
                ? job.Parameters["BackgroundColor"]?.ToString() 
                : null;

            // Remove background
            if (transparent)
            {
                // Remove background by detecting corner colors and making similar pixels transparent
                RemoveBackgroundTransparent(image);
            }
            else if (!string.IsNullOrEmpty(backgroundColor))
            {
                // Remove specific background color
                RemoveBackgroundColor(image, backgroundColor);
            }
            else
            {
                // Default: remove white/light background
                RemoveBackgroundTransparent(image);
            }

            // 3. Upload output file
            using var outputStream = new MemoryStream();
            await image.SaveAsPngAsync(outputStream, cancellationToken);
            outputStream.Position = 0;

            var outputFileName = $"removed_bg_{Guid.NewGuid()}.png";
            var outputFileKey = await fileStorage.UploadAsync(
                outputStream,
                outputFileName,
                "image/png",
                $"image/background-removal/{job.UserId}",
                cancellationToken);

            // 4. Generate presigned URL
            var downloadUrl = await fileStorage.GeneratePresignedUrlAsync(
                outputFileKey,
                TimeSpan.FromHours(24),
                cancellationToken);

            // 5. Update job status
            job.Complete(outputFileKey, downloadUrl, DateTime.UtcNow.Add(TimeSpan.FromHours(24)));
            await jobRepository.UpdateAsync(job, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Job {JobId} status updated to Completed. SignedDownloadUrl: {Url}", 
                jobId, downloadUrl);

            // Record usage (ImageRemoveBackground requires authentication, so UserId should always be present)
            if (job.UserId.HasValue)
            {
                var usageRepository = unitOfWork.Repository<UsageRecord>();
                var processingTime = (int)(job.CompletedAt!.Value - job.StartedAt!.Value).TotalMilliseconds;
                var usageRecord = new UsageRecord(
                    job.UserId.Value,
                    ToolType.ImageRemoveBackground,
                    fileSizeBytes: originalSize,
                    processingTimeMs: processingTime,
                    cost: 0m,
                    jobId: job.Id.ToString());

                await usageRepository.AddAsync(usageRecord, cancellationToken);
            }
            await unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Background removal job {JobId} completed successfully. Output file: {OutputFileKey}, Original: {Original} bytes, Output: {Output} bytes",
                jobId, outputFileKey, originalSize, outputStream.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing background removal job {JobId}", jobId);
            job.Fail(ex.Message);
            await jobRepository.UpdateAsync(job, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw; // Re-throw for Hangfire retry mechanism
        }
    }

    /// <summary>
    /// Converts Excel file to PDF using NPOI and PdfSharpCore
    /// </summary>
    private async Task ConvertExcelToPdf(string inputPath, string outputPath, CancellationToken cancellationToken)
    {
        IWorkbook workbook;
        using (var fileStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
        {
            var isXls = Path.GetExtension(inputPath).Equals(".xls", StringComparison.OrdinalIgnoreCase);
            workbook = isXls ? new HSSFWorkbook(fileStream) : new XSSFWorkbook(fileStream);
        }

        try
        {
            var pdfDocument = new PdfDocument();
            pdfDocument.Info.Title = "Converted Excel Document";

            // Process each sheet
            for (int sheetIndex = 0; sheetIndex < workbook.NumberOfSheets; sheetIndex++)
            {
                var sheet = workbook.GetSheetAt(sheetIndex);
                if (sheet == null) continue;

                var pdfPage = pdfDocument.AddPage();
                var gfx = XGraphics.FromPdfPage(pdfPage);
                var font = new XFont("Arial", 10, XFontStyle.Regular);
                var boldFont = new XFont("Arial", 10, XFontStyle.Bold);

                double yPosition = 40;
                double xPosition = 40;
                double rowHeight = 20;
                double maxWidth = pdfPage.Width - 80;
                double currentX = xPosition;

                // Add sheet name as header
                gfx.DrawString($"Sheet: {sheet.SheetName}", boldFont, XBrushes.Black, 
                    new XRect(xPosition, yPosition, maxWidth, rowHeight), XStringFormats.TopLeft);
                yPosition += rowHeight + 10;

                // Process rows
                for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var row = sheet.GetRow(rowIndex);
                    if (row == null) continue;

                    // Check if we need a new page
                    if (yPosition > pdfPage.Height - 60)
                    {
                        pdfPage = pdfDocument.AddPage();
                        gfx = XGraphics.FromPdfPage(pdfPage);
                        yPosition = 40;
                    }

                    currentX = xPosition;
                    var maxRowHeight = rowHeight;

                    // Process cells in row
                    for (int cellIndex = 0; cellIndex < row.LastCellNum; cellIndex++)
                    {
                        var cell = row.GetCell(cellIndex);
                        if (cell == null) continue;

                        string cellValue = GetCellValueAsString(cell);
                        var cellFont = rowIndex == 0 ? boldFont : font; // Header row in bold

                        // Calculate cell width (approximate)
                        double cellWidth = Math.Max(80, cellValue.Length * 6);

                        // Check if we need to wrap to next line
                        if (currentX + cellWidth > maxWidth + xPosition)
                        {
                            yPosition += maxRowHeight + 5;
                            currentX = xPosition;
                            maxRowHeight = rowHeight;

                            // Check for new page
                            if (yPosition > pdfPage.Height - 60)
                            {
                                pdfPage = pdfDocument.AddPage();
                                gfx = XGraphics.FromPdfPage(pdfPage);
                                yPosition = 40;
                            }
                        }

                        // Draw cell
                        var cellRect = new XRect(currentX, yPosition, cellWidth, maxRowHeight);
                        gfx.DrawRectangle(XPens.LightGray, XBrushes.White, cellRect);
                        gfx.DrawString(cellValue, cellFont, XBrushes.Black, cellRect, XStringFormats.Center);

                        currentX += cellWidth + 2;
                    }

                    yPosition += maxRowHeight + 2;
                }

                // Add new page for next sheet (except last)
                if (sheetIndex < workbook.NumberOfSheets - 1)
                {
                    pdfDocument.AddPage();
                }
            }

            pdfDocument.Save(outputPath);
        }
        finally
        {
            workbook?.Close();
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Converts Word file to PDF using OpenXML SDK
    /// </summary>
    private async Task ConvertWordToPdf(string inputPath, string outputPath, CancellationToken cancellationToken)
    {
        var pdfDocument = new PdfDocument();
        pdfDocument.Info.Title = "Converted Word Document";

        var pdfPage = pdfDocument.AddPage();
        var gfx = XGraphics.FromPdfPage(pdfPage);
        var font = new XFont("Arial", 12, XFontStyle.Regular);
        var boldFont = new XFont("Arial", 12, XFontStyle.Bold);
        var titleFont = new XFont("Arial", 16, XFontStyle.Bold);

        double yPosition = 40;
        double margin = 40;
        double maxWidth = pdfPage.Width - (margin * 2);
        double lineHeight = 18;

        try
        {
            // Check if it's .docx (OpenXML) or .doc (old format)
            var isDocx = Path.GetExtension(inputPath).Equals(".docx", StringComparison.OrdinalIgnoreCase);

            if (isDocx)
            {
                // Use OpenXML SDK for .docx files
                using (var wordDocument = WordprocessingDocument.Open(inputPath, false))
                {
                    var body = wordDocument.MainDocumentPart?.Document?.Body;
                    if (body == null)
                    {
                        throw new InvalidOperationException("Word document body is null");
                    }

                    foreach (var element in body.Elements())
                    {
                        // Check if we need a new page
                        if (yPosition > pdfPage.Height - 80)
                        {
                            pdfPage = pdfDocument.AddPage();
                            gfx = XGraphics.FromPdfPage(pdfPage);
                            yPosition = margin;
                        }

                        if (element is DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph)
                        {
                            var text = GetTextFromParagraph(paragraph);
                            if (string.IsNullOrWhiteSpace(text))
                            {
                                yPosition += lineHeight / 2; // Add spacing for empty paragraphs
                                continue;
                            }

                            // Check if paragraph is a heading (simplified)
                            var isHeading = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value?.StartsWith("Heading") == true;
                            var currentFont = isHeading ? titleFont : font;

                            // Word wrap
                            var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            var currentLine = "";

                            foreach (var word in words)
                            {
                                var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                                var size = gfx.MeasureString(testLine, currentFont);

                                if (size.Width > maxWidth && !string.IsNullOrEmpty(currentLine))
                                {
                                    gfx.DrawString(currentLine, currentFont, XBrushes.Black,
                                        new XRect(margin, yPosition, maxWidth, lineHeight), XStringFormats.TopLeft);
                                    yPosition += lineHeight;
                                    currentLine = word;

                                    // Check for new page
                                    if (yPosition > pdfPage.Height - 80)
                                    {
                                        pdfPage = pdfDocument.AddPage();
                                        gfx = XGraphics.FromPdfPage(pdfPage);
                                        yPosition = margin;
                                    }
                                }
                                else
                                {
                                    currentLine = testLine;
                                }
                            }

                            if (!string.IsNullOrEmpty(currentLine))
                            {
                                gfx.DrawString(currentLine, currentFont, XBrushes.Black,
                                    new XRect(margin, yPosition, maxWidth, lineHeight), XStringFormats.TopLeft);
                                yPosition += lineHeight + (isHeading ? 5 : 2);
                            }
                        }
                        else if (element is DocumentFormat.OpenXml.Wordprocessing.Table table)
                        {
                            // Handle tables (simplified - just extract text)
                            yPosition += lineHeight;
                            foreach (var row in table.Elements<DocumentFormat.OpenXml.Wordprocessing.TableRow>())
                            {
                                var rowText = string.Join(" | ", row.Elements<DocumentFormat.OpenXml.Wordprocessing.TableCell>()
                                    .SelectMany(cell => cell.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                                    .Select(GetTextFromParagraph)
                                    .Where(t => !string.IsNullOrWhiteSpace(t)));

                                if (!string.IsNullOrWhiteSpace(rowText))
                                {
                                    if (yPosition > pdfPage.Height - 80)
                                    {
                                        pdfPage = pdfDocument.AddPage();
                                        gfx = XGraphics.FromPdfPage(pdfPage);
                                        yPosition = margin;
                                    }

                                    gfx.DrawString(rowText, font, XBrushes.Black,
                                        new XRect(margin, yPosition, maxWidth, lineHeight), XStringFormats.TopLeft);
                                    yPosition += lineHeight;
                                }
                            }
                            yPosition += lineHeight;
                        }
                    }
                }
            }
            else
            {
                // .doc files (old format) - basic text extraction
                // Note: Full .doc support requires additional libraries
                gfx.DrawString("Old .doc format detected. Please convert to .docx for better results.", 
                    font, XBrushes.Orange, new XRect(margin, yPosition, maxWidth, 100), XStringFormats.TopLeft);
                _logger.LogWarning("Old .doc format conversion is limited. Consider converting to .docx first.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting Word document to PDF: {Path}", inputPath);
            throw new InvalidOperationException($"Failed to convert Word document: {ex.Message}", ex);
        }

        pdfDocument.Save(outputPath);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Extracts text from a Word paragraph
    /// </summary>
    private static string GetTextFromParagraph(DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph)
    {
        if (paragraph == null) return "";

        var text = new System.Text.StringBuilder();
        foreach (var run in paragraph.Elements<DocumentFormat.OpenXml.Wordprocessing.Run>())
        {
            foreach (var textElement in run.Elements<DocumentFormat.OpenXml.Wordprocessing.Text>())
            {
                text.Append(textElement.Text);
            }
        }

        return text.ToString().Trim();
    }

    /// <summary>
    /// Gets cell value as string from NPOI cell
    /// </summary>
    private static string GetCellValueAsString(ICell cell)
    {
        return cell.CellType switch
        {
            CellType.String => cell.StringCellValue,
            CellType.Numeric => DateUtil.IsCellDateFormatted(cell) 
                ? DateUtil.GetJavaDate(cell.NumericCellValue).ToString("yyyy-MM-dd") 
                : cell.NumericCellValue.ToString(),
            CellType.Boolean => cell.BooleanCellValue.ToString(),
            CellType.Formula => cell.CachedFormulaResultType switch
            {
                CellType.String => cell.StringCellValue,
                CellType.Numeric => cell.NumericCellValue.ToString(),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                _ => ""
            },
            CellType.Blank => "",
            _ => ""
        };
    }

    /// <summary>
    /// Removes background by analyzing border pixels and using improved color distance calculation
    /// </summary>
    private void RemoveBackgroundTransparent(Image<Rgba32> image)
    {
        var width = image.Width;
        var height = image.Height;
        
        // Sample border pixels (not just corners) to better determine background
        var borderPixels = new List<Rgba32>();
        var borderWidth = Math.Min(10, width / 20); // Sample 10 pixels or 5% of width
        var borderHeight = Math.Min(10, height / 20); // Sample 10 pixels or 5% of height
        
        // Top border
        for (int x = 0; x < width; x += Math.Max(1, width / borderWidth))
        {
            borderPixels.Add(image[x, 0]);
        }
        
        // Bottom border
        for (int x = 0; x < width; x += Math.Max(1, width / borderWidth))
        {
            borderPixels.Add(image[x, height - 1]);
        }
        
        // Left border
        for (int y = 0; y < height; y += Math.Max(1, height / borderHeight))
        {
            borderPixels.Add(image[0, y]);
        }
        
        // Right border
        for (int y = 0; y < height; y += Math.Max(1, height / borderHeight))
        {
            borderPixels.Add(image[width - 1, y]);
        }

        // Calculate average border color
        var avgR = borderPixels.Average(c => c.R);
        var avgG = borderPixels.Average(c => c.G);
        var avgB = borderPixels.Average(c => c.B);

        // Use Euclidean distance for better color similarity
        // Lower threshold for more precise removal (only very similar colors)
        var threshold = 25.0; // Euclidean distance threshold

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    ref var pixel = ref row[x];
                    
                    // Calculate Euclidean color distance
                    var rDiff = pixel.R - avgR;
                    var gDiff = pixel.G - avgG;
                    var bDiff = pixel.B - avgB;
                    var distance = Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);

                    // Only remove pixels that are very similar to background
                    // Use a gradual alpha reduction for smoother edges
                    if (distance < threshold)
                    {
                        // Calculate alpha based on distance (feathering effect)
                        var alphaRatio = distance / threshold;
                        pixel.A = (byte)Math.Max(0, Math.Min(255, (int)(pixel.A * alphaRatio)));
                    }
                }
            }
        });
    }

    /// <summary>
    /// Removes specific background color with improved color distance calculation
    /// </summary>
    private void RemoveBackgroundColor(Image<Rgba32> image, string backgroundColor)
    {
        // Parse hex color
        Rgba32 targetColor;
        if (backgroundColor.StartsWith("#"))
        {
            var hex = backgroundColor.Substring(1);
            if (hex.Length == 6)
            {
                var r = Convert.ToByte(hex.Substring(0, 2), 16);
                var g = Convert.ToByte(hex.Substring(2, 2), 16);
                var b = Convert.ToByte(hex.Substring(4, 2), 16);
                targetColor = new Rgba32(r, g, b, 255);
            }
            else
            {
                _logger.LogWarning("Invalid hex color format: {Color}, using default white removal", backgroundColor);
                RemoveBackgroundTransparent(image);
                return;
            }
        }
        else
        {
            _logger.LogWarning("Invalid color format: {Color}, using default white removal", backgroundColor);
            RemoveBackgroundTransparent(image);
            return;
        }

        // Use Euclidean distance for better color similarity
        var threshold = 25.0; // Euclidean distance threshold

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    ref var pixel = ref row[x];
                    
                    // Calculate Euclidean color distance
                    var rDiff = pixel.R - targetColor.R;
                    var gDiff = pixel.G - targetColor.G;
                    var bDiff = pixel.B - targetColor.B;
                    var distance = Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);

                    // Use gradual alpha reduction for smoother edges
                    if (distance < threshold)
                    {
                        var alphaRatio = distance / threshold;
                        pixel.A = (byte)Math.Max(0, Math.Min(255, (int)(pixel.A * alphaRatio)));
                    }
                }
            }
        });
    }

}

