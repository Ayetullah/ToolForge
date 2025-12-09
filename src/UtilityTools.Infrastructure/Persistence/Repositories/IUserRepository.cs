using UtilityTools.Domain.Entities;
using UtilityTools.Domain.Interfaces;

namespace UtilityTools.Infrastructure.Persistence.Repositories;

/// <summary>
/// User-specific repository interface for custom queries
/// </summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetUserWithUsageAsync(Guid userId, CancellationToken cancellationToken = default);
}

