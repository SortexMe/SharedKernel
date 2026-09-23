using Microsoft.Extensions.Logging;
using SharedKernel.Abstractions.CQRS;
using System.Threading;
using System.Threading.Tasks;

namespace SharedKernel.Mediator.Behaviors;

/// <summary>
/// Pipeline behavior that caches query responses for requests implementing <see cref="ICacheableRequest{TResponse}"/>.
/// Requests not implementing <see cref="ICacheableRequest{TResponse}"/> bypass caching and pass directly to the inner handler.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly IResponseCacheProvider? _cacheProvider;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>>? _logger;

    public CachingBehavior(
        IResponseCacheProvider? cacheProvider = null,
        ILogger<CachingBehavior<TRequest, TResponse>>? logger = null)
    {
        _cacheProvider = cacheProvider;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ICacheableRequest<TResponse> cacheableRequest || _cacheProvider is null)
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        var key = cacheableRequest.CacheKey;
        if (string.IsNullOrEmpty(key))
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        var (found, cachedValue) = await _cacheProvider.TryGetAsync<TResponse>(key, cancellationToken).ConfigureAwait(false);
        if (found)
        {
            _logger?.LogDebug("Cache hit for key {CacheKey}", key);
            return cachedValue!;
        }

        _logger?.LogDebug("Cache miss for key {CacheKey}", key);
        var response = await next(cancellationToken).ConfigureAwait(false);

        if (response is not null)
        {
            await _cacheProvider.SetAsync(key, response, cacheableRequest.CacheOptions, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }
}
