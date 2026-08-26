using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace QuickStat.Converters;

/// <summary>
/// <see cref="bool"/> to <see cref="Visibility"/>, with the two knobs WPF's built-in converter
/// lacks: inversion, and whether <see langword="false"/> collapses or merely hides.
/// </summary>
/// <remarks>
/// <para>
/// <c>05-ui-spec.md</c> §H.1 lists <c>BoolToVisibility</c> as one of the three converters the port
/// needs. The framework's <see cref="BooleanToVisibilityConverter"/> would do for the common case,
/// but not for the <c>Collections</c> tab (§B.0), which must be <see cref="Visibility.Collapsed"/>
/// - not <see cref="Visibility.Hidden"/> - until a population is loaded, and not for the places
/// where the flag reads the other way round.
/// </para>
/// <para>
/// Also a <see cref="MarkupExtension"/>, so XAML can write
/// <c>Visibility="{Binding HasPopulation, Converter={conv:BoolToVisibilityConverter}}"</c> without
/// a resource key. That is the convention for every converter in this folder; there is deliberately
/// no converter resource dictionary to keep in step with the class list.
/// </para>
/// </remarks>
public sealed class BoolToVisibilityConverter : MarkupExtension, IValueConverter
{
    /// <summary>A shared instance for code that needs one without XAML.</summary>
    public static readonly BoolToVisibilityConverter Default = new();

    /// <summary>Treat <see langword="true"/> as hidden and <see langword="false"/> as visible.</summary>
    public bool Invert { get; set; }

    /// <summary>
    /// What the falsy case produces: <see cref="Visibility.Collapsed"/> when
    /// <see langword="true"/> (the default), <see cref="Visibility.Hidden"/> when
    /// <see langword="false"/>.
    /// </summary>
    public bool Collapse { get; set; } = true;

    /// <inheritdoc />
    public object Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        bool flag = value is bool b && b;

        if (Invert)
        {
            flag = !flag;
        }

        return flag ? Visibility.Visible : Collapse ? Visibility.Collapsed : Visibility.Hidden;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        bool flag = value is Visibility visibility && visibility == Visibility.Visible;

        return Invert ? !flag : flag;
    }

    /// <inheritdoc />
    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
