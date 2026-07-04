using SharedKernel.Abstractions.CQRS;

namespace SharedKernel.Mediator.Tests.Commands;

public record PingCommand(string message) : IRequest<string>
{
    public string Message { get; set; } = message;
}

public record PingCommandHandler : IRequestHandler<PingCommand, string>
{
    public Task<string> Handle(PingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Pong: {request.Message}");
    }
}