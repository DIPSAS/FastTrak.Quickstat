using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using QuickStat.ViewModels;
using QuickStat.Views.Dialogs;
using Xunit;
using Calendar = System.Windows.Controls.Calendar;

namespace QuickStat.Tests.Ui.Dialogs;

/// <summary>
/// The <c>Angi periode</c> window: it loads without an <see cref="Application"/>, it carries the
/// §D.5 metrics, and its two calendars are configured the same way whatever the machine's culture.
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

        // Self-contained; see SaveSpecDialogTests for why this is asserted through the window's own
        // dictionary rather than as "Application.Current is null".
        Assert.Single(dialog.Resources.MergedDictionaries);
        Assert.NotNull(dialog.Resources["QsFormFaceBrush"]);

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
        // the frame is added around this rather than subtracted from it.
        PeriodDialog dialog = new();
        FrameworkElement root = (FrameworkElement)dialog.Content;

        Assert.Equal(527d, root.Width);
        Assert.Equal(374d, root.Height);
    });

    [Theory]
    [InlineData("nb-NO")]
    [InlineData("nn-NO")]
    [InlineData("en-US")]
    public void BothCalendarsStartInNineteenHundredAndOnAMondayInEveryCulture(string culture) =>
        StaTestRunner.Run(() =>
        {
            // Inside the body, not around it: StaTestRunner starts a thread, and since .NET Core a
            // new thread takes the operating system's culture rather than the creator's.
            using CultureScope scope = new(culture);

            RealisedWindow.Run(new PeriodDialog { DataContext = new PeriodViewModel() }, dialog =>
            {
                foreach (Calendar calendar in new[] { dialog.StartCalendar, dialog.StopCalendar })
                {
                    // FirstDayOfWeek defaults to the current culture's, so nb-NO and en-US disagree
                    // unless it is pinned; the .dfm says dwMonday for both
                    // (Emetra.VclForm.Period.dfm:249, 283).
                    Assert.Equal(DayOfWeek.Monday, calendar.FirstDayOfWeek);
                    Assert.Equal(PeriodViewModel.FirstDate, calendar.DisplayDateStart);
                    Assert.False(calendar.IsTodayHighlighted);
                    Assert.Equal(CalendarSelectionMode.SingleDate, calendar.SelectionMode);
                    Assert.Equal(241d, calendar.Width);
                }
            });
        });

    [Fact]
    public void TheCalendarsShowTheViewModelsDates() => StaTestRunner.Run(() =>
    {
        PeriodViewModel model = new()
        {
            Start = new DateTime(2019, 3, 4, 0, 0, 0, DateTimeKind.Unspecified),
            Stop = new DateTime(2019, 3, 18, 0, 0, 0, DateTimeKind.Unspecified),
        };

        RealisedWindow.Run(new PeriodDialog { DataContext = model }, dialog =>
        {
            Assert.Equal(model.Start, dialog.StartCalendar.SelectedDate);
            Assert.Equal(model.Stop, dialog.StopCalendar.SelectedDate);

            // Picking a date writes back; clearing the selection - Calendar allows it, TCalendarView
            // does not - leaves the period alone rather than producing one the domain cannot hold.
            dialog.StopCalendar.SelectedDate = new DateTime(2019, 4, 1, 0, 0, 0, DateTimeKind.Unspecified);

            Assert.Equal(new DateTime(2019, 4, 1, 0, 0, 0, DateTimeKind.Unspecified), model.Stop);

            dialog.StopCalendar.SelectedDate = null;

            Assert.Equal(new DateTime(2019, 4, 1, 0, 0, 0, DateTimeKind.Unspecified), model.Stop);
        });
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

        RealisedWindow.Run(new PeriodDialog { DataContext = model }, dialog =>
        {
            Assert.False(dialog.OkButton.IsEnabled);
            Assert.Equal(PeriodViewModel.InvalidText, dialog.BottomInfo.Text);

            model.Stop = model.Start.AddDays(1);

            Assert.True(dialog.OkButton.IsEnabled);
            Assert.Equal(PeriodViewModel.ValidText, dialog.BottomInfo.Text);
        });
    });

    [Fact]
    public void TheBannerCarriesTheRunTimeSubHeader() => StaTestRunner.Run(() =>
    {
        PeriodViewModel model = new() { SubHeaderText = "Noe helt annet." };

        RealisedWindow.Run(new PeriodDialog { DataContext = model }, dialog =>
        {
            // Emetra.VclForm.Period.dfm:36 - panWhiteTop is 45 tall, not the save modal's 41.
            Assert.Equal(45d, dialog.Banner.Height);

            StackPanel lines = (StackPanel)dialog.Banner.Child;

            Assert.Equal(PeriodViewModel.DialogHeader, ((TextBlock)lines.Children[0]).Text);
            Assert.Equal("Noe helt annet.", ((TextBlock)lines.Children[1]).Text);
        });
    });

    [Fact]
    public void PressingOkAcceptsAndPressingEscapeDoesNot() => StaTestRunner.Run(() =>
    {
        PeriodViewModel model = new()
        {
            Start = new DateTime(2019, 3, 4, 0, 0, 0, DateTimeKind.Unspecified),
            Stop = new DateTime(2019, 3, 18, 0, 0, 0, DateTimeKind.Unspecified),
        };

        bool? accepted = RealisedWindow.ShowModal(
            new PeriodDialog { DataContext = model },
            dialog => dialog.OkButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)));

        Assert.True(accepted);

        // Closing without pressing OK is a cancel however it happens; WpfPeriodPrompt turns
        // anything that is not exactly true into null.
        bool? closed = RealisedWindow.ShowModal(new PeriodDialog { DataContext = model }, dialog => dialog.Close());

        Assert.NotEqual(true, closed);
    });

    /// <summary>Forces a culture for the duration of a test.</summary>
    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

        internal CultureScope(string name) => CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);

        public void Dispose() => CultureInfo.CurrentCulture = _previous;
    }
}
