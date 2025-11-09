using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.DependencyInjection;
using System.Reflection;

namespace SharedKernel.Mediator.Tests.Commands;

public class ServiceFactoryTests
{
    private readonly IServiceProvider serviceProvider;
    public ServiceFactoryTests()
    {
        serviceProvider = BuildServiceProvider();
    }

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Register command and handler
        services.AddSingleton<IRequestHandler<PingCommand, string>, PingCommandHandler>();

        // Register mediator
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        return services.BuildServiceProvider();
    }

    [Fact]
    public void Should_Throw_When_Mediator_Not_Resolved()
    {
        var mediator = serviceProvider.GetService<IMediator>();
        mediator.Should().NotBeNull();
    }

    [Fact]
    public async Task Mediator_Should_Throw_If_Handler_Missing()
    {
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var command = new NoHandlerCommand("Missing");

        // Act
        Func<Task> act = async () => await mediator.Send(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
