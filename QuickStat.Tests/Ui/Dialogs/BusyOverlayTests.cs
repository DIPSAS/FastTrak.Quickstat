using System.Windows;
using QuickStat.Diagnostics;
using QuickStat.Services;
using QuickStat.Tests.Ui.Services;
using QuickStat.ViewModels;
using QuickStat.Views.Dialogs;
using Xunit;

namespace QuickStat.Tests.Ui.Dialogs;

/// <summary>
/// The busy overlay: it follows <see cref="IShellProgress.IsBusy"/> and nothing else, it survives
/// nesting, and cancelling asks the operation to stop rather than pretending it has.
/// </summary>
/// <remarks>
/// <c>05-ui-spec.md</c> §G.3. The Delphi's <c>Screen.Cursor := crSqlWait</c> was saved and restored
/// rather than assigned, because the package replay runs a collect inside its own wait cursor; the
/// counting in <see cref="IShellProgress.BeginOperation"/> is the same guarantee, and the overlay
/// must not undermine it by assigning the flag itself.
/// </remarks>
public class BusyOverlayTests
{
    [Fact]
    public void TheOverlayIsUpForExactlyAsLongAsTheOutermostOperation()
    {
        ShellProgress progress = new(new InlineUiDispatcher());
        using BusyOverlayViewModel model = new(progress);

        Assert.False(model.IsBusy);

        using (progress.BeginOperation("Preparing the packaged selection"))
        {
            Assert.True(model.IsBusy);

            using (progress.BeginOperation("Collecting data"))
            {
                Assert.True(model.IsBusy);
            }

            // The inner scope closing must not take the overlay down: the replay is still running.
            Assert.True(model.IsBusy);
        }

        Assert.False(model.IsBusy);
    }

    [Fact]
    public void TheMessageAndThePercentageMirrorTheBanner()
    {
        ShellProgress progress = new(new InlineUiDispatcher());
        using BusyOverlayViewModel model = new(progress);

        List<string?> raised = [];

        model.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        using (progress.BeginOperation("Connecting to Testdatabase (NDV) ..."))
        {
            Assert.Equal("Connecting to Testdatabase (NDV) ...", model.Message);

            progress.Report(new OperationProgress("", "Labdata: Alle med høy konfidens", 42));

            Assert.Equal("Labdata: Alle med høy konfidens", model.Message);
            Assert.Equal(42d, model.Percent);
        }

        Assert.Contains(nameof(BusyOverlayViewModel.IsBusy), raised);
        Assert.Contains(nameof(BusyOverlayViewModel.Message), raised);
        Assert.Contains(nameof(BusyOverlayViewModel.Percent), raised);
    }

    [Fact]
    public void NoCancelButtonIsOfferedWhenNothingCanBeCancelled()
    {
        ShellProgress progress = new(new InlineUiDispatcher());
        using BusyOverlayViewModel model = new(progress);

        using (progress.BeginOperation("Collecting data"))
        {
            Assert.False(model.IsCancelOffered);
            Assert.False(model.CanCancel);
            Assert.False(model.CancelCommand.CanExecute(null));
        }
    }

    [Fact]
    public void CancellingSignalsTheTokenAndLeavesTheOverlayUp()
    {
        ShellProgress progress = new(new InlineUiDispatcher());
        using BusyOverlayViewModel model = new(progress);
        using CancellationTokenSource cancellation = new();

        using (progress.BeginOperation("Collecting data"))
        using (model.OfferCancellation(cancellation))
        {
            Assert.True(model.CanCancel);
            Assert.False(model.IsCancelling);

            model.CancelCommand.Execute(null);

            Assert.True(cancellation.IsCancellationRequested);
            Assert.True(model.IsCancelling);

            // The operation has been asked to stop; only it can say when it has.  Assigning IsBusy
            // here would take the overlay down while the query was still running.
            Assert.True(model.IsBusy);
            Assert.False(model.CanCancel);
            Assert.False(model.CancelCommand.CanExecute(null));
        }

        Assert.False(model.IsBusy);
        Assert.False(model.IsCancelling);
        Assert.False(model.IsCancelOffered);
    }

    [Fact]
    public void CancellingANestedRunSignalsEveryToken()
    {
        ShellProgress progress = new(new InlineUiDispatcher());
        using BusyOverlayViewModel model = new(progress);
        using CancellationTokenSource replay = new();
        using CancellationTokenSource collect = new();

        using (progress.BeginOperation("Preparing the packaged selection"))
        using (model.OfferCancellation(replay))
        {
            using (progress.BeginOperation("Collecting data"))
            using (model.OfferCancellation(collect))
            {
                model.CancelCommand.Execute(null);
            }

            // The inner offer is withdrawn with its scope, but the replay is still cancellable-
            // and still marked as cancelling, because its own token is signalled.
            Assert.True(replay.IsCancellationRequested);
            Assert.True(collect.IsCancellationRequested);
            Assert.True(model.IsCancelOffered);
            Assert.True(model.IsCancelling);
        }
    }

    [Fact]
    public void AnOfferSurvivesItsSourceBeingDisposedFirst()
    {
        ShellProgress progress = new(new InlineUiDispatcher());
        using BusyOverlayViewModel model = new(progress);
        CancellationTokenSource cancellation = new();

        using (progress.BeginOperation("Collecting data"))
        using (model.OfferCancellation(cancellation))
        {
            cancellation.Dispose();

            // Cancelling something that has already finished is not an error, and must not take the
            // application down from a button press.
            model.CancelCommand.Execute(null);

            Assert.True(model.IsCancelling);
        }
    }

    [Fact]
    public void DisposingStopsListening()
    {
        ShellProgress progress = new(new InlineUiDispatcher());
        BusyOverlayViewModel model = new(progress);

        model.Dispose();
        model.Dispose();

        bool raised = false;

        model.PropertyChanged += (_, _) => raised = true;

        using (progress.BeginOperation("Collecting data"))
        {
            Assert.False(raised);
        }
    }

    [Fact]
    public void TheViewFollowsTheFlag() => StaTestRunner.Run(() =>
    {
        ShellProgress progress = new(new InlineUiDispatcher());
        using BusyOverlayViewModel model = new(progress);

        RealisedWindow.RunControl(new BusyOverlayView { DataContext = model }, view =>
        {
            Assert.Equal(Visibility.Collapsed, view.Visibility);

            using CancellationTokenSource cancellation = new();

            using (progress.BeginOperation("Collecting data"))
            {
                Assert.Equal(Visibility.Visible, view.Visibility);
                Assert.Equal("Collecting data", view.MessageText.Text);
                Assert.Equal(Visibility.Collapsed, view.CancelButton.Visibility);
                Assert.Equal(Visibility.Collapsed, view.CancellingText.Visibility);

                using (model.OfferCancellation(cancellation))
                {
                    Assert.Equal(Visibility.Visible, view.CancelButton.Visibility);
                    Assert.True(view.CancelButton.IsEnabled);

                    model.CancelCommand.Execute(null);

                    Assert.False(view.CancelButton.IsEnabled);
                    Assert.Equal(Visibility.Visible, view.CancellingText.Visibility);
                }
            }

            Assert.Equal(Visibility.Collapsed, view.Visibility);
        });
    });

    [Fact]
    public void TheBarIsDeterminateBecauseThereIsARealPercentageToShow() => StaTestRunner.Run(() =>
    {
        ShellProgress progress = new(new InlineUiDispatcher());
        using BusyOverlayViewModel model = new(progress);

        RealisedWindow.RunControl(new BusyOverlayView { DataContext = model }, view =>
        {
            // §G.6: the collect run reports one step per patient, so the overlay has a number and
            // shows it.  QsProgressBar has been able to animate IsIndeterminate since PORT-PLAN.md
            // §8.10 (d) - Ui/Theme/ProgressBarIndeterminateTests.cs pins that - so what this
            // asserts is the choice, no longer the absence of an alternative.
            Assert.False(view.ProgressIndicator.IsIndeterminate);

            using (progress.BeginOperation("Collecting data"))
            {
                progress.Report(new OperationProgress("", "Collecting data", 65));

                Assert.Equal(65d, view.ProgressIndicator.Value);
            }
        });
    });
}
