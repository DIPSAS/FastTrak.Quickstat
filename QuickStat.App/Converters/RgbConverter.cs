using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using QuickStat.Domain.DataPoints;

namespace QuickStat.Converters;

/// <summary>
/// The one place a domain <see cref="Rgb"/> becomes a WPF <see cref="Color"/> or <see cref="Brush"/>.
/// </summary>
/// <remarks>
/// <para>
/// <c>QuickStat.Core</c> must never reference <c>PresentationCore</c> or <c>WindowsBase</c>
/// (PORT-PLAN.md §5 Phase 1), so cell colouring - which is domain logic, driven by fourteen analyte
/// classes with hardcoded threshold ladders - produces
/// <see cref="Rgb"/>. Everything that paints converts here.
/// </para>
/// <para>
/// Brushes are cached and frozen. A collect run over a 1500 x 1000 matrix asks for a brush per
/// painted cell, and the palette behind those calls is a couple of dozen distinct colours; without
/// the cache the grid would allocate a <see cref="SolidColorBrush"/> per cell per frame.
/// </para>
/// <para>
/// <see cref="ToBrush"/> is the member <see cref="QuickStat.Controls.Dataset.MatrixGrid"/> wants;
/// the <see cref="IValueConverter"/> face exists for XAML bindings and returns a
/// <see cref="Brush"/> or a <see cref="Color"/> depending on the binding's target type.
/// </para>
/// </remarks>
public sealed class RgbConverter : MarkupExtension, IValueConverter
{
    /// <summary>A shared instance for code that needs one without XAML.</summary>
    public static readonly RgbConverter Default = new();

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Rgb, SolidColorBrush> BrushCache = new();

    /// <summary>Converts a domain colour to a WPF colour.</summary>
    /// <param name="rgb">The domain colour.</param>
    /// <returns>The opaque WPF colour.</returns>
    public static Color ToColor(Rgb rgb) => Color.FromRgb(rgb.R, rgb.G, rgb.B);

    /// <summary>Converts a domain colour to a shared, frozen brush.</summary>
    /// <param name="rgb">The domain colour.</param>
    /// <returns>A frozen <see cref="SolidColorBrush"/>, the same instance for the same colour.</returns>
    /// <remarks>
    /// The returned brush is frozen, so it is safe to hand to any thread and impossible for one
    /// caller to recolour on behalf of every other.
    /// </remarks>
    public static SolidColorBrush ToBrush(Rgb rgb) => BrushCache.GetOrAdd(
        rgb,
        static value =>
        {
            SolidColorBrush brush = new(ToColor(value));

            brush.Freeze();

            return brush;
        });

    /// <inheritdoc />
    /// <remarks>
    /// Returns a <see cref="Color"/> when the binding target is a <see cref="Color"/>, and a
    /// <see cref="Brush"/> otherwise, which is what a <c>Background</c> or <c>Foreground</c> binding
    /// wants. A <see langword="null"/> input - the domain's "no colour here" - yields
    /// <see cref="Binding.DoNothing"/>, so the target keeps whatever the style set rather than going
    /// transparent.
    /// </remarks>
    public object Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        if (value is not Rgb rgb)
        {
            return Binding.DoNothing;
        }

        return targetType == typeof(Color) ? ToColor(rgb) : ToBrush(rgb);
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always: colours only travel outwards.</exception>
    public object ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture) =>
        throw new NotSupportedException("RgbConverter is one-way.");

    /// <inheritdoc />
    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
