using SharedKernel.ValueObjects.Base;
using System;
using System.Collections.Generic;

namespace SharedKernel.ValueObjects;

/// <summary>
/// Represents a physical address value object that encapsulates address-related fields.
/// </summary>
/// <remarks>
/// Implements equality based on the values of its components.
/// Properties are init-only: build an instance with an object initializer and never mutate it, since
/// equality and hashing are derived from its values.
/// </remarks>
public class Address : ValueObject
{
    /// <summary>
    /// Gets the first line of the address.
    /// </summary>
    public string? AddressLine1 { get; init; }

    /// <summary>
    /// Gets the second line of the address.
    /// </summary>
    public string? AddressLine2 { get; init; }

    /// <summary>
    /// Gets the city name.
    /// </summary>
    public string? City { get; init; }

    /// <summary>
    /// Gets the state, region, or province.
    /// </summary>
    public string? State { get; init; }

    /// <summary>
    /// Gets the postal or ZIP code.
    /// </summary>
    public string? PostalCode { get; init; }

    /// <summary>
    /// Gets the identifier for the country.
    /// </summary>
    /// <remarks>
    /// This should correspond to a valid country record in the system or ISO code.
    /// </remarks>
    public Guid CountryId { get; init; }

    /// <summary>
    /// Returns an enumeration of atomic values used for equality comparisons.
    /// </summary>
    /// <returns>Sequence of components that define value-based equality for this address.</returns>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AddressLine1 ?? string.Empty;
        yield return AddressLine2 ?? string.Empty;
        yield return City ?? string.Empty;
        yield return State ?? string.Empty;
        yield return PostalCode ?? string.Empty;
        yield return CountryId;
    }
}
