using System.Windows;
using System.Windows.Controls;
using QuickStat.ViewModels;
using QuickStat.Views.Dialogs;
using Xunit;

namespace QuickStat.Tests.Ui.Dialogs;

/// <summary>
/// The <c>Angi periode</c> window: it loads without an <see cref="Application"/>, it carries the
/// §D.5 metrics, and its two calendars are configured the same way regardless of the machine's
/// culture.
/// </summary>
/// <remarks>
/// Constructing the window at all is the point of the first test. Every <c>StaticResource</c> in a
/// view is resolved when the XAML is parsed, and a key that does not exist is not a build error - it
/// throws a <c>XamlParseException</c> the first time the window is shown, which for a modal dialog
/// is in front of a user. The dialogs merge the theme into their own
/// <see cref="FrameworkElement.Resources"/> precisely so that this check is possible with
/// <c>Application.Current</c> null.
/// </remarks>
public class PeriodDialogTests
{
    [Fact]
    public void TheWindowLoadsWithoutAnApplication() => StaTestRunner.Run(() =>
    {
        PeriodDialog dialog = new();

        Assert.Null(Application.Current);
        Assert.Equal(PeriodViewModel.DialogHeader, dialog.Title);
        Assert.Equal(ResizeMode.NoResize, dialog.ResizeMode);
        Assert.Equal(SizeToContent.WidthAndHeight, dialog.SizeToContent);
        Assert.Equal(WindowStartupLocation.CenterOwner, dialog.WindowStartupLocation);
        Assert.False(dialog.ShowInTaskbar);
    });

    [Fact]
    public void TheClientAreaIsTheDelphiClientArea() => StaTestRunner.Run(() =>
    {
        // Emetra.VclForm.Period.dfm:5-6, ClientWidth 527 x ClientHeight 374.  SizeToContent means
        // the window frame is added around this rather than subtracted from it.
        PeriodDialog dialog = new();
        FrameworkElement root = (FrameworkElement)dialog.Content;

        Assert.Equal(527d, root.Width);
        Assert.Equal(374d, root.Height);
    });

    [Fact]
    public void BothCalendarsStartInNineteenHundredAndOnAMonday() => StaTestRunner.Run(() =>
    {
        PeriodDialog dialog = new() { DataContext = new PeriodViewModel() };

        dialog.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        foreach (Calendar calendar in new[] { dialog.StartCalendar, dialog.StopCalendar })
        {
            // FirstDayOfWeek defaults to the current culture's, so nb-NO and en-US would disagree;
            // the .dfm says dwMonday for both (Emetra.VclForm.Period.dfm:249, 283).
            Assert.Equal(DayOfWeek.Monday, calendar.FirstDayOfWeek);
            Assert.Equal(PeriodViewModel.FirstDate, calendar.DisplayDateStart);
            Assert.False(calendar.IsTodayHighlighted);
            Assert.Equal(CalendarSelectionMode.SingleDate, calendar.SelectionMode);
            Assert.Equal(241d, calendar.Width);
        }
    });

    [Fact]
    public void TheCalendarsShowTheViewModelsDates() => StaTestRunner.Run(() =>
    {
        PeriodViewModel model = new()
        {
            Start = new DateTime(2019, 3, 4, 0, 0, 0, DateTimeKind.Unspecified),
            Stop = new DateTime(2019, 3, 18, 0, 0, 0, DateTimeKind.Unspecified),
        };

        PeriodDialog dialog = new() { DataContext = model };

        dialog.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        Assert.Equal(model.Start, dialog.StartCalendar.SelectedDate);
        Assert.Equal(model.Stop, dialog.StopCalendar.SelectedDate);
    });

    [Fact]
    public void OkIsDisabledWhileTheRangeIsInvalid() => StaTestRunner.Run(() =>
    {
        // Emetra.VclForm.Period.pas:74 - btnOk.Enabled := CalendarView1.Date < CalendarView2.Date.
        PeriodViewModel model = new()
        {
            Start = new DateTime(2019, 3, 18, 0, 0, 0, DateTimeKind.Unspecified),
            Stop = new DateTime(2019, 3, 18, 0, 0, 0, DateTimeKind.Unspecified),
        };

        PeriodDialog dialog = new() { DataContext = model };

        dialog.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        Assert.False(dialog.OkButton.IsEnabled);
        Assert.Equal(PeriodViewModel.InvalidText, dialog.BottomInfo.Text);

        model.Stop = model.Start.AddDays(1);

        Assert.True(dialog.OkButton.IsEnabled);
        Assert.Equal(PeriodViewModel.ValidText, dialog.BottomInfo.Text);
    });

    [Fact]
    public void TheBannerCarriesTheRunTimeSubHeader() => StaTestRunner.Run(() =>
    {
        PeriodViewModel model = new() { SubHeaderText = "Noe helt annet." };
        PeriodDialog dialog = new() { DataContext = model };

        dialog.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        // Emetra.VclForm.Period.dfm:36 - panWhiteTop is 45 tall, not the save modal's 41.
        Assert.Equal(45d, dialog.Banner.Height);

        StackPanel lines = (StackPanel)dialog.Banner.Child;

        Assert.Equal(PeriodViewModel.DialogHeader, ((TextBlock)lines.Children[0]).Text);
        Assert.Equal("Noe helt annet.", ((TextBlock)lines.Children[1]).Text);
    });
}
