namespace SharedKernel.Common.DTOs;

/// <summary>
/// Data Transfer Object representing an authentication token response.
/// </summary>
/// <param name="AccessToken">The JWT or access token issued for authentication.</param>
/// <param name="RefreshToken">The refresh token used to obtain new access tokens.</param>
public record TokenResponseDTO(string AccessToken, string RefreshToken);

// Development Note:
// This DTO is typically returned from authentication endpoints after a successful login or token refresh.
// Both tokens are strings, and security measures should be taken to protect them in transit and storage.
