using QuickStat.Domain.DataPoints;
using Xunit;

namespace QuickStat.Tests.Domain.DataPoints;

/// <summary>
/// The rule table. Its one behaviour that is easy to lose is case sensitivity, and losing it is
/// silent: two of the sixteen registrations differ only in case.
/// </summary>
public class DataPointFactoryTests
{
    [Fact]
    public void LookupIsCaseSensitive()
    {
        DataPointFactory factory = new();

        Assert.True(factory.TryGetRule("DB_VERSION", out DataPointRule? installedVersion));
        Assert.True(factory.TryGetRule("DbVersion", out DataPointRule? serverVersion));

        Assert.NotSame(installedVersion, serverVersion);
        Assert.Same(StandardDataPointRules.DatabaseVersion, installedVersion);
        Assert.Same(StandardDataPointRules.ServerVersion, serverVersion);
    }

    [Theory]
    [InlineData("db_version")]
    [InlineData("DB_Version")]
    [InlineData("DBVERSION")]
    [InlineData("dbversion")]
    [InlineData("bmi")]
    public void ANearMissResolvesToNoRuleRatherThanTheWrongOne(string varName)
    {
        DataPointFactory factory = new();

        Assert.False(factory.TryGetRule(varName, out DataPointRule? rule));
        Assert.Null(rule);
    }

    [Fact]
    public void TheDefaultFactoryCarriesTheSixteenStandardRegistrations()
    {
        DataPointFactory factory = new();

        Assert.Equal(
            StandardDataPointRules.Registrations.Count,
            factory.RegisteredVariableNames.Count);

        foreach (string varName in StandardDataPointRules.Registrations.Keys)
        {
            Assert.True(factory.TryGetRule(varName, out _));
        }
    }

    [Fact]
    public void AFactoryCanBeBuiltEmpty()
    {
        DataPointFactory factory = new([]);

        Assert.Empty(factory.RegisteredVariableNames);
        Assert.False(factory.TryGetRule("BMI", out _));
    }

    [Fact]
    public void RegisterReplacesAnExistingRule()
    {
        // The Delphi uses AddOrSetValue, so a later registration wins.
        DataPointFactory factory = new();
        DataPointRule replacement = new() { FormatValue = static _ => "replaced" };

        factory.Register("BMI", replacement);

        Assert.True(factory.TryGetRule("BMI", out DataPointRule? rule));
        Assert.Same(replacement, rule);
    }

    [Fact]
    public void CreateProducesAnInitialisedDataPoint()
    {
        DataPointFactory factory = new();
        DateTime timestamp = new(2019, 8, 14, 0, 0, 0, DateTimeKind.Unspecified);

        DataPoint dataPoint = factory.Create("PATIENT.AGE", 97, timestamp, 4711);

        Assert.Equal("PATIENT.AGE", dataPoint.VarName);
        Assert.Equal(97, dataPoint.Value);
        Assert.Equal(timestamp, dataPoint.Timestamp);
        Assert.Equal(4711, dataPoint.RowId);
        Assert.Equal(1, dataPoint.UpdateCount);
        Assert.Equal(0, dataPoint.ItemId);
        Assert.Null(dataPoint.Caption);
    }
}
