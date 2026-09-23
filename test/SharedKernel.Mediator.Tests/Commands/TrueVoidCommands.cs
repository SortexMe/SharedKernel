using SharedKernel.Abstractions.CQRS;

namespace SharedKernel.Mediator.Tests.Commands;

// These commands implement the non-generic IRequest and are handled by IRequestHandler<TRequest>.
// They exercise RequestHandlerWrapperImpl<TRequest> — the void pipeline — which VoidCommand
// (declared as IRequest<Unit>) does not reach.

public record AsyncThrowingCommand : IRequest;

public class AsyncThrowingCommandHandler : IRequestHandler<AsyncThrowingCommand>
{
    public async Task Handle(AsyncThrowingCommand request, CancellationToken cancellationToken)
    {
        await Task.Yield(); // fault the Task asynchronously rather than throwing before the first await
        throw new InvalidOperationException("async handler failure");
    }
}

public record TokenProbeCommand : IRequest;

public class TokenProbeCommandHandler : IRequestHandler<TokenProbeCommand>
{
    public static bool LastTokenCanBeCanceled { get; private set; }

    public Task Handle(TokenProbeCommand request, CancellationToken cancellationToken)
    {
        LastTokenCanBeCanceled = cancellationToken.CanBeCanceled;
        return Task.CompletedTask;
    }

    public static void Reset() => LastTokenCanBeCanceled = false;
}

public record TokenProbeQuery : IRequest<bool>;

public class TokenProbeQueryHandler : IRequestHandler<TokenProbeQuery, bool>
{
    public Task<bool> Handle(TokenProbeQuery request, CancellationToken cancellationToken)
        => Task.FromResult(cancellationToken.CanBeCanceled);
}

public record CancellableVoidCommand : IRequest;

public class CancellableVoidCommandHandler : IRequestHandler<CancellableVoidCommand>
{
    public Task Handle(CancellableVoidCommand request, CancellationToken cancellationToken)
        => Task.Delay(TimeSpan.FromSeconds(3), cancellationToken); // bounded so a dropped token fails the test instead of hanging it
}

public record GenericQuery<T> : IRequest<string>;

public class GenericQueryHandler<T> : IRequestHandler<GenericQuery<T>, string> where T : IGenericMarker
{
    public Task<string> Handle(GenericQuery<T> request, CancellationToken cancellationToken)
        => Task.FromResult(typeof(T).Name);
}

public record CovariantQuery : IRequest<string>;

public class CovariantQueryHandler : IRequestHandler<CovariantQuery, string>
{
    public Task<string> Handle(CovariantQuery request, CancellationToken cancellationToken)
        => Task.FromResult("covariant");
}

public record SecretCommand(string UserName, string Password, string RefreshToken) : IRequest;

public class SecretCommandHandler : IRequestHandler<SecretCommand>
{
    public Task Handle(SecretCommand request, CancellationToken cancellationToken) => Task.CompletedTask;
}
