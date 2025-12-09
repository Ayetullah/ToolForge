# Hangfire Background Jobs Setup

## ✅ Completed

### Configuration
- ✅ Hangfire PostgreSQL storage configured
- ✅ Hangfire Dashboard enabled (Development only)
- ✅ Job Processors implemented in Application layer
- ✅ Automatic retry policies (3 attempts with delays)
- ✅ Multiple queues support (default, critical, background)

### Job Processors
- ✅ **Video Compression** - `ProcessVideoCompression`
  - Placeholder implementation
  - Requires FFmpeg installation
  - Automatic retry on failure

- ✅ **Document to PDF** - `ProcessDocumentConversion`
  - Placeholder implementation
  - Requires LibreOffice/unoconv installation
  - Automatic retry on failure

- ✅ **Background Removal** - `ProcessBackgroundRemoval`
  - Placeholder implementation
  - Requires AI service (remove.bg API) or image processing library
  - Automatic retry on failure

### Endpoints
- ✅ `GET /api/jobs/{jobId}/status` - Get job status
- ✅ `GET /api/tools/pdf/status/{jobId}` - Get PDF job status
- ✅ Hangfire Dashboard: `/hangfire` (Development only)

## 🔧 Configuration

### Database Schema
Hangfire uses PostgreSQL with schema name `hangfire`. The schema is created automatically on first run.

### Worker Configuration
```csharp
services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 5;
    options.Queues = new[] { "default", "critical", "background" };
});
```

### Retry Policy
All job processors use `[AutomaticRetry]` attribute:
- **Attempts**: 3
- **Delays**: 60s, 120s, 300s

## 📝 Usage

### Enqueueing Jobs
```csharp
BackgroundJob.Enqueue<JobProcessors>(
    x => x.ProcessVideoCompression(job.Id));
```

### Job Status
Jobs are tracked in the `Jobs` table with status:
- `Pending` - Job created, waiting to be processed
- `Processing` - Job is currently being processed
- `Completed` - Job completed successfully
- `Failed` - Job failed (after retries)

### Job Progress
Jobs support progress tracking via `UpdateProgress(int percentage)` method.

## 🚀 Next Steps

1. **FFmpeg Integration** for video compression
   - Install FFmpeg in worker environment
   - Implement actual compression logic
   - Handle different codecs and formats

2. **LibreOffice/unoconv Integration** for document conversion
   - Install LibreOffice in worker environment
   - Implement document to PDF conversion
   - Support multiple document formats

3. **Background Removal Service**
   - Integrate remove.bg API or
   - Implement image processing with ML.NET/ImageSharp

4. **Job Monitoring**
   - Set up alerts for failed jobs
   - Monitor job queue lengths
   - Track processing times

5. **Job Cleanup**
   - Implement TTL for completed jobs
   - Archive old jobs
   - Clean up temporary files

## 🔍 Monitoring

### Hangfire Dashboard
Access at `/hangfire` in Development mode to:
- View job status
- Retry failed jobs
- Monitor queues
- View job history

### Database Queries
```sql
-- Check pending jobs
SELECT * FROM hangfire.job WHERE StateName = 'Enqueued';

-- Check failed jobs
SELECT * FROM hangfire.job WHERE StateName = 'Failed';

-- Check job statistics
SELECT StateName, COUNT(*) FROM hangfire.job GROUP BY StateName;
```

## 📚 References

- [Hangfire Documentation](https://docs.hangfire.io/)
- [Hangfire PostgreSQL Storage](https://github.com/frankhommers/Hangfire.PostgreSql)
- [Job Processors Implementation](./src/UtilityTools.Application/Jobs/JobProcessors.cs)

