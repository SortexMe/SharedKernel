using SharedKernel.ValueObjects.Base;
using System.Collections.Generic;

namespace SharedKernel.ValueObjects;

/// <summary>
/// Represents a geographical location based on latitude and longitude coordinates.
/// </summary>
/// <remarks>
/// This value object supports equality comparisons based on coordinate values.
/// </remarks>
public class Location : ValueObject
{
    /// <summary>
    /// Gets or sets the longitude of the location.
    /// </summary>
    /// <remarks>
    /// Longitude ranges from -180 to 180 degrees.
    /// </remarks>
    public double Longitude { get; set; }

    /// <summary>
    /// Gets or sets the latitude of the location.
    /// </summary>
    /// <remarks>
    /// Latitude ranges from -90 to 90 degrees.
    /// </remarks>
    public double Latitude { get; set; }

    /// <summary>
    /// Creates a new <see cref="Location"/> instance from the given latitude and longitude.
    /// </summary>
    /// <param name="lat">The latitude component.</param>
    /// <param name="lon">The longitude component.</param>
    /// <returns>A new <see cref="Location"/> object.</returns>
    public static Location FromLatLon(double lat, double lon) => new Location { Longitude = lon, Latitude = lat };

    /// <summary>
    /// Returns an enumeration of atomic values used for equality comparisons.
    /// </summary>
    /// <returns>Sequence of values that define the location's equality.</returns>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Longitude;
        yield return Latitude;
    }
}
