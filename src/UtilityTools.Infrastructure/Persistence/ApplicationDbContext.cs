using Microsoft.EntityFrameworkCore;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Domain.Common;
using UtilityTools.Domain.Entities;

namespace UtilityTools.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext implementation
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();
    
    /// <summary>
    /// Get DbSet for generic repository pattern (no reflection needed)
    /// </summary>
    public DbSet<T> Set<T>() where T : class => base.Set<T>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure BaseEntity properties for all entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.CreatedAt))
                    .IsRequired();

                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.UpdatedAt))
                    .IsRequired(false);

                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.DeletedAt))
                    .IsRequired(false);
            }
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

