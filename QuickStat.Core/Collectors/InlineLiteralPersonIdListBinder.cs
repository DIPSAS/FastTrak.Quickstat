using System.Globalization;
using QuickStat.Configuration;

namespace QuickStat.Collectors;

/// <summary>
/// Writes the ids straight into the statement as a literal in-list - what the Delphi does.
/// </summary>
/// <remarks>
/// <para>
/// The registered default. It works against every database in the estate without a migration, and
/// it makes the generated statement identical to the Delphi's for a given batch, which is exactly
/// what the first port needs to be comparable against a trace.
/// </para>
/// <para>
/// It differs from the Delphi in one respect, and it is an improvement rather than a divergence:
/// the ids are chunked at <see cref="QuickStat.Configuration.SqlOptions.MaxIdsPerBatch"/> instead
/// of being unbounded. Upstream, the four <c>maxint</c> drug-set collectors inline the whole
/// population, which on a large cohort produces a six-figure <c>IN</c> list and a unique,
/// uncacheable plan every time. Chunking cannot change a result, because every <c>{IdList}</c>
/// query is a per-person projection (<c>Docs/Port/03-collectors.md</c> §C.4).
/// </para>
/// <para>
/// Ids are rendered with <see cref="CultureInfo.InvariantCulture"/> and are typed <see cref="int"/>
/// end to end, so there is no injection surface even though this is string concatenation.
/// </para>
/// </remarks>
public sealed class InlineLiteralPersonIdListBinder : IPersonIdListBinder
{
    private readonly SqlOptions _options;

    /// <summary>Creates the binder.</summary>
    /// <param name="options">Supplies <see cref="SqlOptions.MaxIdsPerBatch"/>.</param>
    public InlineLiteralPersonIdListBinder(SqlOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
    }

    /// <inheritdoc />
    public int MaxIdsPerBatch => _options.MaxIdsPerBatch;

    /// <inheritdoc />
    public PersonIdListBinding Bind(IReadOnlyCollection<int> personIds)
    {
        ArgumentNullException.ThrowIfNull(personIds);

        return new PersonIdListBinding(
            "(" + string.Join(",", personIds.Select(id => id.ToString(CultureInfo.InvariantCulture))) + ")",
            TableParameter: null);
    }
}
