using System.IO;
using System.Xml.Linq;
using QuickStat.Tests.Configuration;
using Xunit;

namespace QuickStat.Tests.Ui.Packages;

/// <summary>
/// The package row, read as markup. <c>05-ui-spec.md</c> §B.3.
/// </summary>
/// <remarks>
/// Same technique and the same reason as
/// <c>Ui/Populations/PopulationViewMarkupTests</c>: reading the source is the only way to assert
/// which <em>declared</em> value a run carries, because a constructed control reports the effective
/// one and cannot tell a local value from a style setter.
/// </remarks>
public class PackagesViewMarkupTests
{
    private static readonly XNamespace Wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static XDocument View() => XDocument.Load(
        Path.Combine(RepositoryFiles.Root, "QuickStat.App", "Views", "PackagesTabView.xaml"));

    private static string? Attribute(XElement element, XName name) => (string?)element.Attribute(name);

    /// <summary>
    /// The four runs of the row, and the weight of the title in particular.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The title is <c>Bold</c>, not <c>SemiBold</c>, and that is a fix rather than a preference.</b>
    /// With <c>SemiBold</c> the running application drew the title <em>pixel for pixel identically</em>
    /// to the comment beneath it - the same ink mass and the same 92 px extent for the same string,
    /// measured off the screen while closing the §6 items of <c>Docs/Port/08-parity-checklist.md</c>.
    /// <c>Bold</c> on the same build draws it at 1.54 times the ink. Weight 600 is simply not
    /// arriving; the population list next door has always said <c>Bold</c>
    /// (<c>PopulationPickerView.xaml</c>) and has always looked right.
    /// </para>
    /// <para>
    /// So this is not a style opinion to be tidied back to <c>SemiBold</c> later: §B.3 asks for a
    /// bold title, and this is the value that produces one. PORT-PLAN.md §8.11 (14).
    /// </para>
    /// </remarks>
    [Fact]
    public void TheRowHasItsFourRunsAndABoldTitle()
    {
        XElement template = Assert.Single(View().Descendants(Wpf + "DataTemplate"));

        List<XElement> runs = [.. template.Descendants(Wpf + "TextBlock")];

        Assert.Equal(4, runs.Count);

        XElement code = runs[0];
        XElement title = runs[1];
        XElement population = runs[2];
        XElement comment = runs[3];

        Assert.Equal("{Binding RowId}", Attribute(code, "Text"));
        Assert.Equal("{StaticResource QsCodeBrush}", Attribute(code, "Foreground"));

        Assert.Equal("{Binding Title}", Attribute(title, "Text"));
        Assert.Equal("Bold", Attribute(title, "FontWeight"));
        Assert.Equal("CharacterEllipsis", Attribute(title, "TextTrimming"));
        Assert.Equal("{Binding Title}", Attribute(title, "ToolTip"));

        Assert.Equal("{Binding PopulationLabel}", Attribute(population, "Text"));
        Assert.Equal("{StaticResource QsCategoryBrush}", Attribute(population, "Foreground"));
        Assert.Equal("Right", Attribute(population, "TextAlignment"));
        Assert.Equal("{StaticResource QsSmallFontSize}", Attribute(population, "FontSize"));

        Assert.Equal("{Binding Comment}", Attribute(comment, "Text"));
        Assert.Equal("Wrap", Attribute(comment, "TextWrapping"));
        Assert.Null(Attribute(comment, "FontWeight"));
    }

    /// <summary>An empty comment collapses its line rather than reserving a blank one.</summary>
    /// <remarks>
    /// The VCL measured the drawn text, so a package saved without a comment was a single-line row
    /// there too - and the live rows confirm it: 39 px with one line of comment against 73 px with
    /// three.
    /// </remarks>
    [Fact]
    public void AnEmptyCommentCollapses()
    {
        XElement trigger = Assert.Single(View().Descendants(Wpf + "DataTrigger"));

        Assert.Equal("{Binding Comment}", Attribute(trigger, "Binding"));
        Assert.Equal("", Attribute(trigger, "Value"));

        XElement setter = Assert.Single(trigger.Descendants(Wpf + "Setter"));

        Assert.Equal("Comment", Attribute(setter, "TargetName"));
        Assert.Equal("Visibility", Attribute(setter, "Property"));
        Assert.Equal("Collapsed", Attribute(setter, "Value"));
    }
}
