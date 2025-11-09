using SharedKernel.Abstractions.CQRS;

namespace SharedKernel.Mediator.Tests.Commands;

public record ComplexCommand : IRequest<ComplexResponse>
{
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public List<string> Tags { get; init; } = new();
}

public record ComplexResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int ProcessedCount { get; init; }
}

public class ComplexCommandHandler : IRequestHandler<ComplexCommand, ComplexResponse>
{
    public Task<ComplexResponse> Handle(ComplexCommand request, CancellationToken cancellationToken)
    {
        // Simulate some processing
        var response = new ComplexResponse
        {
            Success = !string.IsNullOrWhiteSpace(request.Name),
            Message = $"Processed {request.Name} with {request.Tags.Count} tags",
            ProcessedCount = request.Tags.Count
        };

        return Task.FromResult(response);
    }
}
