using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace QuickStat.Services;

/// <summary>The real <see cref="IProcessLauncher"/>: <c>ShellExecute</c>, or Excel by name.</summary>
/// <param name="logger">Log. Which Excel was found, or that none was, is the only thing worth saying.</param>
public sealed class ShellProcessLauncher(ILogger<ShellProcessLauncher> logger) : IProcessLauncher
{
    private readonly ILogger<ShellProcessLauncher> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

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

    /// <inheritdoc />
    public void OpenInExcel(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (ExcelLocator.Find() is not { } excel)
        {
            // Not an error: a machine may simply not have Excel, and the shell is then the best
            // answer there is.  Logged, because the alternative is the field report this method
            // exists to answer - "it opened the wrong application and there was no way to tell why".
            _logger.LogInformation(
                "Excel is not registered on this machine; handing {Path} to the shell instead.",
                path);

            OpenWithShell(path);

            return;
        }

        _logger.LogDebug("Opening {Path} in {Excel}.", path, excel);

        // ArgumentList rather than a hand-quoted Arguments string: %TEMP% lives under the user's
        // profile, so a space in the path is ordinary rather than exotic.
        ProcessStartInfo startInfo = new(excel)
        {
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add(path);

        using Process? process = Process.Start(startInfo);

        _ = process;
    }
}
