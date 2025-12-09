using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using UtilityTools.Application.Common.Exceptions;

namespace UtilityTools.Api.Middleware;

/// <summary>
/// Global exception handler middleware to ensure all errors return JSON
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next, 
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "An unhandled exception occurred. Path: {Path}, Method: {Method}",
                context.Request.Path, context.Request.Method);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var (statusCode, error, errors) = exception switch
        {
            ValidationException ve => (
                HttpStatusCode.BadRequest,
                "Validation failed",
                ve.Errors
            ),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "Unauthorized access",
                null
            ),
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                "Resource not found",
                null
            ),
            ArgumentNullException => (
                HttpStatusCode.BadRequest,
                "Required parameter is missing",
                null
            ),
            ArgumentException => (
                HttpStatusCode.BadRequest,
                "Invalid argument",
                null
            ),
            InvalidOperationException => (
                HttpStatusCode.BadRequest,
                "Invalid operation",
                null
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "An error occurred while processing your request",
                null
            )
        };
        
        context.Response.StatusCode = (int)statusCode;
        
        var response = new
        {
            error,
            message = exception.Message,
            errors,
            requestId = context.TraceIdentifier,
            timestamp = DateTime.UtcNow,
            stackTrace = _environment.IsDevelopment() 
                ? exception.StackTrace 
                : null
        };
        
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });
        
        await context.Response.WriteAsync(json);
    }
}

