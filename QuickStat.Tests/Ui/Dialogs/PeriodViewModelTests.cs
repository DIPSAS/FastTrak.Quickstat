using System.ComponentModel;
using QuickStat.Domain.Populations;
using QuickStat.ViewModels;
using Xunit;

namespace QuickStat.Tests.Ui.Dialogs;

/// <summary>
/// The period dialog's state: the half-open rule, the two lines that explain it, and the strings
/// that have to stay Norwegian.
/// </summary>
/// <remarks>
/// Nothing here needs an apartment. The view-model has no dependencies precisely so that the rule
/// <c>OK</c> hangs on can be checked without a window.
/// </remarks>
public class PeriodViewModelTests
{
    private static readonly DateTime March4 = new(2019, 3, 4, 0, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public void TheDefaultRangeIsYesterdayToToday()
    {
        // EPR.PeriodDictionary.pas:65-66, Now - 1 and Now.
        PeriodViewModel model = new();

        Assert.Equal(DateTime.Today.AddDays(-1), model.Start);
        Assert.Equal(DateTime.Today, model.Stop);
        Assert.True(model.CanAccept);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    public void AcceptanceIsStrictlyStartBeforeStop(int dayOffset, bool expected)
    {
        // Emetra.VclForm.Period.pas:74 uses "<", so a single day is the shortest period and two
        // equal dates are refused - which is the whole point of a half-open range.
        PeriodViewModel model = new() { Start = March4, Stop = March4.AddDays(dayOffset) };

        Assert.Equal(expected, model.CanAccept);
        Assert.Equal(expected, model.Period.IsValid);
    }

    [Fact]
    public void ThePeriodIsTheDomainsHalfOpenRange()
    {
        PeriodViewModel model = new() { Start = March4, Stop = March4.AddDays(14) };

        Assert.Equal(new HalfOpenPeriod(March4, March4.AddDays(14)), model.Period);
        Assert.True(model.Period.Contains(March4));
        Assert.False(model.Period.Contains(March4.AddDays(14)));
    }

    [Fact]
    public void TheBottomLineFollowsTheRange()
    {
        PeriodViewModel model = new() { Start = March4, Stop = March4 };

        Assert.Equal(PeriodViewModel.InvalidText, model.BottomInfoText);

        model.Stop = March4.AddDays(1);

        Assert.Equal(PeriodViewModel.ValidText, model.BottomInfoText);
    }

    [Fact]
    public void BothBottomLinesAreTwoLines()
    {
        // rsValidInput and rsInvalidInput both embed a real #10 (Emetra.VclForm.Period.pas:37-42).
        // 05-ui-spec.md §D.5 quotes the valid one as a single line; the Delphi wins.
        Assert.Equal(2, PeriodViewModel.ValidText.Split('\n').Length);
        Assert.Equal(2, PeriodViewModel.InvalidText.Split('\n').Length);

        // A real break, not the two-character escape the MainQuickStat.pas constants carry.
        Assert.DoesNotContain("\\n", PeriodViewModel.ValidText, StringComparison.Ordinal);
        Assert.DoesNotContain("\\n", PeriodViewModel.InvalidText, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryStringIsTheNorwegianOne()
    {
        // PORT-PLAN.md §8.6: the period dialog is a library form and stays Norwegian while the rest
        // of the chrome is English.  Character for character, including the Norwegian letters.
        Assert.Equal("Angi periode", PeriodViewModel.DialogHeader);
        Assert.Equal("Denne spørringen krever at du angir et tidsintervall.", PeriodViewModel.SubHeader);
        Assert.Equal(
            "Tips: Klikk på månedens navn for å \"zoome ut\" hvis datoen du vil ha er langt unna.",
            PeriodViewModel.TipText);
        Assert.Equal(
            "Angis som fra og med første dato (til venstre),\nog til men ikke inkludert siste dato (til høyre).",
            PeriodViewModel.ValidText);
        Assert.Equal(
            "Siste dato må være etter første dato.\nMerk at siste dato ikke er med i perioden.",
            PeriodViewModel.InvalidText);
        Assert.Equal("OK", PeriodViewModel.AcceptCaption);
        Assert.Equal("Avbryt", PeriodViewModel.CancelCaption);
    }

    [Fact]
    public void TheCalendarFacadeIgnoresAClearedSelection()
    {
        // Calendar.SelectedDate is nullable and a control-click clears it; TCalendarView.Date has no
        // empty state and neither has HalfOpenPeriod.
        PeriodViewModel model = new() { Start = March4, Stop = March4.AddDays(1) };

        model.SelectedStart = null;

        Assert.Equal(March4, model.Start);

        model.SelectedStart = March4.AddDays(-3);

        Assert.Equal(March4.AddDays(-3), model.Start);

        model.SelectedStop = null;

        Assert.Equal(March4.AddDays(1), model.Stop);
    }

    [Fact]
    public void ChangingADateNotifiesEverythingDerivedFromIt()
    {
        PeriodViewModel model = new() { Start = March4, Stop = March4 };
        List<string?> raised = [];

        model.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        model.Stop = March4.AddDays(1);

        Assert.Contains(nameof(PeriodViewModel.Stop), raised);
        Assert.Contains(nameof(PeriodViewModel.SelectedStop), raised);
        Assert.Contains(nameof(PeriodViewModel.CanAccept), raised);
        Assert.Contains(nameof(PeriodViewModel.Period), raised);
        Assert.Contains(nameof(PeriodViewModel.BottomInfoText), raised);
    }

    [Fact]
    public void TheCalendarsCannotBeScrolledBeforeNineteenHundred() =>
        // FirstYear = 1900 on both, Emetra.VclForm.Period.dfm:250, 284.
        Assert.Equal(new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), PeriodViewModel.FirstDate);

    [Fact]
    public void TheSubHeaderStartsAtTheOneTheResolverPasses()
    {
        // QueryParameterResolver.PeriodPromptCaption is the same string; the view-model's default is
        // what a caller that passes nothing gets.
        PeriodViewModel model = new();

        Assert.Equal(PeriodViewModel.SubHeader, model.SubHeaderText);
        Assert.IsAssignableFrom<INotifyPropertyChanged>(model);
    }
}
