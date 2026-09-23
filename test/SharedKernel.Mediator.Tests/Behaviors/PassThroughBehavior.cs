using SharedKernel.Abstractions.CQRS;

namespace SharedKernel.Mediator.Tests.Behaviors;

/// <summary>
/// A behavior that calls <c>next()</c> the way MediatR-style behaviors conventionally do — with no arguments.
/// Constrained on <c>notnull</c> rather than <c>IRequest&lt;TResponse&gt;</c> so it can close over void commands.
/// </summary>
public class PassThroughBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static int CallCount { get; private set; }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        CallCount++;
        return await next();
    }

    public static void Reset() => CallCount = 0;
}
