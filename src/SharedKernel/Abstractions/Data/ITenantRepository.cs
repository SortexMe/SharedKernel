using SharedKernel.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace SharedKernel.Abstractions.Data;

/// <summary>
/// Specialized repository for accessing tenant connection metadata.
/// Inherits basic CRUD functionality from <see cref="IRepository{TenantConnection}"/>.
/// </summary>
public interface ITenantRepository : IRepository<TenantConnection>
{
    /// <summary>
    /// Retrieves the tenant-specific connection string using the given connection identifier.
    /// </summary>
    /// <param name="connectionId">The identifier of the tenant connection. If null, the default will be used.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The connection string associated with the tenant, or null if not found.</returns>
    // Development Note:
    // This method supports multi-tenant scenarios by dynamically resolving connection strings.
    // It is typically used during request handling to determine which database to route to.
    Task<string?> GetTenantConnectionString(string? connectionId = null, CancellationToken cancellationToken = default);
}