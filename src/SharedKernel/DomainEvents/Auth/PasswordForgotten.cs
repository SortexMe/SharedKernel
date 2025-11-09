namespace SharedKernel.DomainEvents.Auth;

/// <summary>
/// Domain event that is triggered when a user requests a password reset.
/// </summary>
/// <param name="UserId">The unique identifier of the user who forgot their password.</param>
/// <param name="UserEmail">The email address of the user.</param>
/// <param name="ContactName">The contact name associated with the user.</param>
/// <param name="Token">The password reset token issued for this request.</param>
public record UserPasswordForgotten(string UserId, string UserEmail, string ContactName, string Token) : DomainEventBase;

// Development Notes:
// - This event encapsulates the necessary information to handle password reset workflows.
// - Typically used to trigger sending reset emails or logging security-related activities.
// - Inherits from DomainEventBase to integrate with the domain event dispatching mechanism.
