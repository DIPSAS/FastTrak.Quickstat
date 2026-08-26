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
    /// PORT-PLAN.md R10. Most <c>maxint</c>-batch collectors are in this category -
    /// <c>SpDiagnoseDetailsByLevel</c>, every <c>EXEC Report.Col*</c>, and about eighteen others -
    /// and the discarded rows are counted and logged as <c>Unknown patients found, n =%d</c>. It is
    /// the largest performance defect in the subsystem and it is preserved for parity; adding
    /// <c>{IdList}</c> to these queries is a separate, flagged follow-up.
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
