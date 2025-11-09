using SharedKernel.Enumerations;
using System;

namespace SharedKernel.Entities;

/// <summary>
/// Represents a persistent message for domain events that failed to publish to the message broker.
/// This class can be used as a retry queue by a background service to attempt re-publishing events
/// when no inbox/outbox mechanism is implemented.
/// <para>
/// Alternatively, it can serve as a permanent persistence for published domain events if the message broker
/// does not provide persistence or visibility of published events.
/// </para>
/// <para>
/// It is recommended to delete these messages after successful publishing unless you need
/// to maintain visibility or audit logs of the published domain events.
/// </para>
/// </summary>
public class DomainEventMessage
{
    /// <summary>
    /// Gets the unique identifier of this domain event message.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the type name of the domain event.
    /// </summary>
    public string Type { get; private set; } = null!;

    /// <summary>
    /// Gets the serialized content of the domain event.
    /// </summary>
    public string Content { get; private set; } = null!;

    /// <summary>
    /// Gets the date and time when the domain event originally occurred.
    /// </summary>
    public DateTimeOffset OccurranceTime { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the last date and time this event was processed.
    /// </summary>
    public DateTimeOffset? LastProcessedTime { get; private set; }

    /// <summary>
    /// Gets any error message associated with the last processing failure.
    /// </summary>
    public string? Error { get; private set; }

    /// <summary>
    /// Gets the number of times this domain event message has been processed.
    /// </summary>
    public int? ProcessedTimes { get; private set; }

    /// <summary>
    /// Gets the current processing status of the domain event message.
    /// </summary>
    public DomainEventStatus Status { get; private set; } = DomainEventStatus.New;

    /// <summary>
    /// Internal constructor to instantiate a domain event message.
    /// </summary>
    /// <param name="id">The unique identifier of the domain event message.</param>
    /// <param name="type">The type name of the domain event.</param>
    /// <param name="content">The serialized content of the domain event.</param>
    internal DomainEventMessage(Guid id, string type, string content)
    {
        Id = id;
        Type = type;
        Content = content;
    }

    /// <summary>
    /// Creates a new domain event message instance.
    /// </summary>
    /// <param name="id">The unique identifier of the domain event message.</param>
    /// <param name="type">The type name of the domain event.</param>
    /// <param name="content">The serialized content of the domain event.</param>
    /// <returns>A new <see cref="DomainEventMessage"/> instance.</returns>
    public static DomainEventMessage CreateMessage(Guid id, string type, string content)
    {
        var message = new DomainEventMessage(id, type, content);
        return message;
    }

    /// <summary>
    /// Marks the domain event message as skipped during processing.
    /// Updates status, last processed time, and increments the processed times counter.
    /// </summary>
    public void SkipDomainEvent()
    {
        Status = DomainEventStatus.Skipped;
        LastProcessedTime = DateTimeOffset.UtcNow;
        ProcessedTimes++;
    }

    /// <summary>
    /// Marks the domain event message as successfully dispatched.
    /// Updates status, last processed time, and increments the processed times counter.
    /// <para>
    /// After successful dispatch, it is recommended to remove this message unless it is
    /// required for auditing or visibility purposes.
    /// </para>
    /// </summary>
    public void DispatchDomainEvent()
    {
        Status = DomainEventStatus.Completed;
        LastProcessedTime = DateTimeOffset.UtcNow;
        ProcessedTimes++;
    }

    /// <summary>
    /// Marks the domain event message as failed during dispatch and records the error.
    /// Updates status, last processed time, increments the processed times counter, and sets the error message.
    /// <para>
    /// Failed messages can be retried by a background service for eventual successful dispatch.
    /// </para>
    /// </summary>
    /// <param name="error">The error message describing the failure.</param>
    public void DispatchFailure(string error)
    {
        Status = DomainEventStatus.Failed;
        LastProcessedTime = DateTimeOffset.UtcNow;
        ProcessedTimes++;
        Error = error;
    }
}
