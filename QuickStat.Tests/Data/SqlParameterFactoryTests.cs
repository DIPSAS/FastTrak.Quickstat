using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;
using QuickStat.Data;
using Xunit;

namespace QuickStat.Tests.Data;

/// <summary>
/// CLR value to <c>SqlParameter</c> mapping.
/// </summary>
/// <remarks>
/// <c>SqlParameter</c> needs no connection, so this is testable without a server even though it is
/// the one place step 2.2 touches <c>Microsoft.Data.SqlClient</c> types directly. The mapping
/// reproduces what ADO inferred from a Delphi <c>Variant</c>
/// (<c>Docs/Port/01-data-access.md</c> §2.4), which keeps the existing execution plans.
/// </remarks>
public class SqlParameterFactoryTests
{
    [Fact]
    public void PrefixesTheNameWithTheAdoNetMarker() =>
        Assert.Equal("@StudyId", SqlParameterFactory.Create("StudyId", 1).ParameterName);

    [Theory]
    [InlineData((byte)1)]
    [InlineData((short)1)]
    [InlineData(1)]
    public void MapsSmallIntegralTypesToInt(object value)
    {
        SqlParameter parameter = SqlParameterFactory.Create("p", value);

        Assert.Equal(SqlDbType.Int, parameter.SqlDbType);
        Assert.Equal(1, parameter.Value);
    }

    [Fact]
    public void MapsLongToBigInt()
    {
        SqlParameter parameter = SqlParameterFactory.Create("p", 1L);

        Assert.Equal(SqlDbType.BigInt, parameter.SqlDbType);
        Assert.Equal(1L, parameter.Value);
    }

    [Fact]
    public void MapsBooleanToBit() => Assert.Equal(SqlDbType.Bit, SqlParameterFactory.Create("p", true).SqlDbType);

    [Fact]
    public void MapsDoubleToFloat() => Assert.Equal(SqlDbType.Float, SqlParameterFactory.Create("p", 1.5d).SqlDbType);

    [Fact]
    public void MapsDecimalToMoneyPrecisionAndScale()
    {
        // ADO mapped Currency to adCurrency, i.e. MONEY.
        SqlParameter parameter = SqlParameterFactory.Create("p", 1.5m);

        Assert.Equal(SqlDbType.Decimal, parameter.SqlDbType);
        Assert.Equal(19, parameter.Precision);
        Assert.Equal(4, parameter.Scale);
    }

    [Fact]
    public void MapsStringToNVarCharWithAStablePlanShape()
    {
        // Fixed size, not the actual length: AddWithValue would emit nvarchar(<length>) and produce
        // a fresh plan for every distinct argument length.
        SqlParameter parameter = SqlParameterFactory.Create("p", "NDV");

        Assert.Equal(SqlDbType.NVarChar, parameter.SqlDbType);
        Assert.Equal(4000, parameter.Size);
        Assert.Equal("NDV", parameter.Value);
    }

    [Fact]
    public void MapsALongStringToNVarCharMax()
    {
        SqlParameter parameter = SqlParameterFactory.Create("p", new string('x', 4001));

        Assert.Equal(SqlDbType.NVarChar, parameter.SqlDbType);
        Assert.Equal(-1, parameter.Size);
    }

    [Fact]
    public void KeepsTheEmptyStringAsAnEmptyString()
    {
        // Delphi: '' is varUString of length 0, i.e. N'', not NULL.
        SqlParameter parameter = SqlParameterFactory.Create("p", "");

        Assert.Equal("", parameter.Value);
        Assert.NotEqual(DBNull.Value, parameter.Value);
    }

    [Fact]
    public void MapsDateTimeToDateTimeNotDateTime2()
    {
        // adDBTimeStamp was DATETIME; datetime2 changes comparison semantics against DATETIME
        // columns and can change the plan.
        SqlParameter parameter = SqlParameterFactory.Create("p", new DateTime(2026, 8, 26, 0, 0, 0, DateTimeKind.Unspecified));

        Assert.Equal(SqlDbType.DateTime, parameter.SqlDbType);
    }

    [Fact]
    public void AcceptsTheDelphiZeroDate()
    {
        // 1899-12-30 is what SqlRow yields for a null timestamp, and it is inside DATETIME's range.
        SqlParameter parameter = SqlParameterFactory.Create("p", SqlRow.ZeroDate);

        Assert.Equal(SqlRow.ZeroDate, parameter.Value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1752)]
    public void RejectsADateSqlServerCannotStore(int year)
    {
        SqlParameterBindingException exception = Assert.Throws<SqlParameterBindingException>(
            () => SqlParameterFactory.Create("Born", new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)));

        Assert.Equal("Born", exception.ParameterName);
        Assert.Contains("1753", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MapsDateOnlyToDate()
    {
        SqlParameter parameter = SqlParameterFactory.Create("p", new DateOnly(2026, 8, 26));

        Assert.Equal(SqlDbType.Date, parameter.SqlDbType);
    }

    [Fact]
    public void MapsGuidToUniqueIdentifier() =>
        Assert.Equal(SqlDbType.UniqueIdentifier, SqlParameterFactory.Create("p", Guid.NewGuid()).SqlDbType);

    [Fact]
    public void MapsByteArrayToVarBinaryMax()
    {
        SqlParameter parameter = SqlParameterFactory.Create("p", new byte[] { 1, 2, 3 });

        Assert.Equal(SqlDbType.VarBinary, parameter.SqlDbType);
        Assert.Equal(-1, parameter.Size);
    }

    [Fact]
    public void MapsAnEnumThroughItsUnderlyingType()
    {
        SqlParameter parameter = SqlParameterFactory.Create("p", DayOfWeek.Wednesday);

        Assert.Equal(SqlDbType.Int, parameter.SqlDbType);
        Assert.Equal(3, parameter.Value);
    }

    [Fact]
    public void MapsNullToDbNull()
    {
        Assert.Equal(DBNull.Value, SqlParameterFactory.Create("p", null).Value);
        Assert.Equal(DBNull.Value, SqlParameterFactory.Create("p", DBNull.Value).Value);
    }

    [Fact]
    public void RejectsATypeItCannotMap()
    {
        SqlParameterBindingException exception = Assert.Throws<SqlParameterBindingException>(
            () => SqlParameterFactory.Create("p", new Uri("https://example.invalid")));

        Assert.Equal("p", exception.ParameterName);
    }

    // ---------------------------------------------------------------- table-valued

    [Fact]
    public void BuildsAStructuredParameterThatStreamsItsRows()
    {
        SqlParameter parameter = SqlParameterFactory.CreateTableValued(new SqlTableParameter
        {
            Name = "PersonIds",
            TypeName = "Report.PersonIdList",
            ColumnName = "PersonId",
            Values = [11, 22, 33],
        });

        Assert.Equal("@PersonIds", parameter.ParameterName);
        Assert.Equal(SqlDbType.Structured, parameter.SqlDbType);
        Assert.Equal("Report.PersonIdList", parameter.TypeName);

        IEnumerable<SqlDataRecord> records = Assert.IsAssignableFrom<IEnumerable<SqlDataRecord>>(parameter.Value);

        // The records are streamed, so the values have to be read as they arrive.
        List<int> read = [.. records.Select(record => record.GetInt32(0))];

        Assert.Equal<int>([11, 22, 33], [.. read]);
    }

    [Fact]
    public void NamesTheSingleColumn()
    {
        SqlParameter parameter = SqlParameterFactory.CreateTableValued(new SqlTableParameter
        {
            Name = "Ids",
            TypeName = "Report.PersonIdList",
            ColumnName = "PersonId",
            Values = [1],
        });

        SqlDataRecord record = Assert.Single(Assert.IsAssignableFrom<IEnumerable<SqlDataRecord>>(parameter.Value));

        Assert.Equal("PersonId", record.GetName(0));
        Assert.Equal(SqlDbType.Int, record.GetSqlMetaData(0).SqlDbType);
    }

    [Fact]
    public void PassesAnEmptyListAsNull()
    {
        // SqlClient rejects an empty SqlDataRecord enumeration outright; NULL is how a table-valued
        // parameter expresses 'no rows', and the server sees an empty table variable.
        SqlParameter parameter = SqlParameterFactory.CreateTableValued(new SqlTableParameter
        {
            Name = "Ids",
            TypeName = "Report.PersonIdList",
            ColumnName = "PersonId",
            Values = [],
        });

        Assert.Equal(DBNull.Value, parameter.Value);
    }

    [Fact]
    public void CarriesFarMoreIdsThanSqlServerAllowsParameters()
    {
        SqlParameter parameter = SqlParameterFactory.CreateTableValued(new SqlTableParameter
        {
            Name = "Ids",
            TypeName = "Report.PersonIdList",
            ColumnName = "PersonId",
            Values = [.. Enumerable.Range(1, 50_000)],
        });

        Assert.Equal(50_000, Assert.IsAssignableFrom<IEnumerable<SqlDataRecord>>(parameter.Value).Count());
    }
}
