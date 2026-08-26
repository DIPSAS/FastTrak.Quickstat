using System.Windows;

namespace QuickStat.Services;

/// <summary>The desktop's usable area, in device-independent units.</summary>
/// <remarks>
/// Exists so the off-screen guard rail in <see cref="WindowStateService"/> can be tested without a
/// display, which is the only part of window-state persistence with any logic in it.
/// </remarks>
public interface IMonitorLayout
{
    /// <summary>
    /// The rectangles a restored window is allowed to overlap. Never empty.
    /// </summary>
    /// <remarks>
    /// See <see cref="SystemMonitorLayout"/> for what the WPF implementation can and cannot see.
    /// </remarks>
    IReadOnlyList<Rect> WorkAreas { get; }

    /// <summary>Where a window goes when the stored rectangle is unusable.</summary>
    /// <remarks>The primary monitor's work area - Delphi <c>Screen.WorkareaRect</c>.</remarks>
    Rect PrimaryWorkArea { get; }
}
