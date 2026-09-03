using System.Windows.Controls;
using QuickStat.Controls;
using QuickStat.Tests.Ui.Dialogs;
using QuickStat.Views;
using Xunit;

namespace QuickStat.Tests.Ui.Dataset;

/// <summary>
/// The teal caption bar over the grid shows what the view-model computed, before and after a run.
/// </summary>
/// <remarks>
/// <para>
/// <b>Checklist item 4.1.</b> <see cref="DatasetViewModelTests.TheCaptionStartsAsYourDataset"/> and
/// <see cref="DatasetViewModelTests.TheCaptionIsRowsByColumns"/> already pin the two strings and the
/// argument order - <c>rows x columns</c>, which is the half that is easy to get backwards. What
/// neither can see is whether the <em>bar</em> shows them: <c>Header="{Binding CaptionText}"</c> is
/// one attribute, and deleting it would leave both of those cases passing and the window blank.
/// </para>
/// <para>
/// So this is the binding and the template, not the format string. It realises the view, because a
/// BAML binding is unattached until then and an unrealised bar reports its default - see
/// <c>Ui/Dialogs/RealisedWindow.cs</c> - and it reads the <see cref="TextBlock"/> inside
/// <see cref="SectionHeader"/>'s template rather than the <c>Header</c> property, so a template that
/// stopped painting the heading fails here too.
/// </para>
/// <para>
/// No culture scope, unlike the view-model cases: the two substituted numbers are integers, which
/// format identically everywhere. The format string itself is asserted under a forced <c>en-US</c>
/// where it belongs, in <see cref="DatasetViewModelTests"/>.
/// </para>
/// </remarks>
[Collection(WpfApplicationCollection.Name)]
public class DatasetCaptionBarTests
{
    private readonly WpfApplicationFixture _wpf;

    /// <summary>Takes the assembly's one application; the view names theme keys.</summary>
    /// <param name="wpf">Injected by xUnit from <see cref="WpfApplicationCollection"/>.</param>
    public DatasetCaptionBarTests(WpfApplicationFixture wpf)
    {
        ArgumentNullException.ThrowIfNull(wpf);

        _wpf = wpf;
    }

    [Fact]
    public void TheBarReadsYourDatasetBeforeAnythingIsLoaded()
    {
        string caption = _wpf.Run(() =>
        {
            using DatasetHarness harness = new();

            return CaptionOf(harness);
        });

        Assert.Equal("Your dataset", caption);
    }

    [Fact]
    public void TheBarFollowsTheViewModelWhenTheRunFinishes()
    {
        // One patient over one column, which is what DatasetHarness.LoadAndCollect stages - so the
        // grid size is "1 x 1" and the two numbers cannot be transposed unnoticed here.  That they
        // are rows-then-columns is DatasetViewModelTests' assertion, on a shape where it shows.
        (string Before, string After) seen = _wpf.Run(() =>
        {
            using DatasetHarness harness = new();

            string before = CaptionOf(harness);

            harness.LoadAndCollect();

            return (before, CaptionOf(harness));
        });

        // Stated as a change: a bar that ignored the binding and painted a literal would pass an
        // "after" assertion on its own only if the literal happened to match, but a bar bound to the
        // wrong property would sit unchanged - which this catches whatever the wrong property holds.
        Assert.NotEqual(seen.Before, seen.After);
        Assert.Equal("Population: 1 \"Aktive pasienter\". Grid size: 1 x 1", seen.After);
    }

    /// <summary>Realises the real Dataset tab and reads the text its caption bar paints.</summary>
    /// <param name="harness">The wiring the container would produce.</param>
    /// <returns>The heading string on the teal bar.</returns>
    /// <remarks>
    /// The tab has one <see cref="SectionHeader"/> - asserted rather than assumed, because the
    /// right-hand slot holds <c>Wide columns</c> and <c>Export</c> and a second bar would make
    /// "the caption" ambiguous. Its first descendant <see cref="TextBlock"/> is the heading; the
    /// template puts the heading in column 0 and the content presenter in column 1.
    /// </remarks>
    private static string CaptionOf(DatasetHarness harness)
    {
        DatasetTabView view = new() { DataContext = harness.ViewModel };
        string caption = "";

        RealisedWindow.RunControl(view, realised =>
        {
            realised.UpdateLayout();

            SectionHeader bar = Assert.Single(VisualTree.Descendants<SectionHeader>(realised));

            caption = VisualTree.Descendants<TextBlock>(bar).First().Text;
        });

        return caption;
    }
}
