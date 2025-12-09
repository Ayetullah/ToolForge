using Microsoft.AspNetCore.Mvc;
using UtilityTools.Domain.Interfaces;

namespace UtilityTools.Api.Endpoints;

/// <summary>
/// File download endpoint for presigned URLs
/// </summary>
public static class FileDownloadEndpoint
{
    public static void MapFileDownloadEndpoint(this WebApplication app)
    {
        // ✅ Use catch-all route to handle fileKey with slashes
        app.MapGet("/api/files/download/{**fileKey}", async (
            string fileKey,
            [FromQuery] string? token,
            [FromQuery] long? expires,
            IFileStorage fileStorage,
            ILogger<Program> logger) =>
        {
            // ✅ Decode URL-encoded fileKey
            fileKey = Uri.UnescapeDataString(fileKey);
            
            // Validate token and expiration
            if (string.IsNullOrEmpty(token) || !expires.HasValue)
            {
                return Results.BadRequest(new { error = "Invalid download link" });
            }

            if (DateTime.FromBinary(expires.Value) < DateTime.UtcNow)
            {
                return Results.BadRequest(new { error = "Download link has expired" });
            }

            try
            {
                if (!await fileStorage.ExistsAsync(fileKey))
                {
                    return Results.NotFound(new { error = "File not found" });
                }

                var fileStream = await fileStorage.DownloadAsync(fileKey);
                var metadata = await fileStorage.GetMetadataAsync(fileKey);

                return Results.File(
                    fileStream,
                    contentType: metadata.ContentType,
                    fileDownloadName: metadata.FileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error downloading file: {FileKey}", fileKey);
                return Results.Problem("Error downloading file");
            }
        })
        .WithName("DownloadFile")
        .WithOpenApi();
    }
}

