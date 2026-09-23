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
    private static readonly string _requestName = typeof(TRequest).Name;

    private static readonly PropertyInfo[] _cachedProperties = Array.FindAll(
        typeof(TRequest).GetProperties(BindingFlags.Public | BindingFlags.Instance),
        static p => p.GetIndexParameters().Length == 0);

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();

    private static readonly string[] _secretMarkers =
    [
        "password", "passwd", "pwd", "secret", "token", "apikey", "api_key", "key", "credential", "connectionstring", "authorization",
    ];

    private readonly ILogger<TRequest> _logger;

    public LoggingBehavior(ILogger<TRequest> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static readonly Action<ILogger, string, Exception?> _logHandling =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, nameof(Handle)), "Handling {RequestName}");

    private static readonly Action<ILogger, string, double, Exception?> _logHandled =
        LoggerMessage.Define<string, double>(LogLevel.Information, new EventId(2, nameof(Handle)), "Handled {RequestName} in {ElapsedMilliseconds} ms");

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (!_logger.IsEnabled(LogLevel.Information))
            return next(cancellationToken);

        _logHandling(_logger, _requestName, null);

        if (_logger.IsEnabled(LogLevel.Debug))
            LogProperties(request);

        var start = Stopwatch.GetTimestamp();
        var task = next(cancellationToken);

        if (task.IsCompletedSuccessfully)
        {
            _logHandled(_logger, _requestName, Stopwatch.GetElapsedTime(start).TotalMilliseconds, null);
            return task;
        }

        return AwaitAndLogAsync(task, start);
    }

    private async Task<TResponse> AwaitAndLogAsync(Task<TResponse> task, long start)
    {
        try
        {
            return await task.ConfigureAwait(false);
        }
        finally
        {
            _logHandled(_logger, _requestName, Stopwatch.GetElapsedTime(start).TotalMilliseconds, null);
        }
    }

    /// <summary>
    /// Decides whether a property's value should be replaced with <c>***</c> in the debug log.
    /// The default matches common secret-bearing names case-insensitively.
    /// </summary>
    protected virtual bool ShouldRedact(PropertyInfo property)
    {
        foreach (var marker in _secretMarkers)
        {
            if (property.Name.Contains(marker, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static PropertyInfo[] GetProperties(Type type)
    {
        if (type == typeof(TRequest))
            return _cachedProperties;

        return _propertyCache.GetOrAdd(type, static t => Array.FindAll(
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance),
            static p => p.GetIndexParameters().Length == 0));
    }

    private void LogProperties(TRequest request)
    {
        var properties = GetProperties(request.GetType());
        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            object? value = ShouldRedact(property) ? "***" : property.GetValue(request);
            _logger.LogDebug("Property {Property} : {@Value}", property.Name, value);
        }
    }
}
