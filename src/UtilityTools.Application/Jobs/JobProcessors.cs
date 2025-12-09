using System.Diagnostics;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Domain.Entities;
using UtilityTools.Domain.Enums;
using UtilityTools.Domain.Interfaces;

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
    public async Task ProcessVideoCompression(Guid jobId, CancellationToken cancellationToken = default)
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

            _logger.LogInformation("Processing video compression job {JobId}", jobId);

            // Extract parameters
            var originalFileName = job.Parameters.GetValueOrDefault("OriginalFileName")?.ToString() ?? "video.mp4";
            var quality = job.Parameters.GetValueOrDefault("Quality") is int q ? q : 23;
            var preset = job.Parameters.GetValueOrDefault("Preset")?.ToString() ?? "medium";
            var codec = job.Parameters.GetValueOrDefault("Codec")?.ToString() ?? "libx264";
            var maxWidth = job.Parameters.GetValueOrDefault("MaxWidth") is int mw ? mw : (int?)null;
            var maxHeight = job.Parameters.GetValueOrDefault("MaxHeight") is int mh ? mh : (int?)null;
            var bitrateKbps = job.Parameters.GetValueOrDefault("BitrateKbps") is int br ? br : (int?)null;
            var originalSize = job.Parameters.GetValueOrDefault("OriginalSize") is long os ? os : 0L;

            // 1. Download input file from storage
            var tempInputPath = Path.Combine(Path.GetTempPath(), $"input_{job.Id}{Path.GetExtension(originalFileName)}");
            var tempOutputPath = Path.Combine(Path.GetTempPath(), $"output_{job.Id}.mp4");

            try
            {
                _logger.LogInformation("Downloading input file for job {JobId} to {Path}", jobId, tempInputPath);
                using (var inputStream = await fileStorage.DownloadAsync(job.InputFileKey!))
                using (var fileStream = File.Create(tempInputPath))
                {
                    await inputStream.CopyToAsync(fileStream, cancellationToken);
                }

                // 2. Build FFmpeg command
                var ffmpegArgs = BuildFfmpegArgs(
                    tempInputPath,
                    tempOutputPath,
                    quality,
                    preset,
                    codec,
                    maxWidth,
                    maxHeight,
                    bitrateKbps);

                _logger.LogInformation("Running FFmpeg for job {JobId} with args: {Args}", jobId, ffmpegArgs);

                // 3. Run FFmpeg command
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = ffmpegArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start FFmpeg process");
                }

                // Read output and error streams asynchronously
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                // Wait for process to complete with timeout (10 minutes)
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                var timeoutToken = timeoutCts.Token;

                try
                {
                    await process.WaitForExitAsync(timeoutToken);
                }
                catch (OperationCanceledException)
                {
                    process.Kill();
                    await process.WaitForExitAsync(); // Wait for kill to complete
                    throw new TimeoutException("FFmpeg process timed out after 10 minutes");
                }

                var errorOutput = await errorTask;
                var standardOutput = await outputTask;

                if (process.ExitCode != 0)
                {
                    _logger.LogError("FFmpeg failed for job {JobId}. Exit code: {ExitCode}, Error: {Error}",
                        jobId, process.ExitCode, errorOutput);
                    throw new InvalidOperationException($"FFmpeg failed with exit code {process.ExitCode}: {errorOutput}");
                }

                if (!File.Exists(tempOutputPath))
                {
                    throw new FileNotFoundException($"FFmpeg output file not found: {tempOutputPath}");
                }

                var outputFileInfo = new FileInfo(tempOutputPath);
                _logger.LogInformation("FFmpeg completed for job {JobId}. Output size: {Size} bytes", jobId, outputFileInfo.Length);

                // 4. Upload output file
                using var outputStream = File.OpenRead(tempOutputPath);
                var outputFileName = $"compressed_{Guid.NewGuid()}.mp4";
                var outputFileKey = await fileStorage.UploadAsync(
                    outputStream,
                    outputFileName,
                    "video/mp4",
                    $"video/compress/{job.UserId}",
                    cancellationToken);

                // 5. Generate presigned URL
                var downloadUrl = await fileStorage.GeneratePresignedUrlAsync(
                    outputFileKey,
                    TimeSpan.FromHours(24),
                    cancellationToken);

                // 6. Update job status
                job.Complete(outputFileKey, downloadUrl, DateTime.UtcNow.Add(TimeSpan.FromHours(24)));
                await jobRepository.UpdateAsync(job, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                // Record usage
                var usageRepository = unitOfWork.Repository<UsageRecord>();
                var processingTime = (int)(job.CompletedAt!.Value - job.StartedAt!.Value).TotalMilliseconds;
                var usageRecord = new UsageRecord(
                    job.UserId,
                    ToolType.VideoCompress,
                    fileSizeBytes: originalSize,
                    processingTimeMs: processingTime,
                    cost: 0m); // TODO: Calculate actual cost based on processing time and file size

                await usageRepository.AddAsync(usageRecord, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Video compression job {JobId} completed successfully. Output file: {OutputFileKey}, Compression ratio: {Ratio}%",
                    jobId, outputFileKey, originalSize > 0 ? (100.0 * outputFileInfo.Length / originalSize) : 0);
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing video compression job {JobId}", jobId);
            job.Fail(ex.Message);
            await jobRepository.UpdateAsync(job, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw; // Re-throw for Hangfire retry mechanism
        }
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

            // TODO: Implement LibreOffice/unoconv document conversion
            // 1. Download input file from storage
            // 2. Run LibreOffice/unoconv command
            // 3. Upload output PDF
            // 4. Generate presigned URL
            // 5. Update job status

            // Placeholder implementation
            await Task.Delay(2000, cancellationToken); // Simulate processing

            // For now, mark as failed with a message
            job.Fail("Document conversion requires LibreOffice/unoconv installation. Please configure LibreOffice in the worker environment.");

            await jobRepository.UpdateAsync(job, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document conversion job {JobId}", jobId);
            job.Fail(ex.Message);
            await jobRepository.UpdateAsync(job, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
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

            // TODO: Implement background removal
            // Option 1: Use remove.bg API (requires API key)
            // Option 2: Use image processing library (e.g., ImageSharp with ML.NET)
            // 1. Download input file from storage
            // 2. Process image (remove background)
            // 3. Upload output file
            // 4. Generate presigned URL
            // 5. Update job status

            // Placeholder implementation
            await Task.Delay(2000, cancellationToken); // Simulate processing

            // For now, mark as failed with a message
            job.Fail("Background removal requires AI service integration (remove.bg API) or image processing library. Please configure the service.");

            await jobRepository.UpdateAsync(job, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
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
    /// Builds FFmpeg command line arguments for video compression
    /// </summary>
    private static string BuildFfmpegArgs(
        string inputPath,
        string outputPath,
        int quality,
        string preset,
        string codec,
        int? maxWidth,
        int? maxHeight,
        int? bitrateKbps)
    {
        var args = new List<string>
        {
            "-i", inputPath, // Input file
            "-c:v", codec, // Video codec
            "-preset", preset, // Encoding preset
            "-y" // Overwrite output file
        };

        // Quality settings: Use CRF (Constant Rate Factor) if no bitrate specified
        if (bitrateKbps.HasValue)
        {
            args.AddRange(new[] { "-b:v", $"{bitrateKbps.Value}k" });
        }
        else
        {
            // CRF: 18-28 range, lower = better quality
            // Clamp quality to valid range
            var crf = Math.Clamp(quality, 18, 28);
            args.AddRange(new[] { "-crf", crf.ToString() });
        }

        // Scale filter if dimensions specified
        if (maxWidth.HasValue || maxHeight.HasValue)
        {
            var scaleFilter = "scale=";
            if (maxWidth.HasValue && maxHeight.HasValue)
            {
                scaleFilter += $"{maxWidth.Value}:{maxHeight.Value}";
            }
            else if (maxWidth.HasValue)
            {
                scaleFilter += $"{maxWidth.Value}:-1"; // Maintain aspect ratio
            }
            else
            {
                scaleFilter += $"-1:{maxHeight.Value}"; // Maintain aspect ratio
            }
            scaleFilter += ":flags=lanczos"; // High-quality scaling

            args.AddRange(new[] { "-vf", scaleFilter });
        }

        // Audio settings: Copy audio stream (no re-encoding)
        args.AddRange(new[] { "-c:a", "copy" });

        // Output file
        args.Add(outputPath);

        return string.Join(" ", args);
    }
}

