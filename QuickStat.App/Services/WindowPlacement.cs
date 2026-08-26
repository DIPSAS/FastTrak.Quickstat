using System.Windows;

namespace QuickStat.Services;

/// <summary>A window's remembered geometry: its state, and its bounds when that state is Normal.</summary>
/// <param name="State">
/// Normal, Minimized or Maximized. Saved and restored unconditionally.
/// </param>
/// <param name="Bounds">
/// The restore rectangle, or <see langword="null"/> when the window was not Normal.
/// </param>
/// <remarks>
/// <para>
/// <c>05-ui-spec.md</c> §G.1. The Delphi's <c>TWindowState</c> and WPF's
/// <see cref="System.Windows.WindowState"/> happen to agree member for member - 0 Normal,
/// 1 Minimized, 2 Maximized - so a file written by either build is readable by the other. That is
/// luck rather than design, which is why <see cref="WindowStateService"/> maps by enum and not by
/// cast.
/// </para>
/// <para>
/// Bounds are only meaningful for <see cref="System.Windows.WindowState.Normal"/>: the Delphi writes
/// <c>Left</c>/<c>Top</c>/<c>Width</c>/<c>Height</c> only in that case and ignores them on restore
/// otherwise, so a window closed maximised reopens maximised over whatever the previous normal
/// bounds were.
/// </para>
/// </remarks>
public readonly record struct WindowPlacement(WindowState State, Rect? Bounds);
