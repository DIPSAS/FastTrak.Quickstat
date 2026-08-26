using System.ComponentModel;
using QuickStat.Diagnostics;
using QuickStat.Services;
using Xunit;

namespace QuickStat.Tests.Ui.Services;

/// <summary>The banner's Progress block, <c>05-ui-spec.md</c> §G.6, and the busy flag, §G.3.</summary>
public class ShellProgressTests
{
    private static ShellProgress NewProgress() => new(new InlineUiDispatcher());

    [Fact]
    public void StartsIdle()
    {
        ShellProgress progress = NewProgress();

        Assert.Equal("Progress", progress.Header);
        Assert.Equal("Program is idle", progress.Info);
        Assert.Equal(0, progress.Percent);
        Assert.False(progress.IsError);
        Assert.False(progress.IsBusy);
    }

    [Fact]
    public void DoneSetsAHundredPercentAndTaskCompleted()
    {
        ShellProgress progress = NewProgress();

        progress.Done();

        Assert.Equal(100, progress.Percent);
        Assert.Equal("Task completed", progress.Info);
    }

    [Fact]
    public void ReportUpdatesInfoAndPercent()
    {
        ShellProgress progress = NewProgress();

        progress.Report(new OperationProgress("", "Loading collectors", 42));

        Assert.Equal("Loading collectors", progress.Info);
        Assert.Equal(42, progress.Percent);
    }

    [Fact]
    public void ReportWithAnEmptyHeaderLeavesTheHeadingAlone()
    {
        // §G.6: nothing in QuickStat ever calls SetHeader, so the label is effectively static.  A
        // caller that leaves OperationProgress.Header blank must not blank it.
        ShellProgress progress = NewProgress();

        progress.Report(new OperationProgress("", "working", null));

        Assert.Equal("Progress", progress.Header);
    }

    [Fact]
    public void ReportWithNoPercentLeavesTheBarWhereItIs()
    {
        ShellProgress progress = NewProgress();

        progress.Report(new OperationProgress("", "step one", 30));
        progress.Report(new OperationProgress("", "step two", null));

        Assert.Equal(30, progress.Percent);
    }

    [Fact]
    public void PercentIsClamped()
    {
        ShellProgress progress = NewProgress();

        progress.Report(new OperationProgress("", "", 250));
        Assert.Equal(100, progress.Percent);

        progress.Report(new OperationProgress("", "", -5));
        Assert.Equal(0, progress.Percent);
    }

    [Fact]
    public void FailFlagsTheStatusLineAndTheNextReportClearsIt()
    {
        // §G.2: an exception while building the data hint turns lblInfo red.
        ShellProgress progress = NewProgress();

        progress.Fail("Object reference not set");

        Assert.True(progress.IsError);
        Assert.Equal("Object reference not set", progress.Info);

        progress.SetInfo("Connecting to Testdatabase (NDV) ...");

        Assert.False(progress.IsError);
    }

    [Fact]
    public void BeginOperationSetsBusyUntilDisposed()
    {
        ShellProgress progress = NewProgress();

        using (progress.BeginOperation("Collecting ..."))
        {
            Assert.True(progress.IsBusy);
            Assert.Equal("Collecting ...", progress.Info);
        }

        Assert.False(progress.IsBusy);
    }

    [Fact]
    public void NestedOperationsDoNotClearBusyEarly()
    {
        // The package replay calls the collect action from inside its own wait cursor, which is why
        // the Delphi saves and restores Screen.Cursor rather than assigning crDefault.
        ShellProgress progress = NewProgress();

        IDisposable outer = progress.BeginOperation("Replaying package ...");
        IDisposable inner = progress.BeginOperation("Collecting ...");

        inner.Dispose();
        Assert.True(progress.IsBusy);

        outer.Dispose();
        Assert.False(progress.IsBusy);
    }

    [Fact]
    public void DisposingAnOperationTwiceIsHarmless()
    {
        ShellProgress progress = NewProgress();

        IDisposable outer = progress.BeginOperation("outer");
        IDisposable inner = progress.BeginOperation("inner");

        inner.Dispose();
        inner.Dispose();

        Assert.True(progress.IsBusy);

        outer.Dispose();

        Assert.False(progress.IsBusy);
    }

    [Fact]
    public void BusyIsRaisedOnceInEachDirection()
    {
        ShellProgress progress = NewProgress();
        int raised = 0;

        progress.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IShellProgress.IsBusy))
            {
                raised++;
            }
        };

        IDisposable outer = progress.BeginOperation("outer");
        IDisposable inner = progress.BeginOperation("inner");

        inner.Dispose();
        outer.Dispose();

        Assert.Equal(2, raised);
    }

    [Fact]
    public void UnchangedValuesRaiseNothing()
    {
        ShellProgress progress = NewProgress();
        List<string?> raised = [];

        progress.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        progress.SetInfo("Program is idle");

        Assert.Empty(raised);
    }

    [Fact]
    public void ResetReturnsToIdle()
    {
        ShellProgress progress = NewProgress();

        progress.Done();
        progress.Fail("boom");
        progress.Reset();

        Assert.Equal("Program is idle", progress.Info);
        Assert.Equal(0, progress.Percent);
        Assert.False(progress.IsError);
    }

    [Fact]
    public void ItIsUsableThroughTheProgressInterfaceCoreTakes()
    {
        // Core's login pipeline and collector runner take IProgress<OperationProgress>; the shell
        // registers this instance under both faces so they cannot diverge.
        IProgress<OperationProgress> sink = NewProgress();

        sink.Report(new OperationProgress("Progress", "Connecting to X ...", 10));

        Assert.Equal("Connecting to X ...", ((IShellProgress)sink).Info);
    }

    [Fact]
    public void PropertyChangedNamesMatchTheInterface()
    {
        // MainViewModel maps these names one by one; a mismatch would silently freeze the banner.
        ShellProgress progress = NewProgress();
        List<string?> raised = [];

        progress.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        progress.Report(new OperationProgress("Framdrift", "arbeider", 5));

        Assert.Contains(nameof(IShellProgress.Info), raised);
        Assert.Contains(nameof(IShellProgress.Percent), raised);

        // Header is deliberately absent: Report ignores the header an operation reports, because the
        // Delphi's is a static caption that nothing ever assigns (§G.6, MainQuickStat.pas:433).
        Assert.DoesNotContain(nameof(IShellProgress.Header), raised);
        Assert.Equal("Progress", ((IShellProgress)progress).Header);
    }

    [Fact]
    public void TheHeaderSurvivesEveryOperationThatReportsOneOfItsOwn()
    {
        // The symptom this prevents: Core's login pipeline reports "Connecting" and CollectorRunner
        // reports "Collecting data", so the banner used to read "Collecting data" from the first
        // collect until the process ended.  The operation's name belongs on the line underneath.
        IShellProgress progress = NewProgress();

        ((IProgress<OperationProgress>)progress).Report(new OperationProgress("Connecting", "Connecting to X ...", 10));
        ((IProgress<OperationProgress>)progress).Report(new OperationProgress("Collecting data", "Antropometri", 40));

        Assert.Equal("Progress", progress.Header);
        Assert.Equal("Antropometri", progress.Info);
    }

    [Fact]
    public void PropertyChangedIsRaisedThroughTheInterfaceContract()
    {
        INotifyPropertyChanged notifier = NewProgress();
        bool raised = false;

        notifier.PropertyChanged += (_, _) => raised = true;

        ((IShellProgress)notifier).SetInfo("something new");

        Assert.True(raised);
    }
}
