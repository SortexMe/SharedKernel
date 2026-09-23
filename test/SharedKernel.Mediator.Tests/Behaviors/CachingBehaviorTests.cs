using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.DependencyInjection;
using SharedKernel.Mediator.Behaviors;
using SharedKernel.Options;
using System.Reflection;

namespace SharedKernel.Mediator.Tests.Behaviors;

public record GetUserProfileQuery(int UserId) : ICacheableRequest<string>
{
    public string CacheKey => $"user-profile:{UserId}";
    public CacheEntryOptions? CacheOptions { get; init; }
}

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, string>
{
    public static int ExecutionCount { get; private set; }

    public Task<string> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        ExecutionCount++;
        return Task.FromResult($"Profile for User {request.UserId}");
    }

    public static void Reset() => ExecutionCount = 0;
}

public record NonCacheableQuery(string Value) : IRequest<string>;

public class NonCacheableQueryHandler : IRequestHandler<NonCacheableQuery, string>
{
    public static int ExecutionCount { get; private set; }

    public Task<string> Handle(NonCacheableQuery request, CancellationToken cancellationToken)
    {
        ExecutionCount++;
        return Task.FromResult($"NonCached: {request.Value}");
    }

    public static void Reset() => ExecutionCount = 0;
}

public class CachingBehaviorTests
{
    [Fact]
    public async Task Cache_Hit_Should_Bypass_Handler_Execution()
    {
        // Arrange
        GetUserProfileQueryHandler.Reset();
        var cacheProvider = new MemoryResponseCacheProvider();

        var services = new ServiceCollection();
        services.AddSingleton<IResponseCacheProvider>(cacheProvider);
        services.AddSingleton<IRequestHandler<GetUserProfileQuery, string>, GetUserProfileQueryHandler>();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.AddOpenBehavior(typeof(CachingBehavior<,>));
        });

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act - First call: Cache miss, handler executes
        var result1 = await mediator.Send(new GetUserProfileQuery(42));

        // Act - Second call: Cache hit, handler is NOT executed
        var result2 = await mediator.Send(new GetUserProfileQuery(42));

        // Assert
        result1.Should().Be("Profile for User 42");
        result2.Should().Be("Profile for User 42");
        GetUserProfileQueryHandler.ExecutionCount.Should().Be(1);
    }

    [Fact]
    public async Task Non_Cacheable_Request_Should_Pass_Through_Directly()
    {
        // Arrange
        NonCacheableQueryHandler.Reset();
        var cacheProvider = new MemoryResponseCacheProvider();

        var services = new ServiceCollection();
        services.AddSingleton<IResponseCacheProvider>(cacheProvider);
        services.AddSingleton<IRequestHandler<NonCacheableQuery, string>, NonCacheableQueryHandler>();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.AddOpenBehavior(typeof(CachingBehavior<,>));
        });

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result1 = await mediator.Send(new NonCacheableQuery("test"));
        var result2 = await mediator.Send(new NonCacheableQuery("test"));

        // Assert
        result1.Should().Be("NonCached: test");
        result2.Should().Be("NonCached: test");
        NonCacheableQueryHandler.ExecutionCount.Should().Be(2);
    }

    [Fact]
    public async Task Request_Without_Cache_Provider_Should_Execute_Handler_Normally()
    {
        // Arrange - No IResponseCacheProvider registered
        GetUserProfileQueryHandler.Reset();

        var services = new ServiceCollection();
        services.AddSingleton<IRequestHandler<GetUserProfileQuery, string>, GetUserProfileQueryHandler>();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.AddOpenBehavior(typeof(CachingBehavior<,>));
        });

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(new GetUserProfileQuery(99));

        // Assert
        result.Should().Be("Profile for User 99");
        GetUserProfileQueryHandler.ExecutionCount.Should().Be(1);
    }

    [Fact]
    public async Task Expired_Cache_Entry_Should_Re_Execute_Handler()
    {
        // Arrange
        GetUserProfileQueryHandler.Reset();
        var cacheProvider = new MemoryResponseCacheProvider();

        var services = new ServiceCollection();
        services.AddSingleton<IResponseCacheProvider>(cacheProvider);
        services.AddSingleton<IRequestHandler<GetUserProfileQuery, string>, GetUserProfileQueryHandler>();
        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            options.AddOpenBehavior(typeof(CachingBehavior<,>));
        });

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var query = new GetUserProfileQuery(1)
        {
            CacheOptions = new CacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(50)
            }
        };

        // Act
        var result1 = await mediator.Send(query);
        GetUserProfileQueryHandler.ExecutionCount.Should().Be(1);

        // Wait for expiration
        await Task.Delay(80);

        var result2 = await mediator.Send(query);

        // Assert
        result1.Should().Be("Profile for User 1");
        result2.Should().Be("Profile for User 1");
        GetUserProfileQueryHandler.ExecutionCount.Should().Be(2);
    }

    [Fact]
    public async Task MemoryResponseCacheProvider_RemoveAsync_Evicts_Entry()
    {
        // Arrange
        var cache = new MemoryResponseCacheProvider();
        await cache.SetAsync("key1", "value1");

        // Act & Assert
        var (found1, val1) = await cache.TryGetAsync<string>("key1");
        found1.Should().BeTrue();
        val1.Should().Be("value1");

        await cache.RemoveAsync("key1");

        var (found2, _) = await cache.TryGetAsync<string>("key1");
        found2.Should().BeFalse();
    }
}
