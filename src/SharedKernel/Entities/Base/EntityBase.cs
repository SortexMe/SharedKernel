using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedKernel.Entities.Base;

/// <summary>
/// Abstract base class for all persistent entities.
/// Provides identity-based equality logic and a unique identifier.
/// </summary>
/// <remarks>
/// Implements <see cref="IEntityBase"/> and <see cref="IEquatable{EntityBase}"/> to ensure value equality based on the entity's <see cref="Id"/>.
/// Transient entities — those whose <see cref="Id"/> is still <see cref="Guid.Empty"/> — are only equal to themselves,
/// because they have no identity yet. This base class is intended for entities that do not raise domain events.
/// </remarks>
public abstract class EntityBase : IEntityBase, IEquatable<EntityBase>
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    /// <summary>
    /// Compares two entity instances for equality based on their <see cref="Id"/>.
    /// </summary>
    /// <param name="first">The first entity to compare.</param>
    /// <param name="second">The second entity to compare.</param>
    /// <returns>True if both are null, or both are non-null with the same <see cref="Id"/>.</returns>
    public static bool operator ==(EntityBase? first, EntityBase? second)
    {
        if (ReferenceEquals(first, second))
            return true;

        if (first is null || second is null)
            return false;

        return first.Equals(second);
    }

    /// <summary>
    /// Determines if two entities are not equal.
    /// </summary>
    /// <param name="first">The first entity.</param>
    /// <param name="second">The second entity.</param>
    /// <returns>True if the entities are not equal; otherwise, false.</returns>
    public static bool operator !=(EntityBase? first, EntityBase? second) => !(first == second);

    /// <summary>
    /// Determines whether the specified object is equal to the current entity.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns>True if the object is an <see cref="EntityBase"/> of the same type with the same, non-empty <see cref="Id"/>; otherwise, false.</returns>
    public override bool Equals(object? obj) => Equals(obj as EntityBase);

    /// <summary>
    /// Indicates whether the current entity is equal to another <see cref="EntityBase"/>.
    /// </summary>
    /// <param name="other">The entity to compare with.</param>
    /// <returns>True if both entities are of the same type and have the same, non-empty <see cref="Id"/>; otherwise, false.</returns>
    public bool Equals(EntityBase? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (EntityTypeResolver.GetEntityType(this) != EntityTypeResolver.GetEntityType(other))
            return false;

        // Two unsaved entities share Guid.Empty without being the same thing.
        if (Id == Guid.Empty || other.Id == Guid.Empty)
            return false;

        return other.Id == Id;
    }

    /// <summary>
    /// Returns a hash code for the current entity.
    /// </summary>
    /// <returns>A hash code based on the <see cref="Id"/>.</returns>
    public override int GetHashCode() => Id.GetHashCode();
}
