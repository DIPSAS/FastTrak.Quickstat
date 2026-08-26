using CommunityToolkit.Mvvm.ComponentModel;

namespace QuickStat.ViewModels;

/// <summary>One tickable data element in the <c>Collections</c> list.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.3. This is a compiling stub.</b>
/// </para>
/// <para>
/// <b>What is left to do:</b> when <see cref="IsChecked"/> changes, step 3.3 must
/// </para>
/// <list type="number">
///   <item><description>
///     call <c>CollectDataCommand.NotifyCanExecuteChanged()</c> - the Delphi recomputes on every
///     <c>OnClickCheck</c> (<c>ValidateCollectorSelection</c>), and
///   </description></item>
///   <item><description>
///     push the ticked <see cref="Name"/>s into
///     <see cref="QuickStat.Services.IShellWorkspace.SetCheckedCollectorNames"/>, which is what
///     enables <c>Package dataset specification for reuse</c> on the Dataset tab.
///   </description></item>
/// </list>
/// <para>
/// <b><see cref="Name"/> and <see cref="Title"/> are not interchangeable.</b> The list shows the
/// title; a saved package stores the <em>name</em>
/// (<see cref="QuickStat.Domain.Packages.PackagedSelection.CollectorNames"/>), and the replay looks
/// collectors up by it. The Delphi's <c>TryFindCollector</c> accepts either, which is how the six
/// registry name/title collisions could corrupt a replay.
/// </para>
/// <para>
/// Sorting is <b>ordinal by title</b> (<c>cbDataCollector.Sorted := true</c>, §G.5) - use
/// <see cref="StringComparer.Ordinal"/>, so the <c>^ </c>-prefixed demographic collectors stay at
/// the top and <c>æ ø å</c> sort at the end. That order is not merely cosmetic: PORT-PLAN.md §6 pins
/// it as <b>the column order of every export</b>, because the collect loop walks the sorted list
/// from index 0 and column order is insertion order.
/// </para>
/// </remarks>
/// <param name="name">The collector's name - the persistence format.</param>
/// <param name="title">The Norwegian title shown in the list.</param>
public sealed partial class DataElementViewModel(string name, string title) : ObservableObject
{
    [ObservableProperty]
    private bool _isChecked;

    [ObservableProperty]
    private bool _isCollecting;

    /// <summary>The collector's name. Stored in a package; never shown.</summary>
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

    /// <summary>The Norwegian title, verbatim including any <c>^ </c> sort prefix.</summary>
    public string Title { get; } = title ?? throw new ArgumentNullException(nameof(title));
}
