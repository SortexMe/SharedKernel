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
    private volatile int _behaviorCount = -1;

    [ThreadStatic]
    private static SingleBehaviorInvoker? _singleInvoker;

    [ThreadStatic]
    private static MultiBehaviorInvoker? _multiInvoker;

    // Development Note:
    // Dynamically casts and forwards the request to the typed Handle method.
    public override Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var task = Handle((IRequest<TResponse>)request, serviceProvider, cancellationToken);
        if (task.IsCompletedSuccessfully)
            return Task.FromResult<object?>(task.Result);

        return AwaitResponse(task);
    }

    private static async Task<object?> AwaitResponse(Task<TResponse> task)
    {
        return await task.ConfigureAwait(false);
    }

    // Development Note:
    // Resolves handler and pipeline behaviors from the DI container with zero allocations on hot paths.
    public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

        var count = _behaviorCount;
        if (count == 0)
        {
            return handler.Handle((TRequest)request, cancellationToken);
        }

        if (count == 1)
        {
            return InvokeSingleBehavior(serviceProvider, handler, (TRequest)request, cancellationToken);
        }

        if (count > 1)
        {
            var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>();
            var behaviorArray = behaviors as IPipelineBehavior<TRequest, TResponse>[] ?? behaviors.ToArray();
            return InvokeMultipleBehaviors(behaviorArray, handler, (TRequest)request, cancellationToken);
        }

        return InitializeAndHandle(serviceProvider, handler, (TRequest)request, cancellationToken);
    }

    private Task<TResponse> InitializeAndHandle(
        IServiceProvider serviceProvider,
        IRequestHandler<TRequest, TResponse> handler,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>();
        var behaviorArray = behaviors as IPipelineBehavior<TRequest, TResponse>[] ?? behaviors.ToArray();
        _behaviorCount = behaviorArray.Length;

        return _behaviorCount switch
        {
            0 => handler.Handle(request, cancellationToken),
            1 => InvokeSingleBehavior(behaviorArray[0], handler, request, cancellationToken),
            _ => InvokeMultipleBehaviors(behaviorArray, handler, request, cancellationToken)
        };
    }

    private static Task<TResponse> InvokeSingleBehavior(
        IServiceProvider serviceProvider,
        IRequestHandler<TRequest, TResponse> handler,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var behavior = serviceProvider.GetRequiredService<IPipelineBehavior<TRequest, TResponse>>();
        return InvokeSingleBehavior(behavior, handler, request, cancellationToken);
    }

    private static Task<TResponse> InvokeSingleBehavior(
        IPipelineBehavior<TRequest, TResponse> behavior,
        IRequestHandler<TRequest, TResponse> handler,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var invoker = _singleInvoker;
        if (invoker == null)
        {
            invoker = new SingleBehaviorInvoker();
            _singleInvoker = invoker;
        }

        if (!invoker.InUse)
        {
            invoker.InUse = true;
            invoker.Handler = handler;
            invoker.Request = request;
            invoker.CancellationToken = cancellationToken;

            try
            {
                var task = behavior.Handle(request, invoker.Next, cancellationToken);
                if (task.IsCompleted)
                {
                    invoker.Handler = null!;
                    invoker.Request = default!;
                    invoker.InUse = false;
                    return task;
                }

                _singleInvoker = null;
                return AwaitSingleBehaviorAsync(task, invoker);
            }
            catch
            {
                invoker.Handler = null!;
                invoker.Request = default!;
                invoker.InUse = false;
                throw;
            }
        }

        return behavior.Handle(
            request,
            ct => handler.Handle(request, ct == default ? cancellationToken : ct),
            cancellationToken);
    }

    private static async Task<TResponse> AwaitSingleBehaviorAsync(Task<TResponse> task, SingleBehaviorInvoker invoker)
    {
        try
        {
            return await task.ConfigureAwait(false);
        }
        finally
        {
            invoker.Handler = null!;
            invoker.Request = default!;
            invoker.InUse = false;
        }
    }

    private static Task<TResponse> InvokeMultipleBehaviors(
        IPipelineBehavior<TRequest, TResponse>[] behaviors,
        IRequestHandler<TRequest, TResponse> handler,
        TRequest request,
        CancellationToken cancellationToken)
    {
        if (behaviors.Length <= MultiBehaviorInvoker.MaxPreallocated)
        {
            var invoker = _multiInvoker;
            if (invoker == null)
            {
                invoker = new MultiBehaviorInvoker();
                _multiInvoker = invoker;
            }

            if (!invoker.InUse)
            {
                invoker.InUse = true;
                invoker.Behaviors = behaviors;
                invoker.Handler = handler;
                invoker.Request = request;
                invoker.CancellationToken = cancellationToken;

                try
                {
                    var task = invoker.Run(cancellationToken);
                    if (task.IsCompleted)
                    {
                        invoker.Behaviors = null!;
                        invoker.Handler = null!;
                        invoker.Request = default!;
                        invoker.InUse = false;
                        return task;
                    }

                    _multiInvoker = null;
                    return AwaitMultiBehaviorAsync(task, invoker);
                }
                catch
                {
                    invoker.Behaviors = null!;
                    invoker.Handler = null!;
                    invoker.Request = default!;
                    invoker.InUse = false;
                    throw;
                }
            }
        }

        return InvokePipelineDynamic(behaviors, handler, request, cancellationToken);
    }

    private static async Task<TResponse> AwaitMultiBehaviorAsync(Task<TResponse> task, MultiBehaviorInvoker invoker)
    {
        try
        {
            return await task.ConfigureAwait(false);
        }
        finally
        {
            invoker.Behaviors = null!;
            invoker.Handler = null!;
            invoker.Request = default!;
            invoker.InUse = false;
        }
    }

    private static Task<TResponse> InvokePipelineDynamic(
        IPipelineBehavior<TRequest, TResponse>[] behaviors,
        IRequestHandler<TRequest, TResponse> handler,
        TRequest request,
        CancellationToken cancellationToken)
    {
        Task<TResponse> Execute(int index, CancellationToken ct)
        {
            if ((uint)index < (uint)behaviors.Length)
            {
                var behavior = behaviors[index];
                return behavior.Handle(request, inner => Execute(index + 1, inner == default ? ct : inner), ct);
            }

            return handler.Handle(request, ct);
        }

        return Execute(0, cancellationToken);
    }

    private sealed class SingleBehaviorInvoker
    {
        public IRequestHandler<TRequest, TResponse> Handler = null!;
        public TRequest Request = default!;
        public CancellationToken CancellationToken;
        public bool InUse;
        public readonly RequestHandlerDelegate<TResponse> Next;

        public SingleBehaviorInvoker()
        {
            Next = Execute;
        }

        private Task<TResponse> Execute(CancellationToken ct)
        {
            return Handler.Handle(Request, ct == default ? CancellationToken : ct);
        }
    }

    private sealed class MultiBehaviorInvoker
    {
        public const int MaxPreallocated = 8;
        private readonly RequestHandlerDelegate<TResponse>[] _delegates;
        public IPipelineBehavior<TRequest, TResponse>[] Behaviors = null!;
        public IRequestHandler<TRequest, TResponse> Handler = null!;
        public TRequest Request = default!;
        public CancellationToken CancellationToken;
        public bool InUse;

        public MultiBehaviorInvoker()
        {
            _delegates = new RequestHandlerDelegate<TResponse>[MaxPreallocated + 1];
            for (var i = 0; i <= MaxPreallocated; i++)
            {
                var step = i;
                _delegates[i] = ct => Step(step, ct);
            }
        }

        public Task<TResponse> Run(CancellationToken ct)
        {
            return Behaviors[0].Handle(Request, _delegates[1], ct);
        }

        private Task<TResponse> Step(int index, CancellationToken ct)
        {
            var effectiveCt = ct == default ? CancellationToken : ct;
            if ((uint)index < (uint)Behaviors.Length)
            {
                return Behaviors[index].Handle(Request, _delegates[index + 1], effectiveCt);
            }

            return Handler.Handle(Request, effectiveCt);
        }
    }
}

/// <summary>
/// Wrapper implementation for handling commands that return <see cref="Unit"/>.
/// </summary>
/// <typeparam name="TRequest">The command type.</typeparam>
public class RequestHandlerWrapperImpl<TRequest> : RequestHandlerWrapper where TRequest : IRequest
{
    private volatile int _behaviorCount = -1;

    [ThreadStatic]
    private static SingleBehaviorInvoker? _singleInvoker;

    [ThreadStatic]
    private static MultiBehaviorInvoker? _multiInvoker;

    // Development Note:
    // Dynamically casts and delegates to the typed Handle method.
    public override Task<object?> Handle(object request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var task = Handle((IRequest)request, serviceProvider, cancellationToken);
        if (task.IsCompletedSuccessfully)
            return Task.FromResult<object?>(Unit.Value);

        return AwaitVoidResponse(task);
    }

    private static async Task<object?> AwaitVoidResponse(Task<Unit> task)
    {
        await task.ConfigureAwait(false);
        return Unit.Value;
    }

    // Development Note:
    // Handles command requests and applies pipeline behaviors if any are registered with zero allocations on hot paths.
    public override Task<Unit> Handle(IRequest request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest>>();

        var count = _behaviorCount;
        if (count == 0)
        {
            var task = handler.Handle((TRequest)request, cancellationToken);
            if (task.IsCompletedSuccessfully)
                return Unit.Task;

            return AwaitHandler(task);
        }

        if (count == 1)
        {
            return InvokeSingleBehavior(serviceProvider, handler, (TRequest)request, cancellationToken);
        }

        if (count > 1)
        {
            var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, Unit>>();
            var behaviorArray = behaviors as IPipelineBehavior<TRequest, Unit>[] ?? behaviors.ToArray();
            return InvokeMultipleBehaviors(behaviorArray, handler, (TRequest)request, cancellationToken);
        }

        return InitializeAndHandle(serviceProvider, handler, (TRequest)request, cancellationToken);
    }

    private Task<Unit> InitializeAndHandle(
        IServiceProvider serviceProvider,
        IRequestHandler<TRequest> handler,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, Unit>>();
        var behaviorArray = behaviors as IPipelineBehavior<TRequest, Unit>[] ?? behaviors.ToArray();
        _behaviorCount = behaviorArray.Length;

        if (_behaviorCount == 0)
        {
            var task = handler.Handle(request, cancellationToken);
            if (task.IsCompletedSuccessfully)
                return Unit.Task;

            return AwaitHandler(task);
        }

        if (_behaviorCount == 1)
        {
            return InvokeSingleBehavior(behaviorArray[0], handler, request, cancellationToken);
        }

        return InvokeMultipleBehaviors(behaviorArray, handler, request, cancellationToken);
    }

    private static Task<Unit> InvokeSingleBehavior(
        IServiceProvider serviceProvider,
        IRequestHandler<TRequest> handler,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var behavior = serviceProvider.GetRequiredService<IPipelineBehavior<TRequest, Unit>>();
        return InvokeSingleBehavior(behavior, handler, request, cancellationToken);
    }

    private static Task<Unit> InvokeSingleBehavior(
        IPipelineBehavior<TRequest, Unit> behavior,
        IRequestHandler<TRequest> handler,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var invoker = _singleInvoker;
        if (invoker == null)
        {
            invoker = new SingleBehaviorInvoker();
            _singleInvoker = invoker;
        }

        if (!invoker.InUse)
        {
            invoker.InUse = true;
            invoker.Handler = handler;
            invoker.Request = request;
            invoker.CancellationToken = cancellationToken;

            try
            {
                var task = behavior.Handle(request, invoker.Next, cancellationToken);
                if (task.IsCompleted)
                {
                    invoker.Handler = null!;
                    invoker.Request = default!;
                    invoker.InUse = false;
                    return task;
                }

                _singleInvoker = null;
                return AwaitSingleBehaviorAsync(task, invoker);
            }
            catch
            {
                invoker.Handler = null!;
                invoker.Request = default!;
                invoker.InUse = false;
                throw;
            }
        }

        return behavior.Handle(
            request,
            ct =>
            {
                var innerTask = handler.Handle(request, ct == default ? cancellationToken : ct);
                if (innerTask.IsCompletedSuccessfully)
                    return Unit.Task;

                return AwaitHandler(innerTask);
            },
            cancellationToken);
    }

    private static async Task<Unit> AwaitSingleBehaviorAsync(Task<Unit> task, SingleBehaviorInvoker invoker)
    {
        try
        {
            return await task.ConfigureAwait(false);
        }
        finally
        {
            invoker.Handler = null!;
            invoker.Request = default!;
            invoker.InUse = false;
        }
    }

    private static Task<Unit> InvokeMultipleBehaviors(
        IPipelineBehavior<TRequest, Unit>[] behaviors,
        IRequestHandler<TRequest> handler,
        TRequest request,
        CancellationToken cancellationToken)
    {
        if (behaviors.Length <= MultiBehaviorInvoker.MaxPreallocated)
        {
            var invoker = _multiInvoker;
            if (invoker == null)
            {
                invoker = new MultiBehaviorInvoker();
                _multiInvoker = invoker;
            }

            if (!invoker.InUse)
            {
                invoker.InUse = true;
                invoker.Behaviors = behaviors;
                invoker.Handler = handler;
                invoker.Request = request;
                invoker.CancellationToken = cancellationToken;

                try
                {
                    var task = invoker.Run(cancellationToken);
                    if (task.IsCompleted)
                    {
                        invoker.Behaviors = null!;
                        invoker.Handler = null!;
                        invoker.Request = default!;
                        invoker.InUse = false;
                        return task;
                    }

                    _multiInvoker = null;
                    return AwaitMultiBehaviorAsync(task, invoker);
                }
                catch
                {
                    invoker.Behaviors = null!;
                    invoker.Handler = null!;
                    invoker.Request = default!;
                    invoker.InUse = false;
                    throw;
                }
            }
        }

        return InvokePipelineDynamic(behaviors, handler, request, cancellationToken);
    }

    private static async Task<Unit> AwaitMultiBehaviorAsync(Task<Unit> task, MultiBehaviorInvoker invoker)
    {
        try
        {
            return await task.ConfigureAwait(false);
        }
        finally
        {
            invoker.Behaviors = null!;
            invoker.Handler = null!;
            invoker.Request = default!;
            invoker.InUse = false;
        }
    }

    private static Task<Unit> InvokePipelineDynamic(
        IPipelineBehavior<TRequest, Unit>[] behaviors,
        IRequestHandler<TRequest> handler,
        TRequest request,
        CancellationToken cancellationToken)
    {
        Task<Unit> Execute(int index, CancellationToken ct)
        {
            if ((uint)index < (uint)behaviors.Length)
            {
                var behavior = behaviors[index];
                return behavior.Handle(request, inner => Execute(index + 1, inner == default ? ct : inner), ct);
            }

            var handlerTask = handler.Handle(request, ct);
            if (handlerTask.IsCompletedSuccessfully)
                return Unit.Task;

            return AwaitHandler(handlerTask);
        }

        return Execute(0, cancellationToken);
    }

    private static async Task<Unit> AwaitHandler(Task task)
    {
        await task.ConfigureAwait(false);
        return Unit.Value;
    }

    private sealed class SingleBehaviorInvoker
    {
        public IRequestHandler<TRequest> Handler = null!;
        public TRequest Request = default!;
        public CancellationToken CancellationToken;
        public bool InUse;
        public readonly RequestHandlerDelegate<Unit> Next;

        public SingleBehaviorInvoker()
        {
            Next = Execute;
        }

        private Task<Unit> Execute(CancellationToken ct)
        {
            var task = Handler.Handle(Request, ct == default ? CancellationToken : ct);
            if (task.IsCompletedSuccessfully)
                return Unit.Task;

            return AwaitHandler(task);
        }
    }

    private sealed class MultiBehaviorInvoker
    {
        public const int MaxPreallocated = 8;
        private readonly RequestHandlerDelegate<Unit>[] _delegates;
        public IPipelineBehavior<TRequest, Unit>[] Behaviors = null!;
        public IRequestHandler<TRequest> Handler = null!;
        public TRequest Request = default!;
        public CancellationToken CancellationToken;
        public bool InUse;

        public MultiBehaviorInvoker()
        {
            _delegates = new RequestHandlerDelegate<Unit>[MaxPreallocated + 1];
            for (var i = 0; i <= MaxPreallocated; i++)
            {
                var step = i;
                _delegates[i] = ct => Step(step, ct);
            }
        }

        public Task<Unit> Run(CancellationToken ct)
        {
            return Behaviors[0].Handle(Request, _delegates[1], ct);
        }

        private Task<Unit> Step(int index, CancellationToken ct)
        {
            var effectiveCt = ct == default ? CancellationToken : ct;
            if ((uint)index < (uint)Behaviors.Length)
            {
                return Behaviors[index].Handle(Request, _delegates[index + 1], effectiveCt);
            }

            var task = Handler.Handle(Request, effectiveCt);
            if (task.IsCompletedSuccessfully)
                return Unit.Task;

            return AwaitHandler(task);
        }
    }
}
