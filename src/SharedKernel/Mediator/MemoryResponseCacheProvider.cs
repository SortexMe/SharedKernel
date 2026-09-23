using SharedKernel.Abstractions.CQRS;
using SharedKernel.Options;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SharedKernel.Mediator;

/// <summary>
/// Thread-safe in-memory cache provider for caching CQRS query responses.
/// </summary>
public class MemoryResponseCacheProvider : IResponseCacheProvider
{
    private sealed record CacheItem(object? Value, DateTimeOffset? AbsoluteExpiration, TimeSpan? SlidingExpiration, DateTimeOffset LastAccess);

    private readonly ConcurrentDictionary<string, CacheItem> _cache = new();

    /// <summary>
    /// Gets the number of items currently in the cache.
    /// </summary>
    public int Count => _cache.Count;

    public Task<(bool Found, TResponse? Value)> TryGetAsync<TResponse>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            return Task.FromResult((false, default(TResponse)));

        if (_cache.TryGetValue(key, out var item))
        {
            var now = DateTimeOffset.UtcNow;

            // Check absolute expiration
            if (item.AbsoluteExpiration.HasValue && item.AbsoluteExpiration.Value <= now)
            {
                _cache.TryRemove(key, out _);
                return Task.FromResult((false, default(TResponse)));
            }

            // Check sliding expiration
            if (item.SlidingExpiration.HasValue && (now - item.LastAccess) > item.SlidingExpiration.Value)
            {
                _cache.TryRemove(key, out _);
                return Task.FromResult((false, default(TResponse)));
            }

            // Update sliding last access
            if (item.SlidingExpiration.HasValue)
            {
                _cache.TryUpdate(key, item with { LastAccess = now }, item);
            }

            return Task.FromResult((true, (TResponse?)item.Value));
        }

        return Task.FromResult((false, default(TResponse)));
    }

    public Task SetAsync<TResponse>(string key, TResponse value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            return Task.CompletedTask;

        var now = DateTimeOffset.UtcNow;
        DateTimeOffset? absolute = null;

        if (options?.AbsoluteExpiration.HasValue == true)
        {
            absolute = options.AbsoluteExpiration.Value;
        }
        else if (options?.AbsoluteExpirationRelativeToNow.HasValue == true)
        {
            absolute = now.Add(options.AbsoluteExpirationRelativeToNow.Value);
        }

        var item = new CacheItem(value, absolute, options?.SlidingExpiration, now);
        _cache[key] = item;

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(key))
        {
            _cache.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all entries from the cache.
    /// </summary>
    public void Clear() => _cache.Clear();
}
