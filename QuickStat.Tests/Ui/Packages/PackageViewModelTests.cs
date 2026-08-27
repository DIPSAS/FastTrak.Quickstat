using System.Globalization;
using QuickStat.Domain.Packages;
using QuickStat.ViewModels;
using Xunit;

namespace QuickStat.Tests.Ui.Packages;

/// <summary>
/// One row of the packaged-datasets list, <c>05-ui-spec.md</c> §B.3.
/// </summary>
/// <remarks>
/// The interesting part is <see cref="PackageViewModel.SearchText"/>: it is the string the filter
/// matches against, and it reproduces <c>TPackagedSelection.AsListBox</c>
/// (<c>QuickStat.Selection.pas:147-153</c>) field for field and separator for separator. Change the
/// order or the separator and users stop finding things, silently.
/// </remarks>
public class PackageViewModelTests
{
    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

        internal CultureScope(string name) => CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);

        public void Dispose() => CultureInfo.CurrentCulture = _previous;
    }

    private static PackagedSelection Selection(int rowId = 41, int populationId = 257) => new()
    {
        RowId = rowId,
        StudyId = 124,
        PopulationId = populationId,
        Title = "Diabetes basissett 2024",
        Comment = "Med legemidler",
        CollectorNames = ["QS_BMI", "QS_HBA1C"],
    };

    [Fact]
    public void ItExposesTheStoredSpecificationUnchanged()
    {
        PackagedSelection selection = Selection();
        PackageViewModel row = new(selection);

        Assert.Same(selection, row.Selection);
        Assert.Equal(41, row.RowId);
        Assert.Equal("Diabetes basissett 2024", row.Title);
        Assert.Equal("Med legemidler", row.Comment);
    }

    [Fact]
    public void ANullSelectionIsRefused()
    {
        Assert.Throws<ArgumentNullException>(() => new PackageViewModel(null!));
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("nb-NO")]
    public void ThePopulationLabelIsInvariantAndUngrouped(string culture)
    {
        // Delphi Format('Pop#%d', [fPopulationId]) - no digit grouping, whatever the machine's
        // locale.  A Norwegian machine must not render "Pop#12 345".
        using CultureScope scope = new(culture);

        Assert.Equal("Pop#12345", new PackageViewModel(Selection(populationId: 12345)).PopulationLabel);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("nb-NO")]
    public void TheSearchTextIsTheDelphisTabJoinedRow(string culture)
    {
        using CultureScope scope = new(culture);

        Assert.Equal(
            "41\tDiabetes basissett 2024\tMed legemidler\tPop#257",
            new PackageViewModel(Selection()).SearchText);
    }

    [Fact]
    public void AnEmptyCommentStillLeavesItsSeparator()
    {
        // AsListBox always writes the tab, whether or not there is a comment; a filter typed as
        // "2024\t" would still be a substring miss, but the field count is observable and this keeps
        // it stable.
        PackageViewModel row = new(Selection() with { Comment = "" });

        Assert.Equal("41\tDiabetes basissett 2024\t\tPop#257", row.SearchText);
    }
}
