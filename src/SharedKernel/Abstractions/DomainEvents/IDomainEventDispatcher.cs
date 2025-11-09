using SharedKernel.DomainEvents;
using SharedKernel.Entities.Base;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SharedKernel.Abstractions.DomainEvents;

/// <summary>
/// Defines a contract for dispatching domain events that originate from aggregate roots or domain entities.
/// </summary>
/// <remarks>
/// This interface supports both bulk dispatching of events from multiple entities and dispatching of individual events.
/// Typically used in conjunction with a Unit of Work or after persistence has been completed.
/// </remarks>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches all domain events found within the provided list of domain entities.
    /// </summary>
    /// <param name="entitiesWithEvents">The domain entities containing raised events.</param>
    /// <param name="cancellationToken">Token for canceling the operation.</param>
    /// <returns>A task representing the asynchronous operation, returning the dispatched events.</returns>
    // Development Note:
    // This is used to trigger domain event handlers after changes to aggregates are saved.
    // Events are usually cleared from the entities after dispatching.
    Task<IEnumerable<DomainEventBase>> DispatchEvents(IEnumerable<DomainEntityBase> entitiesWithEvents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatches a single domain event immediately.
    /// </summary>
    /// <param name="domainEvent">The event to dispatch.</param>
    /// <param name="cancellationToken">Token for canceling the operation.</param>
    /// <returns>A task representing the asynchronous operation, returning the dispatched event.</returns>
    // Development Note:
    // Use this to trigger an event outside the normal aggregate/event-raising flow.
    Task<DomainEventBase> DispatchEvent(DomainEventBase domainEvent, CancellationToken cancellationToken = default);
}
