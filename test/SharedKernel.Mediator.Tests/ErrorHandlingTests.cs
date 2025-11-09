using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.DependencyInjection;
using SharedKernel.Mediator.Tests.Commands;
using System.Reflection;

namespace SharedKernel.Mediator.Tests;

public class ErrorHandlingTests
{
    [Fact]
    public async Task Mediator_Should_Throw_InvalidOperationException_For_Missing_Handler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });
        
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var command = new NoHandlerCommand("Test");

        // Act & Assert
        var act = async () => await mediator.Send(command);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Mediator_Should_Throw_InvalidOperationException_For_Dynamic_Missing_Handler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });
        
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        object command = new NoHandlerCommand("Dynamic Test");

        // Act & Assert
        var act = async () => await mediator.Send(command);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Mediator_Should_Throw_ArgumentException_For_Non_IRequest_Dynamic_Object()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });
        
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        object invalidCommand = "This is not an IRequest";

        // Act & Assert
        var act = async () => await mediator.Send(invalidCommand);
        act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*does not implement IRequest*");
    }

    [Fact]
    public async Task Mediator_Should_Propagate_Handler_Exceptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IRequestHandler<ThrowingCommand, string>, ThrowingCommandHandler>();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });
        
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var command = new ThrowingCommand("Test exception");

        // Act & Assert
        var act = async () => await mediator.Send(command);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Handler threw an exception: Test exception");
    }

    [Fact]
    public async Task Mediator_Should_Handle_Handler_Returning_Null()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IRequestHandler<NullReturningCommand, string>, NullReturningCommandHandler>();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });
        
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var command = new NullReturningCommand();

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Mediator_Should_Handle_Large_Object_Requests()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IRequestHandler<LargeDataCommand, string>, LargeDataCommandHandler>();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });
        
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        
        var largeData = new string('x', 10000); // 10KB string
        var command = new LargeDataCommand { Data = largeData };

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.Should().Be($"Processed {largeData.Length} characters");
    }

    [Fact]
    public async Task Mediator_Should_Handle_Deeply_Nested_Generic_Types()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IRequestHandler<NestedGenericCommand<List<Dictionary<string, int>>>, string>, 
            NestedGenericCommandHandler>();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });
        
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        
        var command = new NestedGenericCommand<List<Dictionary<string, int>>>
        {
            Data = new List<Dictionary<string, int>>
            {
                new() { { "key1", 1 }, { "key2", 2 } },
                new() { { "key3", 3 } }
            }
        };

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.Should().Contain("Processed nested generic");
    }
}

// Test command that throws an exception
public record ThrowingCommand(string Message) : IRequest<string>;

public class ThrowingCommandHandler : IRequestHandler<ThrowingCommand, string>
{
    public Task<string> Handle(ThrowingCommand request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException($"Handler threw an exception: {request.Message}");
    }
}

// Test command that returns null
public record NullReturningCommand : IRequest<string>;

public class NullReturningCommandHandler : IRequestHandler<NullReturningCommand, string>
{
    public Task<string> Handle(NullReturningCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult<string>(null!);
    }
}

// Test command with large data
public class LargeDataCommand : IRequest<string>
{
    public string Data { get; set; } = string.Empty;
}

public class LargeDataCommandHandler : IRequestHandler<LargeDataCommand, string>
{
    public Task<string> Handle(LargeDataCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Processed {request.Data.Length} characters");
    }
}

// Test command with nested generics
public class NestedGenericCommand<T> : IRequest<string>
{
    public T Data { get; set; } = default!;
}

public class NestedGenericCommandHandler : IRequestHandler<NestedGenericCommand<List<Dictionary<string, int>>>, string>
{
    public Task<string> Handle(NestedGenericCommand<List<Dictionary<string, int>>> request, CancellationToken cancellationToken)
    {
        var totalItems = request.Data?.Sum(dict => dict.Count) ?? 0;
        return Task.FromResult($"Processed nested generic with {totalItems} total items");
    }
}
