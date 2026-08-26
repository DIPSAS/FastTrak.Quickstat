using CommunityToolkit.Mvvm.ComponentModel;
using QuickStat.Domain.Populations;

namespace QuickStat.ViewModels;

/// <summary>The <c>Angi periode</c> modal: two calendars and a half-open date range.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.6. This is a compiling stub.</b> The window it drives is
/// <c>Views/Dialogs/PeriodDialog.xaml</c>, and the service that shows it is
/// <see cref="QuickStat.Services.WpfPeriodPrompt"/> - which currently always cancels.
/// </para>
/// <para>
/// <b>What is left to do</b> (<c>05-ui-spec.md</c> §D.5): the layout, the two Monday-first
/// <c>Calendar</c> controls, and the settings round-trip keyed with
/// <see cref="PeriodSettingsKey.For"/>.
/// </para>
/// <para>
/// The strings below are the whole dialog's chrome and they are <b>Norwegian</b>, unlike the rest of
/// the application - the period dialog is a library form, not part of QuickStat's own English
/// chrome, and PORT-PLAN.md §8.6 keeps that split exactly as it is.
/// </para>
/// </remarks>
public sealed partial class PeriodViewModel : ObservableObject
{
    /// <summary>Window title and banner.</summary>
    public const string DialogHeader = "Angi periode";

    /// <summary>Sub-header, set at run time.</summary>
    public const string SubHeader = "Denne spørringen krever at du angir et tidsintervall.";

    /// <summary>Hint above the calendars.</summary>
    public const string TipText =
        "Tips: Klikk på månedens navn for å \"zoome ut\" hvis datoen du vil ha er langt unna.";

    /// <summary>Bottom line while the range is valid. States the half-open rule to the user.</summary>
    public const string ValidText =
        "Angis som fra og med første dato (til venstre), og til men ikke inkludert siste dato (til høyre).";

    /// <summary>Bottom line while the range is invalid.</summary>
    public const string InvalidText =
        "Siste dato må være etter første dato.\nMerk at siste dato ikke er med i perioden.";

    /// <summary>Accept button.</summary>
    public const string AcceptCaption = "OK";

    /// <summary>Cancel button.</summary>
    public const string CancelCaption = "Avbryt";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAccept))]
    [NotifyPropertyChangedFor(nameof(Period))]
    private DateTime _start = DateTime.Today.AddDays(-1);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAccept))]
    [NotifyPropertyChangedFor(nameof(Period))]
    private DateTime _stop = DateTime.Today;

    [ObservableProperty]
    private string _subHeaderText = SubHeader;

    /// <summary>The range, as the domain expresses it.</summary>
    public HalfOpenPeriod Period => new(Start, Stop);

    /// <summary>Whether <c>OK</c> is enabled. Strictly <c>Start &lt; Stop</c>; equal dates are rejected.</summary>
    public bool CanAccept => Period.IsValid;
}
