namespace QuickStat.Services;

/// <summary>Hands a file to whatever the shell says should open it.</summary>
/// <remarks>
/// <para>
/// Behind an interface for one reason: <c>Open this dataset in Excel</c> would otherwise start
/// Excel during a unit test.
/// </para>
/// <para>
/// Delphi: <c>TExcelAdapter.LoadWithFile</c>, which pumps messages in a <c>Sleep(50)</c> loop until
/// Excel exits (PORT-PLAN.md §7.3). The port hands the file over and returns; the user is not
/// waiting on a spreadsheet.
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
}
