using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Services;
using QuickStat.Tests.Ui.Dialogs;
using QuickStat.Tests.Ui.Services;
using QuickStat.ViewModels;
using Xunit;

namespace QuickStat.Tests.Ui.Shell;

/// <summary>
/// The busy overlay stops the keyboard as well as the mouse, and the Cancel button underneath it
/// stays reachable.
/// </summary>
/// <remarks>
/// <para>
/// <c>05-ui-spec.md</c> §G.3 is <c>Screen.Cursor := crSqlWait</c> and nothing more, because the
/// Delphi ran the query on the user-interface thread: no message was pumped while it worked, so
/// there was no keyboard to shut out. The port does the work elsewhere and the window stays alive,
/// and the overlay only hit-tests - so <c>Tab</c> walked straight into the check list underneath it.
/// PORT-PLAN.md §8.10 (f).
/// </para>
/// <para>
/// <b>What was measured here, and why the fix is two things rather than one.</b> Disabling the
/// content host stops the keyboard <em>arriving</em>: <see cref="UIElement.Focus"/> on an element
/// inside a disabled subtree returns <see langword="false"/> and tab traversal skips it. It does
/// <em>not</em> evict focus that is already there - the focused element stays focused while
/// disabled, and a disabled element handles no input, so the keyboard would be parked on a dead
/// control with nothing to Tab to. Hence <c>MainWindow.OnBusyOverlayVisibilityChanged</c> moves
/// focus onto the overlay and puts it back afterwards, and hence these cases assert about focus and
/// not only about <c>IsEnabled</c>.
/// </para>
/// <para>
/// The window is composed from the real container, as <see cref="ViewInstantiationTests"/> does, so
/// the view-models are the ones the shell would use - but with a
/// <see cref="WindowStateService"/> over an <c>InMemorySettingsStore</c>, because these cases have to
/// <c>Show()</c> the window and <c>Close()</c> would otherwise write this machine's own
/// <c>QuickStat.ini</c>.
/// </para>
/// </remarks>
[Collection(WpfApplicationCollection.Name)]
public class MainWindowBusyLockoutTests
{
    private readonly WpfApplicationFixture _wpf;

    /// <summary>Takes the assembly's one application.</summary>
    /// <param name="wpf">Injected by xUnit from <see cref="WpfApplicationCollection"/>.</param>
    public MainWindowBusyLockoutTests(WpfApplicationFixture wpf)
    {
        ArgumentNullException.ThrowIfNull(wpf);

        _wpf = wpf;
    }

    [Fact]
    public void TheShellIsDisabledForExactlyAsLongAsTheOverlayIsUp() => RunShell((window, progress) =>
    {
        Assert.True(window.ShellContent.IsEnabled);

        using (progress.BeginOperation("Alfa"))
        {
            Assert.False(window.ShellContent.IsEnabled);

            // The overlay is a sibling of the content host and not inside it, which is the whole
            // reason the shell can be switched off without switching the Cancel button off with it.
            Assert.True(window.BusyOverlay.IsEnabled);

            // BeginOperation counts, so the collect a package replay runs inside its own scope must
            // not hand the shell back when it finishes (§G.3).
            using (progress.BeginOperation("Beta"))
            {
                Assert.False(window.ShellContent.IsEnabled);
            }

            Assert.False(window.ShellContent.IsEnabled);
        }

        Assert.True(window.ShellContent.IsEnabled);
    });

    [Fact]
    public void FocusAlreadyInsideTheShellIsMovedOutWhenTheOverlayGoesUp() => RunShell((window, progress) =>
    {
        // The case IsEnabled = false does not cover on its own: the user was in the tab strip when
        // the operation started.  Left alone, focus stays on the now-disabled control, which handles
        // no input - the keyboard is not merely blocked, it is stranded.
        Assert.True(window.SelectionTabs.Focus());
        Assert.True(window.SelectionTabs.IsKeyboardFocusWithin);

        using (progress.BeginOperation("Alfa"))
        {
            Assert.False(window.SelectionTabs.IsKeyboardFocusWithin);
            Assert.True(window.BusyOverlay.IsKeyboardFocusWithin);
        }
    });

    [Fact]
    public void TabCannotGetBackIntoTheShellWhileTheOverlayIsUp() => RunShell((window, progress) =>
    {
        using (progress.BeginOperation("Alfa"))
        {
            // Both halves of "cannot reach": nothing in the shell will take focus if asked, and
            // traversal from where the keyboard is parked does not find anything there either.
            Assert.False(window.SelectionTabs.Focus());
            Assert.False(window.SelectionTabs.IsKeyboardFocusWithin);

            _ = window.BusyOverlay.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

            Assert.False(window.ShellContent.IsKeyboardFocusWithin);
        }
    });

    [Fact]
    public void TheCancelButtonIsOneTabFromWhereTheKeyboardParks() => RunShell((window, progress) =>
    {
        using CancellationTokenSource cancellation = new();

        using (progress.BeginOperation("Alfa", cancellation))
        {
            window.UpdateLayout();

            Assert.True(window.BusyOverlay.IsFocused);
            Assert.Equal(Visibility.Visible, window.BusyOverlay.CancelButton.Visibility);

            Assert.True(window.BusyOverlay.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
            Assert.True(window.BusyOverlay.CancelButton.IsFocused);
            Assert.True(window.BusyOverlay.CancelButton.IsEnabled);
        }
    });

    [Fact]
    public void FocusGoesBackWhereItWasWhenTheOverlayComesDown() => RunShell((window, progress) =>
    {
        // §G.4's argument about the check list's scroll offset, applied to focus: an operation the
        // user did not initiate from the keyboard must not cost them their place.
        Assert.True(window.SelectionTabs.Focus());

        using (progress.BeginOperation("Alfa"))
        {
            Assert.False(window.SelectionTabs.IsKeyboardFocusWithin);
        }

        Assert.True(window.SelectionTabs.IsKeyboardFocusWithin);
    });

    /// <summary>Builds the shell, shows it far off the desktop, and runs a body against it.</summary>
    /// <param name="body">What to assert, given the window and the progress service driving it.</param>
    /// <remarks>
    /// The window has to be realised: the overlay's <c>Visibility</c> is a XAML binding, and a
    /// binding compiled into BAML stays unattached until then - <c>Ui/Dialogs/RealisedWindow.cs</c>
    /// records the experiment - so an unshown window would never raise <c>IsVisibleChanged</c> and
    /// every case here would pass against a default.
    /// </remarks>
    private void RunShell(Action<MainWindow, IShellProgress> body)
    {
        using ServiceProvider provider = ShellCompositionTests.Build();

        MainViewModel shell = provider.GetRequiredService<MainViewModel>();
        BusyOverlayViewModel overlay = provider.GetRequiredService<BusyOverlayViewModel>();
        IShellProgress progress = provider.GetRequiredService<IShellProgress>();

        WindowStateService windowState = new(
            new InMemorySettingsStore(),
            new FakeMonitorLayout(new Rect(0, 0, 1920, 1040)),
            NullLogger<WindowStateService>.Instance);

        _wpf.Run(() => RealisedWindow.Run(
            new MainWindow(shell, windowState, overlay, NullLogger<MainWindow>.Instance),
            window => body(window, progress)));
    }
}
