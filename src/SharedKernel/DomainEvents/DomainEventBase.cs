using System;
using System.ComponentModel;
using System.Text;
using System.Text.Json.Serialization;

namespace SharedKernel.DomainEvents;

/// <summary>
/// Base record for all domain events, capturing the time the event occurred.
/// </summary>
public record DomainEventBase
{
    /// <summary>
    /// Gets the UTC timestamp when the event occurred. Survives a System.Text.Json round trip, so an
    /// event rehydrated from an outbox keeps its original time.
    /// </summary>
    [JsonInclude]
    public DateTimeOffset OccurrenceTime { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Old misspelled name of <see cref="OccurrenceTime"/>. Will be removed in 3.0.
    /// </summary>
    [Obsolete("Renamed to OccurrenceTime. This alias will be removed in 3.0.", DiagnosticId = "SK0001")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [JsonIgnore]
    public DateTimeOffset OccurranceTime => OccurrenceTime;

    // Keep the obsolete alias out of the record's ToString() output.
    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.Append("OccurrenceTime = ").Append(OccurrenceTime);
        return true;
    }
}

// Development Notes:
// - Serves as the base class for all domain events in the system.
// - Automatically timestamps each event with the UTC time at creation.
// - Enables consistent event tracking and ordering.
