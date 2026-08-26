using System.Globalization;
using Microsoft.Extensions.Logging;
using QuickStat.Data;
using QuickStat.Diagnostics;
using QuickStat.Domain.Matrix;

namespace QuickStat.Collectors;

/// <summary>
/// The default <see cref="ICollectorRunner"/>: chunk, build, execute, read by ordinal, filter,
/// accumulate.
/// </summary>
/// <remarks>
/// <para>
/// This is <c>TDataCollector.RunBatch</c> plus the loop that drives it in
/// <c>TPersonGridData.AddData</c>, and it owns exactly what those owned - nothing about the shape
/// of the read varies between the 126 collectors, which is the strongest argument that the Delphi
/// class hierarchy should not survive the port.
/// </para>
/// <para>
/// Collectors are not run concurrently with each other. The data layer holds one long-lived
/// connection and serialises access, so concurrency here would only queue; revisit once the port is
/// behaviour-equivalent and connections are pooled.
/// </para>
/// </remarks>
public sealed class CollectorRunner : ICollectorRunner
{
    private const string ProgressHeader = "Collecting data";

    private readonly ISqlExecutor _sql;
    private readonly IPersonIdListBinder _binder;
    private readonly ILogger<CollectorRunner> _log;

    /// <summary>Creates the runner.</summary>
    /// <param name="sql">Executes the statements.</param>
    /// <param name="binder">Decides what <c>{IdList}</c> expands to.</param>
    /// <param name="log">Records discarded rows and per-collector totals.</param>
    public CollectorRunner(ISqlExecutor sql, IPersonIdListBinder binder, ILogger<CollectorRunner> log)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentNullException.ThrowIfNull(binder);
        ArgumentNullException.ThrowIfNull(log);

        _sql = sql;
        _binder = binder;
        _log = log;
    }

    /// <inheritdoc />
    public async Task<CollectorRunSummary> RunAsync(
        ICollector collector,
        IReadOnlyList<int> personIds,
        int studyId,
        ICollectorResultSink sink,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(collector);
        ArgumentNullException.ThrowIfNull(personIds);
        ArgumentNullException.ThrowIfNull(sink);

        CollectorDescriptor descriptor = collector.Descriptor;
        VariableNameSet variableNames = new();

        int rowsAccepted = 0;
        int rowsForUnknownPersons = 0;
        int batchCount = 0;

        IReadOnlyList<IReadOnlyList<int>> batches = Chunk(personIds, descriptor);

        foreach (IReadOnlyList<int> batch in batches)
        {
            cancellationToken.ThrowIfCancellationRequested();

            batchCount++;
            Report(progress, descriptor, batchCount, batches.Count);

            SqlRequest request = BuildRequest(collector, batch, studyId);
            SqlResultSet result = await _sql.QueryAsync(request, cancellationToken).ConfigureAwait(false);

            // The batch is the membership filter: rows for anyone else are counted and dropped.
            // Load-bearing, because most collectors have no {IdList} and scan the whole database
            // (PORT-PLAN.md R10).
            HashSet<int> inBatch = [.. batch];

            int itemIdOrdinal = result.IndexOf(CollectorResultRow.ItemIdColumnName);
            int captionOrdinal = result.IndexOf(CollectorResultRow.CaptionColumnName);

            foreach (SqlRow row in result)
            {
                int personId = row.GetInt32(CollectorResultRow.PersonIdOrdinal);

                if (!inBatch.Contains(personId))
                {
                    rowsForUnknownPersons++;
                    continue;
                }

                CollectorResultRow resultRow = new()
                {
                    PersonId = personId,
                    VarName = row.GetString(CollectorResultRow.VarNameOrdinal),
                    Value = row.GetDouble(CollectorResultRow.ValueOrdinal),
                    Timestamp = row.GetDateTime(CollectorResultRow.TimestampOrdinal),
                    RowId = row.GetInt32(CollectorResultRow.RowIdOrdinal),
                    ItemId = itemIdOrdinal >= 0 ? row.GetInt32(itemIdOrdinal) : 0,
                    Caption = captionOrdinal >= 0 ? row.GetString(captionOrdinal) : null,
                };

                string columnName = resultRow.ColumnName(descriptor.VarPrefix);

                // The Delphi records the variable name before it tries to store the datapoint, so a
                // column exists even when every value for it is a rejected duplicate.
                variableNames.Add(columnName);

                if (sink.Add(columnName, resultRow))
                {
                    rowsAccepted++;
                }
            }
        }

        if (rowsForUnknownPersons > 0)
        {
            // Delphi: SilentWarning( 'Unknown patients found, n =%d' ). A large number is expected
            // for a PidBinding.None collector and is a defect for an IdList one.
            _log.LogInformation(
                "Collector {CollectorName} discarded {UnknownRowCount} rows for people outside the cohort ({PidBinding}).",
                descriptor.Name,
                rowsForUnknownPersons,
                descriptor.PidBinding);
        }

        return new CollectorRunSummary
        {
            Descriptor = descriptor,
            VariableNames = variableNames,
            RowsAccepted = rowsAccepted,
            RowsForUnknownPersons = rowsForUnknownPersons,
            BatchCount = batchCount,
        };
    }

    /// <summary>How many people go into one statement.</summary>
    /// <param name="descriptor">The collector's descriptor.</param>
    /// <param name="maxIdsPerBatch">The active binder's ceiling.</param>
    /// <param name="cohortSize">Size of the whole cohort.</param>
    /// <returns>The chunk size, at least one.</returns>
    /// <remarks>
    /// A collector with no <c>{IdList}</c> takes the whole cohort in one statement whatever its
    /// batch size says: there is nothing to chunk, and the cohort is only used to filter the rows
    /// that come back. Everything else is capped by the smaller of the descriptor's batch size and
    /// the binder's ceiling - which is how <c>maxint</c> becomes something a server can plan for.
    /// </remarks>
    internal static int ChunkSizeFor(CollectorDescriptor descriptor, int maxIdsPerBatch, int cohortSize) =>
        descriptor.PidBinding == PidBinding.None
            ? Math.Max(1, cohortSize)
            : Math.Max(1, Math.Min(descriptor.BatchSize, maxIdsPerBatch));

    private IReadOnlyList<IReadOnlyList<int>> Chunk(IReadOnlyList<int> personIds, CollectorDescriptor descriptor)
    {
        if (personIds.Count == 0)
        {
            return [];
        }

        int size = ChunkSizeFor(descriptor, _binder.MaxIdsPerBatch, personIds.Count);
        List<IReadOnlyList<int>> batches = [];

        for (int start = 0; start < personIds.Count; start += size)
        {
            int length = Math.Min(size, personIds.Count - start);
            int[] batch = new int[length];

            for (int offset = 0; offset < length; offset++)
            {
                batch[offset] = personIds[start + offset];
            }

            batches.Add(batch);
        }

        return batches;
    }

    private SqlRequest BuildRequest(ICollector collector, IReadOnlyList<int> batch, int studyId)
    {
        CollectorDescriptor descriptor = collector.Descriptor;

        if (descriptor.PidBinding == PidBinding.SinglePerson)
        {
            // One round trip per patient, bound positionally to :PersonId - the Delphi's
            // FDB.FastQuery( SQL, [FLastId] ) path.
            return new SqlRequest
            {
                CommandText = collector.BuildSql(new CollectorSqlContext(studyId, string.Empty)),
                Values = [batch[0]],
                IsIdempotent = true,
                Label = descriptor.Name,
            };
        }

        PersonIdListBinding binding = descriptor.PidBinding == PidBinding.IdList
            ? _binder.Bind(batch)
            : new PersonIdListBinding(string.Empty, TableParameter: null);

        return new SqlRequest
        {
            CommandText = collector.BuildSql(new CollectorSqlContext(studyId, binding.Fragment)),
            TableParameters = binding.TableParameter is null ? [] : [binding.TableParameter],
            IsIdempotent = true,
            Label = descriptor.Name,
        };
    }

    private static void Report(IProgress<OperationProgress>? progress, CollectorDescriptor descriptor, int batchNumber, int batchCount)
    {
        if (progress is null)
        {
            return;
        }

        double? percent = batchCount > 0 ? 100.0 * (batchNumber - 1) / batchCount : null;

        progress.Report(new OperationProgress(
            ProgressHeader,
            string.Format(CultureInfo.CurrentCulture, "{0} ({1}/{2})", descriptor.Title, batchNumber, batchCount),
            percent));
    }
}
