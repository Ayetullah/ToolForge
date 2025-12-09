namespace UtilityTools.Api.Middleware;

/// <summary>
/// Middleware to add security headers to all responses
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // ✅ Skip security headers for OPTIONS (preflight) requests - CORS handles these
        if (context.Request.Method == "OPTIONS")
        {
            await _next(context);
            return;
        }
        
        // ✅ Security Headers
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        
        // Content Security Policy (CSP) - Relaxed for development
        if (context.Request.Host.Host == "localhost" || context.Request.Host.Host == "127.0.0.1")
        {
            // Development: More permissive CSP
            var csp = "default-src 'self' 'unsafe-inline' 'unsafe-eval' http://localhost:* https://localhost:*; " +
                      "script-src 'self' 'unsafe-inline' 'unsafe-eval' http://localhost:* https://localhost:*; " +
                      "style-src 'self' 'unsafe-inline' http://localhost:* https://localhost:*; " +
                      "img-src 'self' data: https: http:; " +
                      "font-src 'self' data:; " +
                      "connect-src 'self' http://localhost:* https://localhost:*; " +
                      "frame-ancestors 'none';";
            context.Response.Headers.Append("Content-Security-Policy", csp);
        }
        else
        {
            // Production: Strict CSP
            var csp = "default-src 'self'; " +
                      "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " + // unsafe-inline/eval for Swagger
                      "style-src 'self' 'unsafe-inline'; " +
                      "img-src 'self' data: https:; " +
                      "font-src 'self' data:; " +
                      "connect-src 'self'; " +
                      "frame-ancestors 'none';";
            context.Response.Headers.Append("Content-Security-Policy", csp);
        }
        
        // Permissions Policy (formerly Feature-Policy)
        context.Response.Headers.Append("Permissions-Policy", 
            "geolocation=(), microphone=(), camera=()");
        
        // HSTS - Only in production with HTTPS
        if (!context.Request.IsHttps || context.Request.Host.Host == "localhost")
        {
            // Skip HSTS for localhost/HTTP
        }
        else
        {
            context.Response.Headers.Append("Strict-Transport-Security", 
                "max-age=31536000; includeSubDomains; preload");
        }
        
        // Remove server header for security
        context.Response.Headers.Remove("Server");
        
        await _next(context);
    }
}

