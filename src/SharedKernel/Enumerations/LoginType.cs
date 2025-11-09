namespace SharedKernel.Enumerations;

/// <summary>
/// Enumerates the different types of login methods supported.
/// </summary>
public enum LoginType
{
    /// <summary>
    /// Login using a password.
    /// </summary>
    Password = 0,

    /// <summary>
    /// Login using a One-Time Password (OTP).
    /// </summary>
    OTP = 1,

    /// <summary>
    /// Login using an external provider (e.g., OAuth, social login).
    /// </summary>
    ExternalProvider = 2,

    /// <summary>
    /// Login using a magic link sent to the user's email or device.
    /// </summary>
    MagicLink = 3,

    /// <summary>
    /// Login using biometric authentication (e.g., fingerprint, face recognition).
    /// </summary>
    Biometric = 4,
}
