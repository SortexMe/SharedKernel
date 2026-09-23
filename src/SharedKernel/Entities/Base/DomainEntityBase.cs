using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedKernel.Entities.Base;

/// <summary>
/// Serves as the abstract base class for all domain entities with identity and domain event support.
/// Implements equality based on the entity's identity value.
/// </summary>
/// <remarks>
/// Inherits from <see cref="HasDomainEventsBase"/> to support domain event dispatching,
/// and implements <see cref="IEntityBase"/> and <see cref="IEquatable{DomainEntityBase}"/>.
/// Transient entities — those whose <see cref="Id"/> is still <see cref="Guid.Empty"/> — are only equal to themselves,
/// because they have no identity yet.
/// </remarks>
public abstract class DomainEntityBase : HasDomainEventsBase, IEntityBase, IEquatable<DomainEntityBase>
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    /// <summary>
    /// Compares two domain entities for equality using their <see cref="Id"/> values.
    /// </summary>
    /// <param name="first">The first entity to compare.</param>
    /// <param name="second">The second entity to compare.</param>
    /// <returns>True if both are null, or both are non-null with the same <see cref="Id"/>; otherwise, false.</returns>
    public static bool operator ==(DomainEntityBase? first, DomainEntityBase? second)
    {
        if (ReferenceEquals(first, second))
            return true;

        if (first is null || second is null)
            return false;

        return first.Equals(second);
    }

    /// <summary>
    /// Determines if two domain entities are not equal.
    /// </summary>
    /// <param name="first">The first entity.</param>
    /// <param name="second">The second entity.</param>
    /// <returns>True if entities are not equal; otherwise, false.</returns>
    public static bool operator !=(DomainEntityBase? first, DomainEntityBase? second) => !(first == second);

    /// <summary>
    /// Determines whether the specified object is equal to the current entity.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns>True if the object is a <see cref="DomainEntityBase"/> of the same type with the same, non-empty <see cref="Id"/>; otherwise, false.</returns>
    public override bool Equals(object? obj) => Equals(obj as DomainEntityBase);

    /// <summary>
    /// Indicates whether the current object is equal to another <see cref="DomainEntityBase"/> instance.
    /// </summary>
    /// <param name="other">The entity to compare with the current entity.</param>
    /// <returns>True if the entities have the same type and the same, non-empty <see cref="Id"/>; otherwise, false.</returns>
    public bool Equals(DomainEntityBase? other)
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
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code based on the <see cref="Id"/>.</returns>
    public override int GetHashCode() => Id.GetHashCode();
}
