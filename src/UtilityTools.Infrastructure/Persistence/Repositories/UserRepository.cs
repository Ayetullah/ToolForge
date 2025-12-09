using Microsoft.EntityFrameworkCore;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Domain.Entities;

namespace UtilityTools.Infrastructure.Persistence.Repositories;

/// <summary>
/// User repository implementation with custom queries
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(IApplicationDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.Email == email && u.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.RefreshToken == refreshToken && u.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.Email == email && u.DeletedAt == null)
            .AnyAsync(cancellationToken);
    }
    
    /// <summary>
    /// Get user with usage records included
    /// </summary>
    public async Task<User?> GetUserWithUsageAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        return await GetByIdWithIncludesAsync(
            userId,
            cancellationToken,
            u => u.UsageRecords);
    }
}

