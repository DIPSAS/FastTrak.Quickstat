using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickStat.Services;
using QuickStat.Tests.Ui.Dialogs;
using QuickStat.Tests.Ui.Shell;
using QuickStat.ViewModels;
using QuickStat.Views;
using Xunit;

namespace QuickStat.Tests.Ui;

/// <summary>
/// Every view the product ships parses, constructs, and finds every theme key it names.
/// </summary>
/// <remarks>
/// <para>
/// <b>What was missing, and why.</b> <c>{StaticResource}</c> is resolved while XAML is being
/// parsed, so a view that names a theme key and does not merge the theme into its own
/// <c>Resources</c> cannot be constructed at all unless the theme is reachable from
/// <see cref="Application.Current"/>. Step 3.6's four dialogs merge it themselves and have had
/// construction tests since they were written; the seven views of steps 3.1, 3.2, 3.3 and 3.4 do
/// not, and their markup was pinned only as XML - <c>Ui/Populations/PopulationViewMarkupTests.cs</c>
/// is the surviving example - with "does it actually load" answered by launching the executable by
/// hand. PORT-PLAN.md §8.10 (a). <see cref="WpfApplicationFixture"/> is what closes that, and this
/// is what spends it.
/// </para>
/// <para>
/// <b>Why construction is the assertion.</b> A missing or misspelled key is not a build error -
/// <c>{StaticResource QsTelBrush}</c> compiles - so the failure is a
/// <see cref="System.Windows.Markup.XamlParseException"/> out of <c>InitializeComponent</c>, and
/// there is no partly-loaded view to inspect afterwards. Each case below therefore asserts that the
/// constructor returned, that the <c>x:Name</c>d fields the code-behind and the view-model rely on
/// were produced, and - so that the test states what it proves rather than only that nothing threw -
/// that one value really did come out of the application's dictionary rather than out of a default.
/// </para>
/// <para>
/// <b>What is deliberately not re-tested here.</b> Layout, bindings, commands and the scroll
/// bookkeeping all have their own suites. Nothing below asserts a metric or drives a command.
/// </para>
/// </remarks>
[Collection(WpfApplicationCollection.Name)]
public class ViewInstantiationTests
{
    private readonly WpfApplicationFixture _wpf;

    /// <summary>Takes the assembly's one application.</summary>
    /// <param name="wpf">Injected by xUnit from <see cref="WpfApplicationCollection"/>.</param>
    public ViewInstantiationTests(WpfApplicationFixture wpf)
    {
        ArgumentNullException.ThrowIfNull(wpf);

        _wpf = wpf;
    }

    // -------------------------------------------------------------------------------------------
    // The harness itself.  A view test that passed because the fixture quietly did nothing would be
    // worse than no test at all, so the fixture's own claims are asserted first.
    // -------------------------------------------------------------------------------------------

    [Fact]
    public void TheApplicationIsTheOneTheProductShips()
    {
        // Not a stand-in that merges a transcribed list of dictionaries: App.xaml is the single
        // statement of what the shipped theme is, and this is that class running that file.
        Assert.IsType<App>(_wpf.Application);
        Assert.Same(_wpf.Application, Application.Current);
    }

    [Fact]
    public void TheShippedThemeIsMergedIntoTheApplicationsResources()
    {
        (object? brush, object? style, int merged) = _wpf.Run(() => (
            Application.Current.Resources["QsBorderBrush"],
            Application.Current.Resources["QsPrimaryButton"],
            Application.Current.Resources.MergedDictionaries.Count));

        Assert.NotNull(brush);
        Assert.NotNull(style);

        // One, not two.  App.xaml merges only QuickStat.Styles.xaml, because that file pulls the
        // brushes in itself; listing both would give every brush key two instances.
        Assert.Equal(1, merged);
    }

    [Fact]
    public void ASecondFixtureInstanceReusesTheOneApplication()
    {
        // The layer that does the actual work.  xUnit gives one fixture per collection, but that is
        // a convention about this suite's shape; the static Lazy inside the fixture is what makes
        // "one Application" true of the process.  Without it - or with a laxer
        // LazyThreadSafetyMode - the line below would throw "Cannot create more than one
        // System.Windows.Application instance in the same AppDomain" and take the collection with
        // it, which is precisely the failure PORT-PLAN.md §8.10 (a) was afraid of.
        WpfApplicationFixture second = new();

        Assert.Same(_wpf.Application, second.Application);
        Assert.Same(_wpf.Application, Application.Current);
    }

    [Fact]
    public void ThereCanBeNoSecondApplication()
    {
        // The rule the fixture exists to hold: WPF allows one per AppDomain and says so by throwing.
        // Anything that created its own would take this suite down from wherever it ran, which is
        // exactly why the fixture owns the only one.
        InvalidOperationException failure =
            Assert.Throws<InvalidOperationException>(() => _wpf.Run(() => new Application()));

        Assert.Contains("more than one", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EveryBodyRunsOnTheOneBackgroundStaThread()
    {
        (int first, ApartmentState apartment, bool background, string? name) = _wpf.Run(() => (
            Environment.CurrentManagedThreadId,
            Thread.CurrentThread.GetApartmentState(),
            Thread.CurrentThread.IsBackground,
            Thread.CurrentThread.Name));

        int second = _wpf.Run(() => Environment.CurrentManagedThreadId);

        Assert.Equal(first, second);
        Assert.Equal(ApartmentState.STA, apartment);
        Assert.NotEqual(Environment.CurrentManagedThreadId, first);

        // Background, or Dispatcher.Run would keep the test host alive after the last test.
        Assert.True(background);
        Assert.Equal("QuickStat test application", name);
    }

    [Fact]
    public void AFailureInsideSurfacesToTheCaller()
    {
        // The same contract as StaTestRunner: the worker captures and the caller rethrows, so a
        // wrong assertion inside a marshalled body fails the test rather than killing the apartment.
        InvalidTimeZoneException failure = Assert.Throws<InvalidTimeZoneException>(
            () => _wpf.Run(() => throw new InvalidTimeZoneException("boom")));

        Assert.Equal("boom", failure.Message);
        Assert.Contains(nameof(AFailureInsideSurfacesToTheCaller), failure.StackTrace, StringComparison.Ordinal);
    }

    [Fact]
    public void TheApplicationRegistersEveryWindowBuiltOnItsOwnThread()
    {
        // The measured fact behind [assembly: CollectionBehavior(DisableTestParallelization = true)]
        // in Ui/WpfApplicationFixture.cs, checked rather than asserted in a comment: an
        // Application makes every Window in the process its business, on collections it does not
        // synchronise.  If a future WPF stops doing this, this fails and the switch can come out.
        int before = _wpf.Run(() => Application.Current.Windows.Count);

        _wpf.Run(() =>
        {
            Window own = new();

            Assert.Equal(before + 1, Application.Current.Windows.Count);
            Assert.Contains(own, Application.Current.Windows.Cast<Window>());
        });

        // A window built on somebody else's apartment goes on the other list, which
        // Application.Windows does not show - the count is unchanged - but it is the same
        // unsynchronised bookkeeping, reached from every dialog test in the suite.
        StaTestRunner.Run(() => Assert.NotNull(new Window()));

        Assert.Equal(before + 1, _wpf.Run(() => Application.Current.Windows.Count));
    }

    [Fact]
    public void TheApplicationKeepsNoMainWindowBetweenBodies()
    {
        // A Window built on the application's own thread makes itself Application.MainWindow, and
        // both WpfFileDialogService and DialogOwner read that to pick an owner.  The fixture clears
        // it after every body so one test cannot become the next one's owner.
        _wpf.Run(() => Assert.NotNull(new Window()));

        Assert.Null(_wpf.Run(() => Application.Current.MainWindow));
    }

    // -------------------------------------------------------------------------------------------
    // The views.  One case each, in the order the shell composes them.
    // -------------------------------------------------------------------------------------------

    [Fact]
    public void TheBannerLoads() => _wpf.Run(() =>
    {
        AppBannerView view = new();

        // No x:Name in this one: it is bound to MainViewModel throughout and the code-behind is the
        // constructor.  Background="{StaticResource QsBannerBrush}" is on the root element, so the
        // brush having arrived is the proof that the lookup reached the application.
        Assert.Same(Application.Current.Resources["QsBannerBrush"], view.Background);
        Assert.Equal(55d, view.Height);
    });

    [Fact]
    public void ThePopulationTabLoadsWithThePickerInsideIt() => _wpf.Run(() =>
    {
        // This one is worth two: PopulationTabView hosts PopulationPickerView, so a broken key in
        // either file fails here.  The tab's own theme use is the combo box's style.
        PopulationTabView view = new();

        DockPanel root = (DockPanel)view.Content;
        ComboBox projects = Assert.Single(root.Children.OfType<ComboBox>());

        Assert.Same(Application.Current.Resources["QsFlatComboBox"], projects.Style);
        Assert.Single(root.Children.OfType<PopulationPickerView>());
    });

    [Fact]
    public void ThePopulationPickerLoadsWithItsTwoNamedElements() => _wpf.Run(() =>
    {
        PopulationPickerView view = new();

        // FilterBox is named because the placeholder's MultiDataTrigger binds to it by ElementName;
        // PopulationList because §B.1.1's shared-size scope and input bindings hang off it.
        Assert.NotNull(view.FilterBox);
        Assert.NotNull(view.PopulationList);

        Assert.Same(Application.Current.Resources["QsFlatTextBox"], view.FilterBox.Style);

        // BasedOn rather than Same: the container style is the view's own since the row gained an
        // accessible name, which is content and so does not belong in a theme key three lists share.
        Assert.Same(
            Application.Current.Resources["QsPopulationItem"],
            view.PopulationList.ItemContainerStyle.BasedOn);

        // The three run styles are the view's own, not the application's - they are declared in
        // UserControl.Resources and they in turn resolve QsCodeBrush and friends from the theme,
        // which is the nested case that markup read as XML cannot check.
        Assert.NotNull(view.Resources["PopulationCodeRun"]);
        Assert.NotNull(view.Resources["PopulationGroupRun"]);
        Assert.NotNull(view.Resources["PopulationHelpRun"]);
    });

    [Fact]
    public void TheCollectionsTabLoadsWithItsCheckList() => _wpf.Run(() =>
    {
        CollectionsTabView view = new();

        // ElementsList is the one the constructor hands to ScrollKeeper; if the field were null the
        // constructor would already have thrown from ScrollKeeper's null guard.
        Assert.NotNull(view.ElementsList);
        Assert.Same(Application.Current.Resources["QsBorderBrush"], view.ElementsList.BorderBrush);
    });

    [Fact]
    public void ThePackagesTabLoads() => _wpf.Run(() =>
    {
        PackagesTabView view = new();

        DockPanel root = (DockPanel)view.Content;
        ListBox packages = Assert.Single(root.Children.OfType<ListBox>());

        Assert.Same(Application.Current.Resources["QsPackageItem"], packages.ItemContainerStyle.BasedOn);
        Assert.Same(Application.Current.Resources["QsFlatTextBox"], Assert.Single(root.Children.OfType<TextBox>()).Style);
    });

    [Fact]
    public void TheDatasetTabLoadsWithTheGridAndTheHint() => _wpf.Run(() =>
    {
        DatasetTabView view = new();

        // Both names are used by the code-behind - Grid for CellActivated and Refresh, HintPanel by
        // the Canvas placement - so a rename that lost either would be a compile error there and a
        // silent null here.
        Assert.NotNull(view.Grid);
        Assert.NotNull(view.HintPanel);

        Assert.Same(Application.Current.Resources["QsPageBrush"], view.Background);
        Assert.Same(Application.Current.Resources["QsGridLineBrush"], view.Grid.GridLineBrush);

        // The grid's context menu is a keyed resource in the view's own dictionary rather than an
        // inline child, so nothing else would notice if it stopped parsing.
        Assert.IsType<ContextMenu>(view.Resources["DatasetActionsMenu"]);
    });

    [Fact]
    public void TheShellWindowLoadsWithEveryViewInsideIt()
    {
        // The whole tree in one construction: banner, the three tabs and their nested views, the
        // splitter, the dataset pane and the busy overlay.  Composed from the real container so the
        // view-models are the ones the shell would use - a stub could satisfy a binding the product
        // cannot.
        using ServiceProvider provider = ShellCompositionTests.Build();

        MainViewModel shell = provider.GetRequiredService<MainViewModel>();
        IWindowStateService windowState = provider.GetRequiredService<IWindowStateService>();
        BusyOverlayViewModel overlay = provider.GetRequiredService<BusyOverlayViewModel>();
        ILogger<MainWindow> logger = provider.GetRequiredService<ILogger<MainWindow>>();

        _wpf.Run(() =>
        {
            // Constructed and never shown.  Show() would run OnSourceInitialized -> Restore and put
            // a real window on the desktop, and Close() would run OnClosing -> Save + Flush and
            // write this machine's own QuickStat.ini.  Neither belongs in a test.
            MainWindow window = new(shell, windowState, overlay, logger);

            Assert.NotNull(window.LeftColumn);
            Assert.NotNull(window.SelectionTabs);
            Assert.NotNull(window.BusyOverlay);

            Assert.Same(Application.Current.Resources["QsFormFaceBrush"], window.Background);
            Assert.Same(Application.Current.Resources["QsTabControl"], window.SelectionTabs.Style);

            // MainWindow.xaml sets it from MainViewModel.SplitterPosition in the constructor, so a
            // non-default value here is the code-behind and the named column agreeing.
            Assert.Equal(shell.SplitterPosition, window.LeftColumn.Width.Value);

            // The icon is a pack URI into this assembly's resources; a missing Assets entry throws
            // out of the constructor, which no XML-level test can see.
            Assert.NotNull(window.Icon);
        });
    }

    // -------------------------------------------------------------------------------------------
    // Bindings.  This is the half the markup tests explicitly cannot reach.
    // -------------------------------------------------------------------------------------------

    [Fact]
    public void NoViewReportsABindingErrorAgainstItsRealViewModel()
    {
        // A binding path that does not exist is not a build error and not an exception: WPF writes
        // "System.Windows.Data Error: 40" to a trace source and carries on with the target's default
        // value, which is precisely the class of defect "pinned as XML" cannot see - the markup test
        // compares the binding's TEXT against itself.  Listening to that trace source turns it into
        // a failure.
        //
        // Limits, stated rather than implied: the view-models are freshly composed, so their
        // collections are empty and no ItemTemplate is instantiated; the bindings inside the row
        // templates are therefore NOT covered here.  What is covered is every binding on the static
        // chrome of all six views.
        using ServiceProvider provider = ShellCompositionTests.Build();

        MainViewModel shell = provider.GetRequiredService<MainViewModel>();

        List<string> errors = _wpf.Run(() =>
        {
            using BindingErrorSink sink = new();

            PresentationTraceSources.Refresh();
            PresentationTraceSources.DataBindingSource.Listeners.Add(sink);

            SourceLevels restore = PresentationTraceSources.DataBindingSource.Switch.Level;

            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;

            try
            {
                Realise(new AppBannerView(), shell);
                Realise(new PopulationTabView(), shell.Population);
                Realise(new PopulationPickerView(), shell.Population.Picker);
                Realise(new CollectionsTabView(), shell.Collections);
                Realise(new PackagesTabView(), shell.Packages);
                Realise(new DatasetTabView(), shell.Dataset);
            }
            finally
            {
                PresentationTraceSources.DataBindingSource.Switch.Level = restore;
                PresentationTraceSources.DataBindingSource.Listeners.Remove(sink);
            }

            return sink.Messages;
        });

        Assert.Equal([], errors);
    }

    /// <summary>Puts a view on screen far off the desktop with a data context, then takes it down.</summary>
    /// <param name="view">The view under test.</param>
    /// <param name="dataContext">The view-model the shell would give it.</param>
    /// <remarks>
    /// Realising is not optional: a binding written in XAML is compiled into BAML unattached and
    /// stays that way however many times the tree is measured, so an unrealised view reports no
    /// binding errors because it has evaluated no bindings. <c>Ui/Dialogs/RealisedWindow.cs</c>
    /// records the experiment.
    /// </remarks>
    private static void Realise(FrameworkElement view, object dataContext)
    {
        view.DataContext = dataContext;

        RealisedWindow.RunControl(view, realised => realised.UpdateLayout());
    }

    /// <summary>Collects whatever WPF writes to the data-binding trace source.</summary>
    /// <remarks>
    /// <see cref="TraceListener.Write(string)"/> is called for the fragments of one entry and
    /// <see cref="TraceListener.WriteLine(string)"/> for the last of them, so only the line endings
    /// are counted as messages; otherwise one error would arrive as several.
    /// </remarks>
    private sealed class BindingErrorSink : TraceListener
    {
        private readonly List<string> _messages = [];
        private string _pending = "";

        internal List<string> Messages => _messages;

        public override void Write(string? message) => _pending += message;

        public override void WriteLine(string? message)
        {
            _messages.Add(_pending + message);
            _pending = "";
        }
    }
}
