using System.Windows;
using System.Windows.Media;

namespace QuickStat.Tests.Ui;

/// <summary>Walks a realised visual tree, in document order.</summary>
/// <remarks>
/// <para>
/// The tree has to be <em>realised</em> for any of this to mean anything: a control that has never
/// been in a presentation source has no visual children, so every walk over it returns nothing and
/// every <c>Assert.Single</c> against it fails for the wrong reason.
/// <c>Ui/Dialogs/RealisedWindow.cs</c> is what puts one there.
/// </para>
/// <para>
/// Templated content is reached, which is the point: <c>SectionHeader</c> renders its
/// <c>Header</c> through a <c>TextBlock</c> inside a <see cref="System.Windows.Controls.ControlTemplate"/>,
/// and asserting on the control's property would prove the property, not the bar a user sees.
/// </para>
/// <para>
/// <c>Ui/AppBannerIconTests.cs</c> and <c>Ui/Dataset/DatasetTabHintTests.cs</c> each carry a private
/// copy of this walk, written before there was a shared one. They are left alone: they pass, and
/// rewriting a passing test to save nine lines is a change with only downside.
/// </para>
/// </remarks>
internal static class VisualTree
{
    /// <summary>Every descendant of <paramref name="root"/> of type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">What to look for.</typeparam>
    /// <param name="root">Where to start. Not itself returned.</param>
    /// <returns>Matches in depth-first document order, which for a panel is child order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="root"/> is <see langword="null"/>.</exception>
    internal static IEnumerable<T> Descendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        ArgumentNullException.ThrowIfNull(root);

        int children = VisualTreeHelper.GetChildrenCount(root);

        for (int index = 0; index < children; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);

            if (child is T match)
            {
                yield return match;
            }

            foreach (T deeper in Descendants<T>(child))
            {
                yield return deeper;
            }
        }
    }
}
