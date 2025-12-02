using System.Text.Json;
using BankingSystem.Application.Interfaces;
using BankingSystem.Infrastructure.Monitoring;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace BankingSystem.Infrastructure.Services;

/// <summary>
/// Service for caching data using Redis (IDistributedCache)
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<CacheService> _logger;
    private readonly BankingSystemMetrics? _metrics;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public CacheService(
        IDistributedCache distributedCache,
        ILogger<CacheService> logger,
        BankingSystemMetrics? metrics = null)
    {
        _distributedCache = distributedCache;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cachedData = await _distributedCache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(cachedData))
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                _metrics?.RecordCacheMiss(GetCacheType(key));
                return null;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            _metrics?.RecordCacheHit(GetCacheType(key));
            return JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache for key: {Key}", key);
            _metrics?.RecordCacheMiss(GetCacheType(key));
            return null; // Return null on cache error, don't throw
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var serializedData = JsonSerializer.Serialize(value, _jsonOptions);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromMinutes(10)
            };

            await _distributedCache.SetStringAsync(key, serializedData, options, cancellationToken);

            _logger.LogDebug("Cache set for key: {Key}, Expiration: {Expiration}", key, options.AbsoluteExpirationRelativeToNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key: {Key}", key);
            // Don't throw - caching failures should not break the application
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Cache removed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for key: {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // Note: Redis pattern matching would require StackExchange.Redis directly
        // This is a simplified version - for production, use StackExchange.Redis
        _logger.LogWarning("RemoveByPrefix not fully implemented - requires direct Redis access");
        await Task.CompletedTask;
    }

    private static string GetCacheType(string key)
    {
        // Extract cache type from key (e.g., "account:123" -> "account")
        var parts = key.Split(':');
        return parts.Length > 0 ? parts[0] : "unknown";
    }
}
