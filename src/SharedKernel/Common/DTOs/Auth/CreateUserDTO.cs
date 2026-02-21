using System;

namespace SharedKernel.Common.DTOs.Auth;

/// <summary>
/// Data Transfer Object used to create a new user.
/// </summary>
public record CreateUserDTO
{
    /// <summary>
    /// The username of the new user.
    /// </summary>
    public string UserName { get; init; }

    /// <summary>
    /// The contact name of the user.
    /// </summary>
    public string ContactName { get; init; }

    /// <summary>
    /// The email address of the user.
    /// </summary>
    public string Email { get; init; }

    /// <summary>
    /// The password for the user account.
    /// </summary>
    public string Password { get; init; }

    /// <summary>
    /// The phone number of the user.
    /// </summary>
    public string PhoneNumber { get; init; }

    /// <summary>
    /// The identifier of the user's country.
    /// </summary>
    public Guid CountryId { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateUserDTO"/> record.
    /// </summary>
    /// <param name="userName">The username of the new user.</param>
    /// <param name="contactName">The contact name of the user.</param>
    /// <param name="email">The email address of the user.</param>
    /// <param name="password">The password for the user account.</param>
    /// <param name="phoneNumber">The phone number of the user.</param>
    /// <param name="countryId">The identifier of the user's country.</param>
    // Development Note:
    // This DTO is immutable due to the use of 'init' properties and is intended for user creation scenarios,
    // such as API input or service layer consumption.
    public CreateUserDTO(string userName, string contactName, string email, string password, string phoneNumber, Guid countryId)
    {
        UserName = userName;
        ContactName = contactName;
        Email = email;
        Password = password;
        PhoneNumber = phoneNumber;
        CountryId = countryId;
    }
}
