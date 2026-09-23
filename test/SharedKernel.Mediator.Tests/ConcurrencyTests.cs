using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.DependencyInjection;
using SharedKernel.Mediator.Tests.Behaviors;
using SharedKernel.Mediator.Tests.Commands;
using System.Collections.Concurrent;
using System.Reflection;

namespace SharedKernel.Mediator.Tests;

public class ConcurrencyTests
{
    public record AsyncEchoCommand(int Id, string Payload) : IRequest<string>;

    public class AsyncEchoCommandHandler : IRequestHandler<AsyncEchoCommand, string>
    {
        public async Task<string> Handle(AsyncEchoCommand request, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return $"Echo-{request.Id}:{request.Payload}";
        }
    }

    public record AsyncVoidCommand(int Id) : IRequest;

    public class AsyncVoidCommandHandler : IRequestHandler<AsyncVoidCommand>
    {
        public static readonly ConcurrentBag<int> CompletedIds = new();

        public async Task Handle(AsyncVoidCommand request, CancellationToken cancellationToken)
        {
            await Task.Yield();
            CompletedIds.Add(request.Id);
        }
    }

    [Fact]
    public async Task Concurrent_Synchronous_Requests_With_Single_Behavior_Should_Not_Corrupt_State()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IRequestHandler<PingCommand, string>, PingCommandHandler>();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.AddOpenBehavior(typeof(TimingBehavior<,>));
        });

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        const int concurrency = 200;
        var tasks = Enumerable.Range(0, concurrency).Select(async i =>
        {
            var command = new PingCommand($"Msg-{i}");
            var result = await mediator.Send(command);
            return (Expected: $"Pong: Msg-{i}", Actual: result);
        });

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(concurrency);
        foreach (var (expected, actual) in results)
        {
            actual.Should().Be(expected);
        }
    }

    [Fact]
    public async Task Concurrent_Asynchronous_Requests_With_Single_Behavior_Should_Not_Corrupt_State()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IRequestHandler<AsyncEchoCommand, string>, AsyncEchoCommandHandler>();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.AddOpenBehavior(typeof(TimingBehavior<,>));
        });

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        const int concurrency = 200;
        var tasks = Enumerable.Range(0, concurrency).Select(async i =>
        {
            var command = new AsyncEchoCommand(i, $"Data-{i}");
            var result = await mediator.Send(command);
            return (Expected: $"Echo-{i}:Data-{i}", Actual: result);
        });

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(concurrency);
        foreach (var (expected, actual) in results)
        {
            actual.Should().Be(expected);
        }
    }

    [Fact]
    public async Task Concurrent_Asynchronous_Requests_With_Multiple_Behaviors_Should_Not_Corrupt_State()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IRequestHandler<AsyncEchoCommand, string>, AsyncEchoCommandHandler>();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.AddOpenBehavior(typeof(ValidationBehavior<,>));
            options.AddOpenBehavior(typeof(TimingBehavior<,>));
        });

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        const int concurrency = 200;
        var tasks = Enumerable.Range(0, concurrency).Select(async i =>
        {
            var command = new AsyncEchoCommand(i, $"Data-{i}");
            var result = await mediator.Send(command);
            return (Expected: $"Echo-{i}:Data-{i}", Actual: result);
        });

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(concurrency);
        foreach (var (expected, actual) in results)
        {
            actual.Should().Be(expected);
        }
    }

    [Fact]
    public async Task Concurrent_Reentrant_Requests_Should_Execute_Without_Deadlock_Or_Corruption()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IRequestHandler<ReentrantPingCommand, string>, ReentrantPingCommandHandler>();
        services.AddSingleton<IRequestHandler<PingCommand, string>, PingCommandHandler>();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.AddOpenBehavior(typeof(TimingBehavior<,>));
        });

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        const int concurrency = 100;
        var tasks = Enumerable.Range(0, concurrency).Select(async i =>
        {
            var result = await mediator.Send(new ReentrantPingCommand($"Outer-{i}"));
            return result;
        });

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(concurrency);
        results.Should().AllSatisfy(r => r.Should().Be("Nested: Pong: Nested"));
    }

    [Fact]
    public async Task Concurrent_Void_Requests_With_Behaviors_Should_Complete_All_Tasks()
    {
        // Arrange
        AsyncVoidCommandHandler.CompletedIds.Clear();
        var services = new ServiceCollection();
        services.AddSingleton<IRequestHandler<AsyncVoidCommand>, AsyncVoidCommandHandler>();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.AddOpenBehavior(typeof(TimingBehavior<,>));
        });

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        const int concurrency = 200;
        var tasks = Enumerable.Range(0, concurrency).Select(async i =>
        {
            await mediator.Send(new AsyncVoidCommand(i));
        });

        // Act
        await Task.WhenAll(tasks);

        // Assert
        AsyncVoidCommandHandler.CompletedIds.Should().HaveCount(concurrency);
        var expectedSet = Enumerable.Range(0, concurrency).ToHashSet();
        AsyncVoidCommandHandler.CompletedIds.ToHashSet().Should().BeEquivalentTo(expectedSet);
    }
}
