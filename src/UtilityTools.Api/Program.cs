using System.Text;
using AspNetCoreRateLimit;
using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using UtilityTools.Api.Endpoints;
using UtilityTools.Api.Middleware;
using UtilityTools.Infrastructure;
using UtilityTools.Infrastructure.Persistence;
using UtilityTools.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = false;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new HeaderApiVersionReader("X-Version"),
        new QueryStringApiVersionReader("version"));
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UtilityTools API",
        Version = "v1.0",
        Description = "Production-ready SaaS utility tools platform",
        Contact = new OpenApiContact
        {
            Name = "UtilityTools Support",
            Email = "support@utilitytools.com"
        }
    });
    
    // JWT Authentication in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    
});

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// HTTP Context Accessor (for user context)
builder.Services.AddHttpContextAccessor();

// JWT Authentication - using Options pattern
builder.Services.Configure<UtilityTools.Application.Common.Options.JwtSettings>(
    builder.Configuration.GetSection(UtilityTools.Application.Common.Options.JwtSettings.SectionName));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection(UtilityTools.Application.Common.Options.JwtSettings.SectionName);
    var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            var subscriptionTier = context.User.FindFirst("subscription_tier")?.Value;
            return subscriptionTier == "Admin" || subscriptionTier == "admin";
        });
    });
});

// CORS - Environment-specific configuration
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // ✅ Development: Allow localhost origins for frontend
        options.AddPolicy("AllowAll", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:3000",
                    "http://localhost:3001",
                    "https://localhost:3000",
                    "https://localhost:3001")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // ✅ Required for cookies/auth headers
        });
    }
    else
    {
        // Production: Restrictive CORS policy
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "https://yourdomain.com" };
        
        options.AddPolicy("Production", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                  .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "X-Version")
                  .AllowCredentials();
        });
    }
});

// ✅ Health Checks with detailed checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty, 
        name: "postgresql",
        tags: new[] { "db", "sql", "postgresql" })
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), 
        tags: new[] { "self" });

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(UtilityTools.Application.AssemblyReference).Assembly));

// Validation Behavior
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UtilityTools.Application.Common.Behaviours.ValidationBehaviour<,>));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(UtilityTools.Application.AssemblyReference).Assembly);

// AutoMapper
builder.Services.AddAutoMapper(typeof(UtilityTools.Application.Common.Mappings.MappingProfile));

// ✅ Response caching for GET endpoints
builder.Services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 64 * 1024 * 1024; // 64MB
    options.UseCaseSensitivePaths = false;
});

// ✅ Rate Limiting
// ✅ Note: MemoryCache is already registered in DependencyInjection.cs (without SizeLimit)
// AspNetCoreRateLimit will use the same IMemoryCache instance
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Hangfire
builder.Services.AddHangfire(builder.Configuration);

// Job Processors
builder.Services.AddScoped<UtilityTools.Application.Jobs.JobProcessors>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

// ✅ CORS must be VERY FIRST (before exception handler) to handle preflight OPTIONS requests
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("Production");
}

// Global exception handler - must be early in pipeline
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// ✅ Security headers - must be early in pipeline
app.UseMiddleware<SecurityHeadersMiddleware>();

// ✅ Rate limiting - must be before authentication
app.UseIpRateLimiting();

// Only use HTTPS redirect in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// ✅ Response caching - must be after authentication/authorization
app.UseResponseCaching();

// ✅ Health checks with detailed responses
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("self"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            timestamp = DateTime.UtcNow
        });
        await context.Response.WriteAsync(result);
    }
});

// File download endpoint
app.MapFileDownloadEndpoint();

// Controllers
app.MapControllers();

// Run migrations and seed data on startup (all environments)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Apply migrations
        logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");

        // Seed initial data
        var seeder = services.GetRequiredService<DatabaseSeeder>();
        logger.LogInformation("Seeding database...");
        await seeder.SeedAsync();
        logger.LogInformation("Database seeding completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or seeding the database");
        // Don't throw - allow app to start even if migration/seeding fails
        // In production, you might want to fail fast
    }
}

app.Run();
