namespace SharedKernel.Options;

/// <summary>
/// Represents the connection configuration for a message broker host.
/// </summary>
public sealed record MessageBrokerHost
{
    /// <summary>
    /// Gets the hostname or IP address of the message broker.
    /// </summary>
    public required string HostName { get; init; }

    /// <summary>
    /// Gets the port used to connect to the message broker.
    /// Defaults to <c>5674</c>, which is typically used for secure AMQP connections (AMQPS).
    /// </summary>
    public int Port { get; init; } = 5674;

    /// <summary>
    /// Gets the username used for authenticating with the message broker.
    /// </summary>
    public required string UserName { get; init; }

    /// <summary>
    /// Gets the password used for authenticating with the message broker.
    /// </summary>
    public required string Password { get; init; }
}
