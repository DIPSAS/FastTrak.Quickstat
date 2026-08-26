using QuickStat.Domain.Populations;
using Xunit;

namespace QuickStat.Tests.Domain.Populations;

/// <summary>
/// PORT-PLAN.md §7.2: the remembered period is keyed on a hash of the statement, not on the
/// statement.
/// </summary>
public class PeriodSettingsKeyTests
{
    private const string Sql = "EXEC dbo.GetCaseListHbA1c :StudyId, :StartDate, :StopDate";

    [Fact]
    public void TheKeyIsShortAndHexadecimal()
    {
        string key = PeriodSettingsKey.For(Sql);

        Assert.Equal(32, key.Length);
        Assert.All(key, character => Assert.True(
            "0123456789abcdef".Contains(character, StringComparison.Ordinal),
            $"'{character}' is not a lower-case hexadecimal digit."));
    }

    [Fact]
    public void TheKeyIsStable()
    {
        Assert.Equal(PeriodSettingsKey.For(Sql), PeriodSettingsKey.For(Sql));
    }

    [Fact]
    public void DifferentPopulationsGetDifferentKeys()
    {
        Assert.NotEqual(PeriodSettingsKey.For(Sql), PeriodSettingsKey.For(Sql + " -- v2"));
    }

    [Fact]
    public void TheKeyDoesNotContainTheStatement()
    {
        // The whole point: an INI key cannot hold a multi-line string containing '='.
        string key = PeriodSettingsKey.For(Sql);

        Assert.DoesNotContain("EXEC", key, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(":", key, StringComparison.Ordinal);
        Assert.DoesNotContain("=", key, StringComparison.Ordinal);
    }

    [Fact]
    public void AnEmptyStatementStillProducesAKey()
    {
        Assert.Equal(32, PeriodSettingsKey.For("").Length);
    }

    [Fact]
    public void NullIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => PeriodSettingsKey.For(null!));
    }

    [Fact]
    public void TheSuffixesAreDistinct()
    {
        Assert.NotEqual(PeriodSettingsKey.StartKeySuffix, PeriodSettingsKey.StopKeySuffix);
    }
}
