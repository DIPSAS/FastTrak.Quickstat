using QuickStat.Configuration;
using QuickStat.Data;

namespace QuickStat.Collectors;

/// <summary>
/// Passes the ids as a single table-valued parameter.
/// </summary>
/// <remarks>
/// <para>
/// The recommended long-term mechanism (<c>Docs/Port/03-collectors.md</c> §C.4): one parameter, no
/// element limit, one cached plan per collector, and a real cardinality estimate for the optimiser.
/// </para>
/// <para>
/// <b>Not registered by default.</b> It needs the table type named by
/// <see cref="SqlOptions.PersonIdListTypeName"/> to exist, which requires a migration that has not
/// shipped, and there is no capability detection in the contract yet. Registering the literal
/// binder instead keeps the port working against every existing database and keeps the generated
/// statement comparable with a Delphi trace. Swapping the registration is a one-line change once
/// the type and a startup probe exist.
/// </para>
/// </remarks>
public sealed class TableValuedPersonIdListBinder : IPersonIdListBinder
{
    /// <summary>Placeholder name of the table-valued argument, without its marker.</summary>
    public const string ParameterName = "pids";

    private readonly SqlOptions _options;

    /// <summary>Creates the binder.</summary>
    /// <param name="options">
    /// Supplies <see cref="SqlOptions.PersonIdListTypeName"/> and
    /// <see cref="SqlOptions.PersonIdListColumnName"/>.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// <see cref="SqlOptions.PersonIdListTypeName"/> is <see langword="null"/>, which is the
    /// documented way of forcing the literal fallback - so constructing this binder is a
    /// contradiction.
    /// </exception>
    public TableValuedPersonIdListBinder(SqlOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrEmpty(options.PersonIdListTypeName))
        {
            throw new InvalidOperationException(
                $"{nameof(SqlOptions)}.{nameof(SqlOptions.PersonIdListTypeName)} is not set, which selects the " +
                $"chunked-literal fallback. Register {nameof(InlineLiteralPersonIdListBinder)} instead.");
        }

        _options = options;
    }

    /// <inheritdoc />
    /// <remarks>A table-valued parameter has no element limit, so the descriptor's batch size wins.</remarks>
    public int MaxIdsPerBatch => int.MaxValue;

    /// <inheritdoc />
    public PersonIdListBinding Bind(IReadOnlyCollection<int> personIds)
    {
        ArgumentNullException.ThrowIfNull(personIds);

        SqlTableParameter parameter = new()
        {
            Name = ParameterName,
            TypeName = _options.PersonIdListTypeName!,
            ColumnName = _options.PersonIdListColumnName,
            Values = personIds,
        };

        return new PersonIdListBinding(
            "(SELECT " + _options.PersonIdListColumnName + " FROM @" + ParameterName + ")",
            parameter);
    }
}
