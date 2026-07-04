using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedKernel.DomainEvents.Auth;

/// <summary>
/// Domain event triggered when a user successfully resets their password.
/// </summary>
/// <param name="UserId">The unique identifier of the user who reset their password.</param>
/// <param name="UserEmail">The email address of the user.</param>
/// <param name="ContactName">The contact name associated with the user.</param>
public record UserPasswordReset(Guid UserId, string UserEmail, string ContactName) : DomainEventBase;

// Development Notes:
// - This event is typically used to notify other systems or log password reset activity.
// - Inherits from DomainEventBase to support event dispatching within the domain.
