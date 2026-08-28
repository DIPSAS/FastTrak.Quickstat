using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuickStat.Input;

/// <summary>
/// Runs a command when an element - in practice a <see cref="ListBox"/> - is double-clicked with the
/// left button.
/// </summary>
/// <remarks>
/// <para>
/// <b>This exists because the obvious spelling does not work.</b> Both lists used to say
/// <c>&lt;ListBox.InputBindings&gt;&lt;MouseBinding MouseAction="LeftDoubleClick" …&gt;</c>, which
/// compiles, produces no binding error, and silently never fires: an <see cref="InputBinding"/> is
/// matched while the input event bubbles through the element that owns it, and
/// <see cref="ListBoxItem"/> sets <see cref="RoutedEventArgs.Handled"/> on the mouse-down it uses to
/// select itself. The event therefore stops one level below the <see cref="ListBox"/> whose
/// collection holds the binding, and the double-click never reaches it. A double-click on the list's
/// own blank area <em>does</em> fire, which is what makes the bug so easy to miss in a half-full list
/// and impossible to see in a full one.
/// </para>
/// <para>
/// <see cref="Control.MouseDoubleClick"/> is the right event instead: <see cref="ListBoxItem"/> is
/// itself a <see cref="Control"/>, raises it, and lets it <em>bubble</em> - so a handler on the list
/// hears double-clicks on its rows.
/// </para>
/// <para>
/// <b>No hit test, deliberately.</b> The command fires for a double-click anywhere in the list,
/// including below the last row, and is guarded only by its own <see cref="ICommand.CanExecute"/>.
/// That is what the VCL does: <c>TObjectListView</c> is a <c>TDrawGrid</c> and
/// <c>fPopView.OnDblClick := PopulationRequested</c>
/// (<c>EPR.VclFrame.Populations.pas:123</c>) fires on the control, not on a row, with
/// <c>TryGetHighlightedPopulation</c> as the whole of the guard. Adding a hit test would be a
/// quieter application than the one being ported.
/// </para>
/// <para>
/// <see cref="Control"/> raises <see cref="Control.MouseDoubleClick"/> for the right button as well
/// as the left, so the button is checked here; the gesture being replaced was
/// <see cref="MouseAction.LeftDoubleClick"/>.
/// </para>
/// </remarks>
public static class DoubleClick
{
    /// <summary>The command to run. Attach it to the <see cref="ListBox"/>, not to the item.</summary>
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached(
            "Command",
            typeof(ICommand),
            typeof(DoubleClick),
            new PropertyMetadata(null, OnCommandChanged));

    /// <summary>Reads <see cref="CommandProperty"/>.</summary>
    /// <param name="element">The element the property is attached to.</param>
    /// <returns>The command, or <see langword="null"/>.</returns>
    public static ICommand? GetCommand(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);

        return (ICommand?)element.GetValue(CommandProperty);
    }

    /// <summary>Writes <see cref="CommandProperty"/>.</summary>
    /// <param name="element">The element the property is attached to.</param>
    /// <param name="value">The command, or <see langword="null"/> to detach.</param>
    public static void SetCommand(DependencyObject element, ICommand? value)
    {
        ArgumentNullException.ThrowIfNull(element);

        element.SetValue(CommandProperty, value);
    }

    private static void OnCommandChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not Control control)
        {
            return;
        }

        // Unsubscribe unconditionally: the handler is static, so subscribing twice would run the
        // command twice, and re-attaching is what a DataContext swap looks like from here.
        control.MouseDoubleClick -= OnMouseDoubleClick;

        if (e.NewValue is ICommand)
        {
            control.MouseDoubleClick += OnMouseDoubleClick;
        }
    }

    private static void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left
            || sender is not DependencyObject element
            || GetCommand(element) is not { } command
            || !command.CanExecute(null))
        {
            return;
        }

        command.Execute(null);

        // Marked handled for the same reason an InputBinding would have: the gesture has been
        // consumed, and nothing above the list has a second meaning for it.
        e.Handled = true;
    }
}
