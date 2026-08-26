using System.Windows;

namespace QuickStat.Services;

/// <summary>
/// <see cref="IMonitorLayout"/> from <see cref="SystemParameters"/>: the primary work area plus the
/// virtual-screen bounding box.
/// </summary>
/// <remarks>
/// <para>
/// <b>A deliberate simplification, and the one place this differs from the Delphi.</b>
/// <c>TGuiSettings.RectIsVisibleOnMonitors</c> walks <c>Screen.Monitors[n].WorkareaRect</c> and
/// tests each one; WPF exposes no monitor enumeration. Reaching the real per-monitor work areas
/// needs either a reference to Windows Forms - a <c>.csproj</c> change, and a whole second UI
/// framework loaded for one API - or <c>EnumDisplayMonitors</c> through P/Invoke, whose generated
/// marshalling wants <c>AllowUnsafeBlocks</c>, also a <c>.csproj</c> change. Step 3.1 may not edit
/// the project files, so it uses what <see cref="SystemParameters"/> gives.
/// </para>
/// <para>
/// What that costs, precisely: the guard catches a window restored onto a monitor that is no longer
/// attached, which is the case §G.1 exists for and the case users hit. It does not catch a window
/// parked in the empty corner of an L-shaped multi-monitor arrangement, because a bounding box has
/// no hole in it. Upgrading later is one class, and this interface is the seam for it.
/// </para>
/// <para>
/// Everything here is in device-independent units, which is what a WPF window's
/// <c>Left</c>/<c>Top</c>/<c>Width</c>/<c>Height</c> are measured in, so nothing has to know the
/// DPI. The Delphi stored raw pixels and had no DPI awareness at all.
/// </para>
/// </remarks>
public sealed class SystemMonitorLayout : IMonitorLayout
{
    /// <inheritdoc />
    public IReadOnlyList<Rect> WorkAreas =>
    [
        PrimaryWorkArea,
        new Rect(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight),
    ];

    /// <inheritdoc />
    public Rect PrimaryWorkArea => SystemParameters.WorkArea;
}
