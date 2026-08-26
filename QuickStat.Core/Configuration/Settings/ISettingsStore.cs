namespace QuickStat.Configuration.Settings;

/// <summary>
/// Per-user persisted settings: window geometry, remembered periods, and the preferences the
/// Delphi never saved.
/// </summary>
/// <remarks>
/// <para>
/// Only the <c>ssUser</c> scope of the Delphi's three-file store survives
/// (<c>Emetra.Settings.IniFile.pas</c>). <c>ssGlobal</c> and <c>ssMachineUser</c> held nothing
/// QuickStat ever read, and the store also wrote <c>[Directory] RootDir</c>, <c>[Test] LastOpened</c>,
/// <c>[Test] WindowsUserName</c> and three <c>HKCU\Software\Emetra\QuickStat</c> registry values
/// into every file it opened. None of that is ported.
/// </para>
/// <para>
/// The reader/writer asymmetry of the original - <c>ReadDate</c> against <c>WriteDateTime</c> - is
/// gone, and so is the immediate-commit behaviour of <c>WritePrivateProfileString</c>: this store
/// buffers and needs <see cref="Flush"/>.
/// </para>
/// <para>
/// Note that packaged selections are <em>not</em> in here. They live server-side in
/// <c>Report.QuickStat</c>, which is why they are a repository under step 2.3
/// (<see cref="QuickStat.Domain.Packages.IPackageRepository"/>) and not a setting.
/// </para>
/// </remarks>
public interface ISettingsStore
{
    /// <summary>Whether a key exists.</summary>
    /// <param name="section">Section name.</param>
    /// <param name="key">Key name.</param>
    /// <returns><see langword="true"/> when present.</returns>
    bool Contains(string section, string key);

    /// <summary>Reads a string.</summary>
    /// <param name="section">Section name.</param>
    /// <param name="key">Key name.</param>
    /// <param name="defaultValue">Value when absent.</param>
    /// <returns>The stored value or <paramref name="defaultValue"/>.</returns>
    string GetString(string section, string key, string defaultValue = "");

    /// <summary>Reads an integer.</summary>
    /// <param name="section">Section name.</param>
    /// <param name="key">Key name.</param>
    /// <param name="defaultValue">Value when absent or unparsable.</param>
    /// <returns>The stored value or <paramref name="defaultValue"/>.</returns>
    int GetInt32(string section, string key, int defaultValue = 0);

    /// <summary>Reads a boolean.</summary>
    /// <param name="section">Section name.</param>
    /// <param name="key">Key name.</param>
    /// <param name="defaultValue">Value when absent or unparsable.</param>
    /// <returns>The stored value or <paramref name="defaultValue"/>.</returns>
    bool GetBoolean(string section, string key, bool defaultValue = false);

    /// <summary>Reads a double.</summary>
    /// <param name="section">Section name.</param>
    /// <param name="key">Key name.</param>
    /// <param name="defaultValue">Value when absent or unparsable.</param>
    /// <returns>The stored value or <paramref name="defaultValue"/>.</returns>
    double GetDouble(string section, string key, double defaultValue = 0);

    /// <summary>Reads a date and time.</summary>
    /// <param name="section">Section name.</param>
    /// <param name="key">Key name.</param>
    /// <param name="defaultValue">Value when absent or unparsable.</param>
    /// <returns>The stored value or <paramref name="defaultValue"/>.</returns>
    /// <remarks>
    /// Values are written round-trippable and culture-invariant; readers also tolerate the legacy
    /// <c>TIniFile</c> format so an inherited file still parses.
    /// </remarks>
    DateTime GetDateTime(string section, string key, DateTime defaultValue);

    /// <summary>Writes a string.</summary>
    /// <param name="section">Section name.</param>
    /// <param name="key">Key name.</param>
    /// <param name="value">Value to store.</param>
    void SetString(string section, string key, string value);

    /// <summary>Writes an integer.</summary>
    /// <param name="section">Section name.</param>
    /// <param name="key">Key name.</param>
    /// <param name="value">Value to store.</param>
    void SetInt32(string section, string key, int value);

    /// <summary>Writes a boolean.</summary>
    /// <param name="section">Section name.</param>
    /// <param name="key">Key name.</param>
    /// <param name="value">Value to store.</param>
    void SetBoolean(string section, string key, bool value);

    /// <summary>Writes a double.</summary>
    /// <param name="section">Section name.</param>
    /// <param name="key">Key name.</param>
    /// <param name="value">Value to store.</param>
    void SetDouble(string section, string key, double value);

    /// <summary>Writes a date and time.</summary>
    /// <param name="section">Section name.</param>
    /// <param name="key">Key name.</param>
    /// <param name="value">Value to store.</param>
    void SetDateTime(string section, string key, DateTime value);

    /// <summary>Removes a key. Absent keys are not an error.</summary>
    /// <param name="section">Section name.</param>
    /// <param name="key">Key name.</param>
    /// <remarks>The Delphi store had no delete at all, so a stale key could never be cleared.</remarks>
    void Remove(string section, string key);

    /// <summary>Commits buffered writes to disk. Must never throw.</summary>
    void Flush();
}
