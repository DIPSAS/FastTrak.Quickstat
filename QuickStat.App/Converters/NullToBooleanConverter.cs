using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace QuickStat.Converters;

/// <summary>
/// <see langword="null"/> to <see cref="bool"/>: <see langword="true"/> when the value is present.
/// </summary>
/// <remarks>
/// <para>
/// <c>05-ui-spec.md</c> §H.1. Used to gate UI on "something is selected" - the packages toolbar
/// button, the SQL preview pane - where the view-model has no separate flag. An empty string counts
/// as absent, because every place this is used treats a blank selection the same way as none.
/// </para>
/// <para>
/// Prefer a real view-model property where a command is involved:
/// <c>CanExecute</c> is not a binding, and duplicating the rule in XAML is how the two get to
/// disagree.
/// </para>
/// </remarks>
public sealed class NullToBooleanConverter : MarkupExtension, IValueConverter
{
    /// <summary>A shared instance for code that needs one without XAML.</summary>
    public static readonly NullToBooleanConverter Default = new();

    /// <summary>Return <see langword="true"/> when the value <em>is</em> null instead.</summary>
    public bool Invert { get; set; }

    /// <inheritdoc />
    public object Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        bool hasValue = value is not null && value is not string { Length: 0 };

        return Invert ? !hasValue : hasValue;
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always: the original value cannot be recovered.</exception>
    public object ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture) =>
        throw new NotSupportedException("NullToBooleanConverter is one-way.");

    /// <inheritdoc />
    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
