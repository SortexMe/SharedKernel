using SharedKernel.Options;
using System.Threading;
using System.Threading.Tasks;

namespace SharedKernel.Abstractions.CQRS;

/// <summary>
/// Defines an abstraction for caching CQRS query responses.
/// </summary>
public interface IResponseCacheProvider
{
    /// <summary>
    /// Attempts to retrieve a cached response for the specified key.
    /// </summary>
    /// <typeparam name="TResponse">The type of the cached response.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple indicating whether the item was found and the cached value.</returns>
    Task<(bool Found, TResponse? Value)> TryGetAsync<TResponse>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a response in the cache with the given key and expiration options.
    /// </summary>
    /// <typeparam name="TResponse">The type of response to cache.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="value">The response value.</param>
    /// <param name="options">Optional cache entry expiration options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync<TResponse>(string key, TResponse value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entry from the cache.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
