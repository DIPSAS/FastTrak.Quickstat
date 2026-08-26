using QuickStat.Data;

namespace QuickStat.Collectors;

/// <summary>
/// Decides what <c>{IdList}</c> expands to, and binds whatever that needs.
/// </summary>
/// <remarks>
/// <para>
/// One seam, two implementations, no third path
/// (<c>Docs/Port/03-collectors.md</c> §C.4): a table-valued parameter, and chunked literal
/// inlining for databases that do not have the table type. Collector descriptors stay
/// strategy-agnostic; only the runner knows which one is active, and a test can inject a binder
/// that always emits a fixed token to make generated SQL deterministic.
/// </para>
/// <para>
/// What is explicitly <b>not</b> offered is element-wise parameterisation
/// (<c>@p0, @p1, …</c>). SQL Server's hard limit is 2 100 parameters per statement, so a
/// 3 000-patient population would break outright and a 1 000-patient one would defeat plan reuse
/// for no gain.
/// </para>
/// </remarks>
public interface IPersonIdListBinder
{
    /// <summary>Largest number of ids this strategy can put in one statement.</summary>
    /// <remarks>
    /// The runner batches by the smaller of this and
    /// <see cref="CollectorDescriptor.BatchSize"/>. Chunking never changes a result: every
    /// <c>{IdList}</c> query is a per-person projection.
    /// </remarks>
    int MaxIdsPerBatch { get; }

    /// <summary>Produces the fragment for one batch.</summary>
    /// <param name="personIds">The ids in this batch.</param>
    /// <returns>The fragment and, if the strategy needs one, a table-valued argument.</returns>
    PersonIdListBinding Bind(IReadOnlyCollection<int> personIds);
}

/// <summary>What one call to <see cref="IPersonIdListBinder.Bind"/> produced.</summary>
/// <param name="Fragment">
/// The text that replaces <c>{IdList}</c>, <b>including</b> its parentheses - the Delphi
/// substitutes <c>'(' + list + ')'</c>, so the placeholder itself is bare.
/// </param>
/// <param name="TableParameter">The table-valued argument to bind, or <see langword="null"/>.</param>
public readonly record struct PersonIdListBinding(string Fragment, SqlTableParameter? TableParameter);
