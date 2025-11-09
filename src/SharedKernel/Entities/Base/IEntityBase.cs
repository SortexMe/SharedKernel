using System.ComponentModel.DataAnnotations;

namespace SharedKernel.Entities.Base;

/// <summary>
/// Base interface for all entities, requiring a unique identifier.
/// </summary>
public interface IEntityBase
{
    /// <summary>
    /// Gets or sets the unique identifier of the entity.
    /// </summary>
    [MaxLength(100)]
    // Development Note:
    // The identifier is expected to be unique across entities.
    // MaxLength attribute is intended for database schema generation and validation.
    public string Id { get; set; }
}
