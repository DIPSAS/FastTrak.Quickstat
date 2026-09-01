namespace QuickStat.Domain.Packages;

/// <summary>
/// A saved dataset specification: one population plus the set of data elements to collect from it.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>TPackagedSelection</c> (<c>QuickStat.Selection.pas</c>). Stored <b>server-side</b> in
/// <c>Report.QuickStat</c>, written by <c>Report.AddQuickStat</c> and removed by
/// <c>QuickStat.DeletePackage</c> - which is why it is a repository under step 2.3 and not part of
/// the settings store.
/// </para>
/// <para>
/// Opening a package is a full replay: select the population, load it, untick everything, tick each
/// stored collector <em>by name</em>, and run the collect action
/// (<c>MainQuickStat.pas:780-814</c>). That makes <see cref="CollectorNames"/> a persistence format,
/// so a collector name is not free to change - the six known name collisions in the Delphi registry
/// are worth fixing precisely because they corrupt this replay
/// (<c>Docs/Port/03-collectors.md</c> §A.12).
/// </para>
/// </remarks>
public sealed record PackagedSelection
{
    /// <summary><c>Report.QuickStat.RowId</c>; zero for a package that has not been saved yet.</summary>
    public int RowId { get; init; }

    /// <summary>Owning study.</summary>
    public required int StudyId { get; init; }

    /// <summary>
    /// <c>ProcId</c> of the population to replay. Shown in the list as <c>Pop#&lt;n&gt;</c>.
    /// </summary>
    public required int PopulationId { get; init; }

    /// <summary>Package title, bold in the list, and the dataset caption after a replay.</summary>
    public required string Title { get; init; }

    /// <summary>Free-text comment, wrapped under the title.</summary>
    public string Comment { get; init; } = "";

    /// <summary>
    /// Collector <em>names</em> - not titles - stored in <c>Report.QuickStat.DataElements</c>.
    /// </summary>
    /// <remarks>
    /// Persisted as a semicolon-separated list. The Delphi kept them in a sorted, duplicate-ignoring
    /// <c>TStringList</c> (<c>QuickStat.Selection.pas:71-76</c>), so the stored order is
    /// alphabetical and carries no meaning; collection order comes from the registry, not from here.
    /// </remarks>
    public IReadOnlyList<string> CollectorNames { get; init; } = [];

    /// <summary>Separator used in <c>Report.QuickStat.DataElements</c>.</summary>
    public const char CollectorNameSeparator = ';';

    /// <summary>How long a <see cref="Title"/> may be: <c>Report.QuickStat.Title</c> is <c>varchar(80)</c>.</summary>
    /// <remarks>
    /// Assigning a longer string to <c>Report.AddQuickStat</c>'s <c>@Title VARCHAR(80)</c> truncates
    /// it <b>in silence</b> - a parameter assignment, unlike an <c>INSERT</c>, raises no 8152 - and
    /// the Delphi's <c>edtTitle</c> has no <c>MaxLength</c>, so a long title lost its tail there with
    /// no warning either. It is not only the tail that is lost: the procedure upserts on
    /// <c>(StudyId, Title)</c>, so two titles sharing their first 80 characters overwrite one
    /// another. The <c>Save specification</c> box therefore stops here rather than letting the
    /// column do it quietly (PORT-PLAN.md §8.11 (13)). The column is <c>varchar</c> under
    /// <c>Danish_Norwegian_CI_AS</c>, a single-byte code page, so 80 bytes is 80 characters.
    /// </remarks>
    public const int MaxTitleLength = 80;
}
