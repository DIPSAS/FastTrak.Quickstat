using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QuickStat.Controls;
using QuickStat.Tests.Ui.Dialogs;
using Xunit;

namespace QuickStat.Tests.Ui.Theme;

/// <summary>
/// The two pieces of chrome §F.4 describes in pixels: the teal section bar and the selected tab's
/// highlight.
/// </summary>
/// <remarks>
/// <para>
/// <b>Checklist item 8.1.</b> <c>ThemeResourceTests</c> already pins <c>QsTealBrush</c> to
/// <c>#178891</c> and asserts that <c>QsSectionHeader</c> resolves to a style; what neither says is
/// that a bar on screen ends up wearing them. A style that resolved and was never applied - an
/// implicit key removed, a template setter renamed - would leave every one of those cases green and
/// the window grey.
/// </para>
/// <para>
/// So these realise controls and read the rendered tree. The values are transcribed from §F.4 rather
/// than read back out of the dictionary under test, except where the assertion is deliberately
/// <em>identity</em>: <see cref="Assert.Same(object, object)"/> against the application's own brush
/// proves the lookup reached the theme, and the hex beside it proves the theme is the right colour.
/// One without the other is satisfiable by an accident.
/// </para>
/// <para>
/// The selected tab's <b>bold caption</b> is not here - <c>TabCaptionWeightTests</c> owns it,
/// together with the containment rule that the weight must not reach the tab's page. This file adds
/// the other half of the item: the 3 px bar, and which edge it sits on.
/// </para>
/// </remarks>
[Collection(WpfApplicationCollection.Name)]
public class SectionHeaderChromeTests
{
    private static readonly Color Teal = Color.FromRgb(0x17, 0x88, 0x91);
    private static readonly Color White = Color.FromRgb(0xFF, 0xFF, 0xFF);

    private readonly WpfApplicationFixture _wpf;

    /// <summary>Takes the assembly's one application; the implicit styles live in its resources.</summary>
    /// <param name="wpf">Injected by xUnit from <see cref="WpfApplicationCollection"/>.</param>
    public SectionHeaderChromeTests(WpfApplicationFixture wpf)
    {
        ArgumentNullException.ThrowIfNull(wpf);

        _wpf = wpf;
    }

    [Fact]
    public void TheSectionBarIsTealTwentySixHighWithWhiteText()
    {
        (Brush Bar, Brush Text, double Height) painted = _wpf.Run(() =>
        {
            // No Style attribute, exactly as every call site writes it: the implicit style in
            // Application.Resources is what dresses the control, and that is part of what is
            // under test - SectionHeader deliberately has no DefaultStyleKey and no Generic.xaml.
            SectionHeader header = new() { Header = "Select data elements" };
            (Brush Bar, Brush Text, double Height) seen = default!;

            RealisedWindow.RunControl(header, realised =>
            {
                realised.UpdateLayout();

                Border bar = Assert.Single(VisualTree.Descendants<Border>(realised));
                TextBlock heading = VisualTree.Descendants<TextBlock>(realised).First();

                seen = (bar.Background, heading.Foreground, realised.ActualHeight);
            });

            return seen;
        });

        // Identity first - the brush came out of the application's dictionary and not out of a
        // literal somewhere in the template.
        Assert.Same(_wpf.Run(() => Application.Current.Resources["QsTealBrush"]), painted.Bar);
        Assert.Same(_wpf.Run(() => Application.Current.Resources["QsOnAccentBrush"]), painted.Text);

        // Then the colours themselves, from 05-ui-spec.md §F.4.
        Assert.Equal(Teal, ((SolidColorBrush)painted.Bar).Color);
        Assert.Equal(White, ((SolidColorBrush)painted.Text).Color);

        Assert.Equal(SectionHeader.BarHeight, painted.Height);
        Assert.Equal(26d, painted.Height);
    }

    [Fact]
    public void TheSelectedTabCarriesAThreePixelTealBarAlongItsTopEdge()
    {
        (Brush Selected, Brush Other, double Height, VerticalAlignment Edge) bar = _wpf.Run(() =>
        {
            TabItem first = new() { Header = "Population", Content = new TextBlock() };
            TabItem second = new() { Header = "Collections", Content = new TextBlock() };

            TabControl tabs = new();

            tabs.Items.Add(first);
            tabs.Items.Add(second);

            (Brush Selected, Brush Other, double Height, VerticalAlignment Edge) seen = default!;

            RealisedWindow.RunControl(tabs, realised =>
            {
                realised.UpdateLayout();

                first.IsSelected = true;
                realised.UpdateLayout();

                Border highlight = HighlightBarOf(first);

                seen = (highlight.Background, HighlightBarOf(second).Background, highlight.Height, highlight.VerticalAlignment);
            });

            return seen;
        });

        Assert.Same(_wpf.Run(() => Application.Current.Resources["QsTealBrush"]), bar.Selected);
        Assert.Equal(Teal, ((SolidColorBrush)bar.Selected).Color);

        // The unselected tab has the same bar with nothing in it, which is what makes the strip read
        // as one row rather than as tabs of two different heights.
        Assert.Equal(Colors.Transparent, ((SolidColorBrush)bar.Other).Color);

        Assert.Equal(3d, bar.Height);
        Assert.Equal(VerticalAlignment.Top, bar.Edge);
    }

    /// <summary>The named bar out of <c>QsTabItem</c>'s template.</summary>
    /// <param name="tab">A realised tab.</param>
    /// <returns>The 3 px strip along one edge of the tab.</returns>
    /// <remarks>
    /// By name rather than by walking, because the template's other <see cref="Border"/>-less
    /// elements would make a positional walk depend on layout details this item is not about.
    /// </remarks>
    private static Border HighlightBarOf(TabItem tab) =>
        (Border)tab.Template.FindName("HighlightBar", tab);
}
