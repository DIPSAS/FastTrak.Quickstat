using QuickStat.Domain.Populations;
using Xunit;

namespace QuickStat.Tests.Domain.Populations;

/// <summary>
/// PORT-PLAN.md R8: the period is <c>[Start, Stop)</c>. Getting the exclusive end wrong shifts every
/// cohort by a day, silently, so both boundaries are pinned here.
/// </summary>
public class HalfOpenPeriodTests
{
    private static readonly DateTime Start = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime Stop = new(2024, 2, 1, 0, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public void TheFirstInstantIsInside()
    {
        Assert.True(new HalfOpenPeriod(Start, Stop).Contains(Start));
    }

    [Fact]
    public void TheLastInstantBeforeTheEndIsInside()
    {
        Assert.True(new HalfOpenPeriod(Start, Stop).Contains(Stop.AddTicks(-1)));
    }

    [Fact]
    public void TheEndItselfIsOutside()
    {
        // "til men ikke inkludert siste dato" - the dialog says so to the user in as many words.
        Assert.False(new HalfOpenPeriod(Start, Stop).Contains(Stop));
    }

    [Fact]
    public void TheInstantBeforeTheStartIsOutside()
    {
        Assert.False(new HalfOpenPeriod(Start, Stop).Contains(Start.AddTicks(-1)));
    }

    [Fact]
    public void ADayInsideTheRangeIsInside()
    {
        Assert.True(new HalfOpenPeriod(Start, Stop).Contains(new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Unspecified)));
    }

    [Fact]
    public void AProperPeriodIsValid()
    {
        Assert.True(new HalfOpenPeriod(Start, Stop).IsValid);
    }

    [Fact]
    public void AnEmptyPeriodIsRejected()
    {
        // Emetra.VclForm.Period.pas:52 is strictly Start < Stop, so equal dates disable OK.
        Assert.False(new HalfOpenPeriod(Start, Start).IsValid);
    }

    [Fact]
    public void AnInvertedPeriodIsRejected()
    {
        Assert.False(new HalfOpenPeriod(Stop, Start).IsValid);
    }

    [Fact]
    public void OneTickIsEnoughToBeValid()
    {
        Assert.True(new HalfOpenPeriod(Start, Start.AddTicks(1)).IsValid);
    }

    [Fact]
    public void AnEmptyPeriodContainsNothingNotEvenItsOwnStart()
    {
        Assert.False(new HalfOpenPeriod(Start, Start).Contains(Start));
    }
}
