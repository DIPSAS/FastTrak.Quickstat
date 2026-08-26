using System.Globalization;
using QuickStat.Domain.DataPoints;
using Xunit;

namespace QuickStat.Tests.Domain.DataPoints;

/// <summary>One cell value: <c>TDataPoint</c> (<c>EPR.QA.DataPoint.pas:12-22</c>).</summary>
public class DataPointTests
{
    private static readonly CultureInfo Norwegian = CultureInfo.GetCultureInfo("nb-NO");

    private static DataPoint Create(string varName = "AGE", double value = 97, int rowId = 4711)
    {
        DataPoint dataPoint = new() { VarName = varName };

        dataPoint.Update(value, new DateTime(2019, 8, 14, 0, 0, 0, DateTimeKind.Unspecified), rowId);

        return dataPoint;
    }

    [Fact]
    public void UpdateAssignsEverythingAndCountsTheCall()
    {
        DataPoint dataPoint = Create();

        Assert.Equal(97, dataPoint.Value);
        Assert.Equal(new DateTime(2019, 8, 14, 0, 0, 0, DateTimeKind.Unspecified), dataPoint.Timestamp);
        Assert.Equal(4711, dataPoint.RowId);
        Assert.Equal(1, dataPoint.UpdateCount);

        dataPoint.Update(98, new DateTime(2020, 8, 14, 0, 0, 0, DateTimeKind.Unspecified), 4712);

        Assert.Equal(98, dataPoint.Value);
        Assert.Equal(4712, dataPoint.RowId);
        Assert.Equal(2, dataPoint.UpdateCount);
    }

    [Fact]
    public void DescribeMatchesTheDelphiHintLayout()
    {
        DataPoint dataPoint = Create();

        // Format('%s = %g'#10'TimeStamp = %s'#10'RowId = %d'#10'Updates = %d', ...) - bare LF.
        Assert.Equal(
            "AGE = 97\nTimeStamp = 14.08.2019\nRowId = 4711\nUpdates = 1",
            dataPoint.Describe(Norwegian));
    }

    [Fact]
    public void DescribeAppendsTheItemIdOnlyWhenItIsPositive()
    {
        DataPoint dataPoint = Create();

        Assert.DoesNotContain("ItemId", dataPoint.Describe(Norwegian), StringComparison.Ordinal);

        dataPoint.ItemId = 5917;

        Assert.EndsWith("\nItemId = 5917", dataPoint.Describe(Norwegian), StringComparison.Ordinal);
    }

    [Fact]
    public void DescribeAppendsTheCaptionInQuotesWhenItIsSet()
    {
        DataPoint dataPoint = Create();

        Assert.DoesNotContain("Caption", dataPoint.Describe(Norwegian), StringComparison.Ordinal);

        dataPoint.Caption = "Metformin";

        // The Delphi's literal is 'Caption ="%s"' - space before the equals sign, none after.
        Assert.EndsWith("\nCaption =\"Metformin\"", dataPoint.Describe(Norwegian), StringComparison.Ordinal);
    }

    [Fact]
    public void DescribeUsesTheLocaleDecimalSeparatorAndShortDate()
    {
        DataPoint dataPoint = Create(value: 3.5);

        Assert.StartsWith("AGE = 3,5\nTimeStamp = 14.08.2019", dataPoint.Describe(Norwegian), StringComparison.Ordinal);
        Assert.StartsWith("AGE = 3.5\nTimeStamp = 08/14/2019", dataPoint.Describe(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }
}
