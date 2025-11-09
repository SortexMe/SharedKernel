using SharedKernel.Options;
using System.Threading;
using System.Threading.Tasks;

namespace SharedKernel.Abstractions.Services;

/// <summary>
/// Defines an abstraction for a cache service capable of storing, retrieving, and removing cached items.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Retrieves an item from the cache by its key.
    /// </summary>
    /// <typeparam name="T">The expected type of the cached item.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>The cached item if found; otherwise, null.</returns>
    // Development Note:
    // Implementations should handle deserialization and type safety.
    Task<T?> GetItem<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores an item in the cache with the specified key and optional cache entry options.
    /// </summary>
    /// <typeparam name="T">The type of the item to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="item">The item to cache.</param>
    /// <param name="cacheEntryOptions">Optional caching options like expiration.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    // Development Note:
    // CacheEntryOptions allows fine control over cache duration, priority, etc.
    Task SetItem<T>(string key, T item, CacheEntryOptions? cacheEntryOptions = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to remove an item from the cache by its key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    // Development Note:
    // Should not throw if the key does not exist; implementations can optimize for no-op.
    Task TryRemoveItem(string key, CancellationToken cancellationToken = default);
}
