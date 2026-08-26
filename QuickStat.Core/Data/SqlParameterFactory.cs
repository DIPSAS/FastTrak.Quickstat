using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;

namespace QuickStat.Data;

/// <summary>
/// Builds <c>SqlParameter</c>s with explicit types.
/// </summary>
/// <remarks>
/// <para>
/// The mapping reproduces what ADO inferred from a Delphi <c>Variant</c>
/// (<c>Docs/Port/01-data-access.md</c> §2.4): <c>UnicodeString</c> became <c>adVarWChar</c>,
/// <c>TDateTime</c> became <c>adDBTimeStamp</c>, an empty string stayed an empty string rather than
/// becoming <c>NULL</c>. Keeping those choices keeps the existing execution plans.
/// </para>
/// <para>
/// <c>AddWithValue</c> is deliberately not used: it infers <c>datetime2</c> and
/// <c>nvarchar(actual length)</c>, which changes both the plan shape and the plan cache hit rate.
/// </para>
/// </remarks>
internal static class SqlParameterFactory
{
    /// <summary>
    /// Below this year <c>datetime</c> cannot represent the value at all, so it is worth a clear
    /// message rather than SqlClient's own.
    /// </summary>
    private const int MinimumSqlDateTimeYear = 1753;

    /// <summary>Length at which an NVARCHAR parameter is declared as MAX.</summary>
    private const int MaximumInlineStringLength = 4000;

    public static SqlParameter Create(string name, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string parameterName = "@" + name;

        switch (value)
        {
            case null or DBNull:
                // Left untyped on purpose. SqlClient declares an NVARCHAR NULL, which SQL Server
                // converts to whatever the target expects - exactly what ADO did with varNull.
                return new SqlParameter(parameterName, SqlDbType.NVarChar) { Value = DBNull.Value, Size = 1 };

            case string text:
                return new SqlParameter(parameterName, SqlDbType.NVarChar)
                {
                    Size = text.Length > MaximumInlineStringLength ? -1 : MaximumInlineStringLength,
                    Value = text,
                };

            case bool flag:
                return new SqlParameter(parameterName, SqlDbType.Bit) { Value = flag };

            case byte or sbyte or short or ushort or int:
                return new SqlParameter(parameterName, SqlDbType.Int)
                {
                    Value = Convert.ToInt32(value, CultureInfo.InvariantCulture),
                };

            case long or uint:
                return new SqlParameter(parameterName, SqlDbType.BigInt)
                {
                    Value = Convert.ToInt64(value, CultureInfo.InvariantCulture),
                };

            case float or double:
                return new SqlParameter(parameterName, SqlDbType.Float)
                {
                    Value = Convert.ToDouble(value, CultureInfo.InvariantCulture),
                };

            case decimal money:
                // ADO mapped Currency to adCurrency, i.e. MONEY: precision 19, scale 4.
                return new SqlParameter(parameterName, SqlDbType.Decimal)
                {
                    Precision = 19,
                    Scale = 4,
                    Value = money,
                };

            case DateTime timestamp:
                if (timestamp.Year < MinimumSqlDateTimeYear)
                {
                    // Formatted invariantly, and it matters more than it looks. An interpolated hole
                    // uses CurrentCulture, and a culture whose default calendar is not Gregorian -
                    // ar-SA uses Umm al-Qura, which starts in the twentieth century - cannot render
                    // year 1 or 1752 at all. It throws ArgumentOutOfRangeException while building the
                    // message for the exception we are in the middle of throwing, so on an Arabic
                    // machine an out-of-range date produced a baffling error from inside string
                    // formatting instead of the clear one written here. Diagnostics should be
                    // invariant regardless: a log line saying 1752-01-01 is what an operator wants,
                    // whatever their locale.
                    throw new SqlParameterBindingException(
                        string.Create(
                            CultureInfo.InvariantCulture,
                            $"Parameter ':{name}' is {timestamp:yyyy-MM-dd}, which SQL Server's DATETIME cannot " +
                            $"represent (its range starts in {MinimumSqlDateTimeYear}). A default-constructed " +
                            $"DateTime is almost always an unset value that should have been NULL."))
                    {
                        ParameterName = name,
                    };
                }

                return new SqlParameter(parameterName, SqlDbType.DateTime) { Value = timestamp };

            case DateOnly date:
                return new SqlParameter(parameterName, SqlDbType.Date) { Value = date.ToDateTime(TimeOnly.MinValue) };

            case Guid guid:
                return new SqlParameter(parameterName, SqlDbType.UniqueIdentifier) { Value = guid };

            case byte[] bytes:
                return new SqlParameter(parameterName, SqlDbType.VarBinary) { Size = -1, Value = bytes };

            case Enum enumeration:
                return Create(name, Convert.ChangeType(
                    enumeration,
                    Enum.GetUnderlyingType(enumeration.GetType()),
                    CultureInfo.InvariantCulture));

            default:
                throw new SqlParameterBindingException(
                    $"Parameter ':{name}' has unsupported type {value.GetType().FullName}.")
                {
                    ParameterName = name,
                };
        }
    }

    /// <summary>Builds the single-column table-valued parameter.</summary>
    /// <param name="table">Name, type, column and values.</param>
    /// <returns>A structured parameter.</returns>
    /// <remarks>
    /// The values are streamed as <c>SqlDataRecord</c>s rather than materialised into a
    /// <c>DataTable</c>, so a 50 000-patient cohort costs one buffer, not one row object each.
    /// </remarks>
    public static SqlParameter CreateTableValued(SqlTableParameter table)
    {
        ArgumentNullException.ThrowIfNull(table);

        SqlParameter parameter = new("@" + table.Name, SqlDbType.Structured)
        {
            TypeName = table.TypeName,
        };

        // SqlClient rejects an empty SqlDataRecord enumeration outright; NULL is how a table-valued
        // parameter expresses "no rows", and the server sees an empty table variable.
        parameter.Value = table.Values.Count == 0 ? DBNull.Value : Stream(table);

        return parameter;
    }

    private static IEnumerable<SqlDataRecord> Stream(SqlTableParameter table)
    {
        SqlMetaData column = new(table.ColumnName, SqlDbType.Int);
        SqlDataRecord record = new(column);

        foreach (int value in table.Values)
        {
            record.SetInt32(0, value);
            yield return record;
        }
    }
}
