using SharedKernel.Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SharedKernel.Entities.Auth;

/// <summary>
/// Represents an application user entity containing authentication and profile details.
/// </summary>
public class ApplicationUser : DomainEntityBase
{
    /// <summary>
    /// Gets or sets contact name for this user.
    /// </summary>
    [MaxLength(256)]
    public string ContactName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user name for this user.
    /// </summary>
    [MaxLength(256)]
    public string UserName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the normalized user name.
    /// This value is typically stored in uppercase and used for case-insensitive lookups,
    /// enabling efficient indexing and avoiding full table scans in relational databases.
    /// </summary>
    [MaxLength(256)]
    public string NormalizedUserName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the email address associated with the user.
    /// </summary>
    [MaxLength(256)]
    public string Email { get; set; } = null!;

    /// <summary>
    /// Gets or sets the normalized email address.
    /// This value is typically stored in uppercase and used for case-insensitive comparisons
    /// and optimized querying in relational databases.
    /// </summary>
    [MaxLength(256)]
    public string NormalizedEmail { get; set; } = null!;


    /// <summary>
    /// Gets or sets a flag indicating if a user has confirmed their email address.
    /// </summary>
    /// <value>True if the email address has been confirmed, otherwise false.</value>
    public bool EmailConfirmed { get; set; }

    /// <summary>
    /// Gets or sets a salted and hashed representation of the password for this user.
    /// </summary>
    [MaxLength(2000)]
    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// Gets or sets a telephone number for the user.
    /// </summary>
    [MaxLength(256)]
    public string PhoneNumber { get; set; } = null!;

    /// <summary>
    /// Gets or sets a flag indicating if a user has confirmed their telephone address.
    /// </summary>
    /// <value>True if the telephone number has been confirmed, otherwise false.</value>
    public bool PhoneNumberConfirmed { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating if two factor authentication is enabled for this user.
    /// </summary>
    /// <value>True if 2fa is enabled, otherwise false.</value>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// Gets or sets the date and time, in UTC, when any user lockout ends.
    /// </summary>
    /// <remarks>
    /// A value in the past means the user is not locked out.
    /// </remarks>
    public DateTimeOffset? LockoutEnd { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating if the user could be locked out.
    /// </summary>
    /// <value>True if the user could be locked out, otherwise false.</value>
    public bool LockoutEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of failed login attempts for the current user.
    /// </summary>
    public int AccessFailedCount { get; set; }

    /// <summary>
    /// Gets or sets the refresh token used for authentication renewal.
    /// </summary>
    [MaxLength(2000)]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the expiry date and time for the refresh token.
    /// </summary>
    public DateTimeOffset? RefreshTokenExpiry { get; set; }

    /// <summary>
    /// Gets or sets the country identifier related to the user.
    /// </summary>
    [MaxLength(100)]
    public string CountryId { get; set; } = null!;

    /// <summary>
    /// Indicates whether the entity is active. Inactive entities are considered soft-deleted.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the collection of external login providers associated with this user.
    /// </summary>
    public virtual ICollection<ApplicationUserLogin> ApplicationUserLogins { get; set; } = new HashSet<ApplicationUserLogin>();

    /// <summary>
    /// Gets or sets the collection of tokens associated with this user, such as password reset or email confirmation tokens.
    /// </summary>
    public virtual ICollection<ApplicationUserToken> ApplicationUserTokens { get; set; } = new List<ApplicationUserToken>();

    // Development Notes:
    // - This entity extends DomainEntityBase, inheriting a unique Id and domain event support.
    // - Includes fields common to user identity management: email, phone, password, lockout, 2FA.
    // - Supports tracking of refresh tokens for JWT authentication renewal.
    // - Collections manage user tokens and external login info, supporting extensible authentication flows.
    // - Data annotations enforce max length constraints suitable for persistence (e.g., in EF Core).
}
