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
using UtilityTools.Application.Features.Tools.Video.Commands.CompressVideo;

namespace UtilityTools.Api.Controllers;

[ApiController]
[Route("api/tools")]
[Route("api/v{version:apiVersion}/tools")] // ✅ Support both versioned and non-versioned routes
[ApiVersion("1.0")]
[Authorize]
public class ToolsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ToolsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("pdf/merge")]
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
    public async Task<ActionResult<CleanExcelResponse>> CleanExcel(
        [FromForm] IFormFile file,
        [FromForm] bool? removeEmptyRows = true,
        [FromForm] bool? removeEmptyColumns = true,
        [FromForm] bool? trimWhitespace = true,
        [FromForm] bool? removeDuplicates = false,
        [FromForm] bool? standardizeFormats = true,
        [FromForm] string? outputFormat = null)
    {
        if (file == null)
        {
            return BadRequest(new { error = "Excel file is required" });
        }

        var options = new CleanOptions
        {
            RemoveEmptyRows = removeEmptyRows ?? true,
            RemoveEmptyColumns = removeEmptyColumns ?? true,
            TrimWhitespace = trimWhitespace ?? true,
            RemoveDuplicates = removeDuplicates ?? false,
            StandardizeFormats = standardizeFormats ?? true,
            OutputFormat = outputFormat
        };

        var command = new CleanExcelCommand { File = file, Options = options };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("json/format")]
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
    public async Task<ActionResult<SummarizeResponse>> Summarize([FromBody] SummarizeCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("regex/generate")]
    public async Task<ActionResult<GenerateRegexResponse>> GenerateRegex([FromBody] GenerateRegexCommand command)
    {
        var result = await _mediator.Send(command);
        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            return BadRequest(new { error = result.ErrorMessage });
        }
        return Ok(result);
    }

    [HttpPost("video/compress")]
    public async Task<ActionResult<CompressVideoResponse>> CompressVideo(
        [FromForm] IFormFile file,
        [FromForm] int? crf = null,
        [FromForm] int? quality = null,
        [FromForm] string? preset = "medium",
        [FromForm] int? maxWidth = null,
        [FromForm] int? maxHeight = null,
        [FromForm] int? bitrateKbps = null,
        [FromForm] string? codec = "libx264")
    {
        if (file == null)
        {
            return BadRequest(new { error = "Video file is required" });
        }

        // Support both 'crf' (frontend) and 'quality' (legacy)
        var qualityValue = crf ?? quality ?? 23;

        var compressCommand = new CompressVideoCommand
        {
            File = file,
            Quality = qualityValue,
            Preset = preset ?? "medium",
            MaxWidth = maxWidth,
            MaxHeight = maxHeight,
            BitrateKbps = bitrateKbps,
            Codec = codec ?? "libx264"
        };

        var result = await _mediator.Send(compressCommand);
        return Ok(result);
    }
}

