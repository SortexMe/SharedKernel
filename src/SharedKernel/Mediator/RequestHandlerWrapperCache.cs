using SharedKernel.Abstractions.CQRS;
using System;
using System.Collections.Concurrent;

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
    internal RequestHandlerBase GetOrAdd(Type requestType, Type responseType) =>
        _wrappers.GetOrAdd((requestType, responseType), static key =>
        {
            var wrapperType = key.Response == VoidResponse
                ? typeof(RequestHandlerWrapperImpl<>).MakeGenericType(key.Request)
                : typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(key.Request, key.Response);

            var wrapper = Activator.CreateInstance(wrapperType)
                ?? throw new InvalidOperationException($"Could not create wrapper type for {key.Request}");

            return (RequestHandlerBase)wrapper;
        });

    /// <summary>
    /// Removes every cached wrapper. Intended for tests; the cache repopulates on the next send.
    /// </summary>
    public void Clear() => _wrappers.Clear();
}
