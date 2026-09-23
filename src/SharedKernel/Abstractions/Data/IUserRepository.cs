using SharedKernel.Entities.Auth;

namespace SharedKernel.Abstractions.Data;

/// <summary>
/// Repository abstraction for managing user-related persistence operations.
/// Inherits from <see cref="IRepository{ApplicationUser}"/> for standard CRUD behavior.
/// </summary>
/// <remarks>
/// Use this interface to define additional user-specific data access operations if needed.
/// </remarks>
public interface IUserRepository : IRepository<ApplicationUser>
{
    // Development Note:
    // This interface currently relies solely on the generic IRepository functionality.
}
