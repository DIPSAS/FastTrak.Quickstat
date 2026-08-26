using System.ComponentModel;
using QuickStat.Domain.Matrix;
using QuickStat.Domain.Populations;

namespace QuickStat.Services;

/// <summary>
/// The state that more than one tab reads: the loaded population, the one
/// <see cref="PersonMatrix"/>, and the two export flags the Collections tab owns but the Dataset tab
/// applies.
/// </summary>
/// <remarks>
/// <para>
/// In the Delphi all of this lived in <c>TfrmQuickStat</c>'s private fields, which is why five
/// unrelated areas of that form could reach it. Duplicating it into three view-models would be
/// worse, not better - <c>05-ui-spec.md</c> §H.2's "State placement rules" says so in as many
/// words - so it lives here, in a singleton, and each tab's view-model keeps only what it alone
/// reads.
/// </para>
/// <para>
/// <b>§H.2 names two pieces of cross-tab state; there are three.</b> It lists
/// <c>HasPopulation</c> and the checked data elements, and misses
/// <see cref="ExportTimestamps"/> - the <c>Export timestamp for every data element</c> check box,
/// which sits on the Collections tab (§B.2 item 9) and is read at export time by the Dataset tab's
/// two save commands (§D.1). It is here for the same reason as the other two.
/// </para>
/// <para>
/// The identification mode is deliberately <em>not</em> here. It already has one shared home,
/// <see cref="QuickStat.Domain.Anonymisation.IIdentificationPolicy"/> in <c>QuickStat.Core</c>, and
/// a second copy would recreate exactly the display-versus-export divergence PORT-PLAN.md §7.2
/// exists to fix.
/// </para>
/// <para>
/// <b>Ordering contract for step 3.2.</b> <see cref="PersonMatrix"/> is a plain mutable object that
/// raises no notifications, so this type cannot observe it. A population load must therefore be, in
/// this order:
/// </para>
/// <code>
/// matrix.SortBy = MatrixSortOrder.PersonId;   // before Prepare; it throws once locked
/// matrix.PreparePopulation(patients);
/// workspace.SetPopulation(population);        // now Rows.Count is right, so HasPopulation is too
/// workspace.RequestCollectionsTab();          // only on the double-click path - see the method
/// </code>
/// <para>
/// And a collect run must end with <see cref="NotifyDataChanged"/>, after
/// <see cref="PersonMatrix.Lock"/>.
/// </para>
/// </remarks>
public interface IShellWorkspace : INotifyPropertyChanged
{
    /// <summary>The one dataset. The same instance the container hands to everyone else.</summary>
    PersonMatrix Matrix { get; }

    /// <summary>The population currently loaded into <see cref="Matrix"/>, or <see langword="null"/>.</summary>
    Population? Population { get; }

    /// <summary>
    /// Whether a population is loaded <em>and</em> produced at least one patient.
    /// </summary>
    /// <remarks>
    /// The Delphi condition is <c>fGrid.Data.DataRows &gt; 0</c>
    /// (<c>MainQuickStat.pas:567</c>), not "a population was selected": an empty cohort leaves the
    /// <c>Collections</c> tab hidden. Drives that tab's <c>Visibility</c> and <c>IsEnabled</c>
    /// (§B.0).
    /// </remarks>
    bool HasPopulation { get; }

    /// <summary>Whether a collect run has produced at least one column.</summary>
    /// <remarks>
    /// <see cref="PersonMatrix.HasData"/> counts <em>columns</em>, not rows - a cohort with no
    /// collected variables is "no data".
    /// </remarks>
    bool HasData { get; }

    /// <summary>Patients in <see cref="Matrix"/>. The first <c>%d</c> of the dataset caption.</summary>
    int RowCount { get; }

    /// <summary>Data columns in <see cref="Matrix"/>. The second <c>%d</c> of the dataset caption.</summary>
    int ColumnCount { get; }

    /// <summary>
    /// The <em>names</em> - not titles - of the data elements currently ticked on the Collections
    /// tab.
    /// </summary>
    /// <remarks>
    /// A projection, not a second copy of the list: step 3.3 owns the
    /// <c>DataElementViewModel</c> collection and pushes the names here through
    /// <see cref="SetCheckedCollectorNames"/> whenever a box is clicked. Read by
    /// <c>DatasetViewModel.SaveDataPackageCommand.CanExecute</c>, and by whatever builds the
    /// <see cref="QuickStat.Domain.Packages.PackagedSelection"/> - names are the persistence format.
    /// </remarks>
    IReadOnlyList<string> CheckedCollectorNames { get; }

    /// <summary>
    /// The <c>Export timestamp for every data element</c> check box. Written by the Collections tab,
    /// read by the Dataset tab's export commands.
    /// </summary>
    bool ExportTimestamps { get; set; }

    /// <summary>Raised after <see cref="Population"/>, <see cref="HasPopulation"/> or the row count changes.</summary>
    event EventHandler? PopulationChanged;

    /// <summary>Raised after <see cref="NotifyDataChanged"/>, i.e. at the end of a collect run.</summary>
    event EventHandler? DataChanged;

    /// <summary>
    /// Raised when something wants the left pane to switch to the <c>Collections</c> tab.
    /// </summary>
    /// <remarks>
    /// Observed by <see cref="QuickStat.ViewModels.MainViewModel"/>. An event rather than a direct call because
    /// <see cref="QuickStat.ViewModels.MainViewModel"/> constructs the tab view-models, so a tab that injected it back
    /// would be a dependency cycle.
    /// </remarks>
    event EventHandler? CollectionsTabRequested;

    /// <summary>Records which population <see cref="Matrix"/> now holds.</summary>
    /// <param name="population">The population, or <see langword="null"/> to clear.</param>
    /// <remarks>Call it <em>after</em> <see cref="PersonMatrix.PreparePopulation"/>; see the type remarks.</remarks>
    void SetPopulation(Population? population);

    /// <summary>Replaces <see cref="CheckedCollectorNames"/>.</summary>
    /// <param name="names">The ticked collectors' names, in check-list order.</param>
    /// <exception cref="ArgumentNullException"><paramref name="names"/> is <see langword="null"/>.</exception>
    void SetCheckedCollectorNames(IEnumerable<string> names);

    /// <summary>Announces that <see cref="Matrix"/>'s columns or datapoints have changed.</summary>
    /// <remarks>
    /// The end of a collect run, after <see cref="PersonMatrix.Lock"/>. Refreshes
    /// <see cref="HasData"/> and <see cref="ColumnCount"/>, repaints the grid and updates the
    /// dataset caption.
    /// </remarks>
    void NotifyDataChanged();

    /// <summary>Asks the shell to show the <c>Collections</c> tab.</summary>
    /// <remarks>
    /// Only the population double-click path does this. <c>AfterPopulationSelect</c> sets
    /// <c>pgSelections.ActivePage := tbsDataElements</c> (<c>MainQuickStat.pas:540</c>); the package
    /// replay calls <c>LoadPopulationIntoGrid</c> directly and deliberately leaves the user on the
    /// <c>Packages</c> tab. Folding this into <see cref="SetPopulation"/> would move the user's
    /// focus during a replay.
    /// </remarks>
    void RequestCollectionsTab();
}
