using Microsoft.Extensions.Logging;
using SharedKernel.Abstractions.CQRS;
using SharedKernel.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SharedKernel.Mediator.Behaviors;

/// <summary>
/// Pipeline behavior for logging details about requests and responses.
/// Logs request properties and timing information.
/// 
/// Note:
/// This behavior uses reflection to log properties, which might impact performance.
/// If you are using OpenTelemetry or another distributed tracing/logging mechanism,
/// you may not need to adopt this logging behavior, as it can duplicate or interfere
/// with telemetry instrumentation.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly ILogger<TRequest> logger;

    public LoggingBehavior(ILogger<TRequest> logger)
    {
        this.logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        Stopwatch? stopwatch = null;

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);

            // Reflection is used here to enumerate properties and their values.
            // This could be a performance concern in high-throughput scenarios.
            Type myType = request.GetType();
            IList<PropertyInfo> props = myType.GetProperties();
            foreach (PropertyInfo prop in props)
            {
                object? propValue = prop?.GetValue(request, null);
                logger.LogInformation("Property {Property} : {@Value}", prop?.Name, propValue);
            }

            stopwatch = Stopwatch.StartNew();
        }

        var response = await next();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Handled {RequestName} in {ms} ms", typeof(TRequest).Name, stopwatch?.ElapsedMilliseconds);
            stopwatch?.Stop();
        }

        return response;
    }
}
