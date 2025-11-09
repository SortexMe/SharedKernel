namespace SharedKernel.Extensions;

/// <summary>
/// Provides extension methods for <see cref="string"/> to convert to numeric types safely.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts the string representation of a number to a <see cref="long"/>.
    /// Returns 0 if the string is null, empty, whitespace, or invalid.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>The parsed <see cref="long"/> value, or 0 on failure.</returns>
    public static long ToLong(this string? value)
    {
        // Development notes:
        // - If the input is null or whitespace, returns 0 to avoid exceptions.
        // - Uses TryParse to safely parse without throwing exceptions.
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        long.TryParse(value, out long result);
        return result;
    }

    /// <summary>
    /// Converts the string representation of a number to an <see cref="int"/>.
    /// Returns 0 if the string is null, empty, or invalid.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>The parsed <see cref="int"/> value, or 0 on failure.</returns>
    public static int ToInt(this string? value)
    {
        // Development notes:
        // - Uses TryParse to avoid exceptions on invalid input.
        // - Null or invalid strings return 0.
        int.TryParse(value, out int result);
        return result;
    }
}
