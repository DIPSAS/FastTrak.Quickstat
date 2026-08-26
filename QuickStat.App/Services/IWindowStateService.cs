using System.Windows;

namespace QuickStat.Services;

/// <summary>Reads and writes the shell's persisted geometry and the two additions to it.</summary>
/// <remarks>
/// <para>
/// <c>05-ui-spec.md</c> §G.1. The Delphi persists the window state and, when Normal, the bounds -
/// keyed <em>per screen resolution</em>, so a laptop docked to a 4K monitor keeps a separate
/// geometry.
/// </para>
/// <para>
/// The splitter position and the last-used database are <b>additions</b>, not parity. §G.1
/// recommends both explicitly, and both are cheap; they are flagged here and in
/// <c>Docs/Port/07-ui-contracts.md</c> rather than slipped in silently. Everything else §G.1 lists
/// as unpersisted stays unpersisted: the selected tabs, the check-list selection, <c>Frequently used
/// only</c>, <c>Simplified</c>, <c>Wide columns</c>, <c>Show data hint</c> and the identification
/// mode all start at their defaults on every run, as they do today.
/// </para>
/// </remarks>
public interface IWindowStateService
{
    /// <summary>The geometry to open with, or <see langword="null"/> when nothing is stored.</summary>
    /// <param name="defaultSize">
    /// The window's designed size, used when the stored section has bounds keys missing.
    /// </param>
    /// <returns>
    /// The placement, already passed through the off-screen guard rail, or <see langword="null"/>
    /// for "nothing stored - leave the window where XAML put it".
    /// </returns>
    /// <remarks>
    /// Returning <see langword="null"/> rather than a default rectangle is a small, deliberate
    /// improvement: the Delphi reads <c>Left</c> and <c>Top</c> with a default of <b>0</b>, so a
    /// first run - or any run after the ini is deleted - opens the window hard against the top-left
    /// corner of the primary monitor. A null lets the shell keep
    /// <c>WindowStartupLocation="CenterScreen"</c>.
    /// </remarks>
    WindowPlacement? Restore(Size defaultSize);

    /// <summary>Stores the geometry.</summary>
    /// <param name="placement">
    /// The window's state and, when that state is Normal, its restore bounds.
    /// </param>
    /// <remarks>
    /// Writes <c>State</c> always and the four bounds keys only when Normal, exactly as
    /// <c>TGuiSettings.SaveFormState</c> does. Does not flush; the shell flushes once on close.
    /// </remarks>
    void Save(WindowPlacement placement);

    /// <summary>The stored splitter position, or <paramref name="defaultPosition"/>. <b>Addition.</b></summary>
    /// <param name="defaultPosition">Value when nothing is stored. The Delphi design value is 293.</param>
    /// <returns>The left pane's width in device-independent units.</returns>
    double GetSplitterPosition(double defaultPosition);

    /// <summary>Stores the splitter position. <b>Addition.</b></summary>
    /// <param name="position">The left pane's width.</param>
    void SetSplitterPosition(double position);

    /// <summary>The name of the connection selected last time, or <see langword="null"/>. <b>Addition.</b></summary>
    /// <returns>
    /// A <c>&lt;Connection&gt;&lt;Name&gt;</c> from <c>QuickStat.config.xml</c>. The caller must
    /// still check that it is in the current catalogue - the file can change between runs.
    /// </returns>
    string? GetLastDatabase();

    /// <summary>Stores the selected connection's name. <b>Addition.</b></summary>
    /// <param name="name">The connection name, or <see langword="null"/> to forget it.</param>
    void SetLastDatabase(string? name);

    /// <summary>Commits everything written so far. Must never throw.</summary>
    void Flush();
}
