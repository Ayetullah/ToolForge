using Hangfire.Dashboard;

namespace UtilityTools.Api.Filters;

/// <summary>
/// Authorization filter for Hangfire Dashboard
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly IWebHostEnvironment _environment;

    public HangfireAuthorizationFilter(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public bool Authorize(DashboardContext context)
    {
        // In development, allow all requests
        if (_environment.IsDevelopment())
        {
            return true;
        }

        // In production, check if user is authenticated and is admin
        var httpContext = context.GetHttpContext();
        
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        // Check if user has admin role
        var subscriptionTier = httpContext.User.FindFirst("subscription_tier")?.Value;
        return subscriptionTier == "Admin" || subscriptionTier == "admin";
    }
}

