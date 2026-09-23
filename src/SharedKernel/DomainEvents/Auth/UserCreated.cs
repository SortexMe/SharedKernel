using System;

namespace SharedKernel.DomainEvents.Auth;

/// <summary>
/// Domain event representing the creation of a new user.
/// </summary>
/// <param name="UserId">The unique identifier of the newly created user.</param>
/// <param name="UserEmail">The email address of the new user.</param>
/// <param name="ContactName">The contact name associated with the user.</param>
/// <param name="Token">An optional token related to user creation, such as an email verification token.</param>
public record UserCreated(Guid UserId, string UserEmail, string ContactName, string Token) : DomainEventBase
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
// - This event can be used to trigger post-registration processes like sending a welcome email or activating the user.
// - Inherits from DomainEventBase to integrate with domain event dispatch mechanisms.
