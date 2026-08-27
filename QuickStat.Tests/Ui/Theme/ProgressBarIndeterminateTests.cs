using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using QuickStat.Tests.Ui.Dialogs;
using Xunit;

namespace QuickStat.Tests.Ui.Theme;

/// <summary>
/// <c>QsProgressBar</c> honours <see cref="ProgressBar.IsIndeterminate"/>: the flag starts a sweep
/// that really moves, the sweep is a fraction of the track rather than a pixel offset, and clearing
/// the flag - or hiding the bar - releases the clock again.
/// </summary>
/// <remarks>
/// <para>
/// PORT-PLAN.md §8.10 (d). The style replaces the stock <see cref="ProgressBar"/> template with a
/// track and an indicator, and a replaced template also replaces the stock indeterminate animation.
/// Nothing said so: <c>IsIndeterminate="True"</c> is a legal, compiling, documented WPF state that
/// rendered a full green bar standing perfectly still, and the only reason no shipped view ever
/// showed one is that step 3.6 read the template first and worked around it.
/// </para>
/// <para>
/// <b>Why these assertions are about rendered geometry and not about the storyboard.</b> Reading the
/// animation back out of <see cref="ControlTemplate.Triggers"/> would assert that somebody wrote a
/// storyboard, and reading <c>ScaleX</c> would assert that they wrote <em>this</em> storyboard;
/// neither is the claim. The claim is that the green rectangle a user looks at changes position and
/// size, so the tests sample where that rectangle actually is - <see cref="Visual.TransformToAncestor(Visual)"/>
/// composes the render transform for us - and any future template that moves it a different way
/// passes unchanged.
/// </para>
/// <para>
/// It has to be rendered, too, rather than measured and arranged: <see cref="UIElement.IsVisible"/>
/// is false for an element no <c>PresentationSource</c> owns and the trigger has it as a condition,
/// and there is no render loop ticking animation clocks for an unrealised tree.
/// </para>
/// <para>
/// <b>Release is asynchronous</b>, established here rather than assumed:
/// <see cref="ProgressBar.IsIndeterminate"/> going false leaves
/// <see cref="IAnimatable.HasAnimatedProperties"/> true until the next tick of the
/// <c>TimeManager</c>, so every assertion about release is made after the dispatcher has been
/// pumped. A test that read it straight after the assignment would fail against a correct style.
/// </para>
/// </remarks>
public class ProgressBarIndeterminateTests
{
    private const string StylesUri = "/QuickStat;component/Theme/QuickStat.Styles.xaml";

    /// <summary>
    /// One full 1.8 s cycle plus slack. Sampling stops the moment it has seen everything it is
    /// looking for, so a passing run spends about half of it.
    /// </summary>
    private static readonly TimeSpan OneCycle = TimeSpan.FromMilliseconds(2300);

    /// <summary>Long enough for a frame or two - all the release assertions need.</summary>
    private static readonly TimeSpan OneFrameOrTwo = TimeSpan.FromMilliseconds(120);

    [Fact]
    public void TheIndeterminateFlagStartsASweepThatGrowsFromTheLeftAndLeavesToTheRight() =>
        StaTestRunner.Run(() =>
        {
            ProgressBar bar = NewStyledBar();

            (double track, bool animatedBefore, Sweep sweep) = Realise(bar, 640, () =>
            {
                double width = Track(bar).ActualWidth;
                bool before = IsAnimating(bar);

                bar.IsIndeterminate = true;

                return (width, before, Sample(bar, width, OneCycle));
            });

            // The negative half of the control: a determinate bar animates nothing.
            Assert.False(animatedBefore, "a determinate bar was already animating");

            // The defect itself, in one line.  Before this change the indicator sat at the full
            // width of the track and never moved again.
            Assert.True(
                sweep.Positions.Count > 2,
                $"the indicator took only {sweep.Positions.Count} distinct positions in {OneCycle.TotalSeconds:F1} s");

            // Both halves of the cycle happen.  A pulse that only grew would satisfy "it moves" and
            // still look broken, and a bar that only ever hugged the left edge would not read as a
            // sweep at all.
            Assert.True(sweep.Grew, $"the indicator never filled the {track:F0} px track");
            Assert.True(sweep.Shrank, "the indicator never shrank again");
            Assert.True(sweep.LeftTheLeftEdge, "the indicator never travelled away from the left edge");
        });

    [Fact]
    public void ClearingTheFlagReleasesTheClockAndRestoresTheDeterminateGeometry() =>
        StaTestRunner.Run(() =>
        {
            ProgressBar bar = NewStyledBar();

            bar.Value = 25;

            (double track, bool animatedDuring, bool animatedAfter, Rect after, Point origin) =
                Realise(bar, 640, () =>
                {
                    bar.IsIndeterminate = true;

                    // Let the sweep get somewhere other than its first key frame, so a storyboard
                    // that was stopped-and-held rather than removed leaves a visible remnant.
                    Pump(OneFrameOrTwo);

                    bool during = IsAnimating(bar);

                    bar.IsIndeterminate = false;

                    Pump(OneFrameOrTwo);

                    return (Track(bar).ActualWidth, during, IsAnimating(bar), Bounds(bar), Indicator(bar).RenderTransformOrigin);
                });

            Assert.True(animatedDuring, "setting IsIndeterminate started no animation");

            // RepeatBehavior="Forever" never completes on its own: without ExitActions this clock
            // would go on ticking in the MediaContext for the rest of the process.
            Assert.False(animatedAfter, "the indeterminate storyboard outlived the flag");

            // Removed, not stopped - so both animated properties fall back to their base values,
            // and the bar is back to drawing Value=25 as a quarter of the track from the left edge.
            Assert.Equal(new Point(0, 0), origin);
            Assert.Equal(0d, after.Left, 3);
            Assert.Equal(track / 4, after.Width, 3);
        });

    [Fact]
    public void HidingTheBarReleasesTheClockToo() => StaTestRunner.Run(() =>
    {
        ProgressBar bar = NewStyledBar();

        (bool animatedWhileShown, bool animatedWhileHidden) = Realise(bar, 640, () =>
        {
            bar.IsIndeterminate = true;

            Pump(OneFrameOrTwo);

            bool shown = IsAnimating(bar);

            // What BusyOverlayView does when the operation ends: it collapses its subtree, it does
            // not unload it.  The flag would be untouched, so IsVisible is the only condition that
            // can notice, and without it a hidden overlay would keep a live clock behind it.
            bar.Visibility = Visibility.Collapsed;

            Pump(OneFrameOrTwo);

            return (shown, IsAnimating(bar));
        });

        Assert.True(animatedWhileShown, "setting IsIndeterminate started no animation");
        Assert.False(animatedWhileHidden, "a collapsed bar was left animating");
    });

    [Theory]
    [InlineData(320)]
    [InlineData(1000)]
    public void TheSweepIsAFractionOfTheTrackAndNotAPixelOffset(double windowWidth) =>
        StaTestRunner.Run(() =>
        {
            ProgressBar bar = NewStyledBar();

            bar.Value = 25;

            (double track, double determinate, double indeterminate) = Realise(bar, windowWidth, () =>
            {
                double width = Track(bar).ActualWidth;
                double quarter = Indicator(bar).ActualWidth;

                bar.IsIndeterminate = true;
                bar.UpdateLayout();

                return (width, quarter, Indicator(bar).ActualWidth);
            });

            // Guards the guard: without this both theory cases could be measuring nothing.
            Assert.True(track > 100, $"the track measured {track} px, so nothing was really laid out");

            // Determinate: a quarter of the track, from ProgressBar's own arithmetic.  The sweep
            // must not have disturbed it.
            Assert.Equal(track / 4, determinate, 3);

            // Indeterminate: the whole track.  This is the premise the ScaleTransform rests on - a
            // ScaleX of 0..1 over an indicator that is already the full track is a fraction of the
            // control's width at every width, which is precisely what a TranslateTransform in
            // device-independent pixels would not be.  The two theory cases differ by 680 px.
            Assert.Equal(track, indeterminate, 3);
        });

    /// <summary>A bar wearing the shipped style, stretched across whatever hosts it.</summary>
    private static ProgressBar NewStyledBar()
    {
        ResourceDictionary styles =
            (ResourceDictionary)Application.LoadComponent(new Uri(StylesUri, UriKind.Relative));

        return new ProgressBar
        {
            Style = (Style)styles["QsProgressBar"],
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
        };
    }

    /// <summary>
    /// Hosts <paramref name="bar"/> in a realised window of a given width and runs
    /// <paramref name="body"/> against the live tree.
    /// </summary>
    private static T Realise<T>(ProgressBar bar, double windowWidth, Func<T> body)
    {
        T result = default!;

        RealisedWindow.Run(
            new Window { Width = windowWidth, Height = 120, Content = bar },
            _ => result = body());

        return result;
    }

    /// <summary>Where the green rectangle currently is, in the bar's own coordinates.</summary>
    /// <remarks>
    /// <see cref="Visual.TransformToAncestor(Visual)"/> composes every intermediate render transform,
    /// including the indicator's own, so this is what is on screen rather than what was arranged.
    /// </remarks>
    private static Rect Bounds(ProgressBar bar)
    {
        FrameworkElement indicator = Indicator(bar);

        return indicator.TransformToAncestor(bar).TransformBounds(new Rect(indicator.RenderSize));
    }

    /// <summary>Whether anything on the indicator is under the control of an animation clock.</summary>
    /// <remarks>
    /// Both halves are needed and neither is redundant: the sweep animates the element's
    /// <see cref="UIElement.RenderTransformOrigin"/> and the transform's own scale, and a storyboard
    /// that released one but not the other would be exactly the leak this asks about.
    /// </remarks>
    private static bool IsAnimating(ProgressBar bar)
    {
        FrameworkElement indicator = Indicator(bar);

        return indicator.HasAnimatedProperties
            || (indicator.RenderTransform is IAnimatable transform && transform.HasAnimatedProperties);
    }

    /// <summary>Where a sweep went, sampled frame by frame, as fractions of the track.</summary>
    private sealed class Sweep(double track)
    {
        /// <summary>Every distinct (left edge, width) the indicator was seen at.</summary>
        internal HashSet<(double Left, double Width)> Positions { get; } = [];

        /// <summary>Whether the indicator reached (very nearly) the full width of the track.</summary>
        internal bool Grew => Positions.Any(position => position.Width > 0.95 * track);

        /// <summary>Whether it came back down again afterwards.</summary>
        internal bool Shrank => Positions.Any(position => position.Width < 0.5 * track);

        /// <summary>Whether it ever moved off the left edge, i.e. travelled rather than pulsed.</summary>
        internal bool LeftTheLeftEdge => Positions.Any(position => position.Left > 0.5 * track);

        /// <summary>Records the indicator's current rectangle.</summary>
        /// <param name="bounds">Where it is now.</param>
        /// <returns>Whether everything this sweep is looking for has now been seen.</returns>
        internal bool Record(Rect bounds)
        {
            // Rounded to a tenth of a pixel: sub-pixel jitter would otherwise make Positions grow
            // without bound and turn "it moved" into a tautology.
            _ = Positions.Add((Math.Round(bounds.Left, 1), Math.Round(bounds.Width, 1)));

            return Positions.Count > 2 && Grew && Shrank && LeftTheLeftEdge;
        }
    }

    /// <summary>
    /// Pumps the dispatcher - which is what drives WPF's render loop, and so its animation clocks -
    /// and records where the indicator went.
    /// </summary>
    private static Sweep Sample(ProgressBar bar, double track, TimeSpan within)
    {
        Sweep sweep = new(track);

        Pump(within, () => sweep.Record(Bounds(bar)));

        return sweep;
    }

    /// <summary>Runs the message loop for <paramref name="duration"/>.</summary>
    /// <param name="duration">How long to pump for.</param>
    /// <param name="until">
    /// Optional per-frame sampler; returning <see langword="true"/> ends the pump early.
    /// </param>
    private static void Pump(TimeSpan duration, Func<bool>? until = null)
    {
        DispatcherFrame frame = new();

        DispatcherTimer deadline = new(
            duration,
            DispatcherPriority.Background,
            (_, _) => frame.Continue = false,
            Dispatcher.CurrentDispatcher);

        // Render priority runs after WPF has composed a frame, so each callback sees a value the
        // TimeManager has just advanced.
        DispatcherTimer sampler = new(
            TimeSpan.FromMilliseconds(10),
            DispatcherPriority.Render,
            (_, _) =>
            {
                if (until is not null && until())
                {
                    frame.Continue = false;
                }
            },
            Dispatcher.CurrentDispatcher);

        deadline.Start();
        sampler.Start();

        try
        {
            Dispatcher.PushFrame(frame);
        }
        finally
        {
            deadline.Stop();
            sampler.Stop();
        }
    }

    private static FrameworkElement Track(ProgressBar bar) => Part(bar, "PART_Track");

    private static FrameworkElement Indicator(ProgressBar bar) => Part(bar, "PART_Indicator");

    private static FrameworkElement Part(ProgressBar bar, string name)
    {
        object? part = bar.Template.FindName(name, bar);

        Assert.NotNull(part);

        return (FrameworkElement)part;
    }
}
