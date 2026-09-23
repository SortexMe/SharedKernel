using SharedKernel.DomainEvents;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    private readonly List<DomainEventBase> _domainEvents = [];
    private ReadOnlyCollection<DomainEventBase>? _readOnlyView;

    /// <summary>
    /// Gets a read-only view of the domain events registered by the entity.
    /// </summary>
    [NotMapped] // This property is not mapped to any database column.
    public IReadOnlyCollection<DomainEventBase> DomainEvents => _readOnlyView ??= _domainEvents.AsReadOnly();

    /// <summary>
    /// Registers a new domain event to be dispatched later.
    /// </summary>
    /// <param name="domainEvent">The domain event instance to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domainEvent"/> is null.</exception>
    // Development Note:
    // Events registered here are typically dispatched by an external dispatcher after persistence.
    public void RegisterDomainEvent(DomainEventBase domainEvent)
    {
        if (domainEvent is null)
            throw new ArgumentNullException(nameof(domainEvent));

        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events from the entity.
    /// </summary>
    // Development Note:
    // Called after domain events are dispatched to prevent duplicate processing.
    public void ClearDomainEvents() => _domainEvents.Clear();
}
