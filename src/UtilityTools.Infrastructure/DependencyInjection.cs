using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Application.Common.Options;
using UtilityTools.Domain.Interfaces;
using UtilityTools.Infrastructure.FileStorage;
using UtilityTools.Infrastructure.Persistence;
using UtilityTools.Infrastructure.Persistence.Repositories;

namespace UtilityTools.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

          // Repository Pattern
          services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
          services.AddScoped<IUserRepository, UserRepository>();
          services.AddScoped<Application.Common.Interfaces.IUnitOfWork, UnitOfWork>();

        // Configure Options Pattern
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<AiSettings>(configuration.GetSection(AiSettings.SectionName));
        services.Configure<StripeSettings>(configuration.GetSection(StripeSettings.SectionName));
        services.Configure<FileStorageSettings>(configuration.GetSection(FileStorageSettings.SectionName));
        services.Configure<FileLimitsSettings>(configuration.GetSection(FileLimitsSettings.SectionName));
        services.Configure<CacheSettings>(configuration.GetSection(CacheSettings.SectionName));

        // Memory Cache
        // ✅ Note: SizeLimit is NOT set here because AspNetCoreRateLimit doesn't specify Size on entries
        // MemoryCacheService will set Size = 1 for its own entries (see MemoryCacheService.cs line 58)
        var cacheSettings = configuration.GetSection(CacheSettings.SectionName).Get<CacheSettings>() 
            ?? new CacheSettings { SizeLimit = 1024, DefaultExpirationMinutes = 30 };
        services.AddMemoryCache(); // ✅ No SizeLimit - allows AspNetCoreRateLimit to work
        services.AddScoped<ICacheService, Services.MemoryCacheService>();

        // Services
        // ✅ Configure HttpClient with Polly policies for AI service
        services.AddHttpClient<IAiService, Services.AiService>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());
        services.AddScoped<IAiService, Services.AiService>();
        services.AddScoped<ICurrentUserService, Services.CurrentUserService>();
        services.AddScoped<IJwtTokenService, Services.JwtTokenService>();
        services.AddScoped<Application.Common.Interfaces.ISubscriptionService, Application.Common.Services.SubscriptionService>();
        services.AddHttpContextAccessor();
        
        // File Storage - Only Local storage is supported
        services.AddScoped<IFileStorage, LocalFileStorage>();
        
        // Payment Service (Stripe)
        var stripeSettings = configuration.GetSection(StripeSettings.SectionName).Get<StripeSettings>();
        if (stripeSettings != null && !string.IsNullOrEmpty(stripeSettings.SecretKey))
        {
            services.AddScoped<IPaymentService, Services.StripePaymentService>();
        }

        // Email Service
        var emailProvider = configuration["Email:Provider"] ?? "SMTP";
        if (emailProvider.Equals("SMTP", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailService, Services.SmtpEmailService>();
        }
        // TODO: Add SendGrid and Mailgun adapters

        // Database Seeder
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
    
    /// <summary>
    /// Get retry policy for HTTP clients
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Logging will be handled by the service
                });
    }
    
    /// <summary>
    /// Get circuit breaker policy for HTTP clients
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }

    public static IServiceCollection AddHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(connectionString, new PostgreSqlStorageOptions
            {
                SchemaName = "hangfire"
            }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount * 5;
            options.Queues = new[] { "default", "critical", "background" };
        });

        return services;
    }
}

