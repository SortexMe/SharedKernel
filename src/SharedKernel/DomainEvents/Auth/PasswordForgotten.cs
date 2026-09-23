using System;

namespace SharedKernel.DomainEvents.Auth;

/// <summary>
/// Domain event that is triggered when a user requests a password reset.
/// </summary>
/// <param name="UserId">The unique identifier of the user who forgot their password.</param>
/// <param name="UserEmail">The email address of the user.</param>
/// <param name="ContactName">The contact name associated with the user.</param>
/// <param name="Token">The password reset token issued for this request.</param>
public record UserPasswordForgotten(Guid UserId, string UserEmail, string ContactName, string Token) : DomainEventBase
{
    // Token is the raw single-use secret handed to the user; keep it out of ToString() and logs.
    protected override bool PrintMembers(System.Text.StringBuilder builder)
    {
        if (base.PrintMembers(builder))
            builder.Append(", ");

        builder.Append("UserId = ").Append(UserId)
               .Append(", UserEmail = ").Append(UserEmail)
               .Append(", ContactName = ").Append(ContactName)
               .Append(", Token = ***");
        return true;
    }
}

// Development Notes:
// - This event encapsulates the necessary information to handle password reset workflows.
// - Typically used to trigger sending reset emails or logging security-related activities.
// - Inherits from DomainEventBase to integrate with the domain event dispatching mechanism.
