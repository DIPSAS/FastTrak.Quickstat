using QuickStat.Data;
using Xunit;

namespace QuickStat.Tests.Data;

/// <summary>
/// The buffered result set, and the two lookup behaviours it has to keep apart:
/// <c>TDataset.FindField</c> (tolerant) and <c>TDataset.FieldByName</c> (strict).
/// </summary>
public class SqlResultSetTests
{
    private static SqlResultSet Sample() => SqlResultSet.Create(
        ["PersonId", "VarName", "Value"],
        [1, "HB", 13.5],
        [2, "HB", null]);

    [Fact]
    public void ExposesColumnsInOrdinalOrder()
    {
        SqlResultSet result = Sample();

        Assert.Equal(3, result.Columns.Count);
        Assert.Equal(0, result.Columns[0].Ordinal);
        Assert.Equal("PersonId", result.Columns[0].Name);
        Assert.Equal(typeof(int), result.Columns[0].ClrType);
        Assert.Equal(typeof(string), result.Columns[1].ClrType);
        Assert.Equal(typeof(double), result.Columns[2].ClrType);
    }

    [Fact]
    public void CountsAndIndexesRows()
    {
        SqlResultSet result = Sample();

        Assert.Equal(2, result.Count);
        Assert.False(result.IsEmpty);
        Assert.Equal(1, result[0].GetInt32(0));
        Assert.Equal(2, result[1].GetInt32(0));
    }

    [Fact]
    public void Enumerates()
    {
        Assert.Equal<int>([1, 2], [.. Sample().Select(row => row.GetInt32(0))]);
    }

    [Fact]
    public void IndexOfIsCaseInsensitiveAndToleratesAbsence()
    {
        SqlResultSet result = Sample();

        Assert.Equal(1, result.IndexOf("varname"));
        Assert.Equal(-1, result.IndexOf("Caption"));
    }

    [Fact]
    public void GetOrdinalRaisesWhenTheColumnIsAbsent()
    {
        SqlResultSet result = Sample();

        Assert.Equal(2, result.GetOrdinal("VALUE"));

        SqlCommandFailedException exception = Assert.Throws<SqlCommandFailedException>(() => result.GetOrdinal("Caption"));

        // A support engineer reading the log needs to see what the result set actually contained.
        Assert.Contains("Caption", exception.Message, StringComparison.Ordinal);
        Assert.Contains("VarName", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyHasNoColumnsAndNoRows()
    {
        Assert.True(SqlResultSet.Empty.IsEmpty);
        Assert.Empty(SqlResultSet.Empty);
        Assert.Empty(SqlResultSet.Empty.Columns);
    }

    [Fact]
    public void RejectsAnIndexOutsideTheResultSet()
    {
        SqlResultSet result = Sample();

        Assert.Throws<ArgumentOutOfRangeException>(() => result[2]);
    }

    [Fact]
    public void NormalisesDbNullOnConstruction()
    {
        SqlResultSet result = new(
            [new SqlColumn(0, "Value", typeof(object))],
            [[DBNull.Value]]);

        Assert.True(result[0].IsNull(0));
    }

    [Fact]
    public void PadsAShortRowRatherThanThrowing()
    {
        // A fake that supplies fewer values than columns should read as NULL, not as an exception
        // from deep inside an unrelated test.
        SqlResultSet result = new(
            [new SqlColumn(0, "A", typeof(int)), new SqlColumn(1, "B", typeof(int))],
            [[1]]);

        Assert.Equal(1, result[0].GetInt32(0));
        Assert.True(result[0].IsNull(1));
    }

    [Fact]
    public void RejectsARowWiderThanTheColumnList()
    {
        // Silently dropping the tail would hide a typo in someone else's fake for a long time.
        _ = Assert.Throws<ArgumentException>(() => SqlResultSet.Create(["A"], [1, 2]));
    }

    [Fact]
    public void ADuplicatedColumnNameResolvesToTheFirst()
    {
        SqlResultSet result = SqlResultSet.Create(["Id", "Id"], [1, 2]);

        Assert.Equal(0, result.IndexOf("Id"));
    }
}
