using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Xunit;

namespace QuickStat.Tests.Ui.Theme;

/// <summary>
/// What <c>QsTabItem</c> puts on the selected tab's caption must stop at the caption.
/// </summary>
/// <remarks>
/// <para>
/// <b>Reported from a running build.</b> §F.2 asks for the selected tab caption to be bold, and the
/// style said so — on the <see cref="TabItem"/> itself. <see cref="TextElement.FontWeight"/> is an
/// <em>inherited</em> property and a tab's <see cref="ContentControl.Content"/> is its logical
/// child, so the weight walked straight into the page: every label, check box and section header on
/// the selected tab went bold with it.
/// </para>
/// <para>
/// It had always been wrong and had never been visible, because the value was <c>SemiBold</c> — a
/// weight this application does not render (PORT-PLAN.md §8.11 (14)). Correcting the weight to
/// <c>Bold</c> is what made the mis-scoping show up, on the first screen of the application.
/// </para>
/// <para>
/// The test asserts the <em>page</em> rather than the caption, because that is where the damage
/// appears, and it walks the same two inherited properties the style sets.
/// </para>
/// </remarks>
public class TabCaptionWeightTests
{
    private const string StylesUri = "/QuickStat;component/Theme/QuickStat.Styles.xaml";

    [Fact]
    public void TheSelectedTabsEmphasisDoesNotReachItsPage() => StaTestRunner.Run(() =>
    {
        ResourceDictionary styles =
            (ResourceDictionary)Application.LoadComponent(new Uri(StylesUri, UriKind.Relative));

        // The control this one is compared with is deliberately outside the tab, so "unaffected"
        // means "the same as a plain TextBlock" rather than a literal that happens to match WPF's
        // own default of 12.
        TextBlock outside = new() { Text = "Select database" };
        TextBlock page = new() { Text = "Select database" };

        TabItem tab = new()
        {
            Header = "Population",
            Content = page,
            Style = (Style)styles["QsTabItem"],
        };

        TabControl control = new();
        control.Items.Add(tab);
        tab.IsSelected = true;

        control.Measure(new Size(400, 300));
        control.Arrange(new Rect(0, 0, 400, 300));
        control.UpdateLayout();

        // The caption carries both, and they are the point of the style.
        ContentPresenter caption = (ContentPresenter)tab.Template.FindName("Label", tab);

        Assert.Equal(FontWeights.Bold, TextElement.GetFontWeight(caption));
        Assert.Equal((double)styles["QsHeaderFontSize"], TextElement.GetFontSize(caption));

        // The page carries neither: body text on the selected tab is regular, at the base size.
        Assert.Equal(outside.FontWeight, page.FontWeight);
        Assert.Equal(outside.FontSize, page.FontSize);
    });
}
