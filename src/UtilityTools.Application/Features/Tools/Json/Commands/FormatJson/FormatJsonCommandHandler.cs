using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Application.Common.Interfaces;

namespace UtilityTools.Application.Features.Tools.Json.Commands.FormatJson;

public class FormatJsonCommandHandler : IRequestHandler<FormatJsonCommand, FormatJsonResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FormatJsonCommandHandler> _logger;

    public FormatJsonCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<FormatJsonCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<FormatJsonResponse> Handle(FormatJsonCommand request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Support both 'Text' and 'Json' properties (frontend sends 'json')
            var jsonText = !string.IsNullOrEmpty(request.Json) ? request.Json : request.Text;
            if (string.IsNullOrEmpty(jsonText))
            {
                return Task.FromResult(new FormatJsonResponse
                {
                    FormattedJson = string.Empty,
                    IsValid = false,
                    ErrorMessage = "JSON text is required"
                });
            }

            // Parse and format JSON (CPU-bound operation, no async I/O needed)
            using var doc = JsonDocument.Parse(jsonText);
            var options = new JsonSerializerOptions
            {
                WriteIndented = request.Indent,
                // ✅ Use IndentSize for custom indentation (though System.Text.Json doesn't support custom indent size directly)
                // We'll use the default indentation which is 2 spaces
            };

            var formattedJson = JsonSerializer.Serialize(doc, options);

            var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            // Record usage if user is authenticated
            // Note: UserId will be null for unauthenticated requests
            // This is handled by the API layer requiring authorization

            var jsonLength = !string.IsNullOrEmpty(request.Json) ? request.Json.Length : request.Text.Length;
            _logger.LogInformation("JSON formatted successfully. Size: {Size} bytes, Time: {Time}ms", 
                jsonLength, processingTime);

            return Task.FromResult(new FormatJsonResponse
            {
                FormattedJson = formattedJson,
                IsValid = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON provided for formatting");

            return Task.FromResult(new FormatJsonResponse
            {
                FormattedJson = string.Empty,
                IsValid = false,
                ErrorMessage = $"Invalid JSON: {ex.Message}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formatting JSON");
            throw;
        }
    }
}

