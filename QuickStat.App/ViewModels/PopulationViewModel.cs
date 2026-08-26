using CommunityToolkit.Mvvm.ComponentModel;
using QuickStat.Domain.Populations;

namespace QuickStat.ViewModels;

/// <summary>One row of the population list.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.2.</b> A thin, read-only projection of
/// <see cref="QuickStat.Domain.Populations.Population"/> plus the one piece of view state a row
/// owns: <see cref="IsExpanded"/>.
/// </para>
/// <para>
/// <see cref="Population.Group"/> and <see cref="Population.HelpText"/> are database content and
/// Norwegian; they stay Norwegian.
/// </para>
/// </remarks>
public sealed partial class PopulationViewModel : ObservableObject
{
    /// <summary>
    /// Whether <see cref="HelpText"/> is shown underneath the title.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Delphi <c>TObjectListView.ExpandRow</c> (<c>Emetra.VclComp.ListView.pas:752-755</c>):
    /// <c>(not FSimpleView) or (AIndex = Row)</c> - so with <c>Simplified</c> off every row is
    /// expanded, and with it on only the selected one is. Purely client-side; it changes nothing
    /// about what the filter matches.
    /// </para>
    /// <para>
    /// Written by <see cref="PopulationPickerViewModel"/>, which owns the rule; a row does not know
    /// whether it is selected.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>Wraps one catalogue row.</summary>
    /// <param name="population">The catalogue row.</param>
    /// <exception cref="ArgumentNullException"><paramref name="population"/> is <see langword="null"/>.</exception>
    public PopulationViewModel(Population population)
    {
        ArgumentNullException.ThrowIfNull(population);

        Population = population;
    }

    /// <summary>The underlying catalogue row.</summary>
    public Population Population { get; }

    /// <summary><c>ProcId</c>. The purple code column.</summary>
    public int ProcId => Population.ProcId;

    /// <summary><c>ProcTitle</c>. The bold main text.</summary>
    public string Title => Population.Title;

    /// <summary><c>ProcGroup</c>. Right-aligned, fuchsia, one point smaller.</summary>
    public string Group => Population.Group;

    /// <summary><c>HelpText</c>. Wrapped underneath when the row is expanded.</summary>
    public string HelpText => Population.HelpText;

    /// <summary>
    /// What the filter box matches against: <c>ProcId ⇥ Title ⇥ HelpText ⇥ Group</c>.
    /// </summary>
    /// <remarks>
    /// Comes from <see cref="Population.SearchText"/> in <c>QuickStat.Core</c>, which reproduces
    /// <c>TPopulation.fListBoxText</c> field for field. Do not rebuild it here: the field order and
    /// the tab separator are observable, and typing a number filters <c>ProcId</c> by substring.
    /// </remarks>
    public string SearchText => Population.SearchText;
}
