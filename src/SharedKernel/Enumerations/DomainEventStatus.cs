namespace SharedKernel.Enumerations;

/// <summary>
/// Represents the processing status of a domain event message.
/// </summary>
public enum DomainEventStatus
{
    /// <summary>
    /// The domain event is newly created and not yet processed.
    /// </summary>
    New = 0,

    /// <summary>
    /// Processing the domain event has failed.
    /// </summary>
    Failed = 1,

    /// <summary>
    /// The domain event processing was skipped intentionally.
    /// </summary>
    Skipped = 2,

    /// <summary>
    /// The domain event has been successfully processed.
    /// </summary>
    Completed = 3,
}
