using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using QuickStat.Configuration;
using QuickStat.Domain.Packages;
using QuickStat.Tests.Ui.Dialogs;
using QuickStat.Tests.Ui.Populations;
using QuickStat.ViewModels;
using QuickStat.Views;
using Xunit;

namespace QuickStat.Tests.Ui;

/// <summary>
/// What the four item lists announce to a screen reader. PORT-PLAN.md §8.11 (8).
/// </summary>
/// <remarks>
/// <para>
/// <b>The defect these pin.</b> <c>ItemAutomationPeer.GetNameCore</c> asks the row's container for a
/// name and, finding none, returns <c>Item.ToString()</c>. That fallback is invisible while the items
/// are strings - a string's <c>ToString</c> is the string - and wrong the moment they are objects,
/// which is every list in this application. Measured on the running <c>26.0.0.0</c> build: the
/// project combo announced <c>QuickStatConnection { Name = ..., ConnectionString = ... }</c> per
/// entry, and all 213 data elements announced
/// <c>QuickStat.ViewModels.DataElementViewModel</c>. <c>DisplayMemberPath</c> and an
/// <c>ItemTemplate</c> both fix what is <em>drawn</em> and neither touches the name.
/// </para>
/// <para>
/// <b>Why the fix has two halves, and why both are tested.</b> The binding on the container is what
/// a screen reader reads; <c>ToString</c> is what the peer falls back to when the container has no
/// name, which was the state of every row in the product until now and is still the state on every
/// path that reaches a row without realising it. Testing only <c>ToString</c> would pass against
/// markup that had quietly lost its setter, and testing only the peers would leave the fallback -
/// the thing that actually leaked - unpinned. So both, and the fallback is exercised through a real
/// peer rather than asserted about.
/// </para>
/// <para>
/// <b>The lists are fed directly rather than through their view-models.</b> What is under test is the
/// shipped markup's <c>ItemContainerStyle</c>, and the view-model wiring that fills each collection
/// has its own suite. Assigning <c>ItemsSource</c> replaces the binding and nothing else.
/// </para>
/// </remarks>
[Collection(WpfApplicationCollection.Name)]
public class AutomationNameTests
{
    /// <summary>
    /// A password that is obviously not one. It exists to be searched for in the automation tree, so
    /// it has to be a string nothing else could produce.
    /// </summary>
    private const string NotARealPassword = "PW-THIS-MUST-NEVER-BE-ANNOUNCED";

    private readonly WpfApplicationFixture _wpf;

    /// <summary>Takes the assembly's one application.</summary>
    /// <param name="wpf">Injected by xUnit from <see cref="WpfApplicationCollection"/>.</param>
    public AutomationNameTests(WpfApplicationFixture wpf)
    {
        ArgumentNullException.ThrowIfNull(wpf);

        _wpf = wpf;
    }

    // ---------------------------------------------------------------------------------------
    // The realised rows: the binding on the container
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void TheProjectComboAnnouncesTheDatabaseName()
    {
        (List<string> containerNames, List<string> peerNames) = _wpf.Run(() =>
        {
            PopulationTabView view = new();
            ComboBox combo = Combo(view);

            combo.ItemsSource = Connections();

            List<string> onContainers = [];
            List<string> inTree = [];

            RealisedWindow.RunControl(view, _ =>
            {
                // A ComboBox generates no containers until the drop-down has been opened once: the
                // items live in a Popup, and its child tree does not exist before then.  That is
                // precisely the state the connection string was leaking from.
                combo.IsDropDownOpen = true;

                combo.UpdateLayout();

                onContainers = ContainerNames(combo);
                inTree = RowNames(combo);

                combo.IsDropDownOpen = false;
            });

            return (onContainers, inTree);
        });

        Assert.Equal(["EFT00028_TEST_020", "EFT00028_BEHOVPOL"], containerNames);
        Assert.Equal(["EFT00028_TEST_020", "EFT00028_BEHOVPOL"], peerNames);
    }

    [Fact]
    public void TheDataElementRowsAnnounceTheirTitles()
    {
        (List<string> containerNames, List<string> peerNames) = _wpf.Run(() =>
        {
            CollectionsTabView view = new();

            view.ElementsList.ItemsSource = Elements(3);

            List<string> onContainers = [];
            List<string> inTree = [];

            RealisedWindow.RunControl(view, _ =>
            {
                onContainers = ContainerNames(view.ElementsList);
                inTree = RowNames(view.ElementsList);
            });

            return (onContainers, inTree);
        });

        // Verbatim, "^ " and all: an accessible name that disagrees with the visible label breaks
        // voice control, which matches one against the other.
        Assert.Equal(["^ Alder", "Element 001", "Element 002"], containerNames);
        Assert.Equal(["^ Alder", "Element 001", "Element 002"], peerNames);
    }

    [Fact]
    public void ThePopulationRowsAnnounceTheirIdAndTitle()
    {
        (List<string> containerNames, List<string> peerNames) = _wpf.Run(() =>
        {
            PopulationPickerView view = new();

            view.PopulationList.ItemsSource = new[]
            {
                new PopulationViewModel(PopulationTestDoubles.NewPopulation(282, "Diagnoseår mangler")),
                new PopulationViewModel(PopulationTestDoubles.NewPopulation(14, "Alle testpersoner")),
            };

            List<string> onContainers = [];
            List<string> inTree = [];

            RealisedWindow.RunControl(view, _ =>
            {
                onContainers = ContainerNames(view.PopulationList);
                inTree = RowNames(view.PopulationList);
            });

            return (onContainers, inTree);
        });

        Assert.Equal(["282 Diagnoseår mangler", "14 Alle testpersoner"], containerNames);
        Assert.Equal(["282 Diagnoseår mangler", "14 Alle testpersoner"], peerNames);
    }

    [Fact]
    public void ThePackageRowsAnnounceTheirIdAndTitle()
    {
        (List<string> containerNames, List<string> peerNames) = _wpf.Run(() =>
        {
            PackagesTabView view = new();
            ListBox packages = Packages(view);

            packages.ItemsSource = new[]
            {
                new PackageViewModel(Selection(41, "Diabetes basissett 2024")),

                // The same title twice, which nothing prevents: it is why the id leads the name.
                new PackageViewModel(Selection(42, "Diabetes basissett 2024")),
            };

            List<string> onContainers = [];
            List<string> inTree = [];

            RealisedWindow.RunControl(view, _ =>
            {
                onContainers = ContainerNames(packages);
                inTree = RowNames(packages);
            });

            return (onContainers, inTree);
        });

        Assert.Equal(["41 Diabetes basissett 2024", "42 Diabetes basissett 2024"], containerNames);
        Assert.Equal(["41 Diabetes basissett 2024", "42 Diabetes basissett 2024"], peerNames);
    }

    // ---------------------------------------------------------------------------------------
    // The rows virtualisation has not realised: the ToString fallback
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void ARowWhoseContainerHasNoNameIsAnnouncedByToString()
    {
        // The mechanism itself, on a bare ListBox with the framework's own container style: no
        // AutomationProperties.Name anywhere, and the peer reads the item's ToString.  This is what
        // the four lists did before the bindings above existed - it is why they announced
        // "QuickStat.ViewModels.DataElementViewModel" - and it is still the answer on every path
        // that reaches a row without realising it.  Hence ToString is overridden as well.
        List<string> names = _wpf.Run(() =>
        {
            ListBox bare = new()
            {
                ItemsSource = new object[]
                {
                    Connections()[0],
                    Elements(1)[0],
                    new PopulationViewModel(PopulationTestDoubles.NewPopulation(282, "Diagnoseår mangler")),
                    new PackageViewModel(Selection(41, "Diabetes basissett 2024")),
                },
            };

            List<string> read = [];

            RealisedWindow.RunControl(bare, _ => read = RowNames(bare));

            return read;
        });

        Assert.Equal(
            ["EFT00028_TEST_020", "^ Alder", "282 Diagnoseår mangler", "41 Diabetes basissett 2024"],
            names);
    }

    [Fact]
    public void OnlyTheRealisedRowsAreInThePeerTreeAtAll()
    {
        // Measured, not assumed, because it decides how much the bindings above can be worth: a
        // virtualised list puts NO peer in the tree for a row it has not realised - GetChildren
        // returns the realised ones and stops.  A client reaches the rest by realising them first
        // (ItemContainerPattern, VirtualizedItemPattern), at which point the binding applies; until
        // then the name those patterns compare against is the item's ToString.
        (int items, int peers, bool lastRealised) = _wpf.Run(() =>
        {
            CollectionsTabView view = new();

            view.ElementsList.ItemsSource = Elements(213);

            int count = 0;
            bool realised = true;

            RealisedWindow.RunControl(view, _ =>
            {
                count = RowNames(view.ElementsList).Count;
                realised = view.ElementsList.ItemContainerGenerator.ContainerFromIndex(212) is not null;
            });

            return (213, count, realised);
        });

        Assert.False(lastRealised, "Row 212 is off the bottom of an 800x600 window and should be virtualised away.");
        Assert.InRange(peers, 1, items - 1);
    }

    [Fact]
    public void EveryItemTypeStringifiesToTheNameRatherThanItsTypeName()
    {
        // The value the case above reaches through a peer, fixed here for all four types at once and
        // without a window - so a list added later inherits a safe name even before anyone remembers
        // the binding.
        Assert.Equal("EFT00028_TEST_020", Connections()[0].ToString());
        Assert.Equal("^ Alder", Elements(1)[0].ToString());
        Assert.Equal(
            "282 Diagnoseår mangler",
            new PopulationViewModel(PopulationTestDoubles.NewPopulation(282, "Diagnoseår mangler")).ToString());
        Assert.Equal(
            "41 Diabetes basissett 2024",
            new PackageViewModel(Selection(41, "Diabetes basissett 2024")).ToString());
    }

    // ---------------------------------------------------------------------------------------
    // R6: what must never be in the tree at all
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void NoPartOfTheProjectTabAnnouncesAConnectionString()
    {
        // Not "the item peers are right" but "the secret is nowhere", over every peer the tab
        // exposes.  A record's generated ToString put the whole connection string one fallback away
        // from a screen reader, and a UIA client caches what it reads.
        List<string> everything = _wpf.Run(() =>
        {
            PopulationTabView view = new();
            ComboBox combo = Combo(view);

            combo.ItemsSource = new[]
            {
                new QuickStatConnection
                {
                    Name = "EFT00028_TEST_020",
                    StudyName = "NDV",
                    ConnectionString =
                        $"Provider=MSOLEDBSQL;Data Source=.;User ID=sa;Password={NotARealPassword};",
                },
            };

            List<string> names = [];

            RealisedWindow.RunControl(view, _ =>
            {
                combo.IsDropDownOpen = true;

                combo.UpdateLayout();

                names = [.. AllNames(Peer(view))];

                combo.IsDropDownOpen = false;
            });

            return names;
        });

        Assert.Contains("EFT00028_TEST_020", everything);
        Assert.DoesNotContain(everything, name => name.Contains(NotARealPassword, StringComparison.Ordinal));
        Assert.DoesNotContain(everything, name => name.Contains("Provider=", StringComparison.Ordinal));
        Assert.DoesNotContain(everything, name => name.Contains("QuickStatConnection", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------------------------------
    // The chrome the added styles must not have cost
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void TheContainerStylesStillCarryTheThemesChrome()
    {
        // An ItemContainerStyle replaces the style, not the theme style, so a one-setter style leaves
        // the default template alone - stated in the markup and checked here rather than trusted.
        // QsPopulationItem's padding and QsCheckListItem's are the cheapest observable proof that
        // BasedOn resolved and that the combo item kept a template at all.
        (double populationPadding, double elementPadding, bool comboItemHasTemplate) = _wpf.Run(() =>
        {
            PopulationPickerView picker = new();
            CollectionsTabView collections = new();
            PopulationTabView tab = new();
            ComboBox combo = Combo(tab);

            picker.PopulationList.ItemsSource = new[]
            {
                new PopulationViewModel(PopulationTestDoubles.NewPopulation(1, "One")),
            };

            collections.ElementsList.ItemsSource = Elements(1);
            combo.ItemsSource = Connections();

            double population = 0;
            double element = -1;
            bool templated = false;

            RealisedWindow.RunControl(picker, _ =>
                population = ((ListBoxItem)picker.PopulationList.ItemContainerGenerator.ContainerFromIndex(0)!)
                    .Padding.Top);

            RealisedWindow.RunControl(collections, _ =>
                element = ((ListBoxItem)collections.ElementsList.ItemContainerGenerator.ContainerFromIndex(0)!)
                    .Padding.Top);

            RealisedWindow.RunControl(tab, _ =>
            {
                combo.IsDropDownOpen = true;

                combo.UpdateLayout();

                templated = ((ComboBoxItem)combo.ItemContainerGenerator.ContainerFromIndex(0)!).Template is not null;

                combo.IsDropDownOpen = false;
            });

            return (population, element, templated);
        });

        // QsPopulationItem: Padding 4,6, inherited through BasedOn.
        Assert.Equal(6d, populationPadding);

        // QsCheckListItem says 4,2 and the view overrides it to 0 so the collecting highlight spans
        // the row; the added setter must not have disturbed that.
        Assert.Equal(0d, elementPadding);

        Assert.True(comboItemHasTemplate, "The combo item lost its default template to the added style.");
    }

    // ---------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------

    private static ComboBox Combo(PopulationTabView view) =>
        ((DockPanel)view.Content).Children.OfType<ComboBox>().Single();

    private static ListBox Packages(PackagesTabView view) =>
        ((DockPanel)view.Content).Children.OfType<ListBox>().Single();

    private static QuickStatConnection[] Connections() =>
    [
        new()
        {
            Name = "EFT00028_TEST_020",
            StudyName = "NDV",
            ConnectionString = @"FILE NAME=.\FastTrak.UDL",
        },
        new()
        {
            Name = "EFT00028_BEHOVPOL",
            StudyName = "ENDO",
            ConnectionString = @"FILE NAME=.\FastTrak.UDL",
        },
    ];

    /// <summary>The check list, with the first element carrying the <c>^ </c> sort prefix.</summary>
    /// <param name="count">How many rows.</param>
    /// <returns>The rows, in list order.</returns>
    private static DataElementViewModel[] Elements(int count) =>
    [
        .. Enumerable.Range(0, count).Select(index =>
            index == 0
                ? new DataElementViewModel("QS_AGE", "^ Alder")
                : new DataElementViewModel($"C{index:D3}", $"Element {index:D3}")),
    ];

    private static PackagedSelection Selection(int rowId, string title) => new()
    {
        RowId = rowId,
        StudyId = 124,
        PopulationId = 257,
        Title = title,
        Comment = "",
        CollectorNames = ["QS_BMI"],
    };

    private static AutomationPeer Peer(UIElement element) =>
        UIElementAutomationPeer.CreatePeerForElement(element)
        ?? throw new InvalidOperationException($"{element.GetType().Name} produced no automation peer.");

    /// <summary>What <c>AutomationProperties.Name</c> evaluated to on each realised container.</summary>
    /// <param name="list">The list or combo.</param>
    /// <returns>One entry per container, in item order.</returns>
    /// <remarks>
    /// The binding, read straight off the container. Separate from <see cref="RowNames"/> on purpose:
    /// this fails when the markup loses its setter, whereas the peer name would quietly fall through
    /// to <c>ToString</c> and keep passing.
    /// </remarks>
    private static List<string> ContainerNames(ItemsControl list) =>
    [
        .. Enumerable.Range(0, list.Items.Count).Select(index =>
            AutomationProperties.GetName(
                list.ItemContainerGenerator.ContainerFromIndex(index)
                ?? throw new InvalidOperationException($"Item {index} has no container."))),
    ];

    /// <summary>What a screen reader walking the list would be told, row by row.</summary>
    /// <param name="list">The list or combo.</param>
    /// <returns>One name per item, in item order.</returns>
    private static List<string> RowNames(ItemsControl list) =>
        [.. (Peer(list).GetChildren() ?? []).Select(child => child.GetName())];

    /// <summary>Every name in a peer subtree, the element's own first.</summary>
    /// <param name="peer">The root.</param>
    /// <returns>The names, depth first.</returns>
    private static IEnumerable<string> AllNames(AutomationPeer peer)
    {
        yield return peer.GetName();

        foreach (AutomationPeer child in peer.GetChildren() ?? [])
        {
            foreach (string name in AllNames(child))
            {
                yield return name;
            }
        }
    }
}
