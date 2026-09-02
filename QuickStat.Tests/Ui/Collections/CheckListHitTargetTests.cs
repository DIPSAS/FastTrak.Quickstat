using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Data;
using QuickStat.Domain.Anonymisation;
using QuickStat.Services;
using QuickStat.Tests.Ui.Services;
using QuickStat.ViewModels;
using QuickStat.Views;
using Xunit;

namespace QuickStat.Tests.Ui.Collections;

/// <summary>
/// Where on a check-list row a click actually toggles the box - measured through the shipped
/// markup, not reasoned about.
/// </summary>
/// <remarks>
/// <para>
/// <b>The defect these pin.</b> The row template wrapped its <c>CheckBox</c> in a <c>Border</c>
/// carrying <c>Padding="4,2"</c> and a transparent background. A transparent background is
/// hit-testable, so those two pixels at the top and two at the bottom belonged to the border rather
/// than to the box: a click there selected the row and toggled nothing. Measured on the shipped
/// markup at 96 DPI, <b>6 of every 21 pixels of row height were dead</b> - 3 at the top, 3 at the
/// bottom, the last of each being the <c>ListBoxItem</c>'s own 1 px border - and two adjacent rows
/// therefore made a <b>6 px band</b> in which clicking did nothing but move the selection. On a
/// study with 530 data elements that is a lot of missed ticks.
/// </para>
/// <para>
/// The fix moves the vertical padding off the border and gives the box a <c>MinHeight</c> that
/// fills the row, leaving the horizontal inset where it was so the collecting highlight still spans
/// the full width. Row height is unchanged at 21 px, which is what
/// <see cref="TheRowIsStillTwentyOnePixelsTall"/> is for: making the box bigger by making the row
/// taller would have been a different change, and a visible one.
/// </para>
/// <para>
/// <b>Why a measurement and not an assertion about the XAML.</b> The dead band was invisible in the
/// markup - <c>Padding="4,2"</c> on a <c>Border</c> reads as spacing, and only a hit test says
/// whether the pixels it reserves answer to the <c>CheckBox</c> or to the border. The same
/// reasoning as <c>Ui/Theme/CaptionLeftCheckBoxTests.cs</c>, which measures where the ink falls.
/// </para>
/// </remarks>
[Collection(WpfApplicationCollection.Name)]
public class CheckListHitTargetTests
{
    /// <summary>Row height in device-independent pixels, unchanged by the hit-target fix.</summary>
    private const double RowHeight = 21;

    /// <summary>
    /// Rows the <c>ListBoxItem</c>'s own 1 px border may keep at the top and bottom.
    /// </summary>
    /// <remarks>
    /// It carries the default hover and selection outline, so it is left alone; two dead pixels out
    /// of twenty-one is the residue, against six before.
    /// </remarks>
    private const int ContainerBorder = 1;

    private readonly WpfApplicationFixture _wpf;

    /// <summary>Takes the assembly's one application.</summary>
    /// <param name="wpf">Injected by xUnit from <see cref="WpfApplicationCollection"/>.</param>
    public CheckListHitTargetTests(WpfApplicationFixture wpf)
    {
        ArgumentNullException.ThrowIfNull(wpf);

        _wpf = wpf;
    }

    [Fact]
    public void EveryPixelOfARowExceptItsBorderToggglesTheBox()
    {
        string map = _wpf.Run(() => VerticalMap(row: 0));

        // '#' is the CheckBox, '-' is something else that swallowed the click.
        Assert.Equal(new string('-', ContainerBorder) + new string('#', 19) + new string('-', ContainerBorder), map);
    }

    [Fact]
    public void ALongTitleDoesNotChangeTheAnswer()
    {
        // The second row's title overflows the list's width, which is the case that would tempt a
        // template into wrapping or clipping and so into a different geometry.
        string map = _wpf.Run(() => VerticalMap(row: 1));

        Assert.Equal(new string('-', ContainerBorder) + new string('#', 19) + new string('-', ContainerBorder), map);
    }

    [Fact]
    public void TheWholeWidthOfARowTogglesTheBox()
    {
        // Including the far right, well past the end of a short title: the CheckBox stretches and
        // its template root is hit-testable, so the whole row answers. The Delphi's TCheckListBox
        // toggles on the glyph alone - this is the port being kinder, and it is worth pinning
        // because it is one Border padding away from not being true.
        List<string> landed = _wpf.Run(() => HorizontalMap(row: 0));

        Assert.All(landed, where => Assert.Equal("CheckBox", where));
    }

    [Fact]
    public void TheRowIsStillTwentyOnePixelsTall()
    {
        Assert.Equal(RowHeight, _wpf.Run(() => Measure(row: 0).ActualHeight));
    }

    /// <summary>What a click hits at each whole pixel down the middle of one row.</summary>
    /// <param name="row">Row index.</param>
    /// <returns>One character per pixel: <c>#</c> for the box, <c>-</c> for anything else.</returns>
    private static string VerticalMap(int row)
    {
        ListBoxItem container = Measure(row);
        System.Text.StringBuilder map = new();

        for (double y = 0.5; y < container.ActualHeight; y += 1)
        {
            map.Append(IsOnCheckBox(container, container.ActualWidth / 2, y) ? '#' : '-');
        }

        return map.ToString();
    }

    /// <summary>What a click hits across one row, from just inside each edge.</summary>
    /// <param name="row">Row index.</param>
    /// <returns>The type name hit at each sampled fraction of the row's width.</returns>
    private static List<string> HorizontalMap(int row)
    {
        ListBoxItem container = Measure(row);
        double y = container.ActualHeight / 2;
        List<string> landed = [];

        foreach (double fraction in new[] { 0.02, 0.15, 0.50, 0.85, 0.98 })
        {
            DependencyObject? hit = VisualTreeHelper
                .HitTest(container, new Point(container.ActualWidth * fraction, y))?.VisualHit;

            landed.Add(hit is null ? "nothing" : Ancestor<CheckBox>(hit) is not null ? "CheckBox" : hit.GetType().Name);
        }

        return landed;
    }

    private static bool IsOnCheckBox(Visual root, double x, double y) =>
        VisualTreeHelper.HitTest(root, new Point(x, y))?.VisualHit is { } hit && Ancestor<CheckBox>(hit) is not null;

    /// <summary>
    /// Builds the real tab in a real window, lays it out, and hands back one realised row.
    /// </summary>
    /// <param name="row">Row index.</param>
    /// <returns>The container, measured and arranged.</returns>
    /// <remarks>
    /// A <see cref="Window"/> rather than a bare <c>Measure</c>/<c>Arrange</c>: hit testing needs
    /// the visual tree to have been rendered into a real presentation source, and a
    /// <see cref="ListBox"/> generates no containers until its panel has a viewport.
    /// </remarks>
    private static ListBoxItem Measure(int row)
    {
        FakeCollectorRegistry registry = new();

        _ = registry.With("A", "^ Alder");
        _ = registry.With("L", "Labdata: Antall prøver siste 24 mnd (2 år) med høg konfidens");

        ShellWorkspace workspace = new(ShellWorkspaceTests.NewMatrix());
        FakeSessionService session = new();

        CollectionsTabViewModel viewModel = new(
            workspace,
            new IdentificationPolicy(),
            registry,
            new RecordingCollectorRunner(),
            session,
            new ShellProgress(new InlineUiDispatcher()),
            new InlineUiDispatcher(),
            new RecordingUserNotifier(),
            NullLogger<CollectionsTabViewModel>.Instance);

        SessionContext context = FakeSessionService.NewSession();

        session.Raise(context);
        _ = registry.BuildAsync(context).GetAwaiter().GetResult();

        CollectionsTabView view = new() { DataContext = viewModel };
        Window host = new() { Width = 340, Height = 700, Content = view };

        host.Show();
        host.UpdateLayout();

        ListBox list = (ListBox)view.FindName("ElementsList")!;

        list.UpdateLayout();

        ListBoxItem container = (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(row)!;

        // Closed here rather than in a finally: the caller only reads geometry off the container,
        // which survives, and a using block around a Window on a shared apartment is what
        // WpfApplicationFixture's remarks warn about.
        host.Close();

        Assert.True(
            container.ActualWidth > 0,
            string.Create(CultureInfo.InvariantCulture, $"Row {row} was never laid out."));

        return container;
    }

    private static T? Ancestor<T>(DependencyObject from)
        where T : DependencyObject
    {
        DependencyObject? current = from;

        while (current is not null)
        {
            if (current is T found)
            {
                return found;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
