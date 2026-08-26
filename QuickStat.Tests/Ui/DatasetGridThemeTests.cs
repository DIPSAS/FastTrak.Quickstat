using System.Windows;
using System.Windows.Media;
using QuickStat.Controls.Dataset;
using Xunit;

namespace QuickStat.Tests.Ui;

/// <summary>
/// The seam between the theme (step 3.1) and the grid control (step 3.5), which neither step could
/// see both sides of.
/// </summary>
/// <remarks>
/// <para>
/// The two were built in parallel and agree today, but only because two numbers written in two
/// different files happen to match. Nothing else pinned that, and it is the same shape as the
/// Phase 2 defect where <c>PersonMatrix.ColumnOrder</c> was a property nobody read: each half
/// correct, the seam unstated, and both sides' own tests green.
/// </para>
/// <para>
/// This is deliberately <b>not</b> a complaint about the design. The painter uses the flat
/// <c>CurrentRowBackground</c> for an uncoloured cell precisely so that a theme may override it
/// alone, and blends <c>CurrentRowTint</c> into a risk-coloured one so selecting a row cannot erase
/// a red haemoglobin. Both paths are supported; what is asserted here is that the shipped values
/// still land on the same colour, so the two halves of the current row cannot drift apart
/// unnoticed.
/// </para>
/// </remarks>
public class DatasetGridThemeTests
{
    private const string ThemeBrushes = "/QuickStat;component/Theme/QuickStat.Brushes.xaml";

    private static Color ThemeColor(string key) => StaTestRunner.Run(() =>
    {
        // Application.LoadComponent, not "new ResourceDictionary { Source = ... }": the latter needs
        // an Application to resolve a relative pack URI against, and there is none under test - WPF
        // allows one per AppDomain, so creating one would break every later test that wanted its own.
        ResourceDictionary brushes =
            (ResourceDictionary)Application.LoadComponent(new Uri(ThemeBrushes, UriKind.Relative));

        return ((SolidColorBrush)brushes[key]).Color;
    });

    private static Color GridDefault(DependencyProperty property) => StaTestRunner.Run(() =>
        ((SolidColorBrush)property.GetMetadata(typeof(MatrixGrid)).DefaultValue).Color);

    [Fact]
    public void TheFlatCurrentRowFillEqualsTheTintBlendedOverAnEmptyCell()
    {
        Color flat = ThemeColor("QsCurrentRowBrush");
        Color tint = GridDefault(MatrixGrid.CurrentRowTintProperty);

        // 05-ui-spec.md §F.1 derives one from the other: clUnfocusedSelectionColor #E7F2FC at 50 %
        // over white is the #F3F9FE the theme ships.  If someone retunes QsCurrentRowBrush without
        // retuning the tint, an ordinary cell in the current row and a faintly-coloured one beside
        // it stop matching - visible, and with nothing to explain it.
        Assert.Equal(
            flat,
            MatrixGridPalette.Blend(Colors.White, tint, MatrixGridCellPainter.CurrentRowBlendPercent));
    }

    [Theory]
    [InlineData("QsGridLineBrush", nameof(MatrixGrid.GridLineBrush))]
    [InlineData("QsPageBrush", nameof(MatrixGrid.FixedCellBackground))]
    [InlineData("QsTealDarkBrush", nameof(MatrixGrid.FixedCellForeground))]
    [InlineData("QsCurrentCellBrush", nameof(MatrixGrid.CurrentCellBackground))]
    [InlineData("QsCurrentRowBrush", nameof(MatrixGrid.CurrentRowBackground))]
    [InlineData("QsCellNoDataBrush", nameof(MatrixGrid.MissingObjectBackground))]
    public void EveryBrushTheDatasetTabBindsMatchesTheControlsOwnDefault(string themeKey, string propertyName)
    {
        // DatasetTabView binds all six from the theme, so the control's defaults are dead weight if
        // they disagree - and worse than dead weight, because a control used anywhere else, or in a
        // designer, would then render differently from the running application for no stated reason.
        DependencyProperty property = (DependencyProperty)typeof(MatrixGrid)
            .GetField($"{propertyName}Property")!
            .GetValue(null)!;

        Assert.Equal(ThemeColor(themeKey), GridDefault(property));
    }
}
