using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using QuickStat.Diagnostics;
using QuickStat.ViewModels;
using QuickStat.Views.Dialogs;
using Xunit;

namespace QuickStat.Tests.Ui.Dialogs;

/// <summary>
/// The themed replacement for <c>MessageBox</c>: the icon and button set for each of
/// <c>05-ui-spec.md</c> §D.4's messages, and the rule that only <c>Yes</c> is a yes.
/// </summary>
public class NotificationDialogTests
{
    /// <summary>The typeface <c>QsIconGlyph</c> supplies. Same one <c>QuickStat.Theme.SegoeIcons</c> needs.</summary>
    private const string IconFontFamily = "Segoe MDL2 Assets";

    /// <summary>
    /// Severity, question, and the glyph the Delphi's dialog type maps to.
    /// </summary>
    /// <remarks>
    /// <c>MapDlggType</c> (<c>Emetra.Logging.Base.pas:218-226</c>) plus the
    /// information-to-confirmation promotion at <c>:276-277</c>.
    /// </remarks>
    public static TheoryData<NotificationSeverity, bool, string> Glyphs =>
        new()
        {
            { NotificationSeverity.Information, false, NotificationViewModel.InformationGlyph },
            { NotificationSeverity.Warning, false, NotificationViewModel.WarningGlyph },
            { NotificationSeverity.Error, false, NotificationViewModel.ErrorGlyph },
            { NotificationSeverity.Information, true, NotificationViewModel.QuestionGlyph },
            { NotificationSeverity.Warning, true, NotificationViewModel.WarningGlyph },
            { NotificationSeverity.Error, true, NotificationViewModel.ErrorGlyph },
        };

    /// <summary>
    /// Every message <c>05-ui-spec.md</c> §D.4 enumerates, with the severity its call site raises it
    /// at and whether it is a question.
    /// </summary>
    /// <remarks>
    /// Transcribed from the table in its own order, with the <c>MainQuickStat.pas</c> line for each.
    /// The last two rows are not in the notifier's path at all and are recorded as such: the missing
    /// configuration file is an exception before the container exists, and the Norwegian
    /// <c>Det er ikke valgt en gyldig populasjon.</c> belongs to the population frame, i.e. step 3.2.
    /// </remarks>
    public static TheoryData<string, NotificationSeverity, bool> DelphiMessages =>
        new()
        {
            { "Package references an unknown population (:790)", NotificationSeverity.Warning, false },
            { "Package references an unknown collector (:803)", NotificationSeverity.Warning, false },
            { "Selection saved (:740)", NotificationSeverity.Information, false },
            { "Save failed (:743)", NotificationSeverity.Warning, false },
            { "Delete with nothing selected (:890)", NotificationSeverity.Warning, false },
            { "Delete confirmation (:894)", NotificationSeverity.Warning, true },
            { "Population not selected as expected (:545)", NotificationSeverity.Error, false },
            { "No population (:561)", NotificationSeverity.Information, false },
        };

    [Theory]
    [MemberData(nameof(DelphiMessages))]
    public void EveryMessageInTheChecklistGetsTheRightDialog(
        string label,
        NotificationSeverity severity,
        bool isQuestion)
    {
        NotificationViewModel model = new(new UserNotification(label, null, severity, isQuestion));

        // One button for a statement, two for a question, and the dismissing one is No or OK.
        Assert.Equal(isQuestion, model.IsQuestion);
        Assert.Equal(
            isQuestion ? NotificationViewModel.NoCaption : NotificationViewModel.DismissCaption,
            model.DismissText);

        Assert.Equal(
            severity switch
            {
                NotificationSeverity.Error => NotificationViewModel.ErrorGlyph,
                NotificationSeverity.Warning => NotificationViewModel.WarningGlyph,
                _ => isQuestion ? NotificationViewModel.QuestionGlyph : NotificationViewModel.InformationGlyph,
            },
            model.Glyph);
    }

    [Theory]
    [MemberData(nameof(Glyphs))]
    public void TheGlyphFollowsTheDelphisDialogType(
        NotificationSeverity severity,
        bool isQuestion,
        string expected)
    {
        NotificationViewModel model = new(new UserNotification("text", null, severity, isQuestion));

        Assert.Equal(expected, model.Glyph);
    }

    [Fact]
    public void EveryGlyphExistsInTheIconFont()
    {
        // The same check step 3.1 applied to SegoeIcons: a code point the font does not carry
        // renders as a box, and nothing else would say so.  Resolved through WPF rather than from a
        // path under C:\Windows, and the family name is checked as well - an unknown family falls
        // back to the default one, which would carry the wrong glyphs and pass.
        Typeface face = new(
            new FontFamily(IconFontFamily),
            FontStyles.Normal,
            FontWeights.Normal,
            FontStretches.Normal);

        Assert.True(face.TryGetGlyphTypeface(out GlyphTypeface? typeface));
        Assert.Contains(IconFontFamily, typeface!.FamilyNames.Values);

        foreach (string glyph in new[]
        {
            NotificationViewModel.InformationGlyph,
            NotificationViewModel.WarningGlyph,
            NotificationViewModel.ErrorGlyph,
            NotificationViewModel.QuestionGlyph,
        })
        {
            Assert.True(
                typeface.CharacterToGlyphMap.ContainsKey(glyph[0]),
                $"Segoe MDL2 Assets has no glyph for U+{(int)glyph[0]:X4}.");
        }
    }

    [Fact]
    public void AStatementGetsOneButtonAndAQuestionGetsTwo() => StaTestRunner.Run(() =>
    {
        RealisedWindow.Run(Dialog(NotificationSeverity.Warning, isQuestion: false), dialog =>
        {
            // Emetra.Logging.Base.pas:124-125 - the default button set is [mbOK].
            Assert.Equal(Visibility.Collapsed, dialog.YesButton.Visibility);
            Assert.Equal(NotificationViewModel.DismissCaption, dialog.DismissButton.Content);
        });

        RealisedWindow.Run(Dialog(NotificationSeverity.Warning, isQuestion: true), dialog =>
        {
            // PrepareButtonsYesNo, :134-140.  No mbCancel: QuickStat's only LogYesNo call passes
            // ACancel = false (MainQuickStat.pas:894).
            Assert.Equal(Visibility.Visible, dialog.YesButton.Visibility);
            Assert.Equal(NotificationViewModel.YesCaption, dialog.YesButton.Content);
            Assert.Equal(NotificationViewModel.NoCaption, dialog.DismissButton.Content);
        });
    });

    [Fact]
    public void TheAffirmativeIsOnTheLeftAndIsNotTheDefault() => StaTestRunner.Run(() =>
        RealisedWindow.Run(Dialog(NotificationSeverity.Warning, isQuestion: true), dialog =>
        {
            // MessageDlg lays [mbYes, mbNo] out left to right, which is the Windows convention and
            // deliberately the opposite of the two ported .dfm forms.
            Point yes = dialog.YesButton.TranslatePoint(default, dialog);
            Point no = dialog.DismissButton.TranslatePoint(default, dialog);

            Assert.True(yes.X < no.X, $"Yes at {yes.X} should be left of No at {no.X}.");

            // A deliberate change: PrepareButtonsYesNo sets mbYes as the default
            // (Emetra.Logging.Base.pas:135), and this is a destructive confirmation.
            Assert.False(dialog.YesButton.IsDefault);
            Assert.True(dialog.DismissButton.IsDefault);
            Assert.True(dialog.DismissButton.IsCancel);
        }));

    [Fact]
    public void OnlyPressingYesIsAYes() => StaTestRunner.Run(() =>
    {
        Assert.True(RealisedWindow.ShowModal(
            Dialog(NotificationSeverity.Warning, isQuestion: true),
            dialog => dialog.YesButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent))));

        Assert.False(RealisedWindow.ShowModal(
            Dialog(NotificationSeverity.Warning, isQuestion: true),
            dialog => dialog.DismissButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent))));

        // Closing the window without answering is not a yes either.  WpfNotificationPresenter maps
        // anything that is not exactly true to false.
        Assert.NotEqual(
            true,
            RealisedWindow.ShowModal(Dialog(NotificationSeverity.Warning, isQuestion: true), dialog => dialog.Close()));
    });

    [Fact]
    public void TheMessageIsRenderedVerbatimIncludingItsLineBreaks() => StaTestRunner.Run(() =>
    {
        // Decision (d): PiiRedactor.ForDisplay has already turned the literal \n in
        // MainQuickStat.pas's resource strings into real breaks.  The dialog must not convert again,
        // and must not collapse them either.
        const string Message = "The selection is based on an unknown population (ProcId=257).\n"
            + "The data collection can not be performed at this time.\n"
            + "Perhaps the population is from a different protocol?";

        RealisedWindow.Run(
            new NotificationDialog(new UserNotification(Message, null, NotificationSeverity.Warning, false)),
            dialog =>
            {
                Assert.Equal(Message, dialog.MessageText.Text);
                Assert.Equal(TextWrapping.Wrap, dialog.MessageText.TextWrapping);
            });
    });

    [Fact]
    public void TheCaptionIsTheNotificationsOrTheApplicationName() => StaTestRunner.Run(() =>
    {
        RealisedWindow.Run(
            new NotificationDialog(new UserNotification("x", null, NotificationSeverity.Information, false)),
            dialog => Assert.Equal(NotificationViewModel.DefaultTitle, dialog.Title));

        RealisedWindow.Run(
            new NotificationDialog(new UserNotification("x", "Delete package", NotificationSeverity.Warning, true)),
            dialog => Assert.Equal("Delete package", dialog.Title));
    });

    [Fact]
    public void TheGlyphIsColouredBySeverity() => StaTestRunner.Run(() =>
    {
        Color Of(NotificationSeverity severity)
        {
            Color colour = default;

            RealisedWindow.Run(
                Dialog(severity, isQuestion: false),
                dialog => colour = ((SolidColorBrush)dialog.GlyphText.Foreground).Color);

            return colour;
        }

        // Neither literal is in 05-ui-spec.md §F.4, which step 3.6 may not edit; #C42B1C is the one
        // step 3.1 already uses for the banner's error line, so only the amber is new.
        Assert.Equal((Color)ColorConverter.ConvertFromString("#178891")!, Of(NotificationSeverity.Information));
        Assert.Equal((Color)ColorConverter.ConvertFromString("#9D5D00")!, Of(NotificationSeverity.Warning));
        Assert.Equal((Color)ColorConverter.ConvertFromString("#C42B1C")!, Of(NotificationSeverity.Error));
    });

    [Fact]
    public void TheWindowIsAModalWithNoChrome() => StaTestRunner.Run(() =>
    {
        NotificationDialog dialog = Dialog(NotificationSeverity.Information, isQuestion: false);

        // Self-contained; see SaveSpecDialogTests for why this is asserted through the window's own
        // dictionary rather than as "Application.Current is null".
        Assert.Single(dialog.Resources.MergedDictionaries);
        Assert.NotNull(dialog.Resources["QsFormFaceBrush"]);

        Assert.Equal(ResizeMode.NoResize, dialog.ResizeMode);
        Assert.Equal(SizeToContent.WidthAndHeight, dialog.SizeToContent);
        Assert.Equal(WindowStartupLocation.CenterOwner, dialog.WindowStartupLocation);
        Assert.False(dialog.ShowInTaskbar);
    });

    private static NotificationDialog Dialog(NotificationSeverity severity, bool isQuestion) =>
        new(new UserNotification(
            "Do you really want to delete this package:\n\"Diabetes basissett 2024\"?",
            null,
            severity,
            isQuestion));
}
