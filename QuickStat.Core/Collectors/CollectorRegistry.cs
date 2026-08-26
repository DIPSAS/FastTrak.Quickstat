using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using QuickStat.Collectors.Sql;
using QuickStat.Data;

namespace QuickStat.Collectors;

/// <summary>
/// The default <see cref="ICollectorRegistry"/>: two round trips, then
/// <see cref="CollectorRegistryBuilder"/>.
/// </summary>
/// <remarks>
/// Deliberately thin. Everything that decides the content and order of the list is in the pure
/// builder; this class only fetches the study's form classes and probes the database objects the
/// catalog asks for.
/// </remarks>
public sealed class CollectorRegistry : ICollectorRegistry
{
    private readonly ISqlExecutor _sql;
    private readonly ILogger<CollectorRegistry> _log;

    /// <summary>Creates the registry.</summary>
    /// <param name="sql">Used for <c>Report.GetFormClasses</c> and the availability probe.</param>
    /// <param name="log">Records skipped collectors and the resulting count.</param>
    public CollectorRegistry(ISqlExecutor sql, ILogger<CollectorRegistry> log)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentNullException.ThrowIfNull(log);

        _sql = sql;
        _log = log;
    }

    /// <inheritdoc />
    public IReadOnlyList<ICollector> Collectors { get; private set; } = [];

    /// <inheritdoc />
    public async Task<IReadOnlyList<ICollector>> BuildAsync(SessionContext session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        IReadOnlyList<FormClass> formClasses = await LoadFormClassesAsync(session.StudyId, cancellationToken).ConfigureAwait(false);
        IReadOnlySet<string> resolvedObjects = await ProbeDatabaseObjectsAsync(cancellationToken).ConfigureAwait(false);

        CollectorAvailabilityContext availability = new()
        {
            StudyName = session.StudyName,
            StudyId = session.StudyId,
            ResolvedDatabaseObjects = resolvedObjects,
        };

        Collectors = CollectorRegistryBuilder.Build(
            session.StudyName,
            formClasses,
            availability,
            descriptor => _log.LogInformation(
                "Collector {CollectorName} is not offered: the database is missing {RequiredObjects}.",
                descriptor.Name,
                string.Join(", ", descriptor.Availability.RequiredDatabaseObjects)));

        _log.LogInformation(
            "Registered {CollectorCount} collectors for study {StudyName} (gates {OpenGates}, {FormClassCount} form classes).",
            Collectors.Count,
            session.StudyName,
            StudyGates.For(session.StudyName),
            formClasses.Count);

        return Collectors;
    }

    /// <inheritdoc />
    public bool TryFind(string nameOrTitle, [NotNullWhen(true)] out ICollector? collector)
    {
        // TryFindCollector tests SameText against the name *and* the title of each collector in
        // turn, so a title that happens to equal an earlier collector's name would win. Preserved.
        foreach (ICollector candidate in Collectors)
        {
            if (string.Equals(candidate.Descriptor.Name, nameOrTitle, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.Descriptor.Title, nameOrTitle, StringComparison.OrdinalIgnoreCase))
            {
                collector = candidate;
                return true;
            }
        }

        collector = null;
        return false;
    }

    private async Task<IReadOnlyList<FormClass>> LoadFormClassesAsync(int studyId, CancellationToken cancellationToken)
    {
        SqlRequest request = new()
        {
            CommandText = QaSql.FormClasses,
            Values = [studyId],
            IsIdempotent = true,
            Label = "Report.GetFormClasses",
        };

        SqlResultSet result = await _sql.QueryAsync(request, cancellationToken).ConfigureAwait(false);

        if (result.IsEmpty)
        {
            // A study with no form classes gets no dynamic collectors, and that is not an error.
            // The columns are deliberately not demanded here: AddCollectorsStudySpecific calls
            // FieldByName *inside* its `while not EOF` loop (QuickStat.Collectors.pas:396-410), so
            // a result set that carries no rows never has its metadata inspected. Asking for the
            // ordinals up front turned "this study has no forms" into a hard failure whenever the
            // procedure returned nothing at all - which is what SqlResultSet.Empty represents, and
            // what a procedure that returns early produces.
            return [];
        }

        int nameOrdinal = result.GetOrdinal(QaSql.FormNameColumn);
        int titleOrdinal = result.GetOrdinal(QaSql.FormTitleColumn);

        List<FormClass> formClasses = new(result.Count);

        foreach (SqlRow row in result)
        {
            formClasses.Add(new FormClass(row.GetString(nameOrdinal), row.GetString(titleOrdinal)));
        }

        return formClasses;
    }

    /// <summary>
    /// Resolves every database object the catalog asks for, in one round trip.
    /// </summary>
    /// <param name="cancellationToken">Cancels the probe.</param>
    /// <returns>The objects that exist, compared case-insensitively.</returns>
    /// <remarks>
    /// Skips the round trip entirely when nothing needs probing, which is the case until Phase 4
    /// restores the two collectors that inner-join <c>KB.AntibioticResistance2</c> (R7).
    /// </remarks>
    private async Task<IReadOnlySet<string>> ProbeDatabaseObjectsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<string> required = CollectorRegistryBuilder.RequiredDatabaseObjects;

        if (required.Count == 0)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        string values = string.Join(",", required.Select(name => "(" + SqlLiteral.Quote(name) + ")"));

        SqlRequest request = new()
        {
            CommandText =
                "SELECT probe.ObjectName FROM ( VALUES " + values + " ) AS probe(ObjectName) " +
                "WHERE OBJECT_ID(probe.ObjectName) IS NOT NULL",
            IsIdempotent = true,
            Label = "Collector availability probe",
        };

        SqlResultSet result = await _sql.QueryAsync(request, cancellationToken).ConfigureAwait(false);

        HashSet<string> resolved = new(StringComparer.OrdinalIgnoreCase);

        foreach (SqlRow row in result)
        {
            resolved.Add(row.GetString(0));
        }

        return resolved;
    }
}
