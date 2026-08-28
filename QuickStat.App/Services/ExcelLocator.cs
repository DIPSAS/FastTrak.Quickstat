using System.IO;
using System.Security;
using Microsoft.Win32;

namespace QuickStat.Services;

/// <summary>Finds <c>EXCEL.EXE</c> on this machine, the way the Delphi's <c>TExcelAdapter</c> does.</summary>
/// <remarks>
/// <para>
/// <b>Why this exists at all.</b> <c>Open this dataset in Excel</c> used to hand the temporary CSV to
/// <c>ShellExecute</c>, which opens it in whatever is registered for <c>.csv</c> - Notepad, the
/// editor somebody installed last week, anything. The Delphi does not do that: <c>TExcelAdapter</c>
/// (<c>FastTrak/Emetra.Adapters.Office.pas</c>) resolves Excel's COM registration to a real path and
/// starts <b>that</b> executable with the file as its argument, so the menu item means what it says.
/// Reported from the field during the Phase 5 parity pass.
/// </para>
/// <para>
/// <b>Why the Delphi's own parsing could not just be transcribed.</b> It splits the
/// <c>LocalServer32</c> command on a space and takes token 0. That works there because a 32-bit
/// process reads the <c>WOW6432Node</c> view, where the value happens to be quoted:
/// <c>"C:\Program Files\...\EXCEL.EXE" /automation</c>. A 64-bit process reads the 64-bit view, and
/// Office writes that one <em>unquoted</em> - <c>C:\Program Files\...\EXCEL.EXE /automation</c> -
/// where splitting on a space yields <c>C:\Program</c>. Both were measured on the development
/// machine. See <see cref="ParseLocalServerCommand"/>.
/// </para>
/// <para>
/// Nothing here throws for the caller to handle: an answer of <see langword="null"/> means "not
/// found", and <see cref="ShellProcessLauncher"/> falls back to the shell, which is the right
/// behaviour on a machine with no Excel.
/// </para>
/// </remarks>
internal static class ExcelLocator
{
    /// <summary>ProgId → CLSID. The Delphi reads exactly this key.</summary>
    private const string ProgIdKey = @"Software\Classes\Excel.Application\CLSID";

    /// <summary>The canonical Windows answer, kept as a fallback for a broken COM registration.</summary>
    private const string AppPathsKey = @"Software\Microsoft\Windows\CurrentVersion\App Paths\excel.exe";

    private const string Exe = ".exe";

    /// <summary>Locates Excel.</summary>
    /// <returns>The full path to an existing <c>EXCEL.EXE</c>, or <see langword="null"/>.</returns>
    /// <remarks>
    /// Both registry views, because which one holds the registration depends on Office's bitness and
    /// not on ours, and the COM route before <c>App Paths</c> because that is the Delphi's.
    /// </remarks>
    internal static string? Find() =>
        FromComRegistration(RegistryView.Registry64)
        ?? FromComRegistration(RegistryView.Registry32)
        ?? FromAppPaths(RegistryView.Registry64)
        ?? FromAppPaths(RegistryView.Registry32);

    /// <summary>Pulls the executable out of a <c>LocalServer32</c> command line.</summary>
    /// <param name="command">The raw registry value, e.g. <c>C:\...\EXCEL.EXE /automation</c>.</param>
    /// <returns>The path, or <see langword="null"/> when the value is not one.</returns>
    /// <remarks>
    /// <para>
    /// The value is a command line for <c>CreateProcess</c>, so it is either a quoted path followed
    /// by switches or a bare path followed by switches. Quoted is unambiguous. Unquoted is not, in
    /// general - but the first token that <em>ends</em> in <c>.exe</c> at a word boundary is the
    /// executable, and that is enough for every shape Office writes, short 8.3 paths included.
    /// </para>
    /// <para>
    /// Internal rather than private so the shapes can be pinned as data. Starting Excel is not
    /// something a test may do, and the registry on a build agent is not something a test may
    /// assume, so this is the only part of the lookup that can be asserted properly.
    /// </para>
    /// </remarks>
    internal static string? ParseLocalServerCommand(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return null;
        }

        string text = command.Trim();

        if (text[0] == '"')
        {
            int closing = text.IndexOf('"', 1);

            return closing > 1 ? text[1..closing] : null;
        }

        for (int at = text.IndexOf(Exe, StringComparison.OrdinalIgnoreCase);
             at >= 0;
             at = text.IndexOf(Exe, at + 1, StringComparison.OrdinalIgnoreCase))
        {
            int end = at + Exe.Length;

            if (end == text.Length || char.IsWhiteSpace(text[end]))
            {
                return text[..end];
            }
        }

        return null;
    }

    /// <summary><c>Excel.Application</c> → <c>CLSID</c> → <c>LocalServer32</c>. The Delphi's route.</summary>
    /// <param name="view">Which of the two registry views to read.</param>
    /// <returns>The path, or <see langword="null"/>.</returns>
    private static string? FromComRegistration(RegistryView view) =>
        Read(view, ProgIdKey) is { Length: > 0 } clsid
            ? Verify(ParseLocalServerCommand(Read(view, $@"Software\Classes\CLSID\{clsid}\LocalServer32")))
            : null;

    /// <summary><c>App Paths\excel.exe</c>, whose default value is the path and nothing else.</summary>
    /// <param name="view">Which of the two registry views to read.</param>
    /// <returns>The path, or <see langword="null"/>.</returns>
    private static string? FromAppPaths(RegistryView view) => Verify(Read(view, AppPathsKey)?.Trim('"'));

    /// <summary>Reads a key's default value under <c>HKEY_LOCAL_MACHINE</c>.</summary>
    /// <param name="view">Which registry view.</param>
    /// <param name="path">The key.</param>
    /// <returns>The value, or <see langword="null"/> when it is absent or unreadable.</returns>
    /// <remarks>
    /// A locked-down machine can refuse the read. That is a "no Excel here" answer like any other,
    /// not something to propagate out of a menu click.
    /// </remarks>
    private static string? Read(RegistryView view, string path)
    {
        try
        {
            using RegistryKey machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            using RegistryKey? key = machine.OpenSubKey(path);

            return key?.GetValue(null) as string;
        }
        catch (SecurityException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <summary>Drops a path that no longer exists.</summary>
    /// <param name="path">A candidate, or <see langword="null"/>.</param>
    /// <returns><paramref name="path"/> when there is a file there.</returns>
    /// <remarks>
    /// An uninstalled Office routinely leaves its registration behind, and starting a path that is
    /// not there would fail where handing the file to the shell would have worked.
    /// </remarks>
    private static string? Verify(string? path) =>
        !string.IsNullOrWhiteSpace(path) && File.Exists(path) ? path : null;
}
