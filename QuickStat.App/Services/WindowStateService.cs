using System.Globalization;
using System.Windows;
using Microsoft.Extensions.Logging;
using QuickStat.Configuration.Settings;

namespace QuickStat.Services;

/// <summary>The one implementation of <see cref="IWindowStateService"/>, over <see cref="ISettingsStore"/>.</summary>
/// <remarks>
/// The section key reproduces <c>TGuiSettings.FormKey</c>:
/// <c>Format('%s.%dx%d', [Name, Screen.Width, Screen.Height])</c> with the Delphi form's name,
/// <c>frmQuickStat</c>. The dimensions are device-independent units rather than physical pixels,
/// which the Delphi used - so a DPI change also produces a fresh key, and a user who changes
/// scaling gets geometry that fits. Nothing depends on reading a file the Delphi wrote: the port's
/// settings file is new (<c>%APPDATA%\DIPS\QuickStat\QuickStat.ini</c>, PORT-PLAN.md §8.8 h).
/// </remarks>
public sealed class WindowStateService : IWindowStateService
{
    /// <summary>The Delphi form's name, and the first part of the section key.</summary>
    public const string FormName = "frmQuickStat";

    /// <summary>Section holding the two additions, which are not per-resolution.</summary>
    public const string ShellSection = "Shell";

    /// <summary>Key for the window state. Delphi <c>PROP_STATE</c>.</summary>
    public const string StateKey = "State";

    /// <summary>Key for the left edge. Delphi <c>PROP_LEFT</c>.</summary>
    public const string LeftKey = "Left";

    /// <summary>Key for the top edge. Delphi <c>PROP_TOP</c>.</summary>
    public const string TopKey = "Top";

    /// <summary>Key for the width. Delphi <c>PROP_WIDTH</c>.</summary>
    public const string WidthKey = "Width";

    /// <summary>Key for the height. Delphi <c>PROP_HEIGHT</c>.</summary>
    public const string HeightKey = "Height";

    /// <summary>Key for the splitter position. <b>Addition</b>; the Delphi never saved it.</summary>
    public const string SplitterKey = "SplitterPosition";

    /// <summary>Key for the last connection name. <b>Addition</b>; the Delphi never saved it.</summary>
    public const string LastDatabaseKey = "LastDatabase";

    private readonly ISettingsStore _settings;
    private readonly IMonitorLayout _monitors;
    private readonly ILogger<WindowStateService> _logger;
    private readonly string _section;

    /// <summary>Creates the service with the section key for the current screen size.</summary>
    /// <param name="settings">Where the values live.</param>
    /// <param name="monitors">Supplies the guard rail's work areas.</param>
    /// <param name="logger">Log.</param>
    public WindowStateService(ISettingsStore settings, IMonitorLayout monitors, ILogger<WindowStateService> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(monitors);
        ArgumentNullException.ThrowIfNull(logger);

        _settings = settings;
        _monitors = monitors;
        _logger = logger;

        Rect primary = monitors.PrimaryWorkArea;

        _section = SectionKey(primary.Width, primary.Height);
    }

    /// <summary>Builds the per-resolution section key.</summary>
    /// <param name="screenWidth">Screen width in device-independent units.</param>
    /// <param name="screenHeight">Screen height in device-independent units.</param>
    /// <returns><c>frmQuickStat.&lt;w&gt;x&lt;h&gt;</c>.</returns>
    /// <remarks>Invariant culture, so a Norwegian machine and an English one produce the same key.</remarks>
    public static string SectionKey(double screenWidth, double screenHeight) => string.Create(
        CultureInfo.InvariantCulture,
        $"{FormName}.{(int)Math.Round(screenWidth)}x{(int)Math.Round(screenHeight)}");

    /// <summary>
    /// Applies the off-screen guard rail: a rectangle that overlaps no work area is replaced.
    /// </summary>
    /// <param name="bounds">The stored rectangle.</param>
    /// <param name="workAreas">The rectangles the window may overlap.</param>
    /// <param name="fallback">Where to put a window that overlaps none of them.</param>
    /// <returns><paramref name="bounds"/>, or <paramref name="fallback"/>.</returns>
    /// <remarks>
    /// <para>
    /// Delphi <c>TGuiSettings.RectIsVisibleOnMonitors</c> plus the <c>if not … then boundsRect :=
    /// Screen.WorkareaRect</c> that follows it. The whole point is the user who unplugs a second
    /// monitor: the stored rectangle is then at, say, x = 2560 on a desktop that now ends at 1920,
    /// and without this the window opens somewhere nobody can reach it.
    /// </para>
    /// <para>
    /// <see cref="Rect.IntersectsWith"/> matches Delphi's <c>TRect.IntersectsWith</c> in the case
    /// that matters and differs in one: WPF treats a zero-area rectangle as
    /// <see cref="Rect.Empty"/>-like and returns <see langword="false"/>, so a stored width or
    /// height of 0 falls back rather than being restored as an invisible window. That is the better
    /// answer.
    /// </para>
    /// </remarks>
    public static Rect ApplyOffScreenGuard(Rect bounds, IReadOnlyList<Rect> workAreas, Rect fallback)
    {
        ArgumentNullException.ThrowIfNull(workAreas);

        foreach (Rect workArea in workAreas)
        {
            if (bounds.IntersectsWith(workArea))
            {
                return bounds;
            }
        }

        return fallback;
    }

    /// <inheritdoc />
    public WindowPlacement? Restore(Size defaultSize)
    {
        try
        {
            if (!_settings.Contains(_section, StateKey))
            {
                return null;
            }

            WindowState state = ToWindowState(_settings.GetInt32(_section, StateKey, 0));

            if (state != WindowState.Normal)
            {
                // The Delphi exits here without reading the bounds at all.
                return new WindowPlacement(state, null);
            }

            Rect bounds = new(
                _settings.GetDouble(_section, LeftKey, 0),
                _settings.GetDouble(_section, TopKey, 0),
                _settings.GetDouble(_section, WidthKey, defaultSize.Width),
                _settings.GetDouble(_section, HeightKey, defaultSize.Height));

            return new WindowPlacement(
                state,
                ApplyOffScreenGuard(bounds, _monitors.WorkAreas, _monitors.PrimaryWorkArea));
        }
        catch (Exception exception)
        {
            // The Delphi swallows this into a SilentError.  A corrupt geometry key must never stop
            // the application from opening.
            _logger.LogWarning(exception, "Could not restore the window geometry from section {Section}.", _section);

            return null;
        }
    }

    /// <inheritdoc />
    public void Save(WindowPlacement placement)
    {
        try
        {
            _settings.SetInt32(_section, StateKey, (int)placement.State);

            if (placement.State != WindowState.Normal || placement.Bounds is not { } bounds)
            {
                return;
            }

            _settings.SetDouble(_section, LeftKey, bounds.Left);
            _settings.SetDouble(_section, TopKey, bounds.Top);
            _settings.SetDouble(_section, WidthKey, bounds.Width);
            _settings.SetDouble(_section, HeightKey, bounds.Height);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not save the window geometry to section {Section}.", _section);
        }
    }

    /// <inheritdoc />
    public double GetSplitterPosition(double defaultPosition) =>
        _settings.GetDouble(ShellSection, SplitterKey, defaultPosition);

    /// <inheritdoc />
    public void SetSplitterPosition(double position) =>
        _settings.SetDouble(ShellSection, SplitterKey, position);

    /// <inheritdoc />
    public string? GetLastDatabase()
    {
        string value = _settings.GetString(ShellSection, LastDatabaseKey);

        return string.IsNullOrEmpty(value) ? null : value;
    }

    /// <inheritdoc />
    public void SetLastDatabase(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            _settings.Remove(ShellSection, LastDatabaseKey);

            return;
        }

        _settings.SetString(ShellSection, LastDatabaseKey, name);
    }

    /// <inheritdoc />
    public void Flush()
    {
        try
        {
            _settings.Flush();
        }
        catch (Exception exception)
        {
            // ISettingsStore.Flush is documented as never throwing; this is the belt to that brace,
            // because it runs while the window is closing and an exception there is a crash dialog
            // after the user has already said goodbye.
            _logger.LogWarning(exception, "Could not write the settings file.");
        }
    }

    /// <summary>Maps a stored integer to a window state, defaulting anything unknown to Normal.</summary>
    /// <param name="value">The stored value: 0 Normal, 1 Minimized, 2 Maximized.</param>
    /// <returns>The state.</returns>
    /// <remarks>
    /// Mapped rather than cast. The two enumerations agree today, but a cast would turn a corrupt
    /// <c>State=7</c> into an undefined <see cref="WindowState"/> that WPF rejects when assigned.
    /// </remarks>
    private static WindowState ToWindowState(int value) => value switch
    {
        1 => WindowState.Minimized,
        2 => WindowState.Maximized,
        _ => WindowState.Normal,
    };
}
