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
/// <para>
/// The fragment names the parameter with <c>@</c> rather than the Delphi's <c>:</c> on purpose, and
/// this agrees with step 2.2 rather than assuming anything: <c>SqlRequestBinder</c> removes every
/// <see cref="SqlRequest.TableParameters"/> name from the scalar set before it demands values, and
/// it compares those names case-insensitively - so a <c>:pids</c> placeholder would <em>also</em>
/// work, but an <c>@pids</c> one needs no rewriting at all and cannot be mistaken for a positional
/// argument. The type and column names come from
/// <see cref="SqlTableParameter.ForPersonIds(SqlOptions, string, IReadOnlyCollection{int})"/>, so
/// they are stated once for both this step and step 2.3's national-id recovery.
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

        SqlTableParameter parameter = SqlTableParameter.ForPersonIds(_options, ParameterName, personIds);

        return new PersonIdListBinding(
            "(SELECT " + parameter.ColumnName + " FROM @" + parameter.Name + ")",
            parameter);
    }
}
