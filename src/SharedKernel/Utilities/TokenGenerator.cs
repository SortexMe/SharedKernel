using System;
using System.Security.Cryptography;
using System.Text;

namespace SharedKernel.Utilities;

/// <summary>
/// Provides functionality to securely generate and hash random tokens.
/// </summary>
/// <remarks>
/// This utility generates cryptographically secure random tokens and hashes them using SHA-512.
/// Suitable for scenarios like API keys, password reset tokens, and session identifiers.
/// </remarks>
public static class TokenGenerator
{
    /// <summary>
    /// Generates a cryptographically secure random token, hashes it using SHA-512, and returns the result as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="byteLength">
    /// The number of random bytes to generate. Default is <c>32</c>.
    /// This results in a 512-bit random value before hashing.
    /// </param>
    /// <returns>A hashed, lowercase hexadecimal representation of the token.</returns>
    public static string GenerateToken(int byteLength = 32)
    {
        using var rng = RandomNumberGenerator.Create();
        byte[] tokenBytes = new byte[byteLength];
        rng.GetBytes(tokenBytes);
        var base64 = Convert.ToBase64String(tokenBytes);
        var hashedToken = HashToken(base64);
        return hashedToken;
    }

    /// <summary>
    /// Computes a SHA-512 hash from a base64-encoded token and returns it as a lowercase hexadecimal string.
    /// </summary>
    /// <param name="token">The base64-encoded token string.</param>
    /// <returns>A lowercase hexadecimal string representing the SHA-512 hash of the token.</returns>
    private static string HashToken(string token)
    {
        using var sha512 = SHA512.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha512.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
