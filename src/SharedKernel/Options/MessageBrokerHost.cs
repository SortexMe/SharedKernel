using System.Text;

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
    /// Defaults to <c>5671</c>, the standard AMQPS (TLS) port; plain AMQP uses <c>5672</c>.
    /// </summary>
    public int Port { get; init; } = 5671;

    /// <summary>
    /// Gets the username used for authenticating with the message broker.
    /// </summary>
    public required string UserName { get; init; }

    /// <summary>
    /// Gets the password used for authenticating with the message broker.
    /// </summary>
    public required string Password { get; init; }

    // Sealed records get a private PrintMembers; keep the password out of ToString().
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("HostName = ").Append(HostName)
               .Append(", Port = ").Append(Port)
               .Append(", UserName = ").Append(UserName)
               .Append(", Password = ***");
        return true;
    }
}
