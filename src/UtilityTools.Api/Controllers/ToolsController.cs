using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UtilityTools.Application.Features.Tools.Ai.Commands.Summarize;
using UtilityTools.Application.Features.Tools.Document.Commands.ConvertDocToPdf;
using UtilityTools.Application.Features.Tools.Excel.Commands.CleanExcel;
using CleanOptions = UtilityTools.Application.Features.Tools.Excel.Commands.CleanExcel.CleanOptions;
using UtilityTools.Application.Features.Tools.Image.Commands.CompressImage;
using UtilityTools.Application.Features.Tools.Image.Commands.RemoveBackground;
using UtilityTools.Application.Features.Tools.Json.Commands.FormatJson;
using UtilityTools.Application.Features.Tools.Pdf.Commands.MergePdf;
using UtilityTools.Application.Features.Tools.Pdf.Commands.SplitPdf;
using UtilityTools.Application.Features.Tools.Regex.Commands.GenerateRegex;

namespace UtilityTools.Api.Controllers;

[ApiController]
[Route("api/tools")]
[Route("api/v{version:apiVersion}/tools")] // ✅ Support both versioned and non-versioned routes
[ApiVersion("1.0")]
public class ToolsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ToolsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("pdf/merge")]
    [AllowAnonymous] // Free tool - no authentication required
    public async Task<ActionResult<MergePdfResponse>> MergePdf([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count < 2)
        {
            return BadRequest(new { error = "At least 2 PDF files are required" });
        }

        var command = new MergePdfCommand { Files = files };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("pdf/split")]
    [AllowAnonymous] // Free tool - no authentication required
    public async Task<ActionResult<SplitPdfResponse>> SplitPdf([FromForm] IFormFile file, [FromForm] string? pagesSpec = "all")
    {
        if (file == null)
        {
            return BadRequest(new { error = "PDF file is required" });
        }

        var command = new SplitPdfCommand { File = file, PagesSpec = pagesSpec ?? "all" };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("image/compress")]
    [AllowAnonymous] // Free tool - no authentication required
    public async Task<ActionResult<CompressImageResponse>> CompressImage(
        [FromForm] IFormFile file,
        [FromForm] int? quality = 80,
        [FromForm] string? targetFormat = null,
        [FromForm] int? maxWidth = null,
        [FromForm] int? maxHeight = null)
    {
        if (file == null)
        {
            return BadRequest(new { error = "Image file is required" });
        }

        var command = new CompressImageCommand
        {
            File = file,
            Quality = quality ?? 80,
            TargetFormat = targetFormat,
            MaxWidth = maxWidth,
            MaxHeight = maxHeight
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("image/remove-background")]
    [Authorize] // Premium tool (Pro tier) - authentication required
    public async Task<ActionResult<RemoveBackgroundResponse>> RemoveBackground(
        [FromForm] IFormFile file,
        [FromForm] bool transparent = true,
        [FromForm] string? backgroundColor = null)
    {
        if (file == null)
        {
            return BadRequest(new { error = "Image file is required" });
        }

        var command = new RemoveBackgroundCommand
        {
            File = file,
            Transparent = transparent,
            BackgroundColor = backgroundColor
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("convert/doc-to-pdf")]
    [AllowAnonymous] // Free tool - no authentication required
    public async Task<ActionResult<ConvertDocToPdfResponse>> ConvertDocToPdf([FromForm] IFormFile file)
    {
        if (file == null)
        {
            return BadRequest(new { error = "Document file is required" });
        }

        var command = new ConvertDocToPdfCommand { File = file };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("excel/clean")]
    [AllowAnonymous] // Free tool - no authentication required
    public async Task<ActionResult<CleanExcelResponse>> CleanExcel(
        [FromForm] IFormFile file,
        [FromForm] string? removeEmptyRows = null,
        [FromForm] string? removeEmptyColumns = null,
        [FromForm] string? trimWhitespace = null,
        [FromForm] string? removeDuplicates = null,
        [FromForm] string? standardizeFormats = null,
        [FromForm] string? outputFormat = null)
    {
        if (file == null)
        {
            return BadRequest(new { error = "Excel file is required" });
        }

        // Parse boolean values from form data (they come as strings)
        bool ParseBool(string? value, bool defaultValue) => 
            value == null ? defaultValue : 
            (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1");

        var options = new CleanOptions
        {
            RemoveEmptyRows = ParseBool(removeEmptyRows, true),
            RemoveEmptyColumns = ParseBool(removeEmptyColumns, true),
            TrimWhitespace = ParseBool(trimWhitespace, true),
            RemoveDuplicates = ParseBool(removeDuplicates, false),
            StandardizeFormats = ParseBool(standardizeFormats, true),
            OutputFormat = outputFormat
        };

        var command = new CleanExcelCommand { File = file, Options = options };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("json/format")]
    [AllowAnonymous] // Free tool - no authentication required
    public async Task<ActionResult<FormatJsonResponse>> FormatJson([FromBody] FormatJsonCommand command)
    {
        // Support both 'json' (frontend) and 'text' (legacy)
        if (string.IsNullOrEmpty(command.Text) && !string.IsNullOrEmpty(command.Json))
        {
            command.Text = command.Json;
        }

        var result = await _mediator.Send(command);
        if (!result.IsValid)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }
        return Ok(result);
    }

    [HttpPost("ai/summarize")]
    [AllowAnonymous] // Free tool - no authentication required
    public async Task<ActionResult<SummarizeResponse>> Summarize([FromBody] SummarizeCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("regex/generate")]
    [AllowAnonymous] // Free tool - no authentication required
    public async Task<ActionResult<GenerateRegexResponse>> GenerateRegex([FromBody] GenerateRegexCommand command)
    {
        var result = await _mediator.Send(command);
        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            return BadRequest(new { error = result.ErrorMessage });
        }
        return Ok(result);
    }

}

