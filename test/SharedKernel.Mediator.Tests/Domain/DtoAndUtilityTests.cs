using FluentAssertions;
using SharedKernel.Common.DTOs;
using SharedKernel.Common.DTOs.Auth;
using SharedKernel.Common.Exceptions;
using SharedKernel.DomainEvents.Auth;
using SharedKernel.Extensions;
using SharedKernel.Options;
using SharedKernel.Utilities;
using System.Globalization;
using System.Text.Json;

namespace SharedKernel.Mediator.Tests.Domain;

public class DtoAndUtilityTests
{
    // ---------- BaseResponseDTO ----------

    [Fact]
    public void BaseResponseDTO_Error_Round_Trips_Through_SystemTextJson()
    {
        var original = BaseResponseDTO.WithErrors(
        [
            DTOValidationError.CreateDetailedError("Name is required", "Required", "Name"),
            DTOValidationError.CreateSimpleError("Too slow", "Timeout"),
        ]);
        original.StatusCode = 400;
        original.TraceId = "abc";

        var json = JsonSerializer.Serialize(original);
        var back = JsonSerializer.Deserialize<BaseResponseDTO>(json)!;

        back.IsSuccess.Should().BeFalse();
        back.StatusCode.Should().Be(400);
        back.TraceId.Should().Be("abc");
        back.Message.Should().Be(original.Message);
        back.Errors.Should().HaveCount(2);
        back.Errors.First().PropertyName.Should().Be("Name");
        back.Errors.First().ErrorCode.Should().Be("Required");
    }

    [Fact]
    public void BaseResponseDTO_Success_Round_Trips_And_Has_200()
    {
        var original = BaseResponseDTO.WithSuccess();

        original.StatusCode.Should().Be(200);

        var back = JsonSerializer.Deserialize<BaseResponseDTO>(JsonSerializer.Serialize(original))!;
        back.IsSuccess.Should().BeTrue();
        back.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Generic_BaseResponseDTO_Round_Trips()
    {
        var original = BaseResponseDTO<int[]>.WithSuccess([1, 2, 3]);

        var back = JsonSerializer.Deserialize<BaseResponseDTO<int[]>>(JsonSerializer.Serialize(original))!;

        back.Data.Should().Equal(1, 2, 3);
        back.IsSuccess.Should().BeTrue();
        back.StatusCode.Should().Be(200);
    }

    [Fact]
    public void BaseResponseDTO_Round_Trips_With_CamelCase_Web_Defaults()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var original = BaseResponseDTO.WithError(DTOValidationError.CreateSimpleError("bad", "E1"));

        var json = JsonSerializer.Serialize(original, options);
        json.Should().Contain("\"errors\"").And.Contain("\"errorCode\"");

        var back = JsonSerializer.Deserialize<BaseResponseDTO>(json, options)!;
        back.Errors.Should().ContainSingle(e => e.ErrorCode == "E1");
    }

    // ---------- DomainException ----------

    [Fact]
    public void DomainException_From_Errors_Has_A_Meaningful_Message()
    {
        var ex = DomainException.CreateWithErrors(
        [
            DTOValidationError.CreateSimpleError("first problem", "A"),
            DTOValidationError.CreateSimpleError("second problem", "B"),
        ]);

        ex.Message.Should().Contain("first problem").And.Contain("second problem");
    }

    // ---------- secrets never in ToString ----------

    [Fact]
    public void Records_Carrying_Secrets_Redact_Them_In_ToString()
    {
        var dto = new CreateUserDTO("jdoe", "J Doe", "j@d.com", "Sup3rSecret!", "555", Guid.NewGuid());
        var broker = new MessageBrokerHost { HostName = "h", UserName = "u", Password = "brokerPa55" };
        var tokens = new TokenResponseDTO("access-xyz", "refresh-xyz");
        var forgotten = new UserPasswordForgotten(Guid.NewGuid(), "j@d.com", "J", "raw-reset-token");
        var created = new UserCreated(Guid.NewGuid(), "j@d.com", "J", "raw-confirm-token");

        dto.ToString().Should().NotContain("Sup3rSecret!").And.Contain("jdoe");
        broker.ToString().Should().NotContain("brokerPa55").And.Contain("HostName");
        tokens.ToString().Should().NotContain("access-xyz").And.NotContain("refresh-xyz");
        forgotten.ToString().Should().NotContain("raw-reset-token").And.Contain("j@d.com");
        created.ToString().Should().NotContain("raw-confirm-token");
    }

    [Fact]
    public void MessageBrokerHost_Defaults_To_AMQPS_Port()
    {
        new MessageBrokerHost { HostName = "h", UserName = "u", Password = "p" }.Port.Should().Be(5671);
    }

    // ---------- DomainEventBase ----------

    [Fact]
    public void DomainEvent_OccurrenceTime_Round_Trips_Through_Json()
    {
        var original = new UserPasswordReset(Guid.NewGuid(), "j@d.com", "J");
        var json = JsonSerializer.Serialize(original);

        var back = JsonSerializer.Deserialize<UserPasswordReset>(json)!;

        json.Should().Contain("OccurrenceTime").And.NotContain("OccurranceTime");
        back.OccurrenceTime.Should().Be(original.OccurrenceTime);
        back.Should().Be(original);
    }

    [Fact]
    public void DomainEvent_Obsolete_Alias_Mirrors_New_Name_And_Stays_Out_Of_ToString()
    {
        var evt = new UserPasswordReset(Guid.NewGuid(), "j@d.com", "J");

#pragma warning disable SK0001
        evt.OccurranceTime.Should().Be(evt.OccurrenceTime);
#pragma warning restore SK0001
        evt.ToString().Should().Contain("OccurrenceTime").And.NotContain("OccurranceTime");
    }

    // ---------- TokenGenerator ----------

    [Fact]
    public void TokenGenerator_Verify_Uses_Hash_At_Rest()
    {
        var raw = TokenGenerator.GenerateToken();
        var stored = TokenGenerator.HashToken(raw);

        stored.Should().NotBe(raw);
        TokenGenerator.VerifyToken(raw, stored).Should().BeTrue();
        TokenGenerator.VerifyToken(raw + "x", stored).Should().BeFalse();
        TokenGenerator.VerifyToken(null, stored).Should().BeFalse();
        TokenGenerator.VerifyToken(raw, null).Should().BeFalse();
    }

    [Fact]
    public void TokenGenerator_Rejects_Non_Positive_Length()
    {
        var act = () => TokenGenerator.GenerateToken(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---------- StringExtensions ----------

    [Fact]
    public void ToInt_And_ToLong_Ignore_Current_Culture()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            "1234".ToInt().Should().Be(1234);
            "1234".ToLong().Should().Be(1234L);
            "1.234".ToInt().Should().Be(0, "a thousands separator is not a valid integer");
            " 42 ".ToInt().Should().Be(42);
            "".ToInt().Should().Be(0);
            ((string?)null).ToLong().Should().Be(0);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
