using System.Diagnostics;

namespace QuickStat.Services;

/// <summary>The real <see cref="IProcessLauncher"/>: <c>ShellExecute</c>, and nothing else.</summary>
public sealed class ShellProcessLauncher : IProcessLauncher
{
    /// <inheritdoc />
    public void OpenWithShell(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        // UseShellExecute is what makes the file open in its registered application rather than
        // being treated as an executable; it is false by default on .NET.
        ProcessStartInfo startInfo = new(path)
        {
            UseShellExecute = true,
        };

        using Process? process = Process.Start(startInfo);

        // Nothing waits for the process.  The Delphi's Sleep(50) message-pump loop froze QuickStat
        // for as long as the spreadsheet stayed open (PORT-PLAN.md §7.3).
        _ = process;
    }
}
