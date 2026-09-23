using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedKernel.ValueObjects.Base;

/// <summary>
/// Base class for value objects that encapsulate equality based on their properties.
/// </summary>
/// <remarks>
/// Value objects are immutable and compared by the values of their components rather than by identity.
/// Inherit from this class and implement <see cref="GetEqualityComponents"/> to specify
/// the properties that define equality. <c>==</c>, <c>!=</c>, <see cref="Equals(object)"/> and
/// <see cref="GetHashCode"/> are all derived from those components, in order.
/// </remarks>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Compares two value objects for equality by their components.
    /// </summary>
    public static bool operator ==(ValueObject? left, ValueObject? right) => EqualOperator(left, right);

    /// <summary>
    /// Compares two value objects for inequality by their components.
    /// </summary>
    public static bool operator !=(ValueObject? left, ValueObject? right) => !EqualOperator(left, right);

    /// <summary>
    /// Helper method to implement equality operator (==) for value objects.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if both are null, or both are non-null and equal; otherwise, false.</returns>
    protected static bool EqualOperator(ValueObject? left, ValueObject? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    /// Helper method to implement inequality operator (!=) for value objects.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if both are not equal; otherwise, false.</returns>
    protected static bool NotEqualOperator(ValueObject? left, ValueObject? right) => !EqualOperator(left, right);

    /// <summary>
    /// Returns the atomic values that are used to determine equality, in a stable order.
    /// </summary>
    /// <returns>An enumerable of the equality components. Elements may be null.</returns>
    // Development Note:
    // Must be implemented by derived classes to specify which properties are compared.
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// Determines whether the specified object is equal to the current value object.
    /// </summary>
    /// <param name="obj">The object to compare with the current value object.</param>
    /// <returns>True if equal; otherwise, false.</returns>
    public override bool Equals(object? obj) => Equals(obj as ValueObject);

    /// <summary>
    /// Determines whether another value object of the same type has the same components.
    /// </summary>
    /// <param name="other">The value object to compare with.</param>
    /// <returns>True if equal; otherwise, false.</returns>
    public bool Equals(ValueObject? other)
    {
        if (other is null || other.GetType() != GetType())
            return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>An order-sensitive hash of the equality components; zero components is valid.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var component in GetEqualityComponents())
            hash.Add(component);

        return hash.ToHashCode();
    }
}
