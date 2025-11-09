using SharedKernel.Abstractions.CQRS;

namespace SharedKernel.Mediator.Tests.Commands;

public class SlowCommand : IRequest<string>
{
    public int DelayMs { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class SlowCommandHandler : IRequestHandler<SlowCommand, string>
{
    public async Task<string> Handle(SlowCommand request, CancellationToken cancellationToken)
    {
        await Task.Delay(request.DelayMs, cancellationToken);
        return $"Completed after {request.DelayMs}ms: {request.Message}";
    }
}
