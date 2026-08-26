using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Xunit;

namespace QuickStat.Tests.Ui.Theme;

/// <summary>
/// Every brush and style key in <c>Docs/Port/05-ui-spec.md</c> §F.4 exists, is the right type, and -
/// for the brushes - is the right colour.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the only thing standing between a typo and a blank window three steps later.</b> A
/// missing resource key is not a build error: <c>{StaticResource QsTelBrush}</c> compiles, and then
/// throws at run time in whichever wave-2 view happens to use it. Four steps bind to these names
/// after step 3.1 finishes, so the inventory is asserted rather than trusted.
/// </para>
/// <para>
/// The dictionary is loaded by pack URI on an STA thread. <c>Application.Current</c> is
/// <see langword="null"/> under test (PORT-PLAN.md §5 Phase 3), so there is nothing to read
/// <c>Application.Resources</c> from; loading the file directly also means the test fails when the
/// <em>file</em> is wrong rather than when the application happens to merge it.
/// </para>
/// <para>
/// Only <c>QuickStat.Styles.xaml</c> is loaded, because it merges the brushes itself - which is what
/// makes it parseable on its own, and is exactly the composition <c>App.xaml</c> uses.
/// </para>
/// </remarks>
public class ThemeResourceTests
{
    private const string StylesUri = "/QuickStat;component/Theme/QuickStat.Styles.xaml";

    /// <summary>Key and expected <c>#RRGGBB</c>, transcribed from §F.4 in its own order.</summary>
    public static TheoryData<string, string> Brushes =>
        new()
        {
            // Brand
            { "QsTealBrush", "#178891" },
            { "QsTealDarkBrush", "#035F66" },
            { "QsTealHoverBrush", "#1A9BA6" },
            { "QsTealUnfocusedBrush", "#50AEB6" },

            // Surfaces
            { "QsBannerBrush", "#FFFFFF" },
            { "QsPageBrush", "#F4FBFB" },
            { "QsSurfaceBrush", "#FFFFFF" },
            { "QsAltRowBrush", "#F7F7F7" },
            { "QsFormFaceBrush", "#EEEEEE" },

            // Lines
            { "QsBorderBrush", "#D0D6D6" },
            { "QsBorderStrongBrush", "#9AA5A5" },
            { "QsGridLineBrush", "#E2E6E6" },
            { "QsDividerBrush", "#EDF1F1" },

            // Text
            { "QsTextBrush", "#202020" },
            { "QsTitleBrush", "#333333" },
            { "QsMutedTextBrush", "#5E6A6A" },
            { "QsOnAccentBrush", "#FFFFFF" },
            { "QsCodeBrush", "#4B29A4" },
            { "QsCategoryBrush", "#B82E82" },
            { "QsAccentBrush", "#0078D7" },

            // Grid semantics
            { "QsCellEmptyBrush", "#F5F5F5" },
            { "QsCellNoDataBrush", "#FFFAFA" },
            { "QsCurrentCellBrush", "#FFFBD4" },
            { "QsCurrentRowBrush", "#F3F9FE" },
            { "QsHintBackgroundBrush", "#FFFFE1" },
            { "QsHintBorderBrush", "#D8D2A8" },

            // Progress
            { "QsProgressBrush", "#06B025" },
            { "QsProgressTrackBrush", "#E3E9E9" },
        };

    /// <summary>Every named style in §F.4's second table.</summary>
    public static TheoryData<string> StyleKeys =>
        [
            "QsSectionHeader",
            "QsHeaderText",
            "QsTabControl",
            "QsTabItem",
            "QsFlatTextBox",
            "QsFlatComboBox",
            "QsPrimaryButton",
            "QsToolButton",
            "QsPopulationItem",
            "QsPackageItem",
            "QsCheckListItem",
            "QsDataGrid",
            "QsDataGridColumnHeader",
            "QsDataGridCell",
            "QsDataGridRow",
            "QsProgressBar",
            "QsSplitter",
            "QsHintPanel",
        ];

    [Theory]
    [MemberData(nameof(Brushes))]
    public void BrushKeyResolvesToTheSpecifiedColour(string key, string expectedHex)
    {
        (bool found, string? actualHex, bool isFrozen) = StaTestRunner.Run(() =>
        {
            ResourceDictionary theme = LoadTheme();

            return theme[key] is SolidColorBrush brush
                ? (true, ToHex(brush.Color), brush.IsFrozen)
                : (false, (string?)null, false);
        });

        Assert.True(found, $"§F.4 declares '{key}' as a SolidColorBrush; the theme does not.");
        Assert.Equal(expectedHex, actualHex);

        // Shared across the whole application: a consumer that mutated one would recolour it for
        // everybody, so freezing turns that into an exception rather than a rendering mystery.
        Assert.True(isFrozen, $"'{key}' is not frozen.");
    }

    [Theory]
    [MemberData(nameof(StyleKeys))]
    public void StyleKeyResolvesToAStyle(string key)
    {
        bool found = StaTestRunner.Run(() => LoadTheme()[key] is Style);

        Assert.True(found, $"§F.4 declares '{key}' as a named style; the theme does not.");
    }

    [Fact]
    public void EveryBrushInTheThemeIsListedInTheSpecification()
    {
        // The inventory has to hold in both directions.  A brush that exists only in the theme is a
        // brush the next reader cannot find in 05-ui-spec.md §F.4, which is where they will look.
        List<string> keys = StaTestRunner.Run(() =>
        {
            ResourceDictionary theme = LoadTheme();
            List<string> found = [];

            Collect(theme, found);

            return found;
        });

        HashSet<string> specified = [.. Brushes.Select(row => (string)row[0])];
        List<string> undocumented = [.. keys.Where(key => !specified.Contains(key))];

        Assert.Equal([], undocumented);
    }

    [Fact]
    public void SectionHeaderHasAnImplicitStyleSoItNeedsNoStyleAttribute()
    {
        // Written as a keyed style AND as an implicit one based on it, so <q:SectionHeader/> is
        // dressed without every caller remembering Style="{StaticResource QsSectionHeader}".
        bool found = StaTestRunner.Run(() =>
            LoadTheme()[typeof(QuickStat.Controls.SectionHeader)] is Style);

        Assert.True(found);
    }

    [Fact]
    public void TypographyResourcesResolve()
    {
        (bool fonts, bool sizes) = StaTestRunner.Run(() =>
        {
            ResourceDictionary theme = LoadTheme();

            return (
                theme["QsFontFamily"] is FontFamily
                    && theme["QsCodeFontFamily"] is FontFamily
                    && theme["QsIconFontFamily"] is FontFamily,
                theme["QsFontSize"] is double
                    && theme["QsHeaderFontSize"] is double
                    && theme["QsSmallFontSize"] is double
                    && theme["QsWordmarkFontSize"] is double);
        });

        Assert.True(fonts);
        Assert.True(sizes);
    }

    private static void Collect(ResourceDictionary dictionary, List<string> keys)
    {
        foreach (ResourceDictionary merged in dictionary.MergedDictionaries)
        {
            Collect(merged, keys);
        }

        foreach (object key in dictionary.Keys)
        {
            if (key is string name && dictionary[key] is SolidColorBrush)
            {
                keys.Add(name);
            }
        }
    }

    private static ResourceDictionary LoadTheme() =>
        (ResourceDictionary)Application.LoadComponent(new Uri(StylesUri, UriKind.Relative));

    private static string ToHex(Color color) => string.Create(
        CultureInfo.InvariantCulture,
        $"#{color.R:X2}{color.G:X2}{color.B:X2}");
}
