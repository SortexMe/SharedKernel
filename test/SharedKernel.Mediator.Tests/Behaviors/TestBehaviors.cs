using SharedKernel.Abstractions.CQRS;

namespace SharedKernel.Mediator.Tests.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    public static int CallCount { get; private set; }
    
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        CallCount++;
        
        // Simple validation example
        if (request is null)
            throw new ArgumentNullException(nameof(request));
            
        return await next();
    }
    
    public static void Reset() => CallCount = 0;
}

public class TimingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    public static TimeSpan LastExecutionTime { get; private set; }
    
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await next();
        stopwatch.Stop();
        LastExecutionTime = stopwatch.Elapsed;
        return result;
    }
    
    public static void Reset() => LastExecutionTime = TimeSpan.Zero;
}
