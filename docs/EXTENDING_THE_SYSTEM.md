# Extending UtilityTools - Adding a New Tool

This guide demonstrates how to add a new utility tool to the system following Clean Architecture and CQRS patterns.

## Example: Adding a "Text to Speech" Tool

### Step 1: Add Tool Type to Domain

**File**: `src/UtilityTools.Domain/Enums/ToolType.cs`

```csharp
public enum ToolType
{
    // ... existing tools
    TextToSpeech = 11  // Add new tool type
}
```

### Step 2: Create Command in Application Layer

**File**: `src/UtilityTools.Application/Features/Tools/TextToSpeech/Commands/ConvertTextToSpeech/ConvertTextToSpeechCommand.cs`

```csharp
using MediatR;

namespace UtilityTools.Application.Features.Tools.TextToSpeech.Commands.ConvertTextToSpeech;

public class ConvertTextToSpeechCommand : IRequest<ConvertTextToSpeechResponse>
{
    public string Text { get; set; } = string.Empty;
    public string Voice { get; set; } = "en-US";
    public string Format { get; set; } = "mp3";
    public int Speed { get; set; } = 100;
}

public class ConvertTextToSpeechResponse
{
    public string FileKey { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;
}
```

### Step 3: Create Validator

**File**: `src/UtilityTools.Application/Features/Tools/TextToSpeech/Commands/ConvertTextToSpeech/ConvertTextToSpeechCommandValidator.cs`

```csharp
using FluentValidation;

namespace UtilityTools.Application.Features.Tools.TextToSpeech.Commands.ConvertTextToSpeech;

public class ConvertTextToSpeechCommandValidator : AbstractValidator<ConvertTextToSpeechCommand>
{
    public ConvertTextToSpeechCommandValidator()
    {
        RuleFor(v => v.Text)
            .NotEmpty().WithMessage("Text is required.")
            .MaximumLength(5000).WithMessage("Text must not exceed 5000 characters.");

        RuleFor(v => v.Voice)
            .NotEmpty().WithMessage("Voice is required.");

        RuleFor(v => v.Format)
            .Must(f => new[] { "mp3", "wav", "ogg" }.Contains(f.ToLower()))
            .WithMessage("Format must be mp3, wav, or ogg.");

        RuleFor(v => v.Speed)
            .InclusiveBetween(50, 200)
            .WithMessage("Speed must be between 50 and 200.");
    }
}
```

### Step 4: Create Handler

**File**: `src/UtilityTools.Application/Features/Tools/TextToSpeech/Commands/ConvertTextToSpeech/ConvertTextToSpeechCommandHandler.cs`

```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.Interfaces;

namespace UtilityTools.Application.Features.Tools.TextToSpeech.Commands.ConvertTextToSpeech;

public class ConvertTextToSpeechCommandHandler : IRequestHandler<ConvertTextToSpeechCommand, ConvertTextToSpeechResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorage _fileStorage;
    private readonly ITextToSpeechService _ttsService; // Domain interface
    private readonly ILogger<ConvertTextToSpeechCommandHandler> _logger;

    public ConvertTextToSpeechCommandHandler(
        IApplicationDbContext context,
        IFileStorage fileStorage,
        ITextToSpeechService ttsService,
        ILogger<ConvertTextToSpeechCommandHandler> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _ttsService = ttsService;
        _logger = logger;
    }

    public async Task<ConvertTextToSpeechResponse> Handle(ConvertTextToSpeechCommand request, CancellationToken cancellationToken)
    {
        // 1. Generate audio from text
        var audioStream = await _ttsService.ConvertAsync(
            request.Text,
            request.Voice,
            request.Format,
            request.Speed,
            cancellationToken);

        // 2. Upload to storage
        var fileName = $"tts_{Guid.NewGuid()}.{request.Format}";
        var contentType = request.Format switch
        {
            "mp3" => "audio/mpeg",
            "wav" => "audio/wav",
            "ogg" => "audio/ogg",
            _ => "audio/mpeg"
        };

        var fileKey = await _fileStorage.UploadAsync(
            audioStream,
            fileName,
            contentType,
            "text-to-speech",
            cancellationToken);

        // 3. Generate presigned URL
        var downloadUrl = await _fileStorage.GeneratePresignedUrlAsync(
            fileKey,
            TimeSpan.FromHours(24),
            cancellationToken);

        // 4. Record usage
        var usageRecord = new UsageRecord(
            userId: Guid.Parse("current-user-id"), // Get from context
            ToolType.TextToSpeech,
            fileSizeBytes: audioStream.Length,
            processingTimeMs: 0,
            tokensUsed: 0,
            cost: 0.01m); // Calculate based on text length

        _context.UsageRecords.Add(usageRecord);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Text to speech conversion completed: {FileKey}", fileKey);

        return new ConvertTextToSpeechResponse
        {
            FileKey = fileKey,
            DownloadUrl = downloadUrl,
            FileSizeBytes = audioStream.Length,
            ContentType = contentType
        };
    }
}
```

### Step 5: Create Domain Interface

**File**: `src/UtilityTools.Domain/Interfaces/ITextToSpeechService.cs`

```csharp
namespace UtilityTools.Domain.Interfaces;

public interface ITextToSpeechService
{
    Task<Stream> ConvertAsync(
        string text,
        string voice,
        string format,
        int speed,
        CancellationToken cancellationToken = default);
}
```

### Step 6: Implement Service in Infrastructure

**File**: `src/UtilityTools.Infrastructure/Services/TextToSpeechService.cs`

```csharp
using UtilityTools.Domain.Interfaces;

namespace UtilityTools.Infrastructure.Services;

public class TextToSpeechService : ITextToSpeechService
{
    // Implement using Azure Cognitive Services, AWS Polly, Google TTS, etc.
    public async Task<Stream> ConvertAsync(
        string text,
        string voice,
        string format,
        int speed,
        CancellationToken cancellationToken = default)
    {
        // Implementation using your chosen TTS provider
        // Example: Azure Cognitive Services
        throw new NotImplementedException("Implement TTS service");
    }
}
```

### Step 7: Register Service in DI

**File**: `src/UtilityTools.Infrastructure/DependencyInjection.cs`

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ... existing registrations
        
        services.AddScoped<ITextToSpeechService, TextToSpeechService>();
        
        return services;
    }
}
```

### Step 8: Add API Endpoint

**File**: `src/UtilityTools.Api/Program.cs`

```csharp
app.MapPost("/api/tools/text-to-speech", async (
    ConvertTextToSpeechCommand command,
    IMediator mediator) =>
{
    var result = await mediator.Send(command);
    return Results.Ok(result);
})
.WithName("TextToSpeech")
.RequireAuthorization()
.WithOpenApi();
```

### Step 9: Add Tests

**File**: `tests/UtilityTools.Tests.Unit/Features/Tools/TextToSpeech/ConvertTextToSpeechCommandHandlerTests.cs`

```csharp
using Xunit;
using Moq;
using FluentAssertions;
using UtilityTools.Application.Features.Tools.TextToSpeech.Commands.ConvertTextToSpeech;

public class ConvertTextToSpeechCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsResponse()
    {
        // Arrange
        var command = new ConvertTextToSpeechCommand
        {
            Text = "Hello, world!",
            Voice = "en-US",
            Format = "mp3",
            Speed = 100
        };

        // Act & Assert
        // Implement test
    }
}
```

## Summary

Following this pattern ensures:
- ✅ Clean Architecture separation
- ✅ CQRS pattern consistency
- ✅ Validation with FluentValidation
- ✅ Dependency injection
- ✅ Testability
- ✅ Usage tracking
- ✅ File storage integration

Each new tool follows the same pattern, making the codebase maintainable and scalable.

