using SharedKernel.DomainEvents;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedKernel.Entities.Base;

/// <summary>
/// Base class providing domain event tracking capabilities for entities.
/// </summary>
/// <remarks>
/// Maintains a collection of domain events raised by the entity.
/// This class is intended to be inherited by aggregate roots or entities that raise domain events.
/// </remarks>
public abstract class HasDomainEventsBase
{
    // Backing list for domain events; initialized to empty list.
    private List<DomainEventBase> _domainEvents = new List<DomainEventBase>();

    /// <summary>
    /// Gets a read-only collection of domain events that have been registered by the entity.
    /// </summary>
    [NotMapped] // This property is not mapped to any database column.
    public IReadOnlyCollection<DomainEventBase> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Registers a new domain event to be dispatched later.
    /// </summary>
    /// <param name="domainEvent">The domain event instance to register.</param>
    // Development Note:
    // Events registered here are typically dispatched by an external dispatcher after persistence.
    public void RegisterDomainEvent(DomainEventBase domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Clears all domain events from the entity.
    /// </summary>
    // Development Note:
    // Called after domain events are dispatched to prevent duplicate processing.
    public void ClearDomainEvents() => _domainEvents.Clear();
}
