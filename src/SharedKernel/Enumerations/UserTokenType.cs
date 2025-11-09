namespace SharedKernel.Enumerations;

/// <summary>
/// Represents the types of tokens associated with user account operations.
/// </summary>
public enum UserTokenType
{
    /// <summary>
    /// Token used for resetting the user's password.
    /// </summary>
    PasswordReset,

    /// <summary>
    /// Token used for confirming the user's email address.
    /// </summary>
    EmailConfirmation,

    /// <summary>
    /// Token used for two-factor authentication purposes.
    /// </summary>
    TwoFactorAuth
}
