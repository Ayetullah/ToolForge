# Database Setup & Seeding Guide

## Overview

The UtilityTools platform automatically applies database migrations and seeds initial data on application startup.

## Automatic Migration

When the application starts, it will:
1. **Apply pending migrations** - All EF Core migrations are automatically applied
2. **Seed initial data** - Roles and Admin user are created if they don't exist

This happens in **all environments** (Development, Staging, Production).

## Seed Data

### Roles

The following roles are automatically created:
- **Admin** - Administrator role with full access
- **User** - Standard user role
- **Premium** - Premium user role

### Admin User

An admin user is automatically created with:
- **Email**: `admin@utilitytools.com` (configurable)
- **Password**: `Admin@123456` (configurable)
- **Subscription Tier**: Admin
- **Email Verified**: Yes
- **Role**: Admin

## Configuration

Configure seed data in `appsettings.json`:

```json
{
  "SeedData": {
    "AdminEmail": "admin@utilitytools.com",
    "AdminPassword": "Admin@123456"
  }
}
```

### Environment Variables

You can also configure via environment variables:

```bash
SeedData__AdminEmail=admin@yourdomain.com
SeedData__AdminPassword=YourSecurePassword123!
```

## Production Considerations

### Security

⚠️ **Important**: Change the default admin password in production!

1. Use strong passwords
2. Store credentials in secure configuration (Azure Key Vault, AWS Secrets Manager, etc.)
3. Consider disabling automatic admin creation in production

### Migration Strategy

For production deployments:

1. **Option 1: Automatic (Current)**
   - Migrations run automatically on startup
   - Simple but may cause issues if multiple instances start simultaneously

2. **Option 2: Manual (Recommended for Production)**
   - Run migrations manually before deployment
   - Use CI/CD pipeline to run migrations
   - Prevents race conditions

### Disabling Auto-Seed

To disable automatic seeding, modify `Program.cs`:

```csharp
// Comment out or conditionally run seeding
if (app.Environment.IsDevelopment())
{
    var seeder = services.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}
```

## Manual Migration

If you need to run migrations manually:

```bash
# Create a new migration
dotnet ef migrations add MigrationName \
  --project src/UtilityTools.Infrastructure \
  --startup-project src/UtilityTools.Api

# Apply migrations
dotnet ef database update \
  --project src/UtilityTools.Infrastructure \
  --startup-project src/UtilityTools.Api
```

## Seed Data Customization

To add custom seed data, modify `DatabaseSeeder.cs`:

```csharp
public async Task SeedAsync(CancellationToken cancellationToken = default)
{
    await SeedRolesAsync(cancellationToken);
    await SeedAdminUserAsync(cancellationToken);
    
    // Add your custom seeding here
    await SeedCustomDataAsync(cancellationToken);
}
```

## Troubleshooting

### Migration Fails

If migrations fail on startup:
1. Check database connection string
2. Verify database exists
3. Check user permissions
4. Review application logs

### Seed Data Not Created

If seed data is not created:
1. Check if data already exists (seeder is idempotent)
2. Review application logs for errors
3. Verify configuration values
4. Check database permissions

### Admin User Already Exists

The seeder is **idempotent** - it won't create duplicate data. If admin user already exists, it will be skipped.

## Logs

Migration and seeding activities are logged:

```
[INFO] Applying database migrations...
[INFO] Database migrations applied successfully
[INFO] Seeding database...
[INFO] Seeding role: Admin
[INFO] Seeding role: User
[INFO] Seeding role: Premium
[INFO] Admin user created: admin@utilitytools.com
[INFO] Database seeding completed successfully
```

## Best Practices

1. **Always backup database** before running migrations in production
2. **Test migrations** in staging environment first
3. **Use transaction-safe migrations** when possible
4. **Monitor application startup** for migration/seeding errors
5. **Keep seed data minimal** - only essential initial data
6. **Use configuration** for environment-specific seed data

