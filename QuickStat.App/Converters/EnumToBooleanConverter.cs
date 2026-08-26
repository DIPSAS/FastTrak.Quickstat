using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace QuickStat.Converters;

/// <summary>
/// Binds a radio button to one member of an enumeration.
/// </summary>
/// <remarks>
/// <para>
/// This is what turns the three <c>Export options</c> radios into a single
/// <see cref="QuickStat.Domain.Anonymisation.PersonIdentification"/> property
/// (<c>05-ui-spec.md</c> §B.2, §H.1):
/// </para>
/// <code>
/// &lt;RadioButton Content="Fully identified patients"
///              IsChecked="{Binding Identification,
///                          Converter={conv:EnumToBooleanConverter},
///                          ConverterParameter=Full}" /&gt;
/// </code>
/// <para>
/// <see cref="ConvertBack"/> returns <see cref="Binding.DoNothing"/> when the radio is being
/// <em>un</em>checked. Without that, unchecking the outgoing button would write its own value back
/// over the incoming one and the group would never change - a WPF-specific trap with no Delphi
/// equivalent, because the VCL group is driven by <c>OnClick</c> rather than by two-way binding.
/// </para>
/// <para>
/// The Delphi's fourth state - no radio checked at all, which raised
/// <c>EAbort('Unhandled identification strategy.')</c> at export time - cannot occur: the enum
/// always holds one of its members, which is precisely why
/// <see cref="QuickStat.Domain.Anonymisation.IIdentificationPolicy"/> exists.
/// </para>
/// </remarks>
public sealed class EnumToBooleanConverter : MarkupExtension, IValueConverter
{
    /// <summary>A shared instance for code that needs one without XAML.</summary>
    public static readonly EnumToBooleanConverter Default = new();

    /// <inheritdoc />
    public object Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        if (value is null || parameter is null)
        {
            return false;
        }

        object? expected = Coerce(parameter, value.GetType());

        return expected is not null && value.Equals(expected);
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        if (value is not bool isChecked || !isChecked || parameter is null)
        {
            // Only the button being *checked* decides the value; see the class remarks.
            return Binding.DoNothing;
        }

        return Coerce(parameter, targetType) ?? Binding.DoNothing;
    }

    /// <inheritdoc />
    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    /// <summary>Turns the converter parameter into a member of the bound enumeration.</summary>
    /// <param name="parameter">Either the enum member itself or its name as written in XAML.</param>
    /// <param name="enumType">The enumeration the binding uses, possibly nullable.</param>
    /// <returns>The member, or <see langword="null"/> when it cannot be resolved.</returns>
    private static object? Coerce(object parameter, Type? enumType)
    {
        Type? target = enumType is null ? null : Nullable.GetUnderlyingType(enumType) ?? enumType;

        if (target is not null && target.IsInstanceOfType(parameter))
        {
            return parameter;
        }

        if (target is null || !target.IsEnum || parameter is not string name)
        {
            return null;
        }

        // Case-sensitive: a typo in a ConverterParameter should show up as a dead radio button, not
        // as a silently different mode.
        return Enum.TryParse(target, name, ignoreCase: false, out object? parsed) ? parsed : null;
    }
}
