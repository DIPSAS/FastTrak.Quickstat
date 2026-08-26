using QuickStat.Configuration;

namespace QuickStat.Data;

/// <summary>
/// A table-valued argument: one integer column, streamed to the server as a single parameter.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately restricted to <see cref="int"/> values, because every table-valued argument in this
/// application is a list of <c>PersonId</c>s: national-id recovery
/// (<c>Docs/Port/02-populations-patients.md</c> §5.4) and the <c>{IdList}</c> fragment of every
/// batched collector (<c>Docs/Port/03-collectors.md</c> §C.4). A general-purpose TVP abstraction
/// would leak <c>SqlDbType</c> into the contract for no present benefit.
/// </para>
/// <para>
/// The type name comes from <see cref="QuickStat.Configuration.SqlOptions.PersonIdListTypeName"/>,
/// so a customer database without the type - or without the rights to create it - can be detected
/// once and fall back to chunked literals without any collector knowing.
/// </para>
/// </remarks>
public sealed record SqlTableParameter
{
    /// <summary>Placeholder name as it appears in the statement, without the leading marker.</summary>
    public required string Name { get; init; }

    /// <summary>Schema-qualified table type, for example <c>Report.PersonIdList</c>.</summary>
    public required string TypeName { get; init; }

    /// <summary>Name of the type's single column, for example <c>PersonId</c>.</summary>
    public required string ColumnName { get; init; }

    /// <summary>The values. Streamed rather than materialised into a table object.</summary>
    public required IReadOnlyCollection<int> Values { get; init; }

    /// <summary>Builds a person-id list, taking the type and column names from configuration.</summary>
    /// <param name="options">The process-wide SQL options.</param>
    /// <param name="name">Placeholder name as it appears in the statement, without the marker.</param>
    /// <param name="personIds">The ids.</param>
    /// <returns>The table-valued argument.</returns>
    /// <exception cref="InvalidOperationException">
    /// <see cref="SqlOptions.PersonIdListTypeName"/> is <see langword="null"/>, i.e. the database has
    /// no such table type and the caller must fall back to chunks of
    /// <see cref="SqlOptions.MaxIdsPerBatch"/> literals.
    /// </exception>
    /// <remarks>
    /// Declared here, not in each of the two consumers, so that "the type and column names come from
    /// <see cref="SqlOptions"/>" is stated once. Steps 2.3 (national-id recovery) and 2.4 (every
    /// <c>{IdList}</c> collector) both call it.
    /// </remarks>
    public static SqlTableParameter ForPersonIds(SqlOptions options, string name, IReadOnlyCollection<int> personIds)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(personIds);

        string typeName = options.PersonIdListTypeName
            ?? throw new InvalidOperationException(
                "SqlOptions.PersonIdListTypeName is null, so this database has no person-id table type. " +
                $"Fall back to chunks of {options.MaxIdsPerBatch} literals instead.");

        return new SqlTableParameter
        {
            Name = name,
            TypeName = typeName,
            ColumnName = options.PersonIdListColumnName,
            Values = personIds,
        };
    }
}
