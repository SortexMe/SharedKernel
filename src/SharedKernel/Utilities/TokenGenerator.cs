using System;
using System.Security.Cryptography;
using System.Text;

namespace SharedKernel.Utilities;

/// <summary>
/// Generates cryptographically secure random tokens and hashes them for storage at rest.
/// </summary>
/// <remarks>
/// The intended flow for a bearer token (password reset, e-mail confirmation, API key) is:
/// <list type="number">
/// <item><description><see cref="GenerateToken"/> — produce the raw token and hand it to the user (e-mail, response body).</description></item>
/// <item><description><see cref="HashToken"/> — store only the hash. A leaked table then reveals nothing usable.</description></item>
/// <item><description><see cref="VerifyToken"/> — compare a presented raw token against the stored hash in constant time.</description></item>
/// </list>
/// </remarks>
public static class TokenGenerator
{
    /// <summary>
    /// Generates a cryptographically secure random token as a 128-character lowercase hexadecimal string.
    /// </summary>
    /// <param name="byteLength">The number of random bytes to draw. Default is 32 (256 bits of entropy).</param>
    /// <returns>The raw token. Give this to the user; store <see cref="HashToken"/> of it.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="byteLength"/> is not positive.</exception>
    public static string GenerateToken(int byteLength = 32)
    {
        if (byteLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(byteLength), byteLength, "Token length must be a positive number of bytes.");

        // The random bytes are expanded through SHA-512 so the token keeps the 128-hex-character shape
        // earlier versions produced; the entropy is bounded by byteLength, not by the hash length.
        var randomBytes = RandomNumberGenerator.GetBytes(byteLength);
        return ToLowerHex(SHA512.HashData(Encoding.UTF8.GetBytes(Convert.ToBase64String(randomBytes))));
    }

    /// <summary>
    /// Computes the SHA-512 hash of a token as a lowercase hexadecimal string, suitable for storing at rest.
    /// </summary>
    /// <param name="token">The raw token.</param>
    /// <returns>A 128-character lowercase hexadecimal hash.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="token"/> is null.</exception>
    public static string HashToken(string token)
    {
        if (token is null)
            throw new ArgumentNullException(nameof(token));

        return ToLowerHex(SHA512.HashData(Encoding.UTF8.GetBytes(token)));
    }

    /// <summary>
    /// Checks whether a presented raw token matches a stored hash, in constant time.
    /// </summary>
    /// <param name="rawToken">The token presented by the caller. Null or empty never verifies.</param>
    /// <param name="storedHash">The hash produced earlier by <see cref="HashToken"/>. Null or empty never verifies.</param>
    /// <returns><c>true</c> when the hash of <paramref name="rawToken"/> equals <paramref name="storedHash"/>.</returns>
    public static bool VerifyToken(string? rawToken, string? storedHash)
    {
        if (string.IsNullOrEmpty(rawToken) || string.IsNullOrEmpty(storedHash))
            return false;

        var presented = Encoding.UTF8.GetBytes(HashToken(rawToken));
        var expected = Encoding.UTF8.GetBytes(storedHash.ToLowerInvariant());

        return CryptographicOperations.FixedTimeEquals(presented, expected);
    }

    private static string ToLowerHex(byte[] bytes) => Convert.ToHexString(bytes).ToLowerInvariant();
}
