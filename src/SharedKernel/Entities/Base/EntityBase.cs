using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedKernel.Entities.Base;

/// <summary>
/// Abstract base class for all persistent entities.
/// Provides identity-based equality logic and a unique identifier.
/// </summary>
/// <remarks>
/// Implements <see cref="IEntityBase"/> and <see cref="IEquatable{EntityBase}"/> to ensure value equality based on the entity's <see cref="Id"/>.
/// This base class is intended for entities that do not raise domain events.
/// </remarks>
public abstract class EntityBase : IEntityBase, IEquatable<EntityBase>
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    [MaxLength(100)]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Compares two entity instances for equality based on their <see cref="Id"/>.
    /// </summary>
    /// <param name="first">The first entity to compare.</param>
    /// <param name="second">The second entity to compare.</param>
    /// <returns>True if both entities are non-null and have the same <see cref="Id"/>; otherwise, false.</returns>
    // Development Note:
    // Enables use of '==' operator for comparing entity instances by identity.
    public static bool operator ==(EntityBase? first, EntityBase? second)
    {
        return first is not null && second is not null && first.Equals(second);
    }

    /// <summary>
    /// Determines if two entities are not equal.
    /// </summary>
    /// <param name="first">The first entity.</param>
    /// <param name="second">The second entity.</param>
    /// <returns>True if the entities are not equal; otherwise, false.</returns>
    public static bool operator !=(EntityBase? first, EntityBase? second)
    {
        return !(first == second);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current entity.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns>True if the object is an <see cref="EntityBase"/> with the same <see cref="Id"/>; otherwise, false.</returns>
    // Development Note:
    // Prevents accidental reference-based equality by overriding Equals.
    public override bool Equals(object? obj)
    {
        if (obj == null)
            return false;

        if (obj.GetType() != GetType())
            return false;

        if (obj is not EntityBase entity)
            return false;

        return entity.Id == Id;
    }

    /// <summary>
    /// Indicates whether the current entity is equal to another <see cref="EntityBase"/>.
    /// </summary>
    /// <param name="other">The entity to compare with.</param>
    /// <returns>True if both entities are of the same type and have the same <see cref="Id"/>; otherwise, false.</returns>
    public bool Equals(EntityBase? other)
    {
        if (other == null)
            return false;

        if (other.GetType() != GetType())
            return false;

        return other.Id == Id;
    }

    /// <summary>
    /// Returns a hash code for the current entity.
    /// </summary>
    /// <returns>A hash code based on the <see cref="Id"/>.</returns>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
