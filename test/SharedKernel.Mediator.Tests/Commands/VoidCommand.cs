using SharedKernel.Abstractions.CQRS;
using SharedKernel.Mediator;

namespace SharedKernel.Mediator.Tests.Commands;

public record VoidCommand(string Message) : IRequest<Unit>;

public class VoidCommandHandler : IRequestHandler<VoidCommand, Unit>
{
    public static int ExecutionCount { get; private set; }
    
    public Task<Unit> Handle(VoidCommand request, CancellationToken cancellationToken)
    {
        ExecutionCount++;
        return Task.FromResult(Unit.Value);
    }
    
    public static void Reset() => ExecutionCount = 0;
}
