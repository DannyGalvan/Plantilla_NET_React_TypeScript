using Project.Application.Sequences;
using Project.SharedKernel.Domain;

namespace Project.UnitTests.BuildingBlocks;

public sealed class MoneyTests
{
    [Fact]
    public void Amounts_of_the_same_currency_combine()
    {
        var total = new Money(10.50m, "GTQ") + new Money(4.25m, "GTQ") - new Money(0.75m, "GTQ");
        total.ShouldBe(new Money(14.00m, "GTQ"));
        (total * 1.12m).Round().ShouldBe(new Money(15.68m, "GTQ"));
        (new Money(1, "USD") > Money.Zero("USD")).ShouldBeTrue();
    }

    [Fact]
    public void Different_currencies_never_mix()
    {
        Should.Throw<InvalidOperationException>(() => new Money(1, "USD") + new Money(1, "GTQ"));
        Should.Throw<ArgumentException>(() => new Money(1, "usd"));
        Should.Throw<ArgumentException>(() => new Money(1, "DOLLAR"));
    }

    [Fact]
    public void Allocation_keeps_every_cent()
    {
        var parts = new Money(100m, "USD").Allocate(3);
        parts.Select(p => p.Amount).ShouldBe([33.34m, 33.33m, 33.33m]);
        parts.Sum(p => p.Amount).ShouldBe(100m);
        new Money(2.675m, "USD").Round().Amount.ShouldBe(2.68m);
    }
}

public sealed class SequenceFormatterTests
{
    [Fact]
    public void Formats_dates_and_padded_counters()
    {
        var date = new DateTimeOffset(2026, 3, 7, 0, 0, 0, TimeSpan.Zero);
        Project.Modules.Sequences.Application.SequenceFormatter.Format("FAC-{yyyy}{MM}-{00000}", 42, date).ShouldBe("FAC-202603-00042");
        Project.Modules.Sequences.Application.SequenceFormatter.Format("{yy}/{dd}/{0}", 1234, date).ShouldBe("26/07/1234");
        Project.Modules.Sequences.Application.SequenceFormatter.IsValid("FAC-{yyyy}").ShouldBeFalse();
        Project.Modules.Sequences.Application.SequenceFormatter.Period(SequenceReset.Monthly, date).ShouldBe("202603");
    }
}
