using SharedKernel.Options;

namespace SharedKernel.Abstractions.CQRS;

/// <summary>
/// Marker interface for CQRS query requests that can have their responses cached.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface ICacheableRequest<out TResponse> : IRequest<TResponse>
{
    /// <summary>
    /// Gets the unique cache key for this request.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Optional expiration and cache configuration options for this entry.
    /// If null, default cache policy applies.
    /// </summary>
    CacheEntryOptions? CacheOptions => null;
}
