using FluentAssertions;
using SharedKernel.Entities;
using SharedKernel.Entities.Base;
using SharedKernel.ValueObjects;
using SharedKernel.ValueObjects.Base;

namespace SharedKernel.Mediator.Tests.Domain;

public class EntityAndValueObjectTests
{
    private sealed class Order : DomainEntityBase { }
    private sealed class Invoice : EntityBase { }
    private sealed class EmptyValue : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents() { yield break; }
    }

    // ---------- DomainEntityBase ----------

    [Fact]
    public void DomainEntity_Null_Equals_Null()
    {
        Order? a = null, b = null;

        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void DomainEntity_Null_Does_Not_Equal_Instance()
    {
        Order? a = null;
        var b = new Order { Id = Guid.NewGuid() };

        (a == b).Should().BeFalse();
        (b == a).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void Transient_DomainEntities_Are_Not_Equal()
    {
        var a = new Order();
        var b = new Order();

        a.Equals(b).Should().BeFalse("two unsaved entities have no identity yet");
        (a == b).Should().BeFalse();
        new HashSet<Order> { a, b }.Should().HaveCount(2);
    }

    [Fact]
    public void Persisted_DomainEntities_With_Same_Id_Are_Equal()
    {
        var id = Guid.NewGuid();
        var a = new Order { Id = id };
        var b = new Order { Id = id };

        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Same_Reference_Is_Equal_Even_When_Transient()
    {
        var a = new Order();
        var sameReference = a;

        (a == sameReference).Should().BeTrue();
        a.Equals(sameReference).Should().BeTrue();
    }

    // ---------- EntityBase ----------

    [Fact]
    public void Transient_Entities_Are_Not_Equal()
    {
        new Invoice().Equals(new Invoice()).Should().BeFalse();
    }

    [Fact]
    public void Entity_Null_Equals_Null()
    {
        Invoice? a = null, b = null;
        (a == b).Should().BeTrue();
    }

    // ---------- ValueObject ----------

    [Fact]
    public void ValueObject_With_No_Components_Has_A_HashCode()
    {
        var act = () => new EmptyValue().GetHashCode();

        act.Should().NotThrow();
    }

    [Fact]
    public void ValueObject_Equality_Operator_Matches_Equals()
    {
        var a = Location.FromLatLon(51.5, -0.12);
        var b = Location.FromLatLon(51.5, -0.12);

        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void ValueObject_HashCode_Is_Order_Sensitive()
    {
        var a = Location.FromLatLon(10, 20);
        var b = Location.FromLatLon(20, 10);

        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    [Fact]
    public void ValueObject_Null_Comparisons()
    {
        Location? a = null;
        var b = Location.FromLatLon(1, 1);

        (a == null).Should().BeTrue();
        (a == b).Should().BeFalse();
        (b != a).Should().BeTrue();
    }

    // ---------- DomainEventMessage ----------

    [Fact]
    public void DomainEventMessage_Counts_Processing_Attempts()
    {
        var message = DomainEventMessage.CreateMessage(Guid.NewGuid(), "T", "{}");

        message.DispatchFailure("boom");
        message.DispatchFailure("boom");
        message.DispatchDomainEvent();

        message.ProcessedTimes.Should().Be(3);
    }
}
