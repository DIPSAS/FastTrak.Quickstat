using System.Windows;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Services;
using Xunit;

namespace QuickStat.Tests.Ui.Services;

/// <summary>
/// Window geometry persistence, <c>05-ui-spec.md</c> §G.1, and the two additions §G.1 recommends.
/// </summary>
/// <remarks>
/// Nothing here touches a <see cref="Window"/>, which is why it can run on the MTA test thread: the
/// service deals in a <see cref="WindowPlacement"/> and an <see cref="IMonitorLayout"/>, and the
/// window is the only thing that knows how to apply one.
/// </remarks>
public class WindowStateServiceTests
{
    private static readonly Rect Primary = new(0, 0, 1920, 1040);

    private static WindowStateService NewService(
        InMemorySettingsStore settings,
        IMonitorLayout? monitors = null) =>
        new(settings, monitors ?? new FakeMonitorLayout(Primary), NullLogger<WindowStateService>.Instance);

    [Fact]
    public void NothingStoredMeansNothingRestored()
    {
        // Deliberately not "0,0 at the design size", which is what the Delphi produces on a first
        // run because it reads Left and Top with a default of zero.  Null lets the window stay
        // centred.
        Assert.Null(NewService(new InMemorySettingsStore()).Restore(new Size(1320, 840)));
    }

    [Fact]
    public void NormalGeometryRoundTrips()
    {
        InMemorySettingsStore settings = new();
        Rect bounds = new(120, 80, 1280, 800);

        NewService(settings).Save(new WindowPlacement(WindowState.Normal, bounds));

        WindowPlacement? restored = NewService(settings).Restore(new Size(1320, 840));

        Assert.NotNull(restored);
        Assert.Equal(WindowState.Normal, restored.Value.State);
        Assert.Equal(bounds, restored.Value.Bounds);
    }

    [Fact]
    public void MaximisedIsStoredWithoutBoundsAndRestoredWithoutThem()
    {
        // TGuiSettings writes the four bounds keys only when the state is Normal, and exits the
        // restore path before reading them for anything else.
        InMemorySettingsStore settings = new();

        NewService(settings).Save(new WindowPlacement(WindowState.Maximized, new Rect(1, 2, 3, 4)));

        Assert.False(settings.Contains(WindowStateService.SectionKey(1920, 1040), WindowStateService.LeftKey));

        WindowPlacement? restored = NewService(settings).Restore(new Size(1320, 840));

        Assert.NotNull(restored);
        Assert.Equal(WindowState.Maximized, restored.Value.State);
        Assert.Null(restored.Value.Bounds);
    }

    [Fact]
    public void TheSectionKeyIsPerScreenResolution()
    {
        // A laptop docked to a bigger monitor keeps its own geometry, which is the whole reason the
        // Delphi puts the screen size in the section name.
        Assert.Equal("frmQuickStat.1920x1040", WindowStateService.SectionKey(1920, 1040));
        Assert.NotEqual(WindowStateService.SectionKey(1920, 1040), WindowStateService.SectionKey(3840, 2100));
    }

    [Fact]
    public void GeometrySavedOnOneScreenSizeIsNotRestoredOnAnother()
    {
        InMemorySettingsStore settings = new();

        NewService(settings, new FakeMonitorLayout(new Rect(0, 0, 1920, 1040)))
            .Save(new WindowPlacement(WindowState.Normal, new Rect(10, 10, 800, 600)));

        Assert.Null(NewService(settings, new FakeMonitorLayout(new Rect(0, 0, 3840, 2100)))
            .Restore(new Size(1320, 840)));
    }

    [Fact]
    public void ARectangleOnAMonitorThatIsNoLongerThereFallsBackToTheWorkArea()
    {
        // The case §G.1 exists for: the window was on a second monitor at x = 1920, and that monitor
        // has been unplugged.  Without the guard it opens where nobody can reach it.
        Rect offScreen = new(2400, 100, 1280, 800);

        Assert.Equal(
            Primary,
            WindowStateService.ApplyOffScreenGuard(offScreen, [Primary], Primary));
    }

    [Fact]
    public void ARectangleThatOverlapsAnySingleWorkAreaIsKept()
    {
        Rect secondMonitor = new(1920, 0, 1920, 1040);
        Rect onSecond = new(2400, 100, 1280, 800);

        Assert.Equal(
            onSecond,
            WindowStateService.ApplyOffScreenGuard(onSecond, [Primary, secondMonitor], Primary));
    }

    [Fact]
    public void PartialOverlapIsEnoughToKeepTheRectangle()
    {
        // TRect.IntersectsWith, not containment: a window hanging off the right edge is still
        // reachable and the Delphi leaves it alone.
        Rect hangingOff = new(1800, 900, 600, 400);

        Assert.Equal(
            hangingOff,
            WindowStateService.ApplyOffScreenGuard(hangingOff, [Primary], Primary));
    }

    [Fact]
    public void RestoringAnOffScreenRectangleGoesThroughTheGuard()
    {
        InMemorySettingsStore settings = new();
        FakeMonitorLayout twoMonitors = new(Primary, new Rect(1920, 0, 1920, 1040));

        NewService(settings, twoMonitors)
            .Save(new WindowPlacement(WindowState.Normal, new Rect(2400, 100, 1280, 800)));

        // Same screen size, so the same section key - but now only one monitor.
        WindowPlacement? restored = NewService(settings, new FakeMonitorLayout(Primary))
            .Restore(new Size(1320, 840));

        Assert.NotNull(restored);
        Assert.Equal(Primary, restored.Value.Bounds);
    }

    [Fact]
    public void ACorruptStateValueRestoresAsNormal()
    {
        InMemorySettingsStore settings = new();
        string section = WindowStateService.SectionKey(1920, 1040);

        settings.SetInt32(section, WindowStateService.StateKey, 7);
        settings.SetDouble(section, WindowStateService.LeftKey, 10);
        settings.SetDouble(section, WindowStateService.TopKey, 20);
        settings.SetDouble(section, WindowStateService.WidthKey, 900);
        settings.SetDouble(section, WindowStateService.HeightKey, 700);

        WindowPlacement? restored = NewService(settings).Restore(new Size(1320, 840));

        Assert.NotNull(restored);
        Assert.Equal(WindowState.Normal, restored.Value.State);
    }

    [Fact]
    public void MissingBoundsKeysFallBackToTheDesignSize()
    {
        InMemorySettingsStore settings = new();
        string section = WindowStateService.SectionKey(1920, 1040);

        settings.SetInt32(section, WindowStateService.StateKey, 0);

        WindowPlacement? restored = NewService(settings).Restore(new Size(1320, 840));

        Assert.NotNull(restored);
        Assert.Equal(new Rect(0, 0, 1320, 840), restored.Value.Bounds);
    }

    [Fact]
    public void TheSplitterPositionRoundTrips()
    {
        // An addition: §G.1 records that the Delphi never persists splMain, and recommends it.
        InMemorySettingsStore settings = new();

        NewService(settings).SetSplitterPosition(336);

        Assert.Equal(336, NewService(settings).GetSplitterPosition(293));
        Assert.Equal(293, NewService(new InMemorySettingsStore()).GetSplitterPosition(293));
    }

    [Fact]
    public void TheLastDatabaseRoundTripsAndCanBeForgotten()
    {
        // Also an addition.  Note it is stored per user, not per screen size: which database you use
        // has nothing to do with which monitor you are on.
        InMemorySettingsStore settings = new();
        WindowStateService service = NewService(settings);

        Assert.Null(service.GetLastDatabase());

        service.SetLastDatabase("Testdatabase (NDV)");

        Assert.Equal("Testdatabase (NDV)", NewService(settings).GetLastDatabase());

        service.SetLastDatabase(null);

        Assert.Null(NewService(settings).GetLastDatabase());
    }

    [Fact]
    public void FlushReachesTheStore()
    {
        InMemorySettingsStore settings = new();

        NewService(settings).Flush();

        Assert.Equal(1, settings.FlushCount);
    }
}
