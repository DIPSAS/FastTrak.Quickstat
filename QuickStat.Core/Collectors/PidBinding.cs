namespace QuickStat.Collectors;

/// <summary>How the batch of person ids reaches the server for a given collector.</summary>
/// <remarks>Delphi: implied by <c>FMaxBatchSize</c> and the presence of <c>{IdList}</c> in the SQL.</remarks>
public enum PidBinding
{
    /// <summary>
    /// The statement takes no ids at all: it scans the whole database and the client discards rows
    /// for people outside the batch.
    /// </summary>
    /// <remarks>
    /// PORT-PLAN.md R10, counted in §8.15: <b>56 of the 131 collectors</b> are in this category,
    /// and 29 of those are bounded by neither patient, date nor study. The discarded rows are
    /// counted and logged as <c>Unknown patients found, n =%d</c>. It is the largest performance
    /// defect in the subsystem and it is preserved for parity; adding <c>{IdList}</c> to these
    /// queries is a separate, flagged follow-up, costed in §8.15 as 50 mechanical rewrites, one
    /// trap (<c>DRUID_SPECIFIED</c>, whose <c>n &gt; 5</c> threshold is global by design) and five
    /// <c>EXEC Report.*</c> calls that would need a procedure signature changed elsewhere.
    /// </remarks>
    None = 0,

    /// <summary>
    /// One round trip per patient, bound to a <c>:PersonId</c> parameter.
    /// </summary>
    /// <remarks>
    /// <c>TFormInstanceCollector</c> and <c>TFormDataCollector</c>. On a large population this is
    /// <c>patients x (1 + formClasses)</c> round trips and is by far the slowest path.
    /// </remarks>
    SinglePerson = 1,

    /// <summary>The statement contains <c>{IdList}</c>, replaced per batch.</summary>
    IdList = 2,
}
