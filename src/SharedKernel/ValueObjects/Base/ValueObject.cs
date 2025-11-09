using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedKernel.ValueObjects.Base;

/// <summary>
/// Base class for value objects that encapsulate equality based on their properties.
/// </summary>
/// <remarks>
/// Value objects are immutable and compared by the values of their components rather than by identity.
/// Inherit from this class and implement <see cref="GetEqualityComponents"/> to specify
/// the properties that define equality.
/// </remarks>
public abstract class ValueObject
{
    /// <summary>
    /// Helper method to implement equality operator (==) for value objects.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if both are equal; otherwise, false.</returns>
    // Development Note:
    // Uses ReferenceEquals to handle nulls and defers to Equals override.
    protected static bool EqualOperator(ValueObject left, ValueObject right)
    {
        if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null))
        {
            return false;
        }
        return ReferenceEquals(left, right) || left?.Equals(right) == true;
    }

    /// <summary>
    /// Helper method to implement inequality operator (!=) for value objects.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if both are not equal; otherwise, false.</returns>
    protected static bool NotEqualOperator(ValueObject left, ValueObject right)
    {
        return !EqualOperator(left, right);
    }

    /// <summary>
    /// Returns the atomic values that are used to determine equality.
    /// </summary>
    /// <returns>An enumerable of the equality components.</returns>
    // Development Note:
    // Must be implemented by derived classes to specify which properties are compared.
    protected abstract IEnumerable<object> GetEqualityComponents();

    /// <summary>
    /// Determines whether the specified object is equal to the current value object.
    /// </summary>
    /// <param name="obj">The object to compare with the current value object.</param>
    /// <returns>True if equal; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (ValueObject)obj;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code based on the equality components.</returns>
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x != null ? x.GetHashCode() : 0)
            .Aggregate((x, y) => x ^ y);
    }
}
