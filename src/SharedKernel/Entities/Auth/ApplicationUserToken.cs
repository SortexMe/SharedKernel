using SharedKernel.Entities.Base;
using SharedKernel.Enumerations;
using System;
using System.ComponentModel.DataAnnotations;

namespace SharedKernel.Entities.Auth;

/// <summary>
/// Represents a token associated with a user for authentication or authorization purposes.
/// </summary>
public class ApplicationUserToken : EntityBase
{
    /// <summary>
    /// Gets or sets the identifier of the user associated with this token.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the token string value.
    /// </summary>
    [MaxLength(2000)]
    public string Token { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of the token (e.g., password reset, email confirmation).
    /// </summary>
    public UserTokenType TokenType { get; set; }

    /// <summary>
    /// Gets or sets the expiration date and time of the token in UTC.
    /// </summary>
    public DateTimeOffset ExpiryDate { get; set; }

    /// <summary>
    /// Gets or sets the related application user entity.
    /// </summary>
    public virtual ApplicationUser ApplicationUser { get; set; } = null!;

    // Development Notes:
    // - Inherits EntityBase for unique ID and equality.
    // - Stores token string and its type to distinguish between various token usages.
    // - ExpiryDate enforces token validity duration.
    // - Navigation property to ApplicationUser enables easy lookup of owner.
}
