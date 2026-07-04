using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedKernel.DomainEvents.Auth;

/// <summary>
/// Domain event representing the creation of a new user.
/// </summary>
/// <param name="UserId">The unique identifier of the newly created user.</param>
/// <param name="UserEmail">The email address of the new user.</param>
/// <param name="ContactName">The contact name associated with the user.</param>
/// <param name="Token">An optional token related to user creation, such as an email verification token.</param>
public record UserCreated(Guid UserId, string UserEmail, string ContactName, string Token) : DomainEventBase;

// Development Notes:
// - This event can be used to trigger post-registration processes like sending a welcome email or activating the user.
// - Inherits from DomainEventBase to integrate with domain event dispatch mechanisms.
