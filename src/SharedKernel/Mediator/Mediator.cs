using SharedKernel.Abstractions.CQRS;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SharedKernel.Mediator;

/// <summary>
/// A mediator implementation that handles sending requests and invoking the appropriate request handlers.
/// </summary>
/// <remarks>
/// Handler wrappers are resolved through a <see cref="RequestHandlerWrapperCache"/> so the reflection that
/// builds them happens once per (request, response) pair rather than per send. <see cref="ServiceRegistrar"/>
/// registers that cache as a singleton, giving it container lifetime.
/// </remarks>
public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RequestHandlerWrapperCache _wrappers;

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider. Can be a scoped or root provider.</param>
    /// <remarks>
    /// The wrapper cache is taken from <paramref name="serviceProvider"/>. When the provider has no
    /// <see cref="RequestHandlerWrapperCache"/> registered — which happens only if this type is constructed
    /// without going through <c>AddMediator</c> — a private cache is used instead. That is correct but slower,
    /// because it is not shared with other mediator instances.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/> is null.</exception>
    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _wrappers = serviceProvider.GetService(typeof(RequestHandlerWrapperCache)) as RequestHandlerWrapperCache
            ?? new RequestHandlerWrapperCache();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class with an explicit wrapper cache.
    /// </summary>
    /// <param name="serviceProvider">Service provider. Can be a scoped or root provider.</param>
    /// <param name="wrapperCache">The cache to dispatch through.</param>
    /// <exception cref="ArgumentNullException">Thrown when either argument is null.</exception>
    public Mediator(IServiceProvider serviceProvider, RequestHandlerWrapperCache wrapperCache)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _wrappers = wrapperCache ?? throw new ArgumentNullException(nameof(wrapperCache));
    }

    /// <summary>
    /// Sends a strongly-typed request and returns a response.
    /// </summary>
    /// <typeparam name="TResponse">Response type expected from the request.</typeparam>
    /// <param name="request">Request to be handled.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response of the request.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var handler = (RequestHandlerWrapper<TResponse>)_wrappers.GetOrAdd(request.GetType(), typeof(TResponse));

        return handler.Handle(request, _serviceProvider, cancellationToken);
    }

    /// <summary>
    /// Sends a strongly-typed request that does not return a response.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request.</typeparam>
    /// <param name="request">Request to be handled.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the send operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var handler = (RequestHandlerWrapper)_wrappers.GetOrAdd(request.GetType(), RequestHandlerWrapperCache.VoidResponse);
        return handler.Handle(request, _serviceProvider, cancellationToken);
    }

    /// <summary>
    /// Sends a request dynamically without compile-time type information.
    /// </summary>
    /// <param name="request">Request to be handled.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the send operation, returning an object or null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the request does not implement IRequest interface.</exception>
    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var handler = _wrappers.GetOrAdd(request.GetType());

        return handler.Handle(request, _serviceProvider, cancellationToken);
    }
}
