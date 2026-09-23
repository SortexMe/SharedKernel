using FluentAssertions;
using SharedKernel.Abstractions.Data;
using SharedKernel.Common.DTOs;
using SharedKernel.Common.DTOs.Auth;
using SharedKernel.Common.Exceptions;
using SharedKernel.DomainEvents.Auth;
using SharedKernel.DomainModels;
using SharedKernel.Entities.Auth;
using SharedKernel.Enumerations;
using SharedKernel.Utilities;
using System.Globalization;

namespace SharedKernel.Mediator.Tests.Domain;

public class UserDomainModelTests
{
    private sealed class RecordingUserRepository : IUserRepository
    {
        public int UpdateCalls { get; private set; }
        public Task<ApplicationUser> AddAsync(ApplicationUser entity, CancellationToken cancellationToken = default) => Task.FromResult(entity);
        public void Remove(ApplicationUser entity) { }
        public void Update(ApplicationUser entity) => UpdateCalls++;
    }

    private static ApplicationUser ActiveUser() => new()
    {
        Id = Guid.NewGuid(),
        IsActive = true,
        EmailConfirmed = true,
        Email = "jane@example.com",
        ContactName = "Jane",
        UserName = "jane",
        PasswordHash = "hash",
    };

    // ---------- lockout ----------

    [Fact]
    public void LoginUser_Should_Succeed_After_Lockout_Window_Has_Elapsed()
    {
        var user = ActiveUser();
        user.AccessFailedCount = 5;
        user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(-1);
        var model = new UserDomainModel(user, new RecordingUserRepository());

        var act = () => model.LoginUser(isPasswordValid: true, newRefreshToken: "rt");

        act.Should().NotThrow();
        user.AccessFailedCount.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
        user.RefreshToken.Should().Be("rt");
    }

    [Fact]
    public void LoginUser_Should_Reject_While_Lockout_Is_Active_With_Remaining_Time()
    {
        var user = ActiveUser();
        user.AccessFailedCount = 5;
        user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(10);
        var model = new UserDomainModel(user, new RecordingUserRepository());

        var act = () => model.LoginUser(isPasswordValid: true, newRefreshToken: "rt");

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Errors.Should().ContainSingle(e => e.ErrorCode == "AccountLocked");
        ex.Errors.Single().ErrorMessage.Should().MatchRegex(@"\b(9|10) minutes\b", "the message must state a real remaining time");
    }

    [Fact]
    public void LoginUser_Should_Lock_Account_On_Fifth_Failure()
    {
        var user = ActiveUser();
        user.AccessFailedCount = 4;
        var model = new UserDomainModel(user, new RecordingUserRepository());

        var act = () => model.LoginUser(isPasswordValid: false, newRefreshToken: "rt");

        act.Should().Throw<DomainException>();
        user.AccessFailedCount.Should().Be(5);
        user.LockoutEnd.Should().NotBeNull().And.BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void LoginUser_Should_Never_Lock_When_Lockout_Is_Disabled()
    {
        var user = ActiveUser();
        user.LockoutEnabled = false;
        user.AccessFailedCount = 50;
        var model = new UserDomainModel(user, new RecordingUserRepository());

        var act = () => model.LoginUser(isPasswordValid: true, newRefreshToken: "rt");

        act.Should().NotThrow();
    }

    // ---------- refresh token ----------

    [Fact]
    public void RefreshToken_Should_Reject_Null_Token_With_DomainException()
    {
        var user = ActiveUser();
        user.RefreshToken = "stored";
        var model = new UserDomainModel(user, new RecordingUserRepository());

        var act = () => model.RefreshToken(new TokenResponseDTO("at", null!), "new");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RefreshToken_Should_Persist_The_New_Token()
    {
        var user = ActiveUser();
        user.RefreshToken = "stored";
        var repo = new RecordingUserRepository();
        var model = new UserDomainModel(user, repo);

        model.RefreshToken(new TokenResponseDTO("at", "stored"), "new");

        user.RefreshToken.Should().Be("new");
        repo.UpdateCalls.Should().Be(1);
    }

    [Fact]
    public void RefreshToken_Should_Reject_Expired_Token()
    {
        var user = ActiveUser();
        user.RefreshToken = "stored";
        user.RefreshTokenExpiry = DateTimeOffset.UtcNow.AddMinutes(-1);
        var model = new UserDomainModel(user, new RecordingUserRepository());

        var act = () => model.RefreshToken(new TokenResponseDTO("at", "stored"), "new");

        act.Should().Throw<DomainException>();
    }

    // ---------- password reset ----------

    [Fact]
    public void ForgetPassword_Should_Store_A_Hash_And_Emit_The_Raw_Token()
    {
        var user = ActiveUser();
        var model = new UserDomainModel(user, new RecordingUserRepository());

        model.ForgetPassword();

        var stored = user.ApplicationUserTokens.Single(t => t.TokenType == UserTokenType.PasswordReset).Token;
        var emitted = user.DomainEvents.OfType<UserPasswordForgotten>().Single().Token;

        emitted.Should().NotBe(stored, "the database must not hold the bearer secret");
        TokenGenerator.VerifyToken(emitted, stored).Should().BeTrue();
    }

    [Fact]
    public void ResetPassword_Should_Consume_The_Token()
    {
        var user = ActiveUser();
        var model = new UserDomainModel(user, new RecordingUserRepository());
        model.ForgetPassword();
        var raw = user.DomainEvents.OfType<UserPasswordForgotten>().Single().Token;

        model.ResetPassword(raw, "hash-1");
        var replay = () => model.ResetPassword(raw, "hash-2");

        replay.Should().Throw<DomainException>();
        user.PasswordHash.Should().Be("hash-1");
        user.ApplicationUserTokens.Should().NotContain(t => t.TokenType == UserTokenType.PasswordReset);
    }

    [Fact]
    public void ResetPassword_Should_Reject_An_Older_Token_When_A_Newer_One_Exists()
    {
        var user = ActiveUser();
        var model = new UserDomainModel(user, new RecordingUserRepository());
        model.ForgetPassword();
        var first = user.DomainEvents.OfType<UserPasswordForgotten>().Single().Token;
        user.ClearDomainEvents();
        Thread.Sleep(30); // ensure a later ExpiryDate on coarse clocks
        model.ForgetPassword();

        var act = () => model.ResetPassword(first, "hash");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ResetPassword_Should_Reject_Unknown_Token()
    {
        var user = ActiveUser();
        var model = new UserDomainModel(user, new RecordingUserRepository());
        model.ForgetPassword();

        var act = () => model.ResetPassword("not-a-real-token", "hash");

        act.Should().Throw<DomainException>();
    }

    // ---------- email confirmation ----------

    [Fact]
    public void ConfirmEmail_Should_Accept_The_Raw_Token_From_The_Created_Event()
    {
        var user = UserDomainModel.Create(new CreateUserDTO("jane", "Jane", "jane@example.com", "pw", "555", Guid.NewGuid()), "hash");
        var raw = user.DomainEvents.OfType<UserCreated>().Single().Token;
        var stored = user.ApplicationUserTokens.Single().Token;
        var model = new UserDomainModel(user, new RecordingUserRepository());

        raw.Should().NotBe(stored);
        model.ConfirmEmail(raw);

        user.EmailConfirmed.Should().BeTrue();
        user.ApplicationUserTokens.Should().BeEmpty();
    }

    [Fact]
    public void ConfirmEmail_Should_Reject_Wrong_Token()
    {
        var user = UserDomainModel.Create(new CreateUserDTO("jane", "Jane", "jane@example.com", "pw", "555", Guid.NewGuid()), "hash");
        var model = new UserDomainModel(user, new RecordingUserRepository());

        var act = () => model.ConfirmEmail("wrong");

        act.Should().Throw<DomainException>();
        user.EmailConfirmed.Should().BeFalse();
    }

    // ---------- normalization ----------

    [Fact]
    public void Create_Should_Normalize_With_Invariant_Culture()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
        try
        {
            var user = UserDomainModel.Create(new CreateUserDTO("ihsan", "Ihsan", "ihsan@mail.com", "pw", "555", Guid.NewGuid()), "hash");

            user.NormalizedEmail.Should().Be("IHSAN@MAIL.COM");
            user.NormalizedUserName.Should().Be("IHSAN");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Constructor_Should_Reject_Nulls()
    {
        var ctor1 = () => new UserDomainModel(null!, new RecordingUserRepository());
        var ctor2 = () => new UserDomainModel(ActiveUser(), null!);

        ctor1.Should().Throw<ArgumentNullException>();
        ctor2.Should().Throw<ArgumentNullException>();
    }
}
