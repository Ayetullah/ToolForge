using UtilityTools.Domain.Common;
using UtilityTools.Domain.Interfaces;

namespace UtilityTools.Application.Common.Interfaces;

/// <summary>
/// Unit of Work pattern interface for transaction management
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IRepository<T> Repository<T>() where T : BaseEntity;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

