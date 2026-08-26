using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Xunit;

namespace QuickStat.Tests.Ui;

/// <summary>
/// Proves the helper every Phase 3 UI test depends on, including the premise that makes it
/// necessary.
/// </summary>
public class StaTestRunnerTests
{
    [Fact]
    public void TheTestRunnerItselfIsMta()
    {
        // The premise. If a future runner change makes this STA, the helper becomes unnecessary
        // rather than wrong - but nobody should have to rediscover which of the two it is.
        Assert.Equal(ApartmentState.MTA, Thread.CurrentThread.GetApartmentState());
    }

    [Fact]
    public void CreatingAWindowOnTheTestThreadFails()
    {
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => new Window());

        Assert.Contains("STA", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheBodyRunsOnAnStaThread() =>
        Assert.Equal(ApartmentState.STA, StaTestRunner.Run(() => Thread.CurrentThread.GetApartmentState()));

    [Fact]
    public void AWindowCanBeCreatedInside() =>
        StaTestRunner.Run(() =>
        {
            Window window = new();

            Assert.NotNull(window);
        });

    [Fact]
    public void AnElementCanBeMeasuredInside()
    {
        double width = StaTestRunner.Run(() =>
        {
            TextBlock text = new() { Text = "hello", FontSize = 12 };

            text.Measure(new Size(1000, 1000));

            return text.DesiredSize.Width;
        });

        Assert.True(width > 0);
    }

    [Fact]
    public void TwoRunsInOneProcessBothSucceed()
    {
        // Each call creates its own apartment, so tests stay independent whatever order they run in.
        Assert.Equal(1, StaTestRunner.Run(() => 1));
        Assert.Equal(2, StaTestRunner.Run(() => 2));
    }

    [Fact]
    public void NoApplicationIsCreated() =>
        // WPF permits one Application per AppDomain, so creating one here would break every later
        // test that wanted its own.  Production code must therefore tolerate a null Application.Current.
        StaTestRunner.Run(() => Assert.Null(Application.Current));

    [Fact]
    public void AFailureInsideSurfacesToTheCaller()
    {
        InvalidTimeZoneException failure =
            Assert.Throws<InvalidTimeZoneException>(() => StaTestRunner.Run(() => throw new InvalidTimeZoneException("boom")));

        Assert.Equal("boom", failure.Message);
        Assert.Contains(nameof(AFailureInsideSurfacesToTheCaller), failure.StackTrace, StringComparison.Ordinal);
    }

    [Fact]
    public void DispatcherWorkIsPumped()
    {
        bool posted = false;

        StaTestRunner.RunWithDispatcher(() =>
            Dispatcher.CurrentDispatcher.Invoke(
                () => posted = true,
                DispatcherPriority.Background));

        Assert.True(posted);
    }

    [Fact]
    public void AwaitResumesOnTheSameThread()
    {
        int before = 0;
        int after = 0;

        StaTestRunner.RunWithDispatcher(async () =>
        {
            before = Environment.CurrentManagedThreadId;

            await Task.Delay(1).ConfigureAwait(true);

            after = Environment.CurrentManagedThreadId;
        });

        Assert.Equal(before, after);
    }

    [Fact]
    public void AFailureInAnAsyncBodyStillStopsThePump()
    {
        // Without the finally that shuts the dispatcher down, this would hang until the timeout and
        // report a TimeoutException instead of the real cause.
        InvalidTimeZoneException failure = Assert.Throws<InvalidTimeZoneException>(() =>
            StaTestRunner.RunWithDispatcher(async () =>
            {
                await Task.Yield();

                throw new InvalidTimeZoneException("async boom");
            }));

        Assert.Equal("async boom", failure.Message);
    }

    [Fact]
    public void AHangIsReportedAsATimeoutRatherThanHangingTheSuite()
    {
        using ManualResetEventSlim release = new();

        TimeoutException failure = Assert.Throws<TimeoutException>(() =>
            StaTestRunner.Run(() => release.Wait(TimeSpan.FromMinutes(5)), TimeSpan.FromMilliseconds(200)));

        Assert.Contains("did not finish", failure.Message, StringComparison.Ordinal);

        // Let the abandoned worker end; it is a background thread, so it could not have blocked
        // process exit either way.
        release.Set();
    }
}
