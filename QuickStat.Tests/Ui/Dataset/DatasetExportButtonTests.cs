using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using QuickStat.Tests.Ui.Dialogs;
using QuickStat.Views;
using Xunit;

namespace QuickStat.Tests.Ui.Dataset;

/// <summary>
/// The <c>Export</c> button in the Dataset tab's caption bar, and the menu it drops down.
/// </summary>
/// <remarks>
/// <para>
/// <b>An addition, and the tests are about it not becoming a second copy.</b> The Delphi puts its
/// three dataset actions on the grid's right-click menu and nowhere else (§D.2). The button surfaces
/// the same <c>mnuGridPopup</c> somewhere it can be found; what it must never do is grow its own
/// list of captions and commands that can drift from the grid's. So the cases below compare the
/// two menus item by item rather than asserting either against a transcript.
/// </para>
/// <para>
/// The other thing worth pinning is that the menu's bindings resolve at all. A
/// <see cref="ContextMenu"/> is not in the visual tree - it takes its DataContext through its
/// owner - and a <c>Command</c> binding that resolves to nothing produces a permanently greyed item
/// and no error anywhere.
/// </para>
/// </remarks>
[Collection(WpfApplicationCollection.Name)]
public class DatasetExportButtonTests
{
    /// <summary>§D.2, in the .dfm's own order. Two of them differ from the actions' captions.</summary>
    private static readonly string[] Captions =
    [
        "Package dataset specification for reuse",
        "Open this dataset in Excel",
        "Save this dataset to CSV file",
    ];

    private readonly WpfApplicationFixture _wpf;

    /// <summary>Takes the assembly's one application; the view names theme keys.</summary>
    /// <param name="wpf">Injected by xUnit from <see cref="WpfApplicationCollection"/>.</param>
    public DatasetExportButtonTests(WpfApplicationFixture wpf)
    {
        ArgumentNullException.ThrowIfNull(wpf);

        _wpf = wpf;
    }

    [Fact]
    public void TheButtonOffersExactlyWhatTheRightClickOffers()
    {
        (List<string?> Button, List<string?> Grid, bool SameCommands, bool Separate) menus = _wpf.Run(() =>
        {
            using DatasetHarness harness = new();

            harness.LoadAndCollect();

            DatasetTabView view = new() { DataContext = harness.ViewModel };
            (List<string?> Button, List<string?> Grid, bool SameCommands, bool Separate) seen =
                ([], [], false, false);

            RealisedWindow.RunControl(view, _ =>
            {
                ContextMenu fromButton = view.ExportButton.ContextMenu!;
                ContextMenu fromGrid = view.Grid.ContextMenu!;

                seen.Button = Headers(fromButton);
                seen.Grid = Headers(fromGrid);

                // x:Shared="False": one definition, two instances.  If they were the same object the
                // second reference would have taken it off the first.
                seen.Separate = !ReferenceEquals(fromButton, fromGrid);

                seen.SameCommands = Commands(fromButton).SequenceEqual(Commands(fromGrid));
            });

            return seen;
        });

        Assert.Equal(Captions, menus.Button);
        Assert.Equal(menus.Grid, menus.Button);
        Assert.True(menus.SameCommands, "the button and the grid should drive the same commands");
        Assert.True(menus.Separate, "each reference should get its own ContextMenu instance");
    }

    [Fact]
    public void TheMenuReachesTheViewModelThroughTheButton()
    {
        // The failure this is here for is silent: a ContextMenu is outside the visual tree, so if it
        // did not inherit the tab's DataContext through its PlacementTarget, every Command binding
        // would resolve to null and all three items would be greyed for ever with nothing logged.
        (bool Bound, bool Enabled) menu = _wpf.Run(() =>
        {
            using DatasetHarness harness = new();

            harness.LoadAndCollect();

            DatasetTabView view = new() { DataContext = harness.ViewModel };
            (bool Bound, bool Enabled) state = default;

            RealisedWindow.RunControl(view, _ =>
            {
                Open(view);

                List<MenuItem> items = [.. Items(view.ExportButton.ContextMenu!)];

                state.Bound =
                    ReferenceEquals(items[1].Command, harness.ViewModel.OpenInExcelCommand)
                    && ReferenceEquals(items[2].Command, harness.ViewModel.SaveDatasetToCsvCommand);

                // A collected, locked matrix, so both exports are executable - which is only visible
                // once the binding has produced a command to ask.
                state.Enabled = items[1].IsEnabled && items[2].IsEnabled;

                view.ExportButton.ContextMenu!.IsOpen = false;
            });

            return state;
        });

        Assert.True(menu.Bound, "the menu items should carry the view-model's own commands");
        Assert.True(menu.Enabled, "both exports should be available once a dataset has been collected");
    }

    [Fact]
    public void ClickingItDropsTheMenuUnderTheButtonRatherThanAtThePointer()
    {
        // A ContextMenu opens on a right-click and on nothing else, so without the Click handler the
        // button would be inert; and the default placement is the mouse pointer, which for a menu
        // the user asked for by pressing a button is the one place it should not appear.
        (bool Open, bool UnderTheButton, PlacementMode Placement) drop = _wpf.Run(() =>
        {
            using DatasetHarness harness = new();

            harness.LoadAndCollect();

            DatasetTabView view = new() { DataContext = harness.ViewModel };
            (bool Open, bool UnderTheButton, PlacementMode Placement) seen = default;

            RealisedWindow.RunControl(view, _ =>
            {
                ContextMenu menu = view.ExportButton.ContextMenu!;

                Assert.False(menu.IsOpen);

                Open(view);

                seen.Open = menu.IsOpen;
                seen.UnderTheButton = ReferenceEquals(menu.PlacementTarget, view.ExportButton);
                seen.Placement = menu.Placement;

                menu.IsOpen = false;
            });

            return seen;
        });

        Assert.True(drop.Open);
        Assert.True(drop.UnderTheButton);
        Assert.Equal(PlacementMode.Bottom, drop.Placement);
    }

    [Fact]
    public void TheGridKeepsItsOwnRightClickMenu()
    {
        // The button is an addition; it does not replace anything.  A user who already knows to
        // right-click the grid must still find the same three items there.
        List<string?> headers = _wpf.Run(() =>
        {
            using DatasetHarness harness = new();

            DatasetTabView view = new() { DataContext = harness.ViewModel };
            List<string?> found = [];

            RealisedWindow.RunControl(view, _ => found = Headers(view.Grid.ContextMenu!));

            return found;
        });

        Assert.Equal(Captions, headers);
    }

    /// <summary>Raises the button's <c>Click</c>, which is what a press does.</summary>
    /// <param name="view">The realised tab.</param>
    private static void Open(DatasetTabView view) =>
        view.ExportButton.RaiseEvent(new System.Windows.RoutedEventArgs(ButtonBase.ClickEvent));

    /// <summary>The menu's real items, skipping the separator.</summary>
    /// <param name="menu">Either instance.</param>
    /// <returns>The three items, in order.</returns>
    private static IEnumerable<MenuItem> Items(ContextMenu menu) => menu.Items.OfType<MenuItem>();

    private static List<string?> Headers(ContextMenu menu) =>
        [.. Items(menu).Select(item => item.Header as string)];

    private static List<ICommand?> Commands(ContextMenu menu) =>
        [.. Items(menu).Select(item => item.Command)];
}
