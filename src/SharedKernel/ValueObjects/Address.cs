using SharedKernel.ValueObjects.Base;
using System.Collections.Generic;

namespace SharedKernel.ValueObjects;

/// <summary>
/// Represents a physical address value object that encapsulates address-related fields.
/// </summary>
/// <remarks>
/// Implements equality based on the values of its components.
/// Use this class for modeling immutable address information within aggregates.
/// </remarks>
public class Address : ValueObject
{
    /// <summary>
    /// Gets or sets the first line of the address.
    /// </summary>
    public string? AddressLine1 { get; set; }

    /// <summary>
    /// Gets or sets the second line of the address.
    /// </summary>
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Gets or sets the city name.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Gets or sets the state, region, or province.
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Gets or sets the postal or ZIP code.
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Gets or sets the identifier for the country.
    /// </summary>
    /// <remarks>
    /// This should correspond to a valid country record in the system or ISO code.
    /// </remarks>
    public string CountryId { get; set; } = null!;

    /// <summary>
    /// Returns an enumeration of atomic values used for equality comparisons.
    /// </summary>
    /// <returns>Sequence of components that define value-based equality for this address.</returns>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return AddressLine1 ?? string.Empty;
        yield return AddressLine2 ?? string.Empty;
        yield return City ?? string.Empty;
        yield return State ?? string.Empty;
        yield return PostalCode ?? string.Empty;
        yield return CountryId ?? string.Empty;
    }
}
