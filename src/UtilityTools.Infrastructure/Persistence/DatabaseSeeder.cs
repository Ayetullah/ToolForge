using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UtilityTools.Domain.Entities;
using UtilityTools.Domain.Enums;

namespace UtilityTools.Infrastructure.Persistence;

/// <summary>
/// Database seeder for initial data
/// </summary>
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Seed Roles
            await SeedRolesAsync(cancellationToken);

            // Seed Admin User
            await SeedAdminUserAsync(cancellationToken);

            // Seed Test Users
            await SeedTestUsersAsync(cancellationToken);

            // Seed data for all users (UsageRecords and Jobs)
            await SeedUserDataAsync(cancellationToken);

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database");
            throw;
        }
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        var roles = new[]
        {
            new Role("Admin", "Administrator role with full access"),
            new Role("User", "Standard user role"),
            new Role("Premium", "Premium user role")
        };

        foreach (var role in roles)
        {
            var existingRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == role.Name, cancellationToken);

            if (existingRole == null)
            {
                _context.Roles.Add(role);
                _logger.LogInformation("Seeding role: {RoleName}", role.Name);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        var adminEmail = _configuration["SeedData:AdminEmail"] ?? "admin@utilitytools.com";
        var adminPassword = _configuration["SeedData:AdminPassword"] ?? "Admin@123456";

        var existingAdmin = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == adminEmail, cancellationToken);

        if (existingAdmin != null)
        {
            _logger.LogInformation("Admin user already exists: {Email}", adminEmail);
            return;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
        var adminUser = new User(adminEmail, passwordHash, "Admin", "User");

        // Set admin subscription tier
        adminUser.UpdateSubscription(SubscriptionTier.Admin, null, null, null);

        // Verify email for admin
        adminUser.VerifyEmail();

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync(cancellationToken);

        // Assign Admin role
        var adminRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == "Admin", cancellationToken);

        if (adminRole != null)
        {
            adminUser.Roles.Add(adminRole);
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Admin user created: {Email}", adminEmail);
    }

    private async Task SeedTestUsersAsync(CancellationToken cancellationToken)
    {
        var testUsers = new[]
        {
            new { Email = "user@utilitytools.com", Password = "User@123456", FirstName = "John", LastName = "Doe", Tier = SubscriptionTier.Free },
            new { Email = "premium@utilitytools.com", Password = "Premium@123456", FirstName = "Jane", LastName = "Smith", Tier = SubscriptionTier.Pro },
            new { Email = "basic@utilitytools.com", Password = "Basic@123456", FirstName = "Bob", LastName = "Johnson", Tier = SubscriptionTier.Basic }
        };

        var userRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == "User", cancellationToken);

        foreach (var testUser in testUsers)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == testUser.Email, cancellationToken);

            if (existingUser != null)
            {
                _logger.LogInformation("Test user already exists: {Email}", testUser.Email);
                continue;
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(testUser.Password);
            var user = new User(testUser.Email, passwordHash, testUser.FirstName, testUser.LastName);

            // Set subscription tier
            user.UpdateSubscription(testUser.Tier, null, null, null);

            // Verify email
            user.VerifyEmail();

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            // Assign User role
            if (userRole != null)
            {
                user.Roles.Add(userRole);
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation("Test user created: {Email} with tier {Tier}", testUser.Email, testUser.Tier);
        }
    }

    private async Task SeedUserDataAsync(CancellationToken cancellationToken)
    {
        // First, clean up any invalid ToolType values (e.g., VideoCompress which was removed)
        // Use raw SQL to avoid EF Core enum conversion errors when reading invalid enum values
        var validToolTypes = Enum.GetValues<ToolType>().Select(e => e.ToString()).ToList();
        
        // Build SQL with proper quoting for PostgreSQL string literals
        var validToolTypesSql = string.Join(",", validToolTypes.Select(t => $"'{t.Replace("'", "''")}'"));
        
        // Delete invalid UsageRecords using raw SQL
        // Note: validToolTypes comes from enum values, not user input, so this is safe
#pragma warning disable EF1000 // SQL injection warning - safe because values come from enum, not user input
        var deletedUsageRecords = await _context.Database.ExecuteSqlRawAsync(
            $@"DELETE FROM ""UsageRecords"" WHERE ""ToolType"" NOT IN ({validToolTypesSql})",
            cancellationToken);
#pragma warning restore EF1000
        
        if (deletedUsageRecords > 0)
        {
            _logger.LogWarning("Deleted {Count} UsageRecords with invalid ToolType values using raw SQL.", deletedUsageRecords);
        }
        
        // Delete invalid Jobs using raw SQL
#pragma warning disable EF1000 // SQL injection warning - safe because values come from enum, not user input
        var deletedJobs = await _context.Database.ExecuteSqlRawAsync(
            $@"DELETE FROM ""Jobs"" WHERE ""ToolType"" NOT IN ({validToolTypesSql})",
            cancellationToken);
#pragma warning restore EF1000
        
        if (deletedJobs > 0)
        {
            _logger.LogWarning("Deleted {Count} Jobs with invalid ToolType values using raw SQL.", deletedJobs);
        }
        
        var allUsers = await _context.Users
            .Include(u => u.UsageRecords)
            .Include(u => u.Jobs)
            .ToListAsync(cancellationToken);

        var random = new Random();

        foreach (var user in allUsers)
        {
            // Filter out invalid ToolType records from navigation properties
            var validUsageRecords = user.UsageRecords.Where(ur => validToolTypes.Contains(ur.ToolType.ToString())).ToList();
            var validJobs = user.Jobs.Where(j => validToolTypes.Contains(j.ToolType.ToString())).ToList();
            
            // Skip if user already has valid seed data
            if (validUsageRecords.Any() || validJobs.Any())
            {
                _logger.LogInformation("User {Email} already has data, skipping", user.Email);
                continue;
            }

            // Create one UsageRecord for each user
            var toolTypes = Enum.GetValues<ToolType>();
            var randomToolType = toolTypes[random.Next(toolTypes.Length)];

            var usageRecord = new UsageRecord(
                userId: user.Id,
                toolType: randomToolType,
                fileSizeBytes: random.Next(10000, 1000000), // 10KB to 1MB
                processingTimeMs: random.Next(100, 5000), // 100ms to 5s
                tokensUsed: randomToolType == ToolType.AiSummarize ? random.Next(100, 2000) : 0,
                cost: randomToolType == ToolType.AiSummarize ? (decimal)(random.NextDouble() * 0.1) : 0
            );

            usageRecord.AddMetadata("seed", true);
            usageRecord.AddMetadata("tool_version", "1.0");

            _context.UsageRecords.Add(usageRecord);
            _logger.LogInformation("Created UsageRecord for user {Email} with tool {ToolType}", user.Email, randomToolType);

            // Create one Job for each user
            var jobToolType = toolTypes[random.Next(toolTypes.Length)];
            var job = new Job(
                userId: user.Id,
                toolType: jobToolType,
                inputFileKey: $"seed/input/{user.Id}/{Guid.NewGuid()}.tmp",
                parameters: new Dictionary<string, object>
                {
                    { "seed", true },
                    { "quality", "high" }
                }
            );

            // Randomly set job status
            var jobStatus = (JobStatus)random.Next(0, 4); // 0=Pending, 1=Processing, 2=Completed, 3=Failed
            
            if (jobStatus == JobStatus.Processing)
            {
                job.Start();
                job.UpdateProgress(random.Next(10, 90));
            }
            else if (jobStatus == JobStatus.Completed)
            {
                job.Start();
                job.Complete(
                    outputFileKey: $"seed/output/{user.Id}/{Guid.NewGuid()}.tmp",
                    signedDownloadUrl: $"https://example.com/download/{Guid.NewGuid()}",
                    urlExpiresAt: DateTime.UtcNow.AddDays(7)
                );
            }
            else if (jobStatus == JobStatus.Failed)
            {
                job.Start();
                job.Fail("Sample error message for seed data");
            }

            _context.Jobs.Add(job);
            _logger.LogInformation("Created Job for user {Email} with tool {ToolType} and status {Status}", 
                user.Email, jobToolType, jobStatus);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Seeded data for {Count} users", allUsers.Count);
    }
}

