using SharedKernel.Abstractions.CQRS;

namespace SharedKernel.Mediator.Tests.Commands;

public record NoHandlerCommand(string message) : IRequest<string>
{
    public string Message { get; set; } = message;
}