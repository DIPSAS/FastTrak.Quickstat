using QuickStat.Domain.Populations;

namespace QuickStat.ViewModels;

/// <summary>One row of the population list.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.2. This is a compiling stub.</b> Created by step 3.1 so that the shell lays out
/// and so 3.2 and 3.4 never have to open each other's files.
/// </para>
/// <para>
/// <b>What is left to do</b> (<c>05-ui-spec.md</c> §B.1.1, §H.2):
/// </para>
/// <list type="bullet">
///   <item><description>
///     <c>IsExpanded</c>, driving whether <see cref="HelpText"/> is shown underneath. When
///     <c>Simplified</c> is ticked only the selected row expands; when it is not, every row does.
///     Client-side only - it changes nothing about what the filter matches.
///   </description></item>
///   <item><description>
///     The row template: id in <c>QsCodeBrush</c>, title bold in <c>QsTitleBrush</c> with
///     <c>TextTrimming="CharacterEllipsis"</c>, and <see cref="Group"/> right-aligned in
///     <c>QsCategoryBrush</c>, one point smaller, <b>inside</b> the title column.
///   </description></item>
/// </list>
/// <para>
/// <see cref="Population.Group"/> and <see cref="Population.HelpText"/> are database content and
/// Norwegian; they stay Norwegian.
/// </para>
/// </remarks>
/// <param name="population">The catalogue row this wraps.</param>
public sealed class PopulationViewModel(Population population)
{
    /// <summary>The underlying catalogue row.</summary>
    public Population Population { get; } = population ?? throw new ArgumentNullException(nameof(population));

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
