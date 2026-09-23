using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.DependencyInjection;
using SharedKernel.Mediator.Tests.Commands;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SharedKernel.Mediator.Tests;

/// <summary>
/// The wrapper cache is owned by the container, not by the process. These tests pin both halves of that:
/// it is shared widely enough to stay fast, and scoped tightly enough not to leak between containers.
/// </summary>
public class WrapperCacheTests
{
    private static ServiceProvider BuildContainer()
    {
        var services = new ServiceCollection();
        services.AddMediator(options => options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Mediator_Should_Hold_No_Static_Mutable_State()
    {
        var staticFields = typeof(SharedKernel.Mediator.Mediator)
            .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => !f.IsLiteral)
            .ToArray();

        staticFields.Should().BeEmpty(
            "process-wide state leaks between containers and pins types against AssemblyLoadContext unload");
    }

    [Fact]
    public void Cache_Is_Registered_As_A_Singleton()
    {
        var services = new ServiceCollection();
        services.AddMediator(options => options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        var descriptor = services.Single(d => d.ServiceType == typeof(RequestHandlerWrapperCache));

        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public async Task Cache_Is_Shared_Across_Scopes_Within_One_Container()
    {
        using var container = BuildContainer();
        var cache = container.GetRequiredService<RequestHandlerWrapperCache>();
        cache.Clear();

        using (var scope = container.CreateScope())
            await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new PingCommand("a"));

        var afterFirstScope = cache.Count;

        for (var i = 0; i < 5; i++)
        {
            using var scope = container.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new PingCommand("b"));
        }

        afterFirstScope.Should().Be(1);
        cache.Count.Should().Be(1, "later scopes must reuse the wrapper, not rebuild it via reflection");
    }

    [Fact]
    public async Task Each_Container_Gets_Its_Own_Cache()
    {
        using var first = BuildContainer();
        using var second = BuildContainer();

        var firstCache = first.GetRequiredService<RequestHandlerWrapperCache>();
        var secondCache = second.GetRequiredService<RequestHandlerWrapperCache>();

        firstCache.Should().NotBeSameAs(secondCache);

        await first.GetRequiredService<IMediator>().Send(new PingCommand("x"));

        firstCache.Count.Should().Be(1);
        secondCache.Count.Should().Be(0, "one container's cache must not populate another's");
    }

    [Fact]
    public async Task A_Poisoning_Attempt_In_One_Container_Does_Not_Affect_Another()
    {
        using var poisoned = BuildContainer();
        using var clean = BuildContainer();

        // A widened Send caches a wrapper under (CovariantQuery, object) and legitimately finds no handler.
        var widened = async () => await poisoned.GetRequiredService<IMediator>().Send<object>(new CovariantQuery());
        await widened.Should().ThrowAsync<InvalidOperationException>();

        // Both the same container and a separate one must still dispatch correctly.
        (await poisoned.GetRequiredService<IMediator>().Send(new CovariantQuery())).Should().Be("covariant");
        (await clean.GetRequiredService<IMediator>().Send(new CovariantQuery())).Should().Be("covariant");
    }

    [Fact]
    public async Task Disposing_A_Container_Releases_Its_Cached_Wrappers()
    {
        var (weakCache, _) = await BuildUseAndDropContainer();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        weakCache.IsAlive.Should().BeFalse(
            "the cache must die with its container so an unloadable AssemblyLoadContext is not pinned");
    }

    // Kept out of the test body so the container and cache locals cannot stay rooted on the stack frame.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(WeakReference Cache, int Count)> BuildUseAndDropContainer()
    {
        var container = BuildContainer();
        var cache = container.GetRequiredService<RequestHandlerWrapperCache>();
        await container.GetRequiredService<IMediator>().Send(new PingCommand("y"));

        var count = cache.Count;
        var weak = new WeakReference(cache);

        await container.DisposeAsync();
        return (weak, count);
    }

    [Fact]
    public async Task Manually_Constructed_Mediator_Still_Works_Without_A_Registered_Cache()
    {
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<PingCommand, string>, PingCommandHandler>();
        using var provider = services.BuildServiceProvider();

        var mediator = new SharedKernel.Mediator.Mediator(provider);

        (await mediator.Send(new PingCommand("z"))).Should().Be("Pong: z");
    }

    [Fact]
    public async Task Mediator_Accepts_An_Explicit_Cache()
    {
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<PingCommand, string>, PingCommandHandler>();
        using var provider = services.BuildServiceProvider();
        var cache = new RequestHandlerWrapperCache();

        var mediator = new SharedKernel.Mediator.Mediator(provider, cache);
        await mediator.Send(new PingCommand("w"));

        cache.Count.Should().Be(1);
    }
}
