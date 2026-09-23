using SharedKernel.Abstractions.CQRS;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace SharedKernel.Mediator;

/// <summary>
/// Caches the generated handler wrappers the <see cref="Mediator"/> dispatches through, keyed by
/// (request type, response type).
/// </summary>
/// <remarks>
/// <para>
/// Building a wrapper costs a <c>MakeGenericType</c> plus an <c>Activator.CreateInstance</c> — roughly as much
/// as an entire warm <c>Send</c> — so it must not happen per request. Wrappers are stateless and depend only on
/// the two types, which makes them safe to share.
/// </para>
/// <para>
/// This is registered as a singleton by <see cref="ServiceRegistrar.AddRequiredServices"/>, so the cache lives
/// as long as the container rather than as long as the process. That keeps the speed of a shared cache while
/// letting the entries be collected when the container is disposed — which matters for plugin hosts that unload
/// an <see cref="System.Runtime.Loader.AssemblyLoadContext"/>, for multi-container applications, and for tests
/// that must not leak state into one another.
/// </para>
/// <para>
/// The response type is part of the key because <see cref="IRequest{TResponse}"/> is covariant:
/// <c>Send&lt;object&gt;(query)</c> is legal for a <c>query : IRequest&lt;string&gt;</c>, and keying on the
/// request type alone would let that call cache a wrapper that every later, correctly-typed call then fails
/// to cast.
/// </para>
/// </remarks>
public sealed class RequestHandlerWrapperCache
{
    /// <summary>
    /// Response-type key used for requests that implement the non-generic <see cref="IRequest"/>.
    /// </summary>
    internal static readonly Type VoidResponse = typeof(void);

    private readonly ConcurrentDictionary<(Type Request, Type Response), RequestHandlerBase> _wrappers = new();
    private readonly ConcurrentDictionary<Type, RequestHandlerBase> _dynamicWrappers = new();

    /// <summary>
    /// Gets the number of distinct (request, response) pairs currently cached.
    /// </summary>
    public int Count => _wrappers.Count;

    /// <summary>
    /// Returns the cached wrapper for the given pair, creating it on first use.
    /// </summary>
    /// <param name="requestType">The concrete request type.</param>
    /// <param name="responseType">The response type, or <see cref="VoidResponse"/> for a void request.</param>
    /// <returns>The wrapper that dispatches this request type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the wrapper type cannot be constructed.</exception>
    internal RequestHandlerBase GetOrAdd(Type requestType, Type responseType)
    {
        var key = (requestType, responseType);
        if (_wrappers.TryGetValue(key, out var wrapper))
            return wrapper;

        return _wrappers.GetOrAdd(key, static k =>
        {
            var wrapperType = k.Response == VoidResponse
                ? typeof(RequestHandlerWrapperImpl<>).MakeGenericType(k.Request)
                : typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(k.Request, k.Response);

            var created = Activator.CreateInstance(wrapperType)
                ?? throw new InvalidOperationException($"Could not create wrapper type for {k.Request}");

            return (RequestHandlerBase)created;
        });
    }

    /// <summary>
    /// Returns the cached wrapper for dynamic dispatch of the given request type, resolving its response type on first use.
    /// </summary>
    /// <param name="requestType">The concrete request type.</param>
    /// <returns>The wrapper that dispatches this request type.</returns>
    /// <exception cref="ArgumentException">Thrown when the request type does not implement <see cref="IRequest"/>.</exception>
    internal RequestHandlerBase GetOrAdd(Type requestType)
    {
        if (_dynamicWrappers.TryGetValue(requestType, out var wrapper))
            return wrapper;

        return _dynamicWrappers.GetOrAdd(requestType, static (reqType, cache) =>
        {
            var responseType = reqType.GetInterfaces()
                .FirstOrDefault(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
                ?.GetGenericArguments()[0];

            if (responseType is null && !typeof(IRequest).IsAssignableFrom(reqType))
                throw new ArgumentException($"{reqType.Name} does not implement {nameof(IRequest)}", "request");

            return cache.GetOrAdd(reqType, responseType ?? VoidResponse);
        }, this);
    }

    /// <summary>
    /// Removes every cached wrapper. Intended for tests; the cache repopulates on the next send.
    /// </summary>
    public void Clear()
    {
        _wrappers.Clear();
        _dynamicWrappers.Clear();
    }
}
