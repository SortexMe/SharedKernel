using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.DependencyInjection;
using SharedKernel.Mediator.Tests.Commands;
using System.Diagnostics;
using System.Reflection;

namespace SharedKernel.Mediator.Tests;

public class PerformanceTests
{
    private readonly IServiceProvider serviceProvider;
    
    public PerformanceTests()
    {
        serviceProvider = BuildServiceProvider();
    }

    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IRequestHandler<PingCommand, string>, PingCommandHandler>();
        services.AddSingleton<IRequestHandler<ComplexCommand, ComplexResponse>, ComplexCommandHandler>();

        services.AddMediator(options =>
        {
            options.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Mediator_Should_Handle_High_Volume_Requests_Efficiently()
    {
        // Arrange
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        const int requestCount = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = new List<Task<string>>();
        for (int i = 0; i < requestCount; i++)
        {
            var command = new PingCommand($"Load-{i}");
            tasks.Add(mediator.Send(command));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(requestCount);
        results.Should().AllSatisfy(result => result.Should().StartWith("Pong: Load-"));
        
        // Performance assertion - should complete 1000 requests in reasonable time
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // 5 seconds max
        
        // Calculate throughput
        var throughput = requestCount / stopwatch.Elapsed.TotalSeconds;
        throughput.Should().BeGreaterThan(100); // At least 100 requests per second
    }

    [Fact]
    public async Task Mediator_Handler_Cache_Should_Improve_Performance()
    {
        // Arrange
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        const int warmupRequests = 10;
        const int measuredRequests = 100;

        // Warmup - populate handler cache
        for (int i = 0; i < warmupRequests; i++)
        {
            await mediator.Send(new PingCommand($"Warmup-{i}"));
        }

        // Act - Measure performance with cached handlers
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<string>>();
        
        for (int i = 0; i < measuredRequests; i++)
        {
            var command = new PingCommand($"Cached-{i}");
            tasks.Add(mediator.Send(command));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var averageTimePerRequest = stopwatch.ElapsedMilliseconds / (double)measuredRequests;
        averageTimePerRequest.Should().BeLessThan(10); // Less than 10ms per request on average
    }

    [Fact]
    public async Task Mediator_Should_Handle_Memory_Efficiently()
    {
        // Arrange
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        const int requestCount = 500;
        
        // Get initial memory usage
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var initialMemory = GC.GetTotalMemory(false);

        // Act
        var tasks = new List<Task<string>>();
        for (int i = 0; i < requestCount; i++)
        {
            var command = new PingCommand($"Memory-{i}");
            tasks.Add(mediator.Send(command));
        }

        await Task.WhenAll(tasks);

        // Force garbage collection and measure final memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(false);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        var memoryPerRequest = memoryIncrease / (double)requestCount;
        
        // Memory increase should be reasonable (less than 1KB per request)
        memoryPerRequest.Should().BeLessThan(1024);
    }

    [Fact]
    public async Task Mediator_Should_Handle_Concurrent_Different_Request_Types()
    {
        // Arrange
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        const int requestsPerType = 100;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var pingTasks = new List<Task<string>>();
        var complexTasks = new List<Task<ComplexResponse>>();

        // Create mixed concurrent requests
        for (int i = 0; i < requestsPerType; i++)
        {
            pingTasks.Add(mediator.Send(new PingCommand($"Concurrent-Ping-{i}")));
            complexTasks.Add(mediator.Send(new ComplexCommand
            {
                Name = $"User-{i}",
                Age = 20 + (i % 50),
                Tags = new List<string> { $"tag-{i}", $"category-{i % 10}" }
            }));
        }

        var pingResults = await Task.WhenAll(pingTasks);
        var complexResults = await Task.WhenAll(complexTasks);
        stopwatch.Stop();

        // Assert
        pingResults.Should().HaveCount(requestsPerType);
        complexResults.Should().HaveCount(requestsPerType);
        
        pingResults.Should().AllSatisfy(result => result.Should().StartWith("Pong: Concurrent-Ping-"));
        complexResults.Should().AllSatisfy(result => 
        {
            result.Success.Should().BeTrue();
            result.ProcessedCount.Should().Be(2);
        });

        // Performance should be reasonable for concurrent mixed requests
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000);
    }
}
