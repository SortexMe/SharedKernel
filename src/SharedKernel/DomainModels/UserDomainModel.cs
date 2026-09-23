using SharedKernel.Abstractions.Data;
using SharedKernel.Common.DTOs;
using SharedKernel.Common.DTOs.Auth;
using SharedKernel.Common.Exceptions;
using SharedKernel.DomainEvents.Auth;
using SharedKernel.Entities.Auth;
using SharedKernel.Enumerations;
using SharedKernel.Utilities;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SharedKernel.DomainModels;

/// <summary>
/// Domain model that encapsulates business logic related to user operations.
/// Can be extended by derived classes to customize policy settings and domain behaviors.
/// </summary>
/// <remarks>
/// Single-use tokens (password reset, e-mail confirmation) are stored as hashes; the raw token is carried
/// only on the domain event so it can be delivered to the user. Present the raw token back to
/// <see cref="ResetPassword"/> or <see cref="ConfirmEmail"/>; both consume it on success.
/// </remarks>
public class UserDomainModel
{
    /// <summary>Default failed attempts allowed before the account is locked.</summary>
    public const int DefaultMaxFailedAccessAttempts = 5;

    /// <summary>Default duration a lockout lasts once triggered.</summary>
    public static readonly TimeSpan DefaultLockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>Default refresh token lifetime when the caller does not supply one.</summary>
    public static readonly TimeSpan DefaultRefreshTokenLifetimeValue = TimeSpan.FromDays(30);

    /// <summary>Default lifetime of a password-reset token.</summary>
    public static readonly TimeSpan DefaultPasswordResetTokenLifetime = TimeSpan.FromMinutes(30);

    /// <summary>Default lifetime of an e-mail confirmation token.</summary>
    public static readonly TimeSpan DefaultEmailConfirmationTokenLifetime = TimeSpan.FromHours(24);

    /// <summary>Failed attempts allowed before the account is locked. Can be overridden in derived classes.</summary>
    public virtual int MaxFailedAccessAttempts => DefaultMaxFailedAccessAttempts;

    /// <summary>How long a lockout lasts once triggered. Can be overridden in derived classes.</summary>
    public virtual TimeSpan LockoutDuration => DefaultLockoutDuration;

    /// <summary>Refresh token lifetime when the caller does not supply one. Can be overridden in derived classes.</summary>
    public virtual TimeSpan DefaultRefreshTokenLifetime => DefaultRefreshTokenLifetimeValue;

    /// <summary>Lifetime of a password-reset token. Can be overridden in derived classes.</summary>
    public virtual TimeSpan PasswordResetTokenLifetime => DefaultPasswordResetTokenLifetime;

    /// <summary>Lifetime of an e-mail confirmation token. Can be overridden in derived classes.</summary>
    public virtual TimeSpan EmailConfirmationTokenLifetime => DefaultEmailConfirmationTokenLifetime;

    private readonly ApplicationUser _user;
    private readonly IUserRepository? _userRepository;

    /// <summary>
    /// Gets the underlying <see cref="ApplicationUser"/> instance.
    /// </summary>
    protected ApplicationUser User => _user;

    /// <summary>
    /// Gets the <see cref="IUserRepository"/> instance, if one was provided.
    /// </summary>
    protected IUserRepository? UserRepository => _userRepository;

    /// <summary>
    /// Initializes a new instance of <see cref="UserDomainModel"/> without a repository.
    /// </summary>
    /// <param name="user">The user entity.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is null.</exception>
    public UserDomainModel(ApplicationUser user)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _userRepository = null;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="UserDomainModel"/> with a user repository.
    /// </summary>
    /// <param name="user">The user entity.</param>
    /// <param name="userRepository">The user repository for persistence operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> or <paramref name="userRepository"/> is null.</exception>
    public UserDomainModel(ApplicationUser user, IUserRepository userRepository)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    /// <summary>
    /// Initiates the password reset process: issues a reset token, stores its hash, and raises
    /// <see cref="UserPasswordForgotten"/> carrying the raw token for delivery.
    /// </summary>
    public virtual void ForgetPassword()
    {
        EnsureActive();

        var rawToken = IssueToken(UserTokenType.PasswordReset, PasswordResetTokenLifetime);

        _user.RegisterDomainEvent(new UserPasswordForgotten(_user.Id, _user.Email, _user.ContactName, rawToken));
        _userRepository?.Update(_user);
    }

    /// <summary>
    /// Resets the user's password after validating the reset token, then consumes every outstanding reset token.
    /// </summary>
    /// <param name="token">The raw password reset token as delivered to the user.</param>
    /// <param name="passwordHash">The new password hash.</param>
    /// <exception cref="DomainException">Thrown if the token is invalid, expired, or superseded by a newer one.</exception>
    public virtual void ResetPassword(string token, string passwordHash)
    {
        EnsureActive();

        if (!ConsumeToken(UserTokenType.PasswordReset, token))
            throw DomainException.CreateDetailedException("The reset password link is invalid or has expired. Please request a new password reset", "InvalidValidator", "Token");

        _user.PasswordHash = passwordHash;
        _user.RefreshToken = null;
        _user.RefreshTokenExpiry = null;
        _user.LockoutEnd = null;
        _user.AccessFailedCount = 0;

        _user.RegisterDomainEvent(new UserPasswordReset(_user.Id, _user.Email, _user.ContactName));
        _userRepository?.Update(_user);
    }

    /// <summary>
    /// Confirms the user's e-mail address after validating the confirmation token issued by <see cref="Create"/>,
    /// then consumes every outstanding confirmation token.
    /// </summary>
    /// <param name="token">The raw e-mail confirmation token as delivered to the user.</param>
    /// <exception cref="DomainException">Thrown if the token is invalid, expired, or superseded by a newer one.</exception>
    public virtual void ConfirmEmail(string token)
    {
        EnsureActive();

        if (!ConsumeToken(UserTokenType.EmailConfirmation, token))
            throw DomainException.CreateDetailedException("The confirmation link is invalid or has expired. Please request a new confirmation e-mail", "InvalidValidator", "Token");

        _user.EmailConfirmed = true;
        _userRepository?.Update(_user);
    }

    /// <summary>
    /// Rotates the user's refresh token after validating the presented one.
    /// </summary>
    /// <param name="token">The token response holding the refresh token presented by the client.</param>
    /// <param name="newRefreshToken">The new refresh token to set.</param>
    /// <param name="refreshTokenDuration">Optional lifetime for the new refresh token; defaults to <see cref="DefaultRefreshTokenLifetime"/>.</param>
    /// <exception cref="DomainException">Thrown if the presented token is missing, does not match, or has expired.</exception>
    public virtual void RefreshToken(TokenResponseDTO token, string newRefreshToken, TimeSpan? refreshTokenDuration = null)
    {
        EnsureActive();

        var presented = token?.RefreshToken;
        var expired = _user.RefreshTokenExpiry.HasValue && _user.RefreshTokenExpiry.Value < DateTimeOffset.UtcNow;

        if (!FixedTimeEquals(presented, _user.RefreshToken) || expired)
            throw DomainException.CreateDetailedException("Invalid Token", "InvalidValidator", "Token");

        _user.RefreshToken = newRefreshToken;
        _user.RefreshTokenExpiry = DateTimeOffset.UtcNow.Add(refreshTokenDuration ?? DefaultRefreshTokenLifetime);
        _user.LockoutEnd = null;
        _user.AccessFailedCount = 0;
        _userRepository?.Update(_user);
    }

    /// <summary>
    /// Handles user login: enforces e-mail confirmation and lockout, counts failed attempts,
    /// and issues a refresh token on success.
    /// </summary>
    /// <param name="isPasswordValid">Whether the presented password matched the stored hash.</param>
    /// <param name="newRefreshToken">The refresh token to issue on success.</param>
    /// <param name="refreshTokenDuration">Optional refresh token lifetime; defaults to <see cref="DefaultRefreshTokenLifetime"/>.</param>
    /// <exception cref="DomainException">Thrown if the e-mail is unconfirmed, the account is locked, or the password is invalid.</exception>
    public virtual void LoginUser(bool isPasswordValid, string newRefreshToken, TimeSpan? refreshTokenDuration = null)
    {
        EnsureActive();

        if (!_user.EmailConfirmed)
            throw DomainException.CreateDetailedException("Email is not confirmed", "EmailNotConfirmed", "PhoneNumberOrEmail");

        var now = DateTimeOffset.UtcNow;

        if (_user.LockoutEnabled && _user.LockoutEnd is { } lockoutEnd && lockoutEnd > now)
        {
            var remaining = lockoutEnd - now;
            throw DomainException.CreateDetailedException(
                $"Your account is locked. Try again in {(int)remaining.TotalMinutes} minutes and {remaining.Seconds} seconds.",
                "AccountLocked", "PhoneNumberOrEmail");
        }

        // A lockout that has elapsed no longer counts against the user.
        if (_user.LockoutEnd is { } elapsed && elapsed <= now)
        {
            _user.LockoutEnd = null;
            _user.AccessFailedCount = 0;
        }

        if (!isPasswordValid)
        {
            _user.AccessFailedCount++;
            if (_user.LockoutEnabled && _user.AccessFailedCount >= MaxFailedAccessAttempts)
                _user.LockoutEnd = now.Add(LockoutDuration);
            _userRepository?.Update(_user);
            throw DomainException.CreateDetailedException("Invalid username/email or password", "InvalidValidator", "Password");
        }

        _user.RefreshToken = newRefreshToken;
        _user.RefreshTokenExpiry = now.Add(refreshTokenDuration ?? DefaultRefreshTokenLifetime);
        _user.LockoutEnd = null;
        _user.AccessFailedCount = 0;
        _userRepository?.Update(_user);
    }

    /// <summary>
    /// Creates a new <see cref="ApplicationUser"/> from the DTO and password hash, issues an e-mail
    /// confirmation token, and raises <see cref="UserCreated"/> carrying the raw token for delivery.
    /// </summary>
    /// <param name="createUserDTO">The DTO containing user creation data.</param>
    /// <param name="passwordHash">The hashed password.</param>
    /// <param name="emailConfirmationTokenLifetime">Optional lifetime for the confirmation token; defaults to 24 hours.</param>
    /// <returns>The created <see cref="ApplicationUser"/> instance.</returns>
    public static ApplicationUser Create(CreateUserDTO createUserDTO, string passwordHash, TimeSpan? emailConfirmationTokenLifetime = null)
    {
        if (createUserDTO is null)
            throw new ArgumentNullException(nameof(createUserDTO));

        var user = new ApplicationUser
        {
            Id = NewUserId(),
            ContactName = createUserDTO.ContactName,
            UserName = createUserDTO.UserName,
            PhoneNumber = createUserDTO.PhoneNumber,
            Email = createUserDTO.Email,
            PasswordHash = passwordHash,
            CountryId = createUserDTO.CountryId,
            // Invariant casing: culture-sensitive ToUpper() maps "i" to "İ" under tr-TR and breaks lookups.
            NormalizedUserName = createUserDTO.UserName.ToUpperInvariant(),
            NormalizedEmail = createUserDTO.Email.ToUpperInvariant(),
        };

        var rawToken = TokenGenerator.GenerateToken();
        user.ApplicationUserTokens.Add(new ApplicationUserToken
        {
            Token = TokenGenerator.HashToken(rawToken),
            TokenType = UserTokenType.EmailConfirmation,
            ExpiryDate = DateTimeOffset.UtcNow.Add(emailConfirmationTokenLifetime ?? DefaultEmailConfirmationTokenLifetime),
        });

        user.RegisterDomainEvent(new UserCreated(user.Id, user.Email, user.ContactName, rawToken));

        return user;
    }

    // Version-7 GUIDs are time-ordered, which keeps clustered indexes append-only; not available before .NET 9.
    private static Guid NewUserId()
    {
#if NET9_0_OR_GREATER
        return Guid.CreateVersion7();
#else
        return Guid.NewGuid();
#endif
    }

    /// <summary>
    /// Verifies that the user is active, throwing <see cref="DomainException"/> if not.
    /// </summary>
    protected virtual void EnsureActive()
    {
        if (!_user.IsActive)
            throw DomainException.CreateDetailedException("The user was not found or is no longer active.", "UserNotFoundOrDeleted", "PhoneNumberOrEmail");
    }

    /// <summary>
    /// Issues a token of the given type: the hash is stored on the user, the raw value is returned for delivery.
    /// </summary>
    protected virtual string IssueToken(UserTokenType type, TimeSpan lifetime)
    {
        var rawToken = TokenGenerator.GenerateToken();

        _user.ApplicationUserTokens.Add(new ApplicationUserToken
        {
            Token = TokenGenerator.HashToken(rawToken),
            TokenType = type,
            ExpiryDate = DateTimeOffset.UtcNow.Add(lifetime),
        });

        return rawToken;
    }

    /// <summary>
    /// Validates a raw token against the newest unexpired token of the given type and, on success,
    /// removes every token of that type so none can be replayed. Older tokens are never accepted.
    /// </summary>
    protected virtual bool ConsumeToken(UserTokenType type, string? rawToken)
    {
        var now = DateTimeOffset.UtcNow;

        var newest = _user.ApplicationUserTokens
            .Where(t => t.TokenType == type && t.ExpiryDate > now)
            .OrderByDescending(t => t.ExpiryDate)
            .FirstOrDefault();

        if (newest is null || !TokenGenerator.VerifyToken(rawToken, newest.Token))
            return false;

        foreach (var stale in _user.ApplicationUserTokens.Where(t => t.TokenType == type).ToList())
            _user.ApplicationUserTokens.Remove(stale);

        return true;
    }

    /// <summary>
    /// Compares two strings in constant time to prevent timing attacks.
    /// </summary>
    protected virtual bool FixedTimeEquals(string? a, string? b)
    {
        if (a is null || b is null)
            return false;

        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    }
}
