namespace QuickStat.Services;

/// <summary>Starts something on the file the port has just written.</summary>
/// <remarks>
/// <para>
/// Behind an interface for one reason: <c>Open this dataset in Excel</c> would otherwise start
/// Excel during a unit test.
/// </para>
/// <para>
/// Delphi: <c>TMrLauncher</c> and <c>TExcelAdapter.LoadWithFile</c>, which pump messages in a
/// <c>Sleep(50)</c> loop until Excel exits (PORT-PLAN.md §7.3). The port hands the file over and
/// returns; the user is not waiting on a spreadsheet.
/// </para>
/// </remarks>
public interface IProcessLauncher
{
    /// <summary>Opens a file with its registered application.</summary>
    /// <param name="path">Full path to an existing file.</param>
    /// <exception cref="ArgumentException"><paramref name="path"/> is <see langword="null"/> or empty.</exception>
    /// <remarks>
    /// Throws whatever the shell throws when nothing is registered for the extension - which for
    /// <c>.csv</c> on a machine without Excel is a real possibility, and is the caller's to report.
    /// </remarks>
    void OpenWithShell(string path);

    /// <summary>Opens a file in Excel, rather than in whatever owns its extension.</summary>
    /// <param name="path">Full path to an existing file.</param>
    /// <exception cref="ArgumentException"><paramref name="path"/> is <see langword="null"/> or empty.</exception>
    /// <remarks>
    /// <para>
    /// Distinct from <see cref="OpenWithShell"/> because the two are genuinely different requests. A
    /// menu item reading <c>Open this dataset in Excel</c> that opens Notepad has not done what it
    /// says, and <c>.csv</c> is an extension users hand to editors all the time.
    /// </para>
    /// <para>
    /// Falls back to <see cref="OpenWithShell"/> when Excel is not installed: that is the best
    /// available answer, and it is the behaviour the port had for every machine.
    /// </para>
    /// </remarks>
    void OpenInExcel(string path);
}
