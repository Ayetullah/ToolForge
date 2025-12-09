using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using UtilityTools.Application.Common.Interfaces;
using UtilityTools.Domain.Common;
using UtilityTools.Domain.Interfaces;

namespace UtilityTools.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic repository implementation using EF Core
/// </summary>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(IApplicationDbContext context)
    {
        _context = (ApplicationDbContext)(context ?? throw new ArgumentNullException(nameof(context)));
        _dbSet = _context.Set<T>(); // ✅ No reflection needed
    }

    /// <summary>
    /// Get IQueryable for advanced queries with Includes
    /// </summary>
    public IQueryable<T> GetQueryable()
    {
        return _dbSet.Where(e => e.DeletedAt == null);
    }
    
    /// <summary>
    /// Get entity by ID with related entities included
    /// </summary>
    public async Task<T?> GetByIdWithIncludesAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object>>[] includes)
    {
        var query = _dbSet.Where(e => e.Id == id && e.DeletedAt == null);
        
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.DeletedAt == null)
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.DeletedAt == null)
            .FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        // Soft delete
        entity.MarkAsDeleted();
        return Task.CompletedTask;
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.DeletedAt == null)
            .AnyAsync(predicate, cancellationToken);
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(e => e.DeletedAt == null);
        
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.CountAsync(cancellationToken);
    }
}

