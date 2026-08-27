using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using QuickStat.Diagnostics;

namespace QuickStat.Logging;

/// <summary>
/// A minimal, dependency-free <see cref="ILoggerProvider"/> that appends log lines to a text file
/// in a <c>LOGS</c> directory next to the executable.
/// </summary>
/// <remarks>
/// <para>
/// The directory is created if it is missing. The Delphi build never did this, so on a fresh
/// installation every single log line was silently discarded (PORT-PLAN.md §7.2). The path is
/// resolved from <see cref="AppContext.BaseDirectory"/> - never from
/// <see cref="Environment.CurrentDirectory"/> (wrong when launched from a shortcut) and never from
/// <c>Assembly.Location</c> (empty under single-file publish).
/// </para>
/// <para>
/// Writes are serialised behind a lock and each one opens, appends and closes the file, so a crash
/// - including a crash inside an unhandled-exception handler - cannot lose the last lines. The
/// volume this desktop application produces does not justify a buffered writer; if a later phase
/// needs one, that is the single place to change.
/// </para>
/// <para>
/// A logging call must never take the application down: every failure path here is swallowed.
/// </para>
/// </remarks>
[ProviderAlias("File")]
public sealed class FileLoggerProvider : ILoggerProvider
{
    /// <summary>Name of the log directory created beside the executable.</summary>
    public const string LogDirectoryName = "LOGS";

    /// <summary>Prefix of the generated log file names.</summary>
    public const string LogFilePrefix = "quickstat-";

    /// <summary>Extension of the generated log file names.</summary>
    public const string LogFileExtension = ".log";

    /// <summary>
    /// UTF-8 with a byte-order mark. The BOM is written only when the file is first created, and it
    /// is what makes Norwegian characters render correctly in Notepad and Excel.
    /// </summary>
    private static readonly Encoding Utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new(StringComparer.Ordinal);
    private readonly System.Threading.Lock _gate = new();
    private readonly string _logDirectory;

    private bool _disposed;

    /// <summary>
    /// Initialises a provider writing to <see cref="DefaultLogDirectory"/>.
    /// </summary>
    public FileLoggerProvider()
        : this(DefaultLogDirectory)
    {
    }

    /// <summary>
    /// Initialises a provider writing to an explicit directory. Used by tests.
    /// </summary>
    /// <param name="logDirectory">Directory the log files are written to. Created if missing.</param>
    public FileLoggerProvider(string logDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);

        _logDirectory = Path.GetFullPath(logDirectory);
        TryEnsureDirectory();
    }

    /// <summary>
    /// <c>&lt;executable directory&gt;\LOGS</c>.
    /// </summary>
    public static string DefaultLogDirectory => Path.Combine(AppContext.BaseDirectory, LogDirectoryName);

    /// <summary>
    /// The directory this provider writes to.
    /// </summary>
    public string LogDirectory => _logDirectory;

    /// <summary>
    /// The full path of the file the next write would land in. One file per calendar day, named
    /// <c>quickstat-yyyyMMdd.log</c>: a per-run file would litter the directory for an application
    /// users start and stop many times a day, and a single ever-growing file is what the Delphi
    /// build had.
    /// </summary>
    public string CurrentLogFilePath => BuildLogFilePath(DateTime.Now);

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName ?? string.Empty, static (name, provider) => new FileLogger(provider, name), this);

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
        }

        _loggers.Clear();
    }

    /// <summary>
    /// Appends one formatted entry. Never throws.
    /// </summary>
    internal void Write(LogLevel logLevel, string category, EventId eventId, string message, Exception? exception)
    {
        try
        {
            string line = Format(logLevel, category, eventId, message, exception);

            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                // Idempotent, and it recreates the directory if something removed it mid-session.
                Directory.CreateDirectory(_logDirectory);
                File.AppendAllText(BuildLogFilePath(DateTime.Now), line, Utf8WithBom);
            }
        }
        catch (Exception)
        {
            // A logger that throws is a classic source of unexplained crashes - especially during
            // shutdown, and especially from inside an unhandled-exception handler. Degrade silently.
        }
    }

    /// <summary>
    /// Builds one entry, with personal identifiers removed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Redaction happens here and nowhere else on this path</b>, because this is the last point
    /// before bytes reach the disk: no call site can route around it, and no future caller has to
    /// remember it. <c>Docs/Port/01-data-access.md</c> §7.5 asks for exactly this - "apply
    /// <c>ForLog</c> in the logger provider" - and until Phase 5 checked, it was the one write path
    /// that did not, even though <see cref="PiiRedactor"/>'s own summary claims every write path
    /// does. Only <c>UserNotifier</c> and <c>IniSettingsStore</c> called it, so anything logged
    /// through <see cref="ILogger"/> directly reached the file in the clear - including a
    /// fødselsnummer that a call site never realised it was holding, which is precisely the case
    /// <see cref="PiiRedactor"/> detects structurally rather than by convention. R6, and
    /// release-blocking.
    /// </para>
    /// <para>
    /// The message goes through <see cref="PiiRedactor.ForLog"/>, which also folds it onto one line -
    /// matching the Delphi's <c>AnonymizeLogMessage</c>, and incidentally denying a caller the
    /// ability to forge log entries by embedding a newline. The exception goes through
    /// <see cref="PiiRedactor.Redact"/> instead: it is deliberately written as a multi-line block
    /// below the entry, and collapsing a stack trace onto one line would destroy the only thing it
    /// is for. It is redacted all the same, because an exception message quotes its inputs.
    /// </para>
    /// </remarks>
    /// <param name="logLevel">Severity.</param>
    /// <param name="category">Logger category; a type name, never personal.</param>
    /// <param name="eventId">Event id, omitted when zero.</param>
    /// <param name="message">The formatted message, not yet redacted.</param>
    /// <param name="exception">The exception, if any, not yet redacted.</param>
    /// <returns>The line, or lines, to append.</returns>
    private static string Format(LogLevel logLevel, string category, EventId eventId, string message, Exception? exception)
    {
        message = PiiRedactor.ForLog(message);

        StringBuilder builder = new(message.Length + 96);

        builder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))
               .Append(" [").Append(Abbreviate(logLevel)).Append(']')
               .Append(" [T").Append(Environment.CurrentManagedThreadId.ToString("00", CultureInfo.InvariantCulture)).Append(']')
               .Append(' ').Append(category);

        if (eventId.Id != 0)
        {
            builder.Append('(').Append(eventId.Id.ToString(CultureInfo.InvariantCulture)).Append(')');
        }

        builder.Append(": ").Append(message).Append(Environment.NewLine);

        if (exception is not null)
        {
            builder.Append(PiiRedactor.Redact(exception.ToString())).Append(Environment.NewLine);
        }

        return builder.ToString();
    }

    private static string Abbreviate(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Critical => "CRT",
        _ => "NON",
    };

    private string BuildLogFilePath(DateTime timestamp) =>
        Path.Combine(
            _logDirectory,
            string.Concat(LogFilePrefix, timestamp.ToString("yyyyMMdd", CultureInfo.InvariantCulture), LogFileExtension));

    private void TryEnsureDirectory()
    {
        try
        {
            Directory.CreateDirectory(_logDirectory);
        }
        catch (Exception)
        {
            // No log directory means no logging, not a failed start-up.
        }
    }
}
