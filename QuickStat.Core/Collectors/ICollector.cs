namespace QuickStat.Collectors;

/// <summary>
/// One tickable data element: its metadata, plus the ability to produce the statement for a batch.
/// </summary>
/// <remarks>
/// The whole of the Delphi's thirteen-class <c>TDataCollector</c> hierarchy collapses to this. The
/// subclasses differed only in what they put into <c>FSQL</c>, <c>FVarPrefix</c> and
/// <c>FMaxBatchSize</c> - the shape of the read is identical for all 131 collectors - so the
/// variation belongs in <see cref="BuildSql"/> and <see cref="Descriptor"/>, not in a type
/// hierarchy.
/// </remarks>
public interface ICollector
{
    /// <summary>Name, title, prefix, batch size, gate and availability.</summary>
    CollectorDescriptor Descriptor { get; }

    /// <summary>Produces the statement for one batch.</summary>
    /// <param name="context">Study id and the <c>{IdList}</c> replacement for this batch.</param>
    /// <returns>Ready-to-execute SQL with every placeholder except <c>:PersonId</c> substituted.</returns>
    /// <remarks>
    /// Must be pure and deterministic given the same context: golden-file tests compare its output
    /// byte for byte, which is the cheapest way to prove a 131-entry registry is faithful without a
    /// database (PORT-PLAN.md R3). <c>{ItemList}</c>, <c>{LabList}</c> and <c>{FormName}</c> are
    /// substituted once at construction from compile-time constants and never vary per batch; only
    /// <c>{IdList}</c> does.
    /// </remarks>
    string BuildSql(CollectorSqlContext context);
}
