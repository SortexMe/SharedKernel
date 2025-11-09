using System;

namespace SharedKernel.DomainEvents;

/// <summary>
/// Base record for all domain events, capturing the time the event occurred.
/// </summary>
public record DomainEventBase
{
    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    public DateTimeOffset OccurranceTime { get; private set; } = DateTimeOffset.UtcNow;
}

// Development Notes:
// - Serves as the base class for all domain events in the system.
// - Automatically timestamps each event with the UTC time at creation.
// - Enables consistent event tracking and ordering.
