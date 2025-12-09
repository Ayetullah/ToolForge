using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Domain.Entities;
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

            // TODO: Implement FFmpeg video compression
            // 1. Download input file from storage
            // 2. Run FFmpeg command with parameters
            // 3. Upload output file
            // 4. Generate presigned URL
            // 5. Update job status

            // Placeholder implementation
            await Task.Delay(2000, cancellationToken); // Simulate processing

            // For now, mark as failed with a message
            job.Fail("Video compression requires FFmpeg installation. Please configure FFmpeg in the worker environment.");

            await jobRepository.UpdateAsync(job, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
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
}

