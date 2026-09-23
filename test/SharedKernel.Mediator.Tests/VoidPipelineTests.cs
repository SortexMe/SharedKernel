using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.DependencyInjection;
using SharedKernel.Mediator.Tests.Behaviors;
using SharedKernel.Mediator.Tests.Commands;
using System.Reflection;

namespace SharedKernel.Mediator.Tests;

/// <summary>
/// Covers RequestHandlerWrapperImpl&lt;TRequest&gt; — the pipeline for commands that implement the
/// non-generic <see cref="IRequest"/>. Every other test in the suite goes through the generic wrapper.
/// </summary>
public class VoidPipelineTests
{
    private static IServiceProvider Build(bool withBehavior)
    {
        var services = new ServiceCollection();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            if (withBehavior)
                options.AddOpenBehavior(typeof(PassThroughBehavior<,>));
        });
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Async_Handler_Exception_Should_Propagate_Through_Behavior()
    {
        var mediator = Build(withBehavior: true).GetRequiredService<IMediator>();

        var act = async () => await mediator.Send(new AsyncThrowingCommand());

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("async handler failure");
    }

    [Fact]
    public async Task Async_Handler_Exception_Should_Propagate_Without_Behavior()
    {
        var mediator = Build(withBehavior: false).GetRequiredService<IMediator>();

        var act = async () => await mediator.Send(new AsyncThrowingCommand());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Void_Command_Cancellation_Should_Flow_Through_Behavior()
    {
        TokenProbeCommandHandler.Reset();
        var mediator = Build(withBehavior: true).GetRequiredService<IMediator>();
        using var cts = new CancellationTokenSource();

        await mediator.Send(new TokenProbeCommand(), cts.Token);

        TokenProbeCommandHandler.LastTokenCanBeCanceled.Should().BeTrue(
            "the caller's token must reach the handler even when a behavior calls next() with no arguments");
    }

    [Fact]
    public async Task Typed_Request_Cancellation_Should_Flow_Through_Behavior()
    {
        var mediator = Build(withBehavior: true).GetRequiredService<IMediator>();
        using var cts = new CancellationTokenSource();

        var canBeCanceled = await mediator.Send(new TokenProbeQuery(), cts.Token);

        canBeCanceled.Should().BeTrue();
    }

    [Fact]
    public async Task Cancelling_Token_Should_Cancel_Void_Handler_Behind_Behavior()
    {
        var mediator = Build(withBehavior: true).GetRequiredService<IMediator>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var act = async () => await mediator.Send(new CancellableVoidCommand(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Behavior_Should_Wrap_Void_Command()
    {
        PassThroughBehavior<TokenProbeCommand, Unit>.Reset();
        var mediator = Build(withBehavior: true).GetRequiredService<IMediator>();

        await mediator.Send(new TokenProbeCommand());

        PassThroughBehavior<TokenProbeCommand, Unit>.CallCount.Should().Be(1);
    }
}
