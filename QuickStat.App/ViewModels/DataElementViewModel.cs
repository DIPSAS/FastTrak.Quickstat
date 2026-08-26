using CommunityToolkit.Mvvm.ComponentModel;

namespace QuickStat.ViewModels;

/// <summary>One tickable data element in the <c>Collections</c> list.</summary>
/// <remarks>
/// <para>
/// Owned by step 3.3. Delphi: one <c>cbDataCollector</c> item, whose <c>Items.Objects[n]</c> is the
/// <c>IGridDataCollector</c> itself.
/// </para>
/// <para>
/// When <see cref="IsChecked"/> changes the element tells its owner, which does the two things the
/// Delphi's <c>OnClickCheck</c> handler (<c>ValidateCollectorSelection</c>,
/// <c>MainQuickStat.pas:690-713</c>) did: recompute <c>CollectDataCommand.CanExecute</c>, and push
/// the ticked <see cref="Name"/>s into
/// <see cref="QuickStat.Services.IShellWorkspace.SetCheckedCollectorNames"/>, which is what enables
/// <c>Package dataset specification for reuse</c> on the Dataset tab.
/// </para>
/// <para>
/// <b><see cref="Name"/> and <see cref="Title"/> are not interchangeable.</b> The list shows the
/// title; a saved package stores the <em>name</em>
/// (<see cref="QuickStat.Domain.Packages.PackagedSelection.CollectorNames"/>), and the replay looks
/// collectors up by it. The Delphi's <c>TryFindCollector</c> accepts either, which is how the six
/// registry name/title collisions could corrupt a replay.
/// </para>
/// <para>
/// Sorting is by <see cref="TitleOrder"/>, and that order is the column order of every export -
/// PORT-PLAN.md §6. See that property for why it is <em>not</em>
/// <see cref="StringComparer.Ordinal"/>, which is what <c>05-ui-spec.md</c> §G.5 asks for.
/// </para>
/// </remarks>
public sealed partial class DataElementViewModel : ObservableObject
{
    private readonly Action<DataElementViewModel>? _checkedChanged;

    [ObservableProperty]
    private bool _isChecked;

    [ObservableProperty]
    private bool _isCollecting;

    /// <summary>Creates one row of the check list.</summary>
    /// <param name="name">The collector's name - the persistence format.</param>
    /// <param name="title">The Norwegian title shown in the list.</param>
    /// <param name="checkedChanged">
    /// Called after <see cref="IsChecked"/> changes, with this element.
    /// <see cref="CollectionsTabViewModel"/> supplies it; <see langword="null"/> in a test that only
    /// cares about the data.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> or <paramref name="title"/> is <see langword="null"/>.
    /// </exception>
    public DataElementViewModel(string name, string title, Action<DataElementViewModel>? checkedChanged = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(title);

        Name = name;
        Title = title;

        _checkedChanged = checkedChanged;
    }

    /// <summary>
    /// The rule that orders the check list - and therefore the columns of every export.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A deliberate divergence from <c>05-ui-spec.md</c> §G.5, PORT-PLAN.md §6 and
    /// <c>07-ui-contracts.md</c> §5</b>, all three of which say to sort with
    /// <see cref="StringComparer.Ordinal"/> "which is what keeps the <c>^ </c>-prefixed demographic
    /// collectors first". It does not: <c>'^'</c> is U+005E, which is <em>above</em> <c>'A'</c>-
    /// <c>'Z'</c> and below <c>'a'</c>-<c>'z'</c>, and every other collector title begins with a
    /// capital letter. An ordinal sort therefore moves all eleven demographic elements from the top
    /// of the list to the bottom - and, because the collect loop walks the list from index 0 and
    /// column order is insertion order, to the far right of every exported file.
    /// </para>
    /// <para>
    /// <c>cbDataCollector</c> is a <c>TCheckListBox</c>, and <c>Sorted := true</c>
    /// (<c>MainQuickStat.pas:400</c>) puts <c>LBS_SORT</c> on the Win32 list box, which orders items
    /// with <c>CompareStringW(LOCALE_USER_DEFAULT, NORM_IGNORECASE, ...)</c>. That is a linguistic,
    /// case-insensitive collation, in which punctuation sorts before letters - which is exactly what
    /// makes the <c>^ </c> hack work, and what <c>Docs/Screenshots/QuickStat bilde 2.png</c> shows.
    /// <see cref="StringComparer.CurrentCultureIgnoreCase"/> is the .NET equivalent, down to reading
    /// the user's locale rather than a fixed one.
    /// </para>
    /// <para>
    /// A new comparer per call, on purpose: <see cref="StringComparer.CurrentCultureIgnoreCase"/>
    /// captures <see cref="System.Globalization.CultureInfo.CurrentCulture"/> when it is read, so a
    /// cached one would freeze the culture at type-initialisation time.
    /// </para>
    /// </remarks>
    public static StringComparer TitleOrder => StringComparer.CurrentCultureIgnoreCase;

    /// <summary>The collector's name. Stored in a package; never shown.</summary>
    public string Name { get; }

    /// <summary>The Norwegian title, verbatim including any <c>^ </c> sort prefix.</summary>
    public string Title { get; }

    partial void OnIsCheckedChanged(bool value)
    {
        _ = value;

        _checkedChanged?.Invoke(this);
    }
}
