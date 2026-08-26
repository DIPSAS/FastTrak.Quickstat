using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Diagnostics;

namespace QuickStat.Configuration.Settings;

/// <summary>
/// An <see cref="ISettingsStore"/> over a single INI-shaped file, read once on construction, held in
/// memory, and written back atomically on <see cref="Flush"/>.
/// </summary>
/// <remarks>
/// <para>
/// Replaces <c>TIniSettings</c> (<c>Emetra.Settings.IniFile.pas</c>) and drops everything that class
/// did besides storing settings: the <c>ssGlobal</c> and <c>ssMachineUser</c> files, which QuickStat
/// never read; the GUID-named file, which existed to key an installation to a directory; the
/// <c>[Directory] RootDir</c>, <c>[Test] LastOpened</c> and <c>[Test] WindowsUserName</c> keys it
/// wrote into every file it opened; and the three <c>HKCU\Software\Emetra\QuickStat</c> registry
/// values (<c>:277-291</c>, <c>:311-322</c>). See <c>Docs/Port/01-data-access.md</c> §3.5.
/// </para>
/// <para>
/// <strong>Personal identifiers are removed on the way in, always.</strong> Every public write
/// funnels through one private method that redacts the section, the key and the formatted value
/// before any of them reaches the dictionary - so redaction cannot be bypassed by choosing a
/// different overload, and there is no flag that turns it off. Reads redact their section and key
/// arguments the same way, so a lookup still finds what the matching write stored, and
/// <em>loading</em> redacts too, so an inherited or hand-edited file cannot reintroduce one. See
/// <see cref="PiiRedactor"/> for what counts as an identifier and why.
/// </para>
/// <para>
/// <strong>Nothing is committed until <see cref="Flush"/>.</strong> The Delphi wrote through
/// <c>WritePrivateProfileString</c>, which commits on every call - one file write per settings
/// change, and a half-written file if the process died mid-sequence. This buffers, and
/// <see cref="Flush"/> writes to a temporary file in the same directory and then replaces the real
/// one, so an interrupted flush leaves the previous settings intact rather than a truncated file.
/// </para>
/// <para>
/// Instances are safe to use from several threads.
/// </para>
/// </remarks>
public sealed class IniSettingsStore : ISettingsStore, IDisposable
{
    private const string TemporaryFileSuffix = ".tmp";

    /// <summary>
    /// Every rendering of a date this store will read: the one it writes, a few obvious hand-edits,
    /// and what the Delphi would have written on a Norwegian or an American machine.
    /// </summary>
    /// <remarks>
    /// Deliberately an exhaustive list of exact formats rather than a free-form
    /// <see cref="DateTime.TryParse(string, IFormatProvider, DateTimeStyles, out DateTime)"/>.
    /// Free-form parsing is far too eager: against the invariant culture it reads the string
    /// <c>3.5</c> as the fifth of March, so a value that is plainly not a date comes back as one and
    /// the caller never sees its default. Against the current culture it would also make the store
    /// behave differently on a developer's machine, a build agent and a hospital desktop.
    /// </remarks>
    private static readonly string[] AcceptedDateFormats =
    [
        "o",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
        "yyyy-MM-ddTHH:mm:ssK",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm",
        "yyyy-MM-dd",
        "dd.MM.yyyy HH:mm:ss",
        "dd.MM.yyyy HH:mm",
        "dd.MM.yyyy",
        "M/d/yyyy h:mm:ss tt",
        "M/d/yyyy H:mm:ss",
        "M/d/yyyy",
    ];

    private readonly ILogger _logger;
    private readonly Lock _gate = new();

    private readonly OrderedDictionary<string, OrderedDictionary<string, string>> _sections =
        new(StringComparer.OrdinalIgnoreCase);

    private bool _dirty;
    private bool _disposed;

    /// <summary>Opens - or prepares to create - a settings file at a specific path.</summary>
    /// <param name="filePath">Where the file lives. Relative paths are resolved against the process working directory, so callers should pass an absolute one; <see cref="OpenDefault"/> always does.</param>
    /// <param name="logger">Where load and save problems are reported, or <see langword="null"/> for none.</param>
    /// <exception cref="ArgumentException"><paramref name="filePath"/> is <see langword="null"/> or empty.</exception>
    /// <remarks>
    /// A missing file is not an error - the store starts empty, exactly as the Delphi's
    /// <c>TIniFile</c> did. Neither is an unreadable or malformed one: what can be parsed is loaded,
    /// the rest is counted in <see cref="SkippedLineCount"/> and logged. Construction never throws
    /// on file content.
    /// </remarks>
    public IniSettingsStore(string filePath, ILogger<IniSettingsStore>? logger = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        _logger = logger ?? (ILogger)NullLogger<IniSettingsStore>.Instance;
        FilePath = Path.GetFullPath(filePath);

        Load();
    }

    /// <summary>The absolute path of the file this store reads and writes.</summary>
    public string FilePath { get; }

    /// <summary>How many lines of the file could not be understood and were skipped.</summary>
    /// <remarks>Zero for a file this store wrote. Non-zero means the file was edited by hand or truncated.</remarks>
    public int SkippedLineCount { get; private set; }

    /// <summary>Whether there are buffered changes that <see cref="Flush"/> would write.</summary>
    public bool HasUnsavedChanges
    {
        get
        {
            lock (_gate)
            {
                return _dirty;
            }
        }
    }

    /// <summary>Opens the settings file for the current user, at the path <see cref="SettingsPath.Resolve"/> picks.</summary>
    /// <param name="logger">Where load and save problems are reported, or <see langword="null"/> for none.</param>
    /// <returns>A store over the resolved file.</returns>
    public static IniSettingsStore OpenDefault(ILogger<IniSettingsStore>? logger = null)
        => new(SettingsPath.Resolve(), logger);

    /// <inheritdoc />
    public bool Contains(string section, string key)
    {
        ValidateNames(section, key);

        lock (_gate)
        {
            return TryFind(section, key, out _);
        }
    }

    /// <inheritdoc />
    public string GetString(string section, string key, string defaultValue = "")
    {
        ValidateNames(section, key);

        lock (_gate)
        {
            return TryFind(section, key, out string? stored) ? stored : defaultValue;
        }
    }

    /// <inheritdoc />
    public int GetInt32(string section, string key, int defaultValue = 0)
    {
        string text = GetString(section, key);

        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : defaultValue;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Written as <c>True</c> or <c>False</c>; read back from those, and from the <c>1</c> and
    /// <c>0</c> that Delphi's <c>TIniFile.WriteBool</c> produced, so an inherited file still parses.
    /// </remarks>
    public bool GetBoolean(string section, string key, bool defaultValue = false)
    {
        string text = GetString(section, key);

        if (bool.TryParse(text, out bool parsed))
        {
            return parsed;
        }

        return text switch
        {
            "1" => true,
            "0" => false,
            _ => defaultValue,
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// Written and read in <see cref="CultureInfo.InvariantCulture"/>. A value that fails to parse
    /// there is retried with a comma decimal separator, because Delphi's <c>WriteFloat</c> used the
    /// thread locale and would have written <c>3,14</c> on a Norwegian machine.
    /// </remarks>
    public double GetDouble(string section, string key, double defaultValue = 0)
    {
        string text = GetString(section, key);

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            return parsed;
        }

        if (text.Contains(',', StringComparison.Ordinal)
            && !text.Contains('.', StringComparison.Ordinal)
            && double.TryParse(
                text.Replace(',', '.'),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double legacy))
        {
            return legacy;
        }

        return defaultValue;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Written with the round-trip <c>o</c> format, which preserves the sub-second component and the
    /// <see cref="DateTimeKind"/>. Read back from that and from the fixed list in
    /// <see cref="AcceptedDateFormats"/>, which includes what Delphi's <c>WriteDateTime</c> would
    /// have produced - it formatted with the thread locale, forced to the user default at
    /// <c>Emetra.Settings.IniFile.pas:495-496</c>.
    /// </remarks>
    public DateTime GetDateTime(string section, string key, DateTime defaultValue)
    {
        string text = GetString(section, key);

        if (text.Length == 0)
        {
            return defaultValue;
        }

        return DateTime.TryParseExact(
            text,
            AcceptedDateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out DateTime parsed)
            ? parsed
            : defaultValue;
    }

    /// <inheritdoc />
    public void SetString(string section, string key, string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        Store(section, key, value);
    }

    /// <inheritdoc />
    public void SetInt32(string section, string key, int value)
        => Store(section, key, value.ToString(CultureInfo.InvariantCulture));

    /// <inheritdoc />
    public void SetBoolean(string section, string key, bool value)
        => Store(section, key, value ? bool.TrueString : bool.FalseString);

    /// <inheritdoc />
    public void SetDouble(string section, string key, double value)
        => Store(section, key, value.ToString("R", CultureInfo.InvariantCulture));

    /// <inheritdoc />
    public void SetDateTime(string section, string key, DateTime value)
        => Store(section, key, value.ToString("o", CultureInfo.InvariantCulture));

    /// <inheritdoc />
    /// <remarks>
    /// A section left with no keys is removed too, so deleting the last setting of a window leaves
    /// no empty header behind. The Delphi had no delete at all, which is why a key written once -
    /// such as a period under a query that has since been edited - could never be cleared.
    /// </remarks>
    public void Remove(string section, string key)
    {
        ValidateNames(section, key);

        string redactedSection = PiiRedactor.Redact(section);
        string redactedKey = PiiRedactor.Redact(key);

        lock (_gate)
        {
            if (!_sections.TryGetValue(redactedSection, out OrderedDictionary<string, string>? entries))
            {
                return;
            }

            if (!entries.Remove(redactedKey))
            {
                return;
            }

            if (entries.Count == 0)
            {
                _sections.Remove(redactedSection);
            }

            _dirty = true;
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        lock (_gate)
        {
            if (!_dirty)
            {
                return;
            }

            try
            {
                Save();

                _dirty = false;
            }
            catch (Exception exception)
            {
                // Contract: Flush must never throw. Losing a window position is an annoyance;
                // taking the application down while saving one is not acceptable.
                _logger.LogError(exception, "Could not write settings to {SettingsFile}.", FilePath);
            }
        }
    }

    /// <summary>Flushes buffered changes. Called by the container when the host shuts down.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        Flush();
    }

    private static void ValidateNames(string section, string key)
    {
        // An empty section is the file's unnamed leading block, which is legitimate. An empty key is
        // not addressable and would produce a line the reader rejects.
        ArgumentNullException.ThrowIfNull(section);
        ArgumentException.ThrowIfNullOrEmpty(key);
    }

    /// <summary>
    /// The single write path. Every <c>Set*</c> overload ends here, which is what makes redaction
    /// unavoidable rather than merely usual.
    /// </summary>
    private void Store(string section, string key, string formattedValue)
    {
        ValidateNames(section, key);

        string redactedSection = PiiRedactor.Redact(section);
        string redactedKey = PiiRedactor.Redact(key);
        string redactedValue = PiiRedactor.Redact(formattedValue);

        lock (_gate)
        {
            if (!_sections.TryGetValue(redactedSection, out OrderedDictionary<string, string>? entries))
            {
                entries = new OrderedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _sections[redactedSection] = entries;
            }

            if (entries.TryGetValue(redactedKey, out string? existing)
                && string.Equals(existing, redactedValue, StringComparison.Ordinal))
            {
                return;
            }

            entries[redactedKey] = redactedValue;
            _dirty = true;
        }
    }

    private bool TryFind(string section, string key, out string value)
    {
        if (_sections.TryGetValue(PiiRedactor.Redact(section), out OrderedDictionary<string, string>? entries)
            && entries.TryGetValue(PiiRedactor.Redact(key), out string? found))
        {
            value = found;

            return true;
        }

        value = string.Empty;

        return false;
    }

    private void Load()
    {
        string[] lines;

        try
        {
            if (!File.Exists(FilePath))
            {
                return;
            }

            lines = File.ReadAllLines(FilePath, Encoding.UTF8);
        }
        catch (Exception exception)
        {
            // A locked, deleted-between-checks or permission-denied file means "no settings yet",
            // not "the application cannot start".
            _logger.LogWarning(exception, "Could not read settings from {SettingsFile}.", FilePath);

            return;
        }

        OrderedDictionary<string, string> current = SectionFor(string.Empty);
        int skipped = 0;
        bool redactedSomething = false;

        foreach (string line in lines)
        {
            switch (IniFileFormat.ParseLine(line, out string section, out string key, out string value))
            {
                case IniLineKind.Section:
                    {
                        string redacted = PiiRedactor.Redact(section);

                        redactedSomething |= !string.Equals(redacted, section, StringComparison.Ordinal);
                        current = SectionFor(redacted);
                    }

                    break;

                case IniLineKind.Entry:
                    {
                        string redactedKey = PiiRedactor.Redact(key);
                        string redactedValue = PiiRedactor.Redact(value);

                        redactedSomething |= !string.Equals(redactedKey, key, StringComparison.Ordinal)
                            || !string.Equals(redactedValue, value, StringComparison.Ordinal);

                        current[redactedKey] = redactedValue;
                    }

                    break;

                case IniLineKind.Unparsable:
                    skipped++;

                    break;

                case IniLineKind.Blank:
                case IniLineKind.Comment:
                default:
                    break;
            }
        }

        // The unnamed leading block is only kept if something was actually written into it.
        if (_sections.TryGetValue(string.Empty, out OrderedDictionary<string, string>? unnamed)
            && unnamed.Count == 0)
        {
            _sections.Remove(string.Empty);
        }

        SkippedLineCount = skipped;

        if (skipped > 0)
        {
            _logger.LogWarning(
                "Skipped {SkippedLineCount} unreadable line(s) in {SettingsFile}.",
                skipped,
                FilePath);
        }

        if (redactedSomething)
        {
            // An inherited or hand-edited file. Redacting on load - not only on write - is what
            // makes the invariant "this store never holds an unredacted identifier" true of memory
            // as well as of disk, and marking the store dirty means the file itself is cleaned up
            // at the next flush rather than carrying the identifier forward for ever.
            _dirty = true;

            _logger.LogWarning(
                "Removed personal identifiers found in {SettingsFile}; they will be cleared on the next save.",
                FilePath);
        }
    }

    private OrderedDictionary<string, string> SectionFor(string name)
    {
        if (!_sections.TryGetValue(name, out OrderedDictionary<string, string>? entries))
        {
            entries = new OrderedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _sections[name] = entries;
        }

        return entries;
    }

    private void Save()
    {
        string? directory = Path.GetDirectoryName(FilePath);

        if (!string.IsNullOrEmpty(directory))
        {
            // The Delphi never created its log directory and lost every log line as a result
            // (PORT-PLAN.md §7.2). The same mistake is available here; do not make it.
            Directory.CreateDirectory(directory);
        }

        StringBuilder builder = new();

        foreach (string header in IniFileFormat.HeaderLines)
        {
            builder.Append(header).Append("\r\n");
        }

        foreach ((string section, OrderedDictionary<string, string> entries) in _sections)
        {
            builder.Append("\r\n").Append('[').Append(IniFileFormat.EscapeSection(section)).Append(']').Append("\r\n");

            foreach ((string key, string value) in entries)
            {
                builder
                    .Append(IniFileFormat.EscapeKey(key))
                    .Append('=')
                    .Append(IniFileFormat.EscapeValue(value))
                    .Append("\r\n");
            }
        }

        string temporaryPath = FilePath + TemporaryFileSuffix;

        try
        {
            // UTF-8 without a BOM: this file is never read through WritePrivateProfileString, which
            // is the only reader that would have needed one, and a BOM in front of the first section
            // header trips up every naive INI parser that might look at it later.
            File.WriteAllText(
                temporaryPath,
                builder.ToString(),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            File.Move(temporaryPath, FilePath, overwrite: true);
        }
        catch
        {
            TryDeleteTemporaryFile(temporaryPath);

            throw;
        }
    }

    private void TryDeleteTemporaryFile(string temporaryPath)
    {
        try
        {
            File.Delete(temporaryPath);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Could not remove the temporary file {TemporaryFile}.", temporaryPath);
        }
    }
}
