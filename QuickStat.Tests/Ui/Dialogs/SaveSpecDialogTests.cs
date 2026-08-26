using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using QuickStat.ViewModels;
using QuickStat.Views.Dialogs;
using Xunit;

namespace QuickStat.Tests.Ui.Dialogs;

/// <summary>
/// The <c>Save specification</c> modal: §E's metrics, the accept path step 3.4 depends on, and the
/// one validation rule that is an improvement rather than a port.
/// </summary>
public class SaveSpecDialogTests
{
    [Fact]
    public void TheWindowLoadsWithoutAnApplication() => StaTestRunner.Run(() =>
    {
        SaveSpecDialog dialog = new() { DataContext = new SaveSpecViewModel() };

        Assert.Null(Application.Current);
        Assert.Equal(ResizeMode.NoResize, dialog.ResizeMode);
        Assert.Equal(SizeToContent.WidthAndHeight, dialog.SizeToContent);
        Assert.Equal(WindowStartupLocation.CenterOwner, dialog.WindowStartupLocation);
        Assert.False(dialog.ShowInTaskbar);
    });

    [Fact]
    public void TheClientAreaIsTheDelphiClientArea() => StaTestRunner.Run(() =>
    {
        // Emetra.VclForm.EditAndMemo.dfm:5-6, ClientWidth 388 x ClientHeight 288.
        SaveSpecDialog dialog = new();
        FrameworkElement root = (FrameworkElement)dialog.Content;

        Assert.Equal(388d, root.Width);
        Assert.Equal(288d, root.Height);
    });

    [Fact]
    public void TheBannerAndTheTitleBarShowTheSameHeader() => StaTestRunner.Run(() =>
    {
        // SetHeader assigns hdrSaveSpec.Caption and Self.Caption from one string
        // (Emetra.VclForm.EditAndMemo.pas:60-64).
        SaveSpecViewModel model = new();

        RealisedWindow.Run(new SaveSpecDialog { DataContext = model }, dialog =>
        {
            Assert.Equal(SaveSpecViewModel.SaveSpecificationHeader, dialog.Title);
            Assert.Equal(SaveSpecViewModel.SaveSpecificationHeader, dialog.HeaderText.Text);

            // Panel1.Height = 41, and the bar is white with a one-pixel bottom rule.
            Assert.Equal(41d, dialog.Banner.Height);
        });
    });

    [Fact]
    public void TheButtonBarPutsOkOnTheRight() => StaTestRunner.Run(() =>
    {
        // btnSave.Left = 280 against btnClose.Left = 184, so OK is the rightmost - the opposite of
        // the usual Windows order, and what §E's "OK ... Margin R 16" already implies.
        RealisedWindow.Run(new SaveSpecDialog { DataContext = new SaveSpecViewModel { Title = "x" } }, dialog =>
        {
            Point ok = dialog.OkButton.TranslatePoint(default, dialog);
            Point cancel = dialog.CancelButton.TranslatePoint(default, dialog);

            Assert.True(ok.X > cancel.X, $"OK at {ok.X} should be right of Cancel at {cancel.X}.");

            Assert.Equal(92d, dialog.OkButton.Width);
            Assert.Equal(30d, dialog.OkButton.Height);
            Assert.Equal(88d, dialog.CancelButton.Width);
            Assert.Equal(30d, dialog.CancelButton.Height);

            Assert.True(dialog.OkButton.IsDefault);
            Assert.True(dialog.CancelButton.IsCancel);
        });
    });

    [Fact]
    public void OkIsDeadUntilTheNameIsFilledIn() => StaTestRunner.Run(() =>
    {
        // An improvement, flagged in §E: the Delphi has no validation and accepts an empty title.
        SaveSpecViewModel model = new();

        RealisedWindow.Run(new SaveSpecDialog { DataContext = model }, dialog =>
        {
            Assert.False(dialog.OkButton.IsEnabled);

            dialog.TitleBox.Text = "   ";

            Assert.False(dialog.OkButton.IsEnabled);

            dialog.TitleBox.Text = "Diabetes basissett 2024";

            Assert.True(dialog.OkButton.IsEnabled);
            Assert.Equal("Diabetes basissett 2024", model.Title);
        });
    });

    [Fact]
    public void TypingReachesTheViewModelOnEveryKeystroke() => StaTestRunner.Run(() =>
    {
        SaveSpecViewModel model = new();

        RealisedWindow.Run(new SaveSpecDialog { DataContext = model }, dialog =>
        {
            dialog.TitleBox.Text = "Navn";
            dialog.CommentBox.Text = "En kommentar\nover to linjer";

            Assert.Equal("Navn", model.Title);
            Assert.Equal("En kommentar\nover to linjer", model.Comment);

            // memComment is a TMemo, so Enter inserts a line rather than pressing OK.
            Assert.True(dialog.CommentBox.AcceptsReturn);
        });
    });

    [Fact]
    public void TheNameBoxHasTheFocusWhenTheDialogOpens() => StaTestRunner.Run(() =>
        RealisedWindow.Run(new SaveSpecDialog { DataContext = new SaveSpecViewModel() }, dialog =>
            // edtTitle is TabOrder 0 in the .dfm, so the caret is there when the form appears.
            Assert.Same(dialog.TitleBox, FocusManager.GetFocusedElement(dialog))));

    [Fact]
    public void PressingOkAcceptsAndClosingDoesNot() => StaTestRunner.Run(() =>
    {
        SaveSpecViewModel model = new() { Title = "Diabetes basissett 2024", Comment = "Til årsrapporten" };

        bool? accepted = RealisedWindow.ShowModal(
            new SaveSpecDialog { DataContext = model },
            dialog => dialog.OkButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)));

        Assert.True(accepted);

        // Cancel does nothing at all - no side effects, §E - so the view-model still holds the text.
        bool? cancelled = RealisedWindow.ShowModal(
            new SaveSpecDialog { DataContext = model },
            dialog => dialog.CancelButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)));

        Assert.False(cancelled);
        Assert.Equal("Diabetes basissett 2024", model.Title);
        Assert.Equal("Til årsrapporten", model.Comment);
    });

    [Fact]
    public void TheHeaderIsTheOnlyOneTheApplicationEverUses()
    {
        // §I.3 asks whether the dialog should be cleared for "Save selection".  It does not arise:
        // actSavePatientSelection is bound to nothing and PORT-PLAN.md §7.1 removes it, so the
        // second header must not come back.
        Assert.Equal("Save specification", SaveSpecViewModel.SaveSpecificationHeader);
        Assert.Equal(SaveSpecViewModel.SaveSpecificationHeader, new SaveSpecViewModel().Header);
    }

    [Fact]
    public void ClearEmptiesBothFields()
    {
        // TfrmSaveSpec.Clear, Emetra.VclForm.EditAndMemo.pas:44-48.  A transient view-model makes it
        // unnecessary at the call site, but the method is part of the ported surface.
        SaveSpecViewModel model = new() { Title = "Navn", Comment = "Kommentar" };

        model.Clear();

        Assert.Equal("", model.Title);
        Assert.Equal("", model.Comment);
        Assert.False(model.CanSave);
    }

    [Fact]
    public void ThePackagesTabsExactFlowWorks() => StaTestRunner.Run(() =>
    {
        // Step 3.4's save path, in the order it performs it: construct, Clear, set the header, show,
        // and read the two fields back off the view-model.  Pinned here because neither half of the
        // seam could see the other while they were being built.
        SaveSpecViewModel model = new();

        model.Clear();
        model.Header = SaveSpecViewModel.SaveSpecificationHeader;

        bool? accepted = RealisedWindow.ShowModal(new SaveSpecDialog { DataContext = model }, dialog =>
        {
            Assert.Equal("", dialog.TitleBox.Text);
            Assert.Equal("", dialog.CommentBox.Text);
            Assert.False(dialog.OkButton.IsEnabled);

            dialog.TitleBox.Text = "Diabetes basissett 2024";
            dialog.CommentBox.Text = "Til årsrapporten";
            dialog.OkButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        });

        Assert.True(accepted);
        Assert.Equal("Diabetes basissett 2024", model.Title);
        Assert.Equal("Til årsrapporten", model.Comment);
    });

    [Fact]
    public void TheViewModelStaysConstructibleWithoutTheContainer()
    {
        // Step 3.4 writes "new SaveSpecViewModel()" in its code-behind, so the parameterless path is
        // part of the contract and not an accident of the current implementation.
        Assert.Single(typeof(SaveSpecViewModel).GetConstructors());
        Assert.Empty(typeof(SaveSpecViewModel).GetConstructors()[0].GetParameters());
    }

    [Fact]
    public void TheBodyIsInsetSixteenOnEverySide() => StaTestRunner.Run(() =>
    {
        // Panel2.BorderWidth = 16.
        SaveSpecDialog dialog = new();
        DockPanel root = (DockPanel)dialog.Content;
        Grid body = root.Children.OfType<Grid>().Single();

        Assert.Equal(new Thickness(16), body.Margin);
    });
}
