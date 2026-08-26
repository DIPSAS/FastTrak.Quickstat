using CommunityToolkit.Mvvm.ComponentModel;
using QuickStat.Domain.Populations;

namespace QuickStat.ViewModels;

/// <summary>The <c>Angi periode</c> modal: two calendars and a half-open date range.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.6.</b> The window it drives is <c>Views/Dialogs/PeriodDialog.xaml</c>, and the
/// service that shows it is <see cref="QuickStat.Services.WpfPeriodPrompt"/>.
/// </para>
/// <para>
/// <c>05-ui-spec.md</c> §D.5 and <c>Emetra.VclForm.Period.pas</c>. The view-model holds nothing but
/// the two dates and the strings derived from them: the settings round trip belongs to the prompt,
/// which owns the read-show-write sequence, so this type has no dependencies and needs no container.
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

    /// <summary>
    /// Bottom line while the range is valid. States the half-open rule to the user.
    /// </summary>
    /// <remarks>
    /// Two lines, as <c>rsValidInput</c> is (<c>Emetra.VclForm.Period.pas:37-39</c>, where the break
    /// is a real <c>#10</c> and not the literal <c>\n</c> that <c>MainQuickStat.pas</c>'s constants
    /// carry). <c>05-ui-spec.md</c> §D.5 quotes it as one line; the Delphi wins. The label wraps
    /// either way, so this only decides where the break falls.
    /// </remarks>
    public const string ValidText =
        "Angis som fra og med første dato (til venstre),\nog til men ikke inkludert siste dato (til høyre).";

    /// <summary>Bottom line while the range is invalid.</summary>
    /// <remarks><c>rsInvalidInput</c>, <c>Emetra.VclForm.Period.pas:40-42</c>.</remarks>
    public const string InvalidText =
        "Siste dato må være etter første dato.\nMerk at siste dato ikke er med i perioden.";

    /// <summary>Accept button.</summary>
    public const string AcceptCaption = "OK";

    /// <summary>Cancel button.</summary>
    public const string CancelCaption = "Avbryt";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAccept))]
    [NotifyPropertyChangedFor(nameof(Period))]
    [NotifyPropertyChangedFor(nameof(BottomInfoText))]
    [NotifyPropertyChangedFor(nameof(SelectedStart))]
    private DateTime _start = DateTime.Today.AddDays(-1);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAccept))]
    [NotifyPropertyChangedFor(nameof(Period))]
    [NotifyPropertyChangedFor(nameof(BottomInfoText))]
    [NotifyPropertyChangedFor(nameof(SelectedStop))]
    private DateTime _stop = DateTime.Today;

    [ObservableProperty]
    private string _subHeaderText = SubHeader;

    /// <summary>
    /// Earliest date either calendar will show. Delphi <c>FirstYear = 1900</c>
    /// (<c>Emetra.VclForm.Period.dfm:250, 284</c>).
    /// </summary>
    public static DateTime FirstDate => new(1900, 1, 1);

    /// <summary>The range, as the domain expresses it.</summary>
    public HalfOpenPeriod Period => new(Start, Stop);

    /// <summary>Whether <c>OK</c> is enabled. Strictly <c>Start &lt; Stop</c>; equal dates are rejected.</summary>
    /// <remarks>
    /// <c>VerifyInput</c> (<c>Emetra.VclForm.Period.pas:72-79</c>), and the same comparison again in
    /// <c>TryGetPeriod</c> (<c>:52</c>) so that an untouched dialog cannot return an invalid range.
    /// </remarks>
    public bool CanAccept => Period.IsValid;

    /// <summary>The line at the bottom left, which says either the rule or why <c>OK</c> is dead.</summary>
    public string BottomInfoText => CanAccept ? ValidText : InvalidText;

    /// <summary>
    /// <see cref="Start"/> as the calendar binds it.
    /// </summary>
    /// <remarks>
    /// <c>Calendar.SelectedDate</c> is nullable and WPF clears it on a control-click, but
    /// <c>TCalendarView.Date</c> has no empty state and neither has
    /// <see cref="HalfOpenPeriod"/>. A null is therefore ignored rather than propagated: the dialog
    /// briefly shows no highlight and the period is unchanged, which is better than a binding error
    /// and a range the view-model cannot represent.
    /// </remarks>
    public DateTime? SelectedStart
    {
        get => Start;
        set
        {
            if (value is { } date)
            {
                Start = date;
            }
        }
    }

    /// <summary><see cref="Stop"/> as the calendar binds it. See <see cref="SelectedStart"/>.</summary>
    public DateTime? SelectedStop
    {
        get => Stop;
        set
        {
            if (value is { } date)
            {
                Stop = date;
            }
        }
    }
}
