using SharedKernel.Entities.Base;
using System.Threading;
using System.Threading.Tasks;

namespace SharedKernel.Abstractions.Data;

/// <summary>
/// Defines the contract for a generic repository that provides basic data operations such as Add, Remove, and Update.
/// </summary>
/// <typeparam name="T">The entity type, which must implement <see cref="IEntityBase"/>.</typeparam>
/// <remarks>
/// This repository is designed to be used with data access technologies like Entity Framework Core or any other ORM.
/// It will **attach** changes (e.g., add, update, or remove) to the underlying data context, but will **not commit** them.
/// Committing changes must be handled externally using a Unit of Work pattern or a transaction boundary.
/// </remarks>
public interface IRepository<T> where T : IEntityBase
{
    /// <summary>
    /// Asynchronously adds a new entity to the data context.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that returns the added entity.</returns>
    // Development Note:
    // This method queues the entity to be added to the data store.
    // Committing must be done via IUnitOfWork or similar mechanism.
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an existing entity for removal from the data context.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    // Development Note:
    // The removal is tracked but not executed until a commit is performed.
    void Remove(T entity);

    /// <summary>
    /// Marks an existing entity for update in the data context.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    // Development Note:
    // Changes are tracked, but persistence occurs only after committing the unit of work.
    void Update(T entity);
}
