using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedKernel.Entities.Base;

/// <summary>
/// Serves as the abstract base class for all domain entities with identity and domain event support.
/// Implements equality based on the entity's identity value.
/// </summary>
/// <remarks>
/// Inherits from <see cref="HasDomainEventsBase"/> to support domain event dispatching,
/// and implements <see cref="IEntityBase"/> and <see cref="IEquatable{DomainEntityBase}"/>.
/// </remarks>
public abstract class DomainEntityBase : HasDomainEventsBase, IEntityBase, IEquatable<DomainEntityBase>
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    [MaxLength(100)]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Compares two domain entities for equality using their <see cref="Id"/> values.
    /// </summary>
    /// <param name="first">The first entity to compare.</param>
    /// <param name="second">The second entity to compare.</param>
    /// <returns>True if both are non-null and their Ids are equal; otherwise, false.</returns>
    // Development Note:
    // Custom equality operator to allow intuitive comparisons between aggregate root instances.
    public static bool operator ==(DomainEntityBase? first, DomainEntityBase? second)
    {
        return first is not null && second is not null && first.Equals(second);
    }

    /// <summary>
    /// Determines if two domain entities are not equal.
    /// </summary>
    /// <param name="first">The first entity.</param>
    /// <param name="second">The second entity.</param>
    /// <returns>True if entities are not equal; otherwise, false.</returns>
    public static bool operator !=(DomainEntityBase? first, DomainEntityBase? second)
    {
        return !(first == second);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current entity.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns>True if the object is a <see cref="DomainEntityBase"/> with the same <see cref="Id"/>; otherwise, false.</returns>
    // Development Note:
    // Ensures domain entities are compared by identity, not reference or value.
    public override bool Equals(object? obj)
    {
        if (obj == null)
            return false;

        if (obj.GetType() != GetType())
            return false;

        if (obj is not DomainEntityBase entity)
            return false;

        return entity.Id == Id;
    }

    /// <summary>
    /// Indicates whether the current object is equal to another <see cref="DomainEntityBase"/> instance.
    /// </summary>
    /// <param name="other">The entity to compare with the current entity.</param>
    /// <returns>True if the entities have the same type and <see cref="Id"/>; otherwise, false.</returns>
    public bool Equals(DomainEntityBase? other)
    {
        if (other == null)
            return false;

        if (other.GetType() != GetType())
            return false;

        return other.Id == Id;
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code based on the <see cref="Id"/>.</returns>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
