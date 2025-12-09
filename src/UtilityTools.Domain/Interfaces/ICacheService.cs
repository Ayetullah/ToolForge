namespace UtilityTools.Domain.Interfaces;

/// <summary>
/// Abstraction for caching operations
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get a value from cache
    /// </summary>
    T? Get<T>(string key) where T : class;

    /// <summary>
    /// Set a value in cache with default expiration
    /// </summary>
    void Set<T>(string key, T value, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// Remove a value from cache
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// Check if a key exists in cache
    /// </summary>
    bool Exists(string key);

    /// <summary>
    /// Clear all cache entries
    /// </summary>
    void Clear();
}

