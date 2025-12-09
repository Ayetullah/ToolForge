using Microsoft.EntityFrameworkCore;
using UtilityTools.Domain.Entities;

namespace UtilityTools.Application.Common.Interfaces;

/// <summary>
/// Application DbContext interface for dependency injection
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Job> Jobs { get; }
    DbSet<UsageRecord> UsageRecords { get; }
    
    /// <summary>
    /// Get DbSet for generic repository pattern
    /// </summary>
    DbSet<T> Set<T>() where T : class;
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

