using SharedKernel.Abstractions.Data;
using SharedKernel.Common.DTOs;
using SharedKernel.Common.DTOs.Auth;
using SharedKernel.Common.Exceptions;
using SharedKernel.DomainEvents.Auth;
using SharedKernel.Entities.Auth;
using SharedKernel.Enumerations;
using SharedKernel.Extensions;
using SharedKernel.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedKernel.DomainModels;

/// <summary>
/// Domain model that encapsulates business logic related to user operations.
/// </summary>
public sealed class UserDomainModel
{
    private readonly ApplicationUser user;
    private readonly IUserRepository userRepository;

    /// <summary>
    /// Initializes a new instance of <see cref="UserDomainModel"/>.
    /// </summary>
    /// <param name="user">The user entity.</param>
    /// <param name="userRepository">The user repository for persistence operations.</param>
    public UserDomainModel(ApplicationUser user, IUserRepository userRepository)
    {
        this.user = user;
        this.userRepository = userRepository;
    }

    /// <summary>
    /// Initiates the password reset process by generating a reset token,
    /// registering a domain event, and updating the user.
    /// </summary>
    public void ForgetPassword()
    {
        var token = TokenGenerator.GenerateToken();
        user.ApplicationUserTokens.Add(new ApplicationUserToken
        {
            Token = token,
            TokenType = UserTokenType.PasswordReset,
            ExpiryDate = DateTimeOffset.UtcNow.AddMinutes(30),
        });

        user.RegisterDomainEvent(new UserPasswordForgotten(user.Id, user.Email, user.ContactName, token));
        userRepository.Update(user);
    }

    /// <summary>
    /// Resets the user's password after validating the reset token.
    /// Updates user state and registers a password reset domain event.
    /// </summary>
    /// <param name="token">The password reset token.</param>
    /// <param name="passwordHash">The new password hash.</param>
    /// <exception cref="DomainException">
    /// Thrown if the token is invalid or expired.
    /// </exception>
    public void ResetPassword(string token, string passwordHash)
    {
        var userToken = user.ApplicationUserTokens.FirstOrDefault(m => m.Token == token && m.TokenType == UserTokenType.PasswordReset && m.ExpiryDate > DateTimeOffset.UtcNow);

        if (userToken is null)
            throw DomainException.CreateDetailedException("The reset password link is invalid or has expired. Please request a new password reset", "InvalidValidator", "Token");

        var hasNewerToken = user.ApplicationUserTokens.Where(m => m.TokenType == UserTokenType.PasswordReset && m.Token != token && m.ExpiryDate > userToken.ExpiryDate).Any();

        if (hasNewerToken)
            throw DomainException.CreateDetailedException("The reset password link is invalid or has expired. Please request a new password reset", "InvalidValidator", "Token");

        user.PasswordHash = passwordHash;
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;

        user.RegisterDomainEvent(new UserPasswordReset(user.Id, user.Email, user.ContactName));
        userRepository.Update(user);
    }

    /// <summary>
    /// Refreshes the user's JWT refresh token after validation.
    /// </summary>
    /// <param name="token">The current token response DTO.</param>
    /// <param name="newRefreshToken">The new refresh token to set.</param>
    /// <param name="refreshTokenDuration">Optional duration for refresh token validity.</param>
    /// <exception cref="DomainException">
    /// Thrown if the provided token is invalid or expired.
    /// </exception>
    public void RefreshToken(TokenResponseDTO token, string newRefreshToken, TimeSpan? refreshTokenDuration = null)
    {
        var errors = new List<DTOValidationError>();
        if (!token.RefreshToken.Equals(user.RefreshToken) || (user.RefreshTokenExpiry.HasValue && user.RefreshTokenExpiry.Value < DateTimeOffset.UtcNow))
            errors.Add(DTOValidationError.CreateDetailedError("Invalid Token", "InvalidValidator", "Token"));

        errors.ThrowDomainException();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = DateTimeOffset.UtcNow.Add(refreshTokenDuration ?? TimeSpan.FromDays(30));
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
    }

    /// <summary>
    /// Handles user login process including password validation, lockout logic,
    /// refresh token issuance, and updating user state.
    /// </summary>
    /// <param name="isPasswordValid">Indicates whether the provided password is valid.</param>
    /// <param name="newRefreshToken">The new refresh token to issue upon successful login.</param>
    /// <param name="refreshTokenDuration">Optional duration for refresh token validity.</param>
    /// <exception cref="DomainException">
    /// Thrown if the email is unconfirmed, account is locked, or password is invalid.
    /// </exception>
    public void LoginUser(bool isPasswordValid, string newRefreshToken, TimeSpan? refreshTokenDuration = null)
    {
        if (!user.EmailConfirmed)
        {
            throw DomainException.CreateDetailedException("Email is not confirmed", "EmailNotConfirmed", "PhoneNumberOrEmail");
        }

        if (user.AccessFailedCount >= 5)
        {
            var remaining = GetRemainingLockoutTime();
            throw DomainException.CreateDetailedException($"Your account is locked. Try again in {remaining?.Minutes} minutes and {remaining?.Seconds} seconds.", "AccountLocked", "PhoneNumberOrEmail");
        }

        if (!isPasswordValid)
        {
            user.AccessFailedCount++;
            if (user.AccessFailedCount >= 5)
                user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
            userRepository.Update(user);
            throw DomainException.CreateDetailedException("Invalid username/email or password", "InvalidValidator", "Password");
        }

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = DateTimeOffset.UtcNow.Add(refreshTokenDuration ?? TimeSpan.FromDays(30));
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        userRepository.Update(user);
    }

    /// <summary>
    /// Creates a new <see cref="ApplicationUser"/> based on the provided DTO,
    /// password hash, and security group ID.
    /// </summary>
    /// <param name="createUserDTO">The DTO containing user creation data.</param>
    /// <param name="passwordHash">The hashed password.</param>
    /// <returns>The created <see cref="ApplicationUser"/> instance.</returns>
    public static ApplicationUser Create(CreateUserDTO createUserDTO, string passwordHash)
    {
        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7().ToString(),
            ContactName = createUserDTO.ContactName,
            UserName = createUserDTO.UserName,
            PhoneNumber = createUserDTO.PhoneNumber,
            Email = createUserDTO.Email,
            PasswordHash = passwordHash,
            CountryId = createUserDTO.CountryId,
            NormalizedUserName = createUserDTO.UserName.ToUpper(),
            NormalizedEmail = createUserDTO.Email.ToUpper(),
        };

        var token = TokenGenerator.GenerateToken();
        user.ApplicationUserTokens.Add(new ApplicationUserToken
        {
            Token = token,
            TokenType = UserTokenType.EmailConfirmation,
            ExpiryDate = DateTimeOffset.UtcNow.AddHours(24),
        });

        user.RegisterDomainEvent(new UserCreated(user.Id, user.Email, user.ContactName, token));

        return user;
    }

    /// <summary>
    /// Gets the remaining lockout time if the user is currently locked out.
    /// </summary>
    /// <returns>The remaining <see cref="TimeSpan"/> until lockout expires, or null if not locked out.</returns>
    private TimeSpan? GetRemainingLockoutTime()
    {
        if (!user.LockoutEnd.HasValue || user.LockoutEnd.Value <= DateTimeOffset.UtcNow)
            return null;

        return user.LockoutEnd.Value - DateTimeOffset.UtcNow;
    }
}
// Development Notes:
// - This domain model encapsulates all user-related business logic and enforces domain rules.
// - Uses domain events to notify other parts
