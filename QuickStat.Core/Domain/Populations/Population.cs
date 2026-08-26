namespace QuickStat.Domain.Populations;

/// <summary>
/// One row of the server-side population catalogue: a saved, named, parameterised query that
/// returns a patient cohort.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>TPopulation</c> (<c>CRF.Population.pas:81-95</c>), loaded from
/// <c>Populations.GetStudyPopulations</c> / <c>Populations.GetPopularPopulations</c>, which project
/// <c>dbo.DbProcList</c>. QuickStat only ever reads this catalogue; it never writes to it.
/// </para>
/// <para>
/// The Delphi read all seven columns with <c>FieldByName</c>, so one missing column killed the
/// whole load. Only <see cref="ProcId"/>, <see cref="Title"/> and <see cref="QueryText"/> are
/// genuinely required; the rest default.
/// </para>
/// </remarks>
public sealed record Population
{
    /// <summary><c>ProcId</c> - primary key, and the blue code column in the list.</summary>
    public required int ProcId { get; init; }

    /// <summary><c>ProcTitle</c> - the bold main text.</summary>
    public required string Title { get; init; }

    /// <summary>
    /// <c>SqlText</c> - the statement QuickStat executes to get the patients.
    /// </summary>
    /// <remarks>
    /// Arbitrary server-authored T-SQL, run verbatim (<c>CRF.Patient.List.pas:283</c>). In practice
    /// <c>EXEC dbo.GetCaseListXxx :StudyId[, :StartDate, :StopDate]</c>, but nothing in the client
    /// constrains it - which is PORT-PLAN.md R2 and the reason
    /// <see cref="QuickStat.Data.ISqlTextRewriter"/> has to be a real scanner.
    /// </remarks>
    public required string QueryText { get; init; }

    /// <summary><c>ProcGroup</c> - category, right-aligned in magenta, e.g. <c>Type 1</c>.</summary>
    /// <remarks>Database content and Norwegian; it stays Norwegian.</remarks>
    public string Group { get; init; } = "";

    /// <summary><c>HelpText</c> - the wrapped grey description shown when a row is expanded.</summary>
    public string HelpText { get; init; } = "";

    /// <summary><c>InfoCaption</c> - loaded by the Delphi and never rendered anywhere.</summary>
    /// <remarks>Kept so the mapping is complete and a future view can use it.</remarks>
    public string InfoCaption { get; init; } = "";

    /// <summary><c>ProcSourceCode</c> - the <c>CREATE PROCEDURE</c> text in the preview pane.</summary>
    public string SourceCode { get; init; } = "";

    /// <summary>
    /// The tab-joined string the list filter matches against, case-insensitively and as a plain
    /// substring.
    /// </summary>
    /// <remarks>
    /// Reproduces <c>TPopulation.fListBoxText</c> (<c>CRF.Population.pas:94</c>) field for field and
    /// separator for separator. Implemented here rather than left to step 2.3 because it <em>is</em>
    /// the contract: the observable consequences are that typing a number filters <c>ProcId</c> by
    /// substring (<c>26</c> matches 26, 126, 260) and that the group name and help text are
    /// searchable. Getting the field order or the separator wrong changes what users find.
    /// </remarks>
    public string SearchText => $"{ProcId}\t{Title}\t{HelpText}\t{Group}";
}
