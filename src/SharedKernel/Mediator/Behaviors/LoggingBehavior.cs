using Microsoft.Extensions.Logging;
using SharedKernel.Abstractions.CQRS;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SharedKernel.Mediator.Behaviors;

/// <summary>
/// Pipeline behavior that logs the name and duration of every request.
/// <para>
/// At <see cref="LogLevel.Information"/> only the request name and elapsed time are logged.
/// At <see cref="LogLevel.Debug"/> the request's public properties are also logged, except any whose
/// name suggests a secret (password, token, secret, key, credential, connection string), which are
/// written as <c>***</c>. Override <see cref="ShouldRedact"/> to change that rule.
/// </para>
/// <para>
/// Property logging uses reflection and can be a cost in high-throughput scenarios. If you already have
/// OpenTelemetry or similar tracing, this behavior may duplicate what your instrumentation records.
/// </para>
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private static readonly string[] SecretMarkers =
    [
        "password", "passwd", "pwd", "secret", "token", "apikey", "api_key", "key", "credential", "connectionstring", "authorization",
    ];

    private readonly ILogger<TRequest> _logger;

    public LoggingBehavior(ILogger<TRequest> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var requestName = typeof(TRequest).Name;

        if (!_logger.IsEnabled(LogLevel.Information))
            return await next(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Handling {RequestName}", requestName);

        if (_logger.IsEnabled(LogLevel.Debug))
            LogProperties(request);

        var start = Stopwatch.GetTimestamp();

        try
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _logger.LogInformation("Handled {RequestName} in {ElapsedMilliseconds} ms", requestName, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
    }

    /// <summary>
    /// Decides whether a property's value should be replaced with <c>***</c> in the debug log.
    /// The default matches common secret-bearing names case-insensitively.
    /// </summary>
    protected virtual bool ShouldRedact(PropertyInfo property)
    {
        foreach (var marker in SecretMarkers)
        {
            if (property.Name.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void LogProperties(TRequest request)
    {
        foreach (var property in request.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0)
                continue;

            object? value = ShouldRedact(property) ? "***" : property.GetValue(request);
            _logger.LogDebug("Property {Property} : {@Value}", property.Name, value);
        }
    }
}
