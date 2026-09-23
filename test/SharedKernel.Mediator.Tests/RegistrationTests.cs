using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.DependencyInjection;
using SharedKernel.Mediator.Tests.Commands;
using System.Reflection;

namespace SharedKernel.Mediator.Tests;

public class RegistrationTests
{
    [Fact]
    public async Task Open_Generic_Handler_Should_Be_Registered_And_Resolved()
    {
        var services = new ServiceCollection();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.RegisterGenericHandlers = true;
        });
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        var result = await mediator.Send(new GenericQuery<Alpha>());

        result.Should().Be(nameof(Alpha));
    }

    [Fact]
    public void Handlers_Should_Honour_Configured_Lifetime()
    {
        var services = new ServiceCollection();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.Lifetime = ServiceLifetime.Singleton;
        });

        var handler = services.Single(d => d.ServiceType == typeof(IRequestHandler<PingCommand, string>));
        var voidHandler = services.Single(d => d.ServiceType == typeof(IRequestHandler<TokenProbeCommand>));

        handler.Lifetime.Should().Be(ServiceLifetime.Singleton);
        voidHandler.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void RegistrationTimeout_Zero_Should_Disable_The_Timeout()
    {
        var services = new ServiceCollection();

        var act = () => services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.RegisterGenericHandlers = true;
            options.RegistrationTimeout = 0;
        });

        act.Should().NotThrow("the documented meaning of 0 is 'no timeout'");
    }

    [Fact]
    public async Task Send_With_Wider_Response_Type_Should_Not_Poison_Cache_For_Correct_Calls()
    {
        var services = new ServiceCollection();
        services.AddMediator(options => options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // IRequest<out T> is covariant, so this compiles and legitimately fails to find IRequestHandler<CovariantQuery, object>.
        var widened = async () => await mediator.Send<object>(new CovariantQuery());
        await widened.Should().ThrowAsync<InvalidOperationException>();

        // The correctly-typed call afterwards must still work.
        var result = await mediator.Send(new CovariantQuery());

        result.Should().Be("covariant");
    }
}

public interface IGenericMarker { }
public class Alpha : IGenericMarker { }
public class Beta : IGenericMarker { }
