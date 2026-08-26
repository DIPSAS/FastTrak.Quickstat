using QuickStat.Diagnostics;

namespace QuickStat.Collectors;

/// <summary>Executes one collector over a cohort, batching as its descriptor requires.</summary>
/// <remarks>
/// Owns exactly what <c>TDataCollector.RunBatch</c> owned: chunking, <c>{IdList}</c> binding,
/// reading by ordinal, discarding rows for people outside the batch, and accumulating the distinct
/// column names.
/// </remarks>
public interface ICollectorRunner
{
    /// <summary>Runs a collector.</summary>
    /// <param name="collector">The collector.</param>
    /// <param name="personIds">The cohort, in matrix order.</param>
    /// <param name="studyId">Current study.</param>
    /// <param name="sink">Where accepted rows go.</param>
    /// <param name="progress">Per-batch progress, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancels between batches and inside a statement.</param>
    /// <returns>Column names and counts.</returns>
    /// <remarks>
    /// Collectors are not run in parallel with each other in the first version. It changes the load
    /// profile on the server and the single long-lived connection forbids it; revisit with pooled
    /// connections once the port is behaviour-equivalent.
    /// </remarks>
    Task<CollectorRunSummary> RunAsync(
        ICollector collector,
        IReadOnlyList<int> personIds,
        int studyId,
        ICollectorResultSink sink,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
