using System;
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
    // Development Note:
    // The identifier is expected to be unique across entities.
    // MaxLength attribute is intended for database schema generation and validation.
    public Guid Id { get; set; }
}
