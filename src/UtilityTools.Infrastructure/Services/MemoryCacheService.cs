using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UtilityTools.Domain.Interfaces;

namespace UtilityTools.Infrastructure.Services;

/// <summary>
/// In-memory cache service implementation using IMemoryCache
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly TimeSpan _defaultExpiration;

    public MemoryCacheService(
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
        
        var expirationMinutes = _configuration.GetValue<int>("Cache:DefaultExpirationMinutes", 30);
        _defaultExpiration = TimeSpan.FromMinutes(expirationMinutes);
    }

    public T? Get<T>(string key) where T : class
    {
        try
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return value;
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from cache for key: {Key}", key);
            return null;
        }
    }

    public void Set<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
                Size = 1 // Each entry counts as 1 unit towards SizeLimit
            };

            _cache.Set(key, value, options);
            _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", key, expiration ?? _defaultExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in cache for key: {Key}", key);
        }
    }

    public void Remove(string key)
    {
        try
        {
            _cache.Remove(key);
            _logger.LogDebug("Removed cache entry for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entry for key: {Key}", key);
        }
    }

    public bool Exists(string key)
    {
        return _cache.TryGetValue(key, out _);
    }

    public void Clear()
    {
        // IMemoryCache doesn't have a built-in Clear method
        // This would require tracking all keys, which is not recommended
        // For production, consider using a different approach or implementing key tracking
        _logger.LogWarning("Clear() called on MemoryCache - this operation is not fully supported. Consider removing specific keys instead.");
    }
}

