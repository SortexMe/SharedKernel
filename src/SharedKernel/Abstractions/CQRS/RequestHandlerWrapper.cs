using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Mediator;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharedKernel.Abstractions.CQRS;

/// <summary>
/// Base class for request handler wrappers, enabling dynamic dispatch for CQRS handlers.
/// </summary>
public abstract class RequestHandlerBase
{
    /// <summary>
    /// Handles the request using a resolved handler from the <see cref="IServiceProvider"/>.
    /// </summary>
    /// <param name="request">The request instance.</param>
    /// <param name="serviceProvider">The service provider used to resolve dependencies.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public abstract Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Abstract base class for request handlers that return a response.
/// </summary>
/// <typeparam name="TResponse">The type of the response returned.</typeparam>
public abstract class RequestHandlerWrapper<TResponse> : RequestHandlerBase
{
    /// <summary>
    /// Handles a typed request and returns a typed response.
    /// </summary>
    /// <param name="request">The typed request instance.</param>
    /// <param name="serviceProvider">Service provider for dependency resolution.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Abstract base class for request handlers that return <see cref="Unit"/>.
/// </summary>
public abstract class RequestHandlerWrapper : RequestHandlerBase
{
    /// <summary>
    /// Handles a command-type request that does not return a result.
    /// </summary>
    /// <param name="request">The request instance.</param>
    /// <param name="serviceProvider">Service provider for dependency resolution.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the operation.</returns>
    public abstract Task Handle(IRequest request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>
/// Wrapper implementation for handling typed requests with response types.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse> where TRequest : IRequest<TResponse>
{
    // Development Note:
    // Dynamically casts and forwards the request to the typed Handle method.
    public override async Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken) =>
        await Handle((IRequest<TResponse>)request, serviceProvider, cancellationToken).ConfigureAwait(false);

    // Development Note:
    // Resolves handler and pipeline behaviors from the DI container.
    public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToArray();

        return behaviors.Length == 0
            ? handler.Handle((TRequest)request, cancellationToken)
            : BuildPipeline(behaviors, handler).Invoke((TRequest)request, cancellationToken);
    }

    // Development Note:
    // Builds the middleware pipeline for the handler using the IPipelineBehavior interfaces.
    private static Func<TRequest, CancellationToken, Task<TResponse>> BuildPipeline(IPipelineBehavior<TRequest, TResponse>[] behaviors, IRequestHandler<TRequest, TResponse> handler)
    {
        Func<TRequest, CancellationToken, Task<TResponse>> pipeline = handler.Handle;

        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var current = behaviors[i];
            var next = pipeline;
            pipeline = (req, ct) => current.Handle(req, (ct) => next(req, ct), ct);
        }

        return pipeline;
    }
}

/// <summary>
/// Wrapper implementation for handling commands that return <see cref="Unit"/>.
/// </summary>
/// <typeparam name="TRequest">The command type.</typeparam>
public class RequestHandlerWrapperImpl<TRequest> : RequestHandlerWrapper where TRequest : IRequest
{
    // Development Note:
    // Dynamically casts and delegates to the typed Handle method.
    public override async Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken) =>
        await Handle((IRequest)request, serviceProvider, cancellationToken).ConfigureAwait(false);

    // Development Note:
    // Handles command requests and applies pipeline behaviors if any are registered.
    public override async Task<Unit> Handle(IRequest request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest>>();
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, Unit>>().ToArray();

        if (behaviors.Length == 0)
            await handler.Handle((TRequest)request, cancellationToken);
        else
            await BuildPipeline(behaviors, handler).Invoke((TRequest)request, cancellationToken);

        return Unit.Value;
    }

    // Development Note:
    // Builds a pipeline of command behaviors wrapping around the handler.
    private static Func<TRequest, CancellationToken, Task> BuildPipeline(IPipelineBehavior<TRequest, Unit>[] behaviors, IRequestHandler<TRequest> handler)
    {
        Func<TRequest, CancellationToken, Task> pipeline = handler.Handle;

        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var current = behaviors[i];
            var next = pipeline;
            pipeline = async (req, ct) =>
                await current.Handle(req, (ct) => next(req, ct).ContinueWith(_ => Unit.Value, ct), ct);
        }

        return pipeline;
    }
}


#region Old Code
//public abstract class RequestHandlerBase
//{
//    public abstract Task<object?> Handle(object request, IServiceProvider serviceProvider,
//        CancellationToken cancellationToken);
//}

//public abstract class RequestHandlerWrapper<TResponse> : RequestHandlerBase
//{
//    public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider,
//        CancellationToken cancellationToken);
//}

//public abstract class RequestHandlerWrapper : RequestHandlerBase
//{
//    public abstract Task<Unit> Handle(IRequest request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
//}

//public class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse> where TRequest : IRequest<TResponse>
//{
//    public override async Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken) => await Handle((IRequest<TResponse>)request, serviceProvider, cancellationToken).ConfigureAwait(false);

//    public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
//    {
//        Task<TResponse> Handler(CancellationToken t = default) => serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>()
//            .Handle((TRequest)request, t == default ? cancellationToken : t);

//        return serviceProvider
//            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
//            .Reverse()
//            .Aggregate((RequestHandlerDelegate<TResponse>)Handler,
//                (next, pipeline) => (t) => pipeline.Handle((TRequest)request, next, t == default ? cancellationToken : t))();
//    }
//}

//public class RequestHandlerWrapperImpl<TRequest> : RequestHandlerWrapper where TRequest : IRequest
//{
//    public override async Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken) => await Handle((IRequest)request, serviceProvider, cancellationToken).ConfigureAwait(false);

//    public override Task<Unit> Handle(IRequest request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
//    {
//        async Task<Unit> Handler(CancellationToken t = default)
//        {
//            await serviceProvider.GetRequiredService<IRequestHandler<TRequest>>()
//                .Handle((TRequest)request, t == default ? cancellationToken : t);

//            return Unit.Value;
//        }

//        return serviceProvider
//            .GetServices<IPipelineBehavior<TRequest, Unit>>()
//            .Reverse()
//            .Aggregate((RequestHandlerDelegate<Unit>)Handler,
//                (next, pipeline) => (t) => pipeline.Handle((TRequest)request, next, t == default ? cancellationToken : t))();
//    }
//}
#endregion