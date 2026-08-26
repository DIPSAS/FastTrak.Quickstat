using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;

namespace QuickStat.Tests.Ui.Theme;

/// <summary>
/// <c>QsCaptionLeftCheckBox</c> puts the caption to the left of the box without mirroring the
/// checkmark.
/// </summary>
/// <remarks>
/// <para>
/// The Delphi's <c>Alignment = taLeftJustify</c> puts a check box's caption on its left, and two
/// places need it: <i>Wide columns</i> (§C.1) and <i>Simplified</i> (§B.1). A stock WPF
/// <see cref="CheckBox"/> offers only <see cref="FrameworkElement.FlowDirection"/> for this, and
/// that mirrors the entire visual subtree — including the <c>Path</c> the default template draws the
/// tick with, which then points down-left. It is obvious on screen, and it was reported from a
/// running build rather than found by a test.
/// </para>
/// <para>
/// These tests render the control and compare pixels rather than reading the style's setters back,
/// because the setters are not the claim. The claim is what the user sees, and it rests on a WPF
/// behaviour the style depends on but cannot state: an implicit style in <c>Style.Resources</c> is
/// found by elements inside the control's own template. If a future theme drew the mark with
/// something other than a <c>Path</c> — the Fluent theme uses a font glyph — the setters would still
/// read as correct while the tick silently went backwards again.
/// </para>
/// </remarks>
public class CaptionLeftCheckBoxTests
{
    private const string StylesUri = "/QuickStat;component/Theme/QuickStat.Styles.xaml";

    /// <summary>
    /// Renders a content-less check box and returns its pixels. No content means every lit pixel is
    /// the box or its mark, and the control is arranged at exactly its desired size, so there is no
    /// slack for the flow direction to shift it sideways — the only difference a mirroring can make
    /// is to the glyph itself.
    /// </summary>
    private static byte[] RenderBox(FlowDirection flow, bool styled) => StaTestRunner.Run(() =>
    {
        CheckBox box = new() { IsChecked = true, FlowDirection = flow };

        if (styled)
        {
            ResourceDictionary styles =
                (ResourceDictionary)Application.LoadComponent(new Uri(StylesUri, UriKind.Relative));

            box.Style = (Style)styles["QsCaptionLeftCheckBox"];

            // The style sets FlowDirection itself, and a local value would win over it - which
            // would quietly test the wrong thing.
            box.ClearValue(FrameworkElement.FlowDirectionProperty);
        }

        // A CheckBox with no visual parent renders nothing at all: bitmap.Render on the control
        // alone produces a fully transparent image, and two blank images compare equal.  That is
        // how the first version of this test passed while asserting nothing, which is why
        // TheRenderIsNotBlank exists below.
        StackPanel host = new() { Background = Brushes.White };
        host.Children.Add(box);

        host.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        host.Arrange(new Rect(host.DesiredSize));
        host.UpdateLayout();

        const double scale = 8.0;
        RenderTargetBitmap bitmap = new(
            (int)(host.DesiredSize.Width * scale),
            (int)(host.DesiredSize.Height * scale),
            96 * scale,
            96 * scale,
            PixelFormats.Pbgra32);
        bitmap.Render(host);

        int stride = bitmap.PixelWidth * 4;
        byte[] pixels = new byte[stride * bitmap.PixelHeight];
        bitmap.CopyPixels(pixels, stride, 0);

        return pixels;
    });

    private static int CountInk(byte[] pixels)
    {
        int ink = 0;

        for (int i = 0; i < pixels.Length; i += 4)
        {
            // Pbgra32, on a white host: anything appreciably darker than the background is the box
            // border or the tick.
            if (pixels[i] < 200 || pixels[i + 1] < 200 || pixels[i + 2] < 200)
            {
                ink++;
            }
        }

        return ink;
    }

    [Fact]
    public void TheStyledBoxDrawsTheSameTickAsAPlainLeftToRightOne()
    {
        // The whole requirement in one comparison: moving the caption to the left of the box must
        // change nothing about the box itself.
        Assert.Equal(
            RenderBox(FlowDirection.LeftToRight, styled: false),
            RenderBox(FlowDirection.RightToLeft, styled: true));
    }

    [Fact]
    public void AndWouldFailIfTheMarkWereStillBeingMirrored()
    {
        // Guards the guard.  Without this, a style that had become a no-op would leave the test
        // above passing.
        Assert.NotEqual(
            RenderBox(FlowDirection.LeftToRight, styled: false),
            RenderBox(FlowDirection.RightToLeft, styled: false));
    }

    [Theory]
    [InlineData(FlowDirection.LeftToRight, false)]
    [InlineData(FlowDirection.RightToLeft, false)]
    [InlineData(FlowDirection.RightToLeft, true)]
    public void TheRenderIsNotBlank(FlowDirection flow, bool styled)
    {
        // Both comparisons above are between byte arrays, so a rendering path that produced nothing
        // would make one vacuously true and the other fail for a reason that has no connection to
        // check marks.  That is not hypothetical: it is what the first version of this file did.
        Assert.True(CountInk(RenderBox(flow, styled)) > 100, "the check box rendered no visible ink");
    }
}
