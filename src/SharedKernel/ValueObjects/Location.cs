using SharedKernel.ValueObjects.Base;
using System;
using System.Collections.Generic;

namespace SharedKernel.ValueObjects;

/// <summary>
/// Represents an immutable geographical location using the WGS84 coordinate system.
/// </summary>
/// <remarks>
/// <para>
/// This value object encapsulates latitude and longitude values and ensures that all instances
/// are always valid according to the following rules:
/// </para>
/// <list type="bullet">
/// <item><description>Latitude must be between -90 and 90 degrees.</description></item>
/// <item><description>Longitude must be between -180 and 180 degrees.</description></item>
/// <item><description>The coordinates (0,0) are disallowed, as they usually indicate missing data.</description></item>
/// </list>
/// <para>
/// Instances are immutable: values cannot be modified after creation. Validation occurs
/// at creation time through the <see cref="FromLatLon(double, double)"/> factory method.
/// </para>
/// <para>
/// Equality is based on the latitude and longitude values. Two <see cref="Location"/> instances
/// are considered equal if both their latitude and longitude are equal.
/// </para>
/// <para>
/// Usage of this value object is recommended in domain-driven design (DDD) applications
/// where geographical coordinates need to be represented safely and consistently.
/// </para>
/// <para>
/// This object is suitable for persistence in relational or document databases, and can
/// be used as a property of aggregates or other value objects.
/// </para>
/// </remarks>
public sealed class Location : ValueObject
{
    /// <summary>
    /// Gets the longitude of the location.
    /// </summary>
    public double Longitude { get; }

    /// <summary>
    /// Gets the latitude of the location.
    /// </summary>
    public double Latitude { get; }

    private Location(double latitude, double longitude)
    {
        Validate(latitude, longitude);

        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>
    /// Creates a new <see cref="Location"/> instance with the specified latitude and longitude.
    /// </summary>
    /// <param name="latitude">The latitude, in degrees, between -90 and 90.</param>
    /// <param name="longitude">The longitude, in degrees, between -180 and 180.</param>
    /// <returns>A new, validated <see cref="Location"/> object.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="latitude"/> or <paramref name="longitude"/> are out of range.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if both <paramref name="latitude"/> and <paramref name="longitude"/> are 0.
    /// </exception>
    public static Location FromLatLon(double latitude, double longitude) => new(latitude, longitude);

    private static void Validate(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude),
                "Latitude must be between -90 and 90 degrees.");

        if (longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude),
                "Longitude must be between -180 and 180 degrees.");

        if (latitude == 0 && longitude == 0)
            throw new ArgumentException(
                "Coordinates (0,0) are usually invalid and indicate missing data.");
    }

    /// <summary>
    /// Returns the atomic values that define equality for this <see cref="Location"/>.
    /// </summary>
    /// <returns>A sequence of objects representing the equality components.</returns>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Longitude;
        yield return Latitude;
    }
}
