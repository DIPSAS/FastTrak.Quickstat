using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

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

    private static string Format(LogLevel logLevel, string category, EventId eventId, string message, Exception? exception)
    {
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
            builder.Append(exception).Append(Environment.NewLine);
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
