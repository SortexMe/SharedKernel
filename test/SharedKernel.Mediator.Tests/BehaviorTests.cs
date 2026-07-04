using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.DependencyInjection;
using SharedKernel.Mediator.Behaviors;
using SharedKernel.Mediator.Tests.Behaviors;
using SharedKernel.Mediator.Tests.Commands;
using System.Reflection;

namespace SharedKernel.Mediator.Tests;

public class BehaviorTests
{
    private readonly IServiceProvider serviceProvider;
    private readonly Mock<ILogger<PingCommand>> loggerMock;
    
    public BehaviorTests()
    {
        loggerMock = new Mock<ILogger<PingCommand>>();
        serviceProvider = BuildServiceProvider();
    }

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Register command and handler
        services.AddSingleton<IRequestHandler<PingCommand, string>, PingCommandHandler>();
        services.AddSingleton<IRequestHandler<ComplexCommand, ComplexResponse>, ComplexCommandHandler>();
        services.AddSingleton<IRequestHandler<SlowCommand, string>, SlowCommandHandler>();

        // Register behaviors
        loggerMock.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
        services.AddSingleton(typeof(ILogger<PingCommand>), loggerMock.Object);

        // Register mediator
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        return services.BuildServiceProvider();
    }

    private ServiceProvider BuildServiceProviderWithMultipleBehaviors()
    {
        var services = new ServiceCollection();

        // Register handlers
        services.AddSingleton<IRequestHandler<PingCommand, string>, PingCommandHandler>();
        services.AddSingleton<IRequestHandler<ComplexCommand, ComplexResponse>, ComplexCommandHandler>();
        services.AddSingleton<IRequestHandler<SlowCommand, string>, SlowCommandHandler>();

        // Register logger mocks
        loggerMock.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
        services.AddSingleton(typeof(ILogger<PingCommand>), loggerMock.Object);
        
        var complexLoggerMock = new Mock<ILogger<ComplexCommand>>();
        complexLoggerMock.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
        services.AddSingleton(typeof(ILogger<ComplexCommand>), complexLoggerMock.Object);
        
        var slowLoggerMock = new Mock<ILogger<SlowCommand>>();
        slowLoggerMock.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
        services.AddSingleton(typeof(ILogger<SlowCommand>), slowLoggerMock.Object);

        // Register mediator with multiple behaviors
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.AddOpenBehavior(typeof(ValidationBehavior<,>));
            options.AddOpenBehavior(typeof(TimingBehavior<,>));
            options.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Mediator_Should_Invoke_LoggingBehavior()
    {
        // Arrange
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var command = new PingCommand("Trace");

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.Should().Be("Pong: Trace");

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Handling {nameof(PingCommand)}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Handled {nameof(PingCommand)}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Mediator_Should_Execute_Multiple_Behaviors_In_Order()
    {
        // Arrange
        ValidationBehavior<ComplexCommand, ComplexResponse>.Reset();
        TimingBehavior<ComplexCommand, ComplexResponse>.Reset();
        
        using var serviceProvider = BuildServiceProviderWithMultipleBehaviors();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        
        var command = new ComplexCommand
        {
            Name = "Test",
            Age = 25,
            Tags = new List<string> { "tag1", "tag2" }
        };

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Test");
        result.ProcessedCount.Should().Be(2);
        
        // Verify behaviors were called
        ValidationBehavior<ComplexCommand, ComplexResponse>.CallCount.Should().Be(1);
        TimingBehavior<ComplexCommand, ComplexResponse>.LastExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task LoggingBehavior_Should_Log_Request_Properties()
    {
        // Arrange
        var complexLoggerMock = new Mock<ILogger<ComplexCommand>>();
        complexLoggerMock.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
        
        var services = new ServiceCollection();
        services.AddSingleton<IRequestHandler<ComplexCommand, ComplexResponse>, ComplexCommandHandler>();
        services.AddSingleton(typeof(ILogger<ComplexCommand>), complexLoggerMock.Object);
        
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });
        
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        
        var command = new ComplexCommand
        {
            Name = "TestUser",
            Age = 30,
            Tags = new List<string> { "admin", "user" }
        };

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        
        // Verify property logging
        complexLoggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Property Name")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
            
        complexLoggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Property Age")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Behaviors_Should_Handle_Cancellation_Token()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IRequestHandler<SlowCommand, string>, SlowCommandHandler>();
        
        // Simple setup without extra behaviors that might interfere
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });
        
        using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        
        var command = new SlowCommand
        {
            DelayMs = 5000, // 5 seconds delay
            Message = "This should be cancelled"
        };

        // Act & Assert
        using var cts = new CancellationTokenSource();
        var task = mediator.Send(command, cts.Token);
        
        // Cancel after a short delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            cts.Cancel();
        });
        
        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
    }
}
