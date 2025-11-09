using SharedKernel.Abstractions.CQRS;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharedKernel.Mediator;

/// <summary>
/// A mediator implementation that handles sending requests and invoking the appropriate request handlers.
/// Uses a concurrent dictionary cache to store handler wrappers for request types for performance.
/// </summary>
public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Cache of request handlers keyed by request type.
    /// This dictionary stores wrappers around request handlers to avoid repeated reflection or
    /// handler resolution on every request dispatch.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, RequestHandlerBase> _requestHandlers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider. Can be a scoped or root provider.</param>
    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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

        // Get or create a cached request handler wrapper for the specific request type
        var handler = (RequestHandlerWrapper<TResponse>)_requestHandlers.GetOrAdd(request.GetType(), static requestType =>
        {
            var wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, typeof(TResponse));
            var wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper type for {requestType}");
            return (RequestHandlerBase)wrapper;
        });

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

        var handler = (RequestHandlerWrapper)_requestHandlers.GetOrAdd(request.GetType(), static requestType =>
        {
            var wrapperType = typeof(RequestHandlerWrapperImpl<>).MakeGenericType(requestType);
            var wrapper = Activator.CreateInstance(wrapperType)
                ?? throw new InvalidOperationException($"Could not create wrapper type for {requestType}");
            return (RequestHandlerBase)wrapper;
        });

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

        var handler = _requestHandlers.GetOrAdd(request.GetType(), static requestType =>
        {
            Type wrapperType;

            var requestInterfaceType = requestType.GetInterfaces()
                .FirstOrDefault(static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

            if (requestInterfaceType is null)
            {
                requestInterfaceType = requestType.GetInterfaces().FirstOrDefault(static i => i == typeof(IRequest));

                if (requestInterfaceType is null)
                    throw new ArgumentException($"{requestType.Name} does not implement {nameof(IRequest)}", nameof(request));

                wrapperType = typeof(RequestHandlerWrapperImpl<>).MakeGenericType(requestType);
            }
            else
            {
                var responseType = requestInterfaceType.GetGenericArguments()[0];
                wrapperType = typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, responseType);
            }

            var wrapper = Activator.CreateInstance(wrapperType) ?? throw new InvalidOperationException($"Could not create wrapper for type {requestType}");

            return (RequestHandlerBase)wrapper;
        });

        // Call via dynamic dispatch to avoid reflection overhead, improving performance.
        return handler.Handle(request, _serviceProvider, cancellationToken);
    }
}
