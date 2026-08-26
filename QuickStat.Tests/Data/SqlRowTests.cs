using QuickStat.Data;
using Xunit;

namespace QuickStat.Tests.Data;

/// <summary>
/// Delphi <c>TField</c> null semantics. Getting these wrong changes values in every export without
/// raising anything (<c>Docs/Port/01-data-access.md</c> §2.4).
/// </summary>
public class SqlRowTests
{
    private static SqlRow Row(params object?[] values) =>
        SqlResultSet.Create(["c0", "c1", "c2", "c3", "c4", "c5", "c6"], values)[0];

    [Fact]
    public void ZeroDateIsTheDelphiTDateTimeZero() =>
        Assert.Equal(new DateTime(1899, 12, 30, 0, 0, 0, DateTimeKind.Unspecified), SqlRow.ZeroDate);

    [Fact]
    public void NullReadsAsTheDelphiDefaultForEveryAccessor()
    {
        SqlRow row = Row(null, null, null, null, null, null, null);

        Assert.True(row.IsNull(0));
        Assert.Null(row.GetValue(0));

        // AsInteger -> 0, AsString -> '', AsFloat -> 0, AsBoolean -> False, AsDateTime -> 0.0.
        Assert.Equal(0, row.GetInt32(0));
        Assert.Equal(0L, row.GetInt64(1));
        Assert.Equal("", row.GetString(2));
        Assert.Equal(0d, row.GetDouble(3));
        Assert.Equal(0m, row.GetDecimal(4));
        Assert.False(row.GetBoolean(5));
        Assert.Equal(SqlRow.ZeroDate, row.GetDateTime(6));
    }

    [Fact]
    public void NullDoesNotBecomeDateTimeMinValue()
    {
        // 0001-01-01 would format as a plausible-looking date and would also be rejected by
        // SQL Server's DATETIME range on the way back in.
        Assert.NotEqual(DateTime.MinValue, Row(null, null, null, null, null, null, null).GetDateTime(0));
    }

    [Fact]
    public void ExplicitDefaultsWin()
    {
        SqlRow row = Row(null, null, null, null, null, null, null);

        Assert.Equal(-1, row.GetInt32(0, -1));
        Assert.Equal("n/a", row.GetString(1, "n/a"));
        Assert.Equal(1.5, row.GetDouble(2, 1.5));
        Assert.True(row.GetBoolean(3, true));
        Assert.Equal(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), row.GetDateTime(4, new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)));
    }

    [Fact]
    public void DbNullIsIndistinguishableFromNull()
    {
        SqlRow row = Row(DBNull.Value, null, null, null, null, null, null);

        Assert.True(row.IsNull(0));
        Assert.Equal(0, row.GetInt32(0));
    }

    [Fact]
    public void ReadsTypedValuesThrough()
    {
        DateTime when = new(2026, 8, 26, 14, 30, 0, DateTimeKind.Unspecified);

        SqlRow row = Row(42, 42L, "text", 1.25d, 9.99m, true, when);

        Assert.Equal(42, row.GetInt32(0));
        Assert.Equal(42L, row.GetInt64(1));
        Assert.Equal("text", row.GetString(2));
        Assert.Equal(1.25d, row.GetDouble(3));
        Assert.Equal(9.99m, row.GetDecimal(4));
        Assert.True(row.GetBoolean(5));
        Assert.Equal(when, row.GetDateTime(6));
    }

    [Fact]
    public void ConvertsBetweenColumnTypesTheWayTFieldDid()
    {
        SqlRow row = Row(3.6d, 42, 1, 0, "7", null, null);

        // TFloatField.GetAsInteger rounds.
        Assert.Equal(4, row.GetInt32(0));

        // TIntegerField.GetAsString renders.
        Assert.Equal("42", row.GetString(1));

        // TIntegerField.GetAsBoolean is 'not zero'.
        Assert.True(row.GetBoolean(2));
        Assert.False(row.GetBoolean(3));

        // TStringField.GetAsInteger parses.
        Assert.Equal(7, row.GetInt32(4));
    }

    [Theory]
    [InlineData("True", true)]
    [InlineData("false", false)]
    [InlineData("T", true)]
    [InlineData("Y", true)]
    [InlineData("J", true)]
    [InlineData("1", true)]
    [InlineData("N", false)]
    [InlineData("0", false)]
    public void ReadsBooleanTextTheWayTStringFieldDid(string text, bool expected) =>
        Assert.Equal(expected, Row(text, null, null, null, null, null, null).GetBoolean(0));

    [Fact]
    public void ReportsItsWidth() => Assert.Equal(7, Row(1, 2, 3, 4, 5, 6, 7).FieldCount);

    [Fact]
    public void RejectsAnOrdinalOutsideTheRow()
    {
        SqlRow row = Row(1, 2, 3, 4, 5, 6, 7);

        Assert.Throws<ArgumentOutOfRangeException>(() => row.GetInt32(7));
        Assert.Throws<ArgumentOutOfRangeException>(() => row.GetInt32(-1));
    }

    [Fact]
    public void ADefaultRowHasNoColumnsRatherThanCrashing()
    {
        SqlRow row = default;

        Assert.Equal(0, row.FieldCount);
        Assert.Throws<ArgumentOutOfRangeException>(() => row.GetInt32(0));
    }
}
