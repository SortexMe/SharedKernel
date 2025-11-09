using System;

namespace SharedKernel.Extensions;

/// <summary>
/// Provides extension methods for <see cref="DateTimeOffset"/> to simplify conversions to <see cref="DateOnly"/>.
/// </summary>
public static class DateTimeOffsetExtensions
{
    /// <summary>
    /// Converts the <see cref="DateTimeOffset"/> to a <see cref="DateOnly"/> using the local date components.
    /// </summary>
    /// <param name="dateTimeOffset">The date and time with offset to convert.</param>
    /// <returns>A <see cref="DateOnly"/> representing the local date part of the <paramref name="dateTimeOffset"/>.</returns>
    public static DateOnly ToDateOnly(this DateTimeOffset dateTimeOffset)
    {
        // Development notes:
        // - Constructs a DateOnly from the Year, Month, and Day properties of the DateTimeOffset.
        // - Uses the local date values, ignoring time and offset.
        return new DateOnly(dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day);
    }

    /// <summary>
    /// Converts the <see cref="DateTimeOffset"/> to a <see cref="DateOnly"/> using the UTC date components.
    /// </summary>
    /// <param name="dateTimeOffset">The date and time with offset to convert.</param>
    /// <returns>A <see cref="DateOnly"/> representing the UTC date part of the <paramref name="dateTimeOffset"/>.</returns>
    public static DateOnly ToUtcDateOnly(this DateTimeOffset dateTimeOffset)
    {
        // Development notes:
        // - Converts the DateTimeOffset to UTC, then extracts the date component.
        // - Useful when date values should be normalized to UTC.
        return DateOnly.FromDateTime(dateTimeOffset.UtcDateTime);
    }
}
