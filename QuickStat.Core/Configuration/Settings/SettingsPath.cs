namespace QuickStat.Configuration.Settings;

/// <summary>
/// Where the settings file lives.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Everything here is anchored on the executable directory or on a shell folder, never on
/// the working directory</strong> (PORT-PLAN.md §4.1). QuickStat is launched from shortcuts, so
/// the working directory is whatever the shortcut says it is - frequently
/// <c>C:\Windows\System32</c>. The Delphi anchored on <c>ExtractFilePath(ParamStr(0))</c>
/// (<c>Emetra.Settings.IniFile.pas:174</c>), which is the executable directory, and this keeps that.
/// </para>
/// <para>
/// The anchor is <see cref="AppContext.BaseDirectory"/>, deliberately <em>not</em>
/// <c>Assembly.Location</c>: the latter is an empty string under single-file publish, which
/// PORT-PLAN.md §8.7 leaves on the table as a one-line deployment change. An empty anchor would
/// silently turn every path below into a relative one - which is exactly the working-directory bug
/// this class exists to avoid.
/// </para>
/// </remarks>
public static class SettingsPath
{
    /// <summary>The settings file name, in both locations.</summary>
    public const string FileName = "QuickStat.ini";

    /// <summary>
    /// The folder beside the executable that holds a portable settings file. Matches the Delphi's
    /// <c>&lt;exedir&gt;\Settings\</c> convention (<c>Emetra.Settings.IniFile.pas:253</c>).
    /// </summary>
    public const string PortableFolderName = "Settings";

    /// <summary>The roaming-profile folder, replacing the Delphi's <c>Emetra\Shared</c>.</summary>
    public const string RoamingFolderName = @"DIPS\QuickStat";

    /// <summary>
    /// The directory the running executable was loaded from, without a trailing separator.
    /// </summary>
    public static string ExecutableDirectory =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppContext.BaseDirectory));

    /// <summary>
    /// <c>&lt;exedir&gt;\Settings\QuickStat.ini</c> - the portable, xcopy-deployment location.
    /// </summary>
    /// <remarks>
    /// QuickStat is deployed by copying a folder into <c>.\bin</c> (PORT-PLAN.md §8.7), and an
    /// installation that wants its settings to travel with that folder - a locked-down terminal
    /// server, a USB stick, a test rig - creates this file. It is never created automatically; see
    /// <see cref="Resolve"/>.
    /// </remarks>
    public static string PortableFilePath =>
        Path.Combine(ExecutableDirectory, PortableFolderName, FileName);

    /// <summary>
    /// <c>%APPDATA%\DIPS\QuickStat\QuickStat.ini</c> - the per-user location, and the default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A deliberate change from <c>%APPDATA%\Emetra\Shared\&lt;guid&gt;.ini</c>
    /// (<c>Emetra.Settings.IniFile.pas:266-273</c>, <c>Docs/Port/01-data-access.md</c> §3.5).
    /// The GUID in the old name existed only to key an installation to a directory, which the
    /// Delphi needed because several FastTrak applications shared <c>Emetra\Shared</c>; nothing in
    /// the .NET application does.
    /// </para>
    /// <para>
    /// <c>%APPDATA%</c> is the roaming profile, matching the Delphi's <c>CSIDL_APPDATA</c>. If the
    /// shell cannot name it - which should not happen on Windows - this falls back to
    /// <see cref="PortableFilePath"/> rather than to a relative path.
    /// </para>
    /// </remarks>
    public static string RoamingFilePath
    {
        get
        {
            string roaming = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData,
                Environment.SpecialFolderOption.DoNotVerify);

            return string.IsNullOrEmpty(roaming)
                ? PortableFilePath
                : Path.Combine(roaming, RoamingFolderName, FileName);
        }
    }

    /// <summary>
    /// Picks the settings file to use.
    /// </summary>
    /// <returns>
    /// <see cref="PortableFilePath"/> when that file already exists, otherwise
    /// <see cref="RoamingFilePath"/>.
    /// </returns>
    /// <remarks>
    /// The portable file has to be created by hand, so its presence is an unambiguous statement of
    /// intent by whoever deployed the application; there is no way to end up in portable mode by
    /// accident. Everyone else gets the per-user file, which is created on the first
    /// <see cref="ISettingsStore.Flush"/>.
    /// </remarks>
    public static string Resolve() => File.Exists(PortableFilePath) ? PortableFilePath : RoamingFilePath;
}
