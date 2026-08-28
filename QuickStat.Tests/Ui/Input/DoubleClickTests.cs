using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using QuickStat.Input;
using QuickStat.Tests.Configuration;
using QuickStat.Tests.Ui.Dialogs;
using Xunit;

namespace QuickStat.Tests.Ui.Input;

/// <summary>
/// <see cref="DoubleClick"/>, and the routing fact it exists because of.
/// </summary>
/// <remarks>
/// <para>
/// <b>These were written after the bug, not before it.</b> Both lists in the application said
/// <c>&lt;MouseBinding MouseAction="LeftDoubleClick" …&gt;</c> inside
/// <c>ListBox.InputBindings</c>, and neither ever fired on a row - so double-clicking a population
/// did nothing at all, and the package replay, which has no keyboard equivalent by design, was
/// unreachable by any input. It survived every test in the suite because the test that covered it
/// asserted the <em>markup</em>: it read the <c>.xaml</c>, found a <c>MouseBinding</c> with the right
/// gesture and the right command, and passed. A test that asserts what was typed cannot notice that
/// what was typed does not work.
/// </para>
/// <para>
/// So these raise real routed events through a realised tree instead.
/// <see cref="TheItemSwallowsTheMouseDownAnInputBindingWouldNeed"/> pins the mechanism and
/// <see cref="ADoubleClickOnARowRunsTheListsCommand"/> pins the fix; between them they say why the
/// spelling in the two views is not interchangeable with the obvious one.
/// </para>
/// </remarks>
public class DoubleClickTests
{
    [Fact]
    public void TheItemSwallowsTheMouseDownAnInputBindingWouldNeed()
    {
        // The whole cause, in one assertion. An InputBinding is matched while the input event
        // bubbles through the element whose collection holds it, so a MouseBinding on the ListBox
        // needs the mouse-down to reach the ListBox. ListBoxItem selects itself on that event and
        // marks it handled, so it never gets there.
        (bool handledByTheItem, bool reachedTheList) = StaTestRunner.Run(() =>
        {
            ListBox list = new() { ItemsSource = new[] { "one", "two" } };
            bool sawIt = false;

            list.AddHandler(
                UIElement.MouseLeftButtonDownEvent,
                new MouseButtonEventHandler((_, _) => sawIt = true),
                handledEventsToo: false);

            bool handled = false;

            RealisedWindow.RunControl(list, _ =>
            {
                ListBoxItem row = Row(list, 0);

                MouseButtonEventArgs down = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
                {
                    RoutedEvent = UIElement.MouseLeftButtonDownEvent,
                    Source = row,
                };

                row.RaiseEvent(down);

                handled = down.Handled;
            });

            return (handled, sawIt);
        });

        Assert.True(handledByTheItem, "ListBoxItem no longer marks the selecting mouse-down handled.");
        Assert.False(reachedTheList, "The mouse-down reached the ListBox, so an InputBinding there would have worked.");
    }

    [Fact]
    public void ADoubleClickOnARowRunsTheListsCommand()
    {
        // MouseDoubleClick is raised by the item - ListBoxItem is a Control - and bubbles, which is
        // the property the fix rests on. Raised from the row, caught on the list.
        int runs = StaTestRunner.Run(() => Raise(MouseButton.Left, canExecute: true));

        Assert.Equal(1, runs);
    }

    [Fact]
    public void TheSpellingItReplacesNeverFires()
    {
        // The negative control, kept rather than performed once: the exact markup both views
        // carried, driven by the exact same double click, running the exact same command - and the
        // command is never reached. If this ever starts passing, WPF has changed and the behaviour
        // could go back to being one line of XAML.
        int runs = StaTestRunner.Run(() =>
        {
            StubCommand command = new(canExecute: true);
            ListBox list = new() { ItemsSource = new[] { "one", "two" } };

            list.InputBindings.Add(
                new MouseBinding(command, new MouseGesture(MouseAction.LeftDoubleClick)));

            RealisedWindow.RunControl(list, _ => DoubleClickOn(Row(list, 0)));

            return command.Runs;
        });

        Assert.Equal(0, runs);
    }

    [Fact]
    public void ARightDoubleClickDoesNothing()
    {
        // Control raises MouseDoubleClick for the right button too, and the gesture being replaced
        // was LeftDoubleClick. Right-clicking a package opens its context menu; it must not also
        // replay it.
        int runs = StaTestRunner.Run(() => Raise(MouseButton.Right, canExecute: true));

        Assert.Equal(0, runs);
    }

    [Fact]
    public void TheCommandsOwnGuardStillDecides()
    {
        // No hit test and no second guard: CanExecute is the whole of it, which is what the VCL's
        // TryGetHighlightedPopulation amounted to.
        int runs = StaTestRunner.Run(() => Raise(MouseButton.Left, canExecute: false));

        Assert.Equal(0, runs);
    }

    [Fact]
    public void ClearingThePropertyUnhooksTheHandler()
    {
        int runs = StaTestRunner.Run(() =>
        {
            StubCommand command = new(canExecute: true);
            ListBox list = new() { ItemsSource = new[] { "one" } };

            DoubleClick.SetCommand(list, command);
            DoubleClick.SetCommand(list, null);

            RealisedWindow.RunControl(list, _ => DoubleClickOn(Row(list, 0)));

            return command.Runs;
        });

        Assert.Equal(0, runs);
    }

    [Fact]
    public void ReattachingDoesNotRunItTwice()
    {
        // The handler is static, so a second subscription would be a second call rather than a
        // no-op - which is what a DataContext swap looks like from inside the property changed
        // callback.
        int runs = StaTestRunner.Run(() =>
        {
            StubCommand first = new(canExecute: true);
            StubCommand second = new(canExecute: true);
            ListBox list = new() { ItemsSource = new[] { "one" } };

            DoubleClick.SetCommand(list, first);
            DoubleClick.SetCommand(list, second);

            RealisedWindow.RunControl(list, _ => DoubleClickOn(Row(list, 0)));

            return first.Runs + second.Runs;
        });

        Assert.Equal(1, runs);
    }

    [Fact]
    public void NoViewGoesBackToAMouseBinding()
    {
        // Repo-wide, because the defect was in two views and the second one had no keyboard
        // fallback: the package replay was reachable by nothing at all. A MouseBinding on a
        // ListBox is silent rather than broken, so nothing downstream would say so again.
        XNamespace wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace input = "clr-namespace:QuickStat.Input";

        List<string> offenders = [];

        foreach (string path in Directory.EnumerateFiles(
            Path.Combine(RepositoryFiles.Root, "QuickStat.App"),
            "*.xaml",
            SearchOption.AllDirectories))
        {
            if (XDocument.Load(path).Descendants(wpf + "MouseBinding").Any())
            {
                offenders.Add(Path.GetFileName(path));
            }
        }

        Assert.Equal([], offenders);

        // And the one list with no Enter beside it still has its only way in.
        XDocument packages = XDocument.Load(
            Path.Combine(RepositoryFiles.Root, "QuickStat.App", "Views", "PackagesTabView.xaml"));

        XElement list = Assert.Single(packages.Descendants(wpf + "ListBox"));

        Assert.Equal(
            "{Binding OpenPackageCommand}",
            (string?)list.Attribute(input + "DoubleClick.Command"));
    }

    private static int Raise(MouseButton button, bool canExecute)
    {
        StubCommand command = new(canExecute);
        ListBox list = new() { ItemsSource = new[] { "one", "two" } };

        DoubleClick.SetCommand(list, command);

        RealisedWindow.RunControl(list, _ => DoubleClickOn(Row(list, 0), button));

        return command.Runs;
    }

    /// <summary>
    /// A real second click on <paramref name="row"/>: <see cref="Mouse.MouseDownEvent"/> with
    /// <see cref="MouseButtonEventArgs.ClickCount"/> 2, raised on the row and left to route.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Not <see cref="Control.MouseDoubleClickEvent"/> raised directly, and the difference is the
    /// point.</b> That event is registered <see cref="RoutingStrategy.Direct"/> - it does not bubble
    /// - so raising it on the row would prove nothing about a handler on the list. What actually
    /// happens at run time is that <see cref="Control"/> class-handles the bubbling mouse-down
    /// <em>with <c>handledEventsToo</c></em>, so the list still sees it after
    /// <see cref="ListBoxItem"/> has marked it handled, and raises its own direct
    /// <see cref="Control.MouseDoubleClick"/> on itself. That whole path is what these cases
    /// exercise, because the bug being fixed was a wrong belief about exactly this routing.
    /// </para>
    /// <para>
    /// <see cref="MouseButtonEventArgs.ClickCount"/> has no public setter - WPF fills it from the
    /// device - so it is set by reflection. That is confined to this helper, and the alternative is
    /// a synthetic shortcut of the kind that hid the defect in the first place.
    /// </para>
    /// </remarks>
    private static void DoubleClickOn(ListBoxItem row, MouseButton button = MouseButton.Left)
    {
        MouseButtonEventArgs args = new(Mouse.PrimaryDevice, 0, button)
        {
            RoutedEvent = Mouse.MouseDownEvent,
            Source = row,
        };

        typeof(MouseButtonEventArgs)
            .GetProperty(nameof(MouseButtonEventArgs.ClickCount))!
            .SetValue(args, 2);

        Assert.Equal(2, args.ClickCount);

        row.RaiseEvent(args);
    }

    private static ListBoxItem Row(ListBox list, int index)
    {
        list.UpdateLayout();

        object? container = list.ItemContainerGenerator.ContainerFromIndex(index);

        return Assert.IsType<ListBoxItem>(container);
    }

    private sealed class StubCommand(bool canExecute) : ICommand
    {
        public int Runs { get; private set; }

        event EventHandler? ICommand.CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => canExecute;

        public void Execute(object? parameter) => Runs++;
    }
}
