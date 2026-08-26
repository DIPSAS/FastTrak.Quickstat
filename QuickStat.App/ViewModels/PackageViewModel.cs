using System.Globalization;
using QuickStat.Domain.Packages;

namespace QuickStat.ViewModels;

/// <summary>One row of the packaged-datasets list.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.4. This is a compiling stub.</b>
/// </para>
/// <para>
/// <b>What is left to do</b> (<c>05-ui-spec.md</c> §B.3): the row template. Two lines - id in
/// <c>QsCodeBrush</c>, title bold, <c>Pop#&lt;n&gt;</c> right-aligned in <c>QsCategoryBrush</c> one
/// point smaller, then the comment word-wrapped underneath. Variable row height, 2 px horizontal
/// padding and none vertical, a one-pixel divider between rows.
/// </para>
/// </remarks>
/// <param name="selection">The stored specification this wraps.</param>
public sealed class PackageViewModel(PackagedSelection selection)
{
    /// <summary>The stored specification.</summary>
    public PackagedSelection Selection { get; } = selection ?? throw new ArgumentNullException(nameof(selection));

    /// <summary><c>Report.QuickStat.RowId</c>. The purple code column.</summary>
    public int RowId => Selection.RowId;

    /// <summary>The package title, bold, and the dataset caption after a replay.</summary>
    public string Title => Selection.Title;

    /// <summary>Free text, word-wrapped underneath the title.</summary>
    public string Comment => Selection.Comment;

    /// <summary>The population this replays, rendered <c>Pop#&lt;n&gt;</c>.</summary>
    public string PopulationLabel =>
        string.Create(CultureInfo.InvariantCulture, $"Pop#{Selection.PopulationId}");

    /// <summary>
    /// What the filter box matches against: <c>RowId ⇥ Title ⇥ Comment ⇥ Pop#&lt;n&gt;</c>.
    /// </summary>
    /// <remarks>
    /// The packages filter is <b>not</b> the same filter as the population one - see
    /// <c>Docs/Port/07-ui-contracts.md</c>. This one uppercases both sides and <b>does</b> trim the
    /// filter text.
    /// </remarks>
    public string SearchText => string.Create(
        CultureInfo.InvariantCulture,
        $"{RowId}\t{Title}\t{Comment}\t{PopulationLabel}");
}
