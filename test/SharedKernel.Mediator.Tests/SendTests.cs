using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.DependencyInjection;
using SharedKernel.Mediator.Tests.Commands;
using System.Reflection;

namespace SharedKernel.Mediator.Tests;

public class SendTests
{
    private readonly IServiceProvider serviceProvider;
    
    public SendTests()
    {
        serviceProvider = BuildServiceProvider();
    }

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Register all handlers
        services.AddSingleton<IRequestHandler<PingCommand, string>, PingCommandHandler>();
        services.AddSingleton<IRequestHandler<ComplexCommand, ComplexResponse>, ComplexCommandHandler>();
        services.AddSingleton<IRequestHandler<VoidCommand, Unit>, VoidCommandHandler>();
        services.AddSingleton<IRequestHandler<SlowCommand, string>, SlowCommandHandler>();

        // Register mediator
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Mediator_Should_Handle_Simple_Request_Response()
    {
        // Arrange
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var command = new PingCommand("Simple");

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.Should().Be("Pong: Simple");
    }

    [Fact]
    public async Task Mediator_Should_Handle_Complex_Request_Response()
    {
        // Arrange
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var command = new ComplexCommand
        {
            Name = "John Doe",
            Age = 30,
            Tags = new List<string> { "developer", "senior", "full-stack" }
        };

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("John Doe");
        result.Message.Should().Contain("3 tags");
        result.ProcessedCount.Should().Be(3);
    }

    [Fact]
    public async Task Mediator_Should_Handle_Void_Commands()
    {
        // Arrange
        VoidCommandHandler.Reset();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var command = new VoidCommand("Test void command");

        // Act
        await mediator.Send(command);

        // Assert
        VoidCommandHandler.ExecutionCount.Should().Be(1);
    }

    [Fact]
    public async Task Mediator_Should_Handle_Dynamic_Requests()
    {
        // Arrange
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        object command = new PingCommand("Dynamic");

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("Pong: Dynamic");
    }

    [Fact]
    public async Task Mediator_Should_Handle_Dynamic_Void_Requests()
    {
        // Arrange
        VoidCommandHandler.Reset();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        object command = new VoidCommand("Dynamic void");

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.Should().NotBeNull(); // Unit type is returned, not null
        VoidCommandHandler.ExecutionCount.Should().Be(1);
    }

    [Fact]
    public async Task Mediator_Should_Handle_Multiple_Requests_Concurrently()
    {
        // Arrange
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var tasks = new List<Task<string>>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var command = new PingCommand($"Concurrent-{i}");
            tasks.Add(mediator.Send(command));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().AllSatisfy(result => result.Should().StartWith("Pong: Concurrent-"));
        
        // Verify all results are unique
        results.Distinct().Should().HaveCount(10);
    }

    [Fact]
    public async Task Mediator_Should_Respect_Cancellation_Token()
    {
        // Arrange
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var cts = new CancellationTokenSource();
        var command = new SlowCommand
        {
            DelayMs = 2000,
            Message = "This will be cancelled"
        };

        // Act
        var task = mediator.Send(command, cts.Token);
        cts.CancelAfter(100); // Cancel after 100ms

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Valid Name")]
    public async Task Mediator_Should_Handle_Various_Input_Values(string name)
    {
        // Arrange
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var command = new ComplexCommand
        {
            Name = name,
            Age = 25,
            Tags = new List<string> { "test" }
        };

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().Be(!string.IsNullOrWhiteSpace(name));
        result.ProcessedCount.Should().Be(1);
    }

    [Fact]
    public async Task Mediator_Should_Handle_Empty_Collections()
    {
        // Arrange
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var command = new ComplexCommand
        {
            Name = "Test",
            Age = 25,
            Tags = new List<string>() // Empty collection
        };

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ProcessedCount.Should().Be(0);
        result.Message.Should().Contain("0 tags");
    }

    [Fact]
    public void Mediator_Should_Throw_ArgumentNullException_For_Null_Request()
    {
        // Arrange
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act & Assert
        var act = async () => await mediator.Send<string>(null!);
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Mediator_Should_Throw_ArgumentNullException_For_Null_Dynamic_Request()
    {
        // Arrange
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act & Assert
        var act = async () => await mediator.Send((object)null!);
        act.Should().ThrowAsync<ArgumentNullException>();
    }
}
