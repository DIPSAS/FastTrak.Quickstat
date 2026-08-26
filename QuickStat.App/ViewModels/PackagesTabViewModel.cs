using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace QuickStat.ViewModels;

/// <summary>The <c>Packages</c> tab: filter, delete button, and the packaged-datasets list.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.4. This is a compiling stub with no behaviour.</b>
/// </para>
/// <para>
/// <b>What is left to do</b> (<c>05-ui-spec.md</c> §B.3, §D.1, §D.3, §D.4):
/// </para>
/// <list type="bullet">
///   <item><description>
///     Load through <see cref="QuickStat.Domain.Packages.IPackageRepository"/> after login and after
///     every save or delete.
///   </description></item>
///   <item><description>
///     Live filtering. <b>This filter is not the population filter.</b> PORT-PLAN.md §8.8 (i):
///     uppercase both sides with <c>ToUpper(CultureInfo.CurrentCulture)</c>, <b>trim</b> the filter
///     text, compare with <c>StringComparison.Ordinal</c>, and treat an empty filter as "match
///     everything".
///   </description></item>
///   <item><description>
///     <see cref="DeletePackageCommand"/>: <c>CanExecute</c> is
///     <c>SelectedPackage is not null</c> - an improvement over the Delphi, which enables the action
///     always and warns at execute time. Confirm with
///     <see cref="QuickStat.Diagnostics.IUserNotifier.ConfirmAsync"/>:
///     <c>Do you really want to delete this package:</c> / <c>"&lt;title&gt;"?</c> - real line
///     breaks, not the literal <c>\n</c> the Delphi resource string contains (§I.8).
///   </description></item>
///   <item><description>
///     <see cref="OpenPackageCommand"/>, on double click: the full replay of §B.3 - find the
///     population, load it, untick everything, tick each stored collector <b>by name</b>, run the
///     collect, then <c>DatasetViewModel.SetCaption(package.Title)</c>. Note it must <b>not</b>
///     call <see cref="QuickStat.Services.IShellWorkspace.RequestCollectionsTab"/>: the Delphi
///     leaves the user on this tab during a replay.
///   </description></item>
///   <item><description>
///     Subscribe to <see cref="DatasetViewModel.SaveDataPackageRequested"/> in the constructor. That
///     is where <c>Package dataset specification for reuse</c> arrives from the grid's context menu:
///     show <c>SaveSpecDialog</c> (step 3.6) after clearing it, build a
///     <see cref="QuickStat.Domain.Packages.PackagedSelection"/> from
///     <see cref="QuickStat.Services.IShellWorkspace.CheckedCollectorNames"/> and the current
///     population, save, and reload the list.
///   </description></item>
/// </list>
/// </remarks>
public sealed partial class PackagesTabViewModel : ObservableObject
{
    /// <summary>Teal header. <b>Not</b> <c>Packages</c> - that is only the tab caption.</summary>
    public const string PackagesHeader = "Packaged datasets";

    /// <summary>The toolbar button and the context-menu item. Delphi <c>actDeletePackage</c>.</summary>
    public const string DeletePackageCaption = "Delete this package";

    [ObservableProperty]
    private string _filterText = "";

    [ObservableProperty]
    private PackageViewModel? _selectedPackage;

    /// <summary>The saved specifications for this study.</summary>
    public ObservableCollection<PackageViewModel> Packages { get; } = [];

    /// <summary>Deletes the selected package after confirming.</summary>
    /// <remarks>Step 3.4 replaces this with the real command; the stub is permanently disabled.</remarks>
    public IAsyncRelayCommand DeletePackageCommand { get; } =
        new AsyncRelayCommand(static () => Task.CompletedTask, static () => false);

    /// <summary>Replays the selected package. Double click.</summary>
    /// <remarks>Step 3.4 replaces this with the real command; the stub is permanently disabled.</remarks>
    public IAsyncRelayCommand OpenPackageCommand { get; } =
        new AsyncRelayCommand(static () => Task.CompletedTask, static () => false);
}
