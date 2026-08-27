using System.Windows.Media;
using QuickStat.Domain.DataPoints;

namespace QuickStat.Controls.Dataset;

/// <summary>
/// Turns domain colours into frozen WPF brushes, once each, and reproduces the Delphi colour blend.
/// </summary>
/// <remarks>
/// <para>
/// The cache is the point. <c>QuickStat.Core</c> never references WPF, so a cell's colour arrives as
/// a <see cref="Rgb"/> and has to be converted at the boundary
/// (<c>Docs/Port/06-contracts.md</c> §1). The documented worst case is a sparse 1500 × 1000 matrix,
/// and allocating a <see cref="SolidColorBrush"/> per cell per frame would not survive it - so every
/// brush is created at most once and <c>Freeze</c>d, which also lets the render thread use it
/// without taking a lock.
/// </para>
/// <para>
/// There are only ever a handful of distinct colours in practice: nine risk-ladder colours, the
/// empty-cell grey, white, and the blends of those with the current-row tint.
/// </para>
/// </remarks>
internal sealed class MatrixGridPalette
{
    private readonly Dictionary<uint, SolidColorBrush> _brushes = [];

    /// <summary>How many distinct brushes the cache holds.</summary>
    /// <remarks>Exists so a test can prove the cache is a cache and not a factory.</remarks>
    public int Count => _brushes.Count;

    /// <summary>Converts a domain colour to a WPF one.</summary>
    /// <param name="rgb">The domain colour.</param>
    /// <returns>The opaque WPF colour.</returns>
    public static Color ToColor(Rgb rgb) => Color.FromRgb(rgb.R, rgb.G, rgb.B);

    /// <summary>
    /// Blends two colours the way <c>TColorCalculator.BlendColors</c> does.
    /// </summary>
    /// <param name="from">The colour being tinted - in this control, the cell's own background.</param>
    /// <param name="to">The tint.</param>
    /// <param name="percent">How far to move, 0 to 100.</param>
    /// <returns>The blended colour, fully opaque.</returns>
    /// <remarks>
    /// <para>
    /// <c>Result := A + Round( (B - A) * pct / 100 )</c> per channel
    /// (<c>Emetra.VclUtil.ColorCalculator.pas:229-238</c>). The division is floating point and the
    /// rounding is Delphi's <c>Round</c>, which is the FPU's - <b>half to even</b>, the same rule
    /// <see cref="Math.Round(double)"/> follows.
    /// </para>
    /// <para>
    /// <b>An earlier revision truncated toward zero and said so in this comment, citing the same
    /// unit.</b> The Pascal says <c>round</c>, and the difference is visible: blending white with
    /// <c>#E7F2FC</c> at 50 % gives <c>#F3F9FD</c> with the FPU's rule and <c>#F3F9FE</c> with
    /// truncation - one step in blue, in the tint that covers the whole current row. Phase 5 read
    /// both blends off the running <c>22.12.21.547</c> build: over white it paints <c>#F3F9FD</c>
    /// and over the empty-cell grey <c>#F5F5F5</c> it paints <c>#EEF3F9</c>, and only half-to-even
    /// produces both (see <c>PORT-PLAN.md</c> §8.14). Every midpoint here lands on <c>.5</c>,
    /// because the tint is applied at exactly 50 %, so the rule is not a detail.
    /// </para>
    /// <para>
    /// The guard for VCL system colours is not ported: nothing here can be one, because every colour
    /// arrives as a plain <see cref="Rgb"/> triple.
    /// </para>
    /// </remarks>
    public static Color Blend(Color from, Color to, int percent)
    {
        return Color.FromRgb(
            BlendChannel(from.R, to.R, percent),
            BlendChannel(from.G, to.G, percent),
            BlendChannel(from.B, to.B, percent));
    }

    /// <summary>The frozen brush for a colour, created once.</summary>
    /// <param name="color">The colour.</param>
    /// <returns>A frozen <see cref="SolidColorBrush"/>.</returns>
    public SolidColorBrush Brush(Color color)
    {
        uint key = ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;

        if (_brushes.TryGetValue(key, out SolidColorBrush? cached))
        {
            return cached;
        }

        SolidColorBrush brush = new(color);

        brush.Freeze();

        _brushes.Add(key, brush);

        return brush;
    }

    private static byte BlendChannel(byte from, byte to, int percent) =>
        (byte)(from + (int)Math.Round((to - from) * percent / 100d, MidpointRounding.ToEven));
}
