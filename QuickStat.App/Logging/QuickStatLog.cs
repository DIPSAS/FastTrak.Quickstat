using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace QuickStat.Logging;

/// <summary>
/// Where the log goes, how loud it is, and how long it is kept.
/// </summary>
/// <remarks>
/// <para>
/// <b>Serilog sits behind <c>Microsoft.Extensions.Logging</c>, not in front of it.</b> Every call
/// site in the port logs through <c>ILogger&lt;T&gt;</c>; <c>QuickStat.Core</c> references only
/// <c>Microsoft.Extensions.Logging.Abstractions</c>. Serilog appears in this folder and nowhere
/// else - not even in <c>App.xaml.cs</c>, which calls
/// <see cref="AddQuickStatLog(ILoggingBuilder, string?, string?)"/> and never names a Serilog type -
/// so replacing it again is a change to one directory. The sibling C# project
/// <c>FastTrak.PersonInfoSync</c> takes the other route, a <c>global using Serilog</c> in every
/// project; that is the one thing deliberately not copied from it, because it would put a
/// third-party logging type in <c>QuickStat.Core</c>'s public surface.
/// </para>
/// <para>
/// <b>Why Serilog at all.</b> <c>Docs/Port/01-data-access.md</c> §7.5 offered the choice - "a small
/// custom <c>ILoggerProvider</c> (or Serilog, if the team prefers a dependency)" - and the port took
/// the custom route largely to keep the file byte-compatible with the Delphi's plaintext log. The
/// product owner withdrew that requirement on 2026-08-27: the format is not worth preserving, only
/// what is logged, when and how. That removed the only argument for hand-rolling daily rolling,
/// retention, shared-file access and encoding, all of which <c>Serilog.Sinks.File</c> already does.
/// PORT-PLAN.md §1.1 records the dependency change.
/// </para>
/// <para>
/// <b>What is deliberately kept from the provider it replaced:</b> the entry format (so an older log
/// file still reads the same way), the <c>LOGS</c> directory beside the executable, UTF-8 with a
/// byte-order mark, and above all the redaction - see <see cref="QuickStatLogFormatter"/>, which is
/// the R6 choke point.
/// </para>
/// </remarks>
public static class QuickStatLog
{
    /// <summary>Name of the log directory created beside the executable.</summary>
    public const string LogDirectoryName = "LOGS";

    /// <summary>Prefix of the generated log file names.</summary>
    public const string LogFilePrefix = "quickstat-";

    /// <summary>Extension of the generated log file names.</summary>
    public const string LogFileExtension = ".log";

    /// <summary>
    /// Environment variable that overrides the level, for diagnosing a problem on a user's machine.
    /// </summary>
    /// <remarks>
    /// <c>Docs/Port/01-data-access.md</c> §7.5 asks for this and for an <c>[Logging] Level</c>
    /// setting. Only the variable is wired: the settings store lives in the container, and the
    /// logger has to exist before the container is built. The
    /// <see cref="LoggingLevelSwitch"/> registered by
    /// <see cref="AddQuickStatLog(ILoggingBuilder, string?, string?)"/> is the seam for the other
    /// half - whoever binds the setting resolves the switch and assigns
    /// <see cref="LoggingLevelSwitch.MinimumLevel"/>, and it takes effect without a restart.
    /// </remarks>
    public const string LevelVariable = "QUICKSTAT_LOG_LEVEL";

    /// <summary>
    /// How many daily files are kept. Mirrors the Delphi's <c>MaxFile = 10</c>.
    /// </summary>
    public const int RetainedFileCount = 10;

    /// <summary>The level used when nothing asks for another one.</summary>
    public const string DefaultLevelName = nameof(LogEventLevel.Information);

    /// <summary>
    /// UTF-8 with a byte-order mark, which is what makes Norwegian characters render correctly in
    /// Notepad and Excel.
    /// </summary>
    private static readonly Encoding Utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    /// <summary><c>&lt;executable directory&gt;\LOGS</c>. The first choice.</summary>
    /// <remarks>
    /// Resolved from <see cref="AppContext.BaseDirectory"/> - never from
    /// <see cref="Environment.CurrentDirectory"/>, which is wrong when the application is launched
    /// from a shortcut, and never from <c>Assembly.Location</c>, which is empty under single-file
    /// publish.
    /// </remarks>
    public static string PreferredLogDirectory => Path.Combine(AppContext.BaseDirectory, LogDirectoryName);

    /// <summary>
    /// <c>%LOCALAPPDATA%\DIPS\QuickStat\logs</c>, used when the preferred directory cannot be
    /// created.
    /// </summary>
    /// <remarks>
    /// A per-machine installation under <c>Program Files</c> is not writable by an ordinary user, and
    /// <c>Docs/Port/01-data-access.md</c> §7.5 asks for this fallback rather than the Delphi's
    /// behaviour, which was to discard every log line in silence.
    /// </remarks>
    public static string FallbackLogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DIPS",
        "QuickStat",
        "logs");

    /// <summary>Picks a log directory that exists and can be written to.</summary>
    /// <returns>The directory, or <see langword="null"/> when neither candidate can be created.</returns>
    public static string? ResolveLogDirectory() =>
        ResolveLogDirectory(PreferredLogDirectory, FallbackLogDirectory);

    /// <summary>The testable half of <see cref="ResolveLogDirectory()"/>.</summary>
    /// <param name="preferred">First choice.</param>
    /// <param name="fallback">Used when the first choice cannot be created.</param>
    /// <returns>The directory, or <see langword="null"/> when neither can be created.</returns>
    public static string? ResolveLogDirectory(string preferred, string fallback) =>
        TryCreateDirectory(preferred) ?? TryCreateDirectory(fallback);

    /// <summary>
    /// The log file name, before Serilog inserts the date: <c>quickstat-&lt;user&gt;@&lt;machine&gt;-.log</c>.
    /// </summary>
    /// <returns>The file name.</returns>
    /// <remarks>
    /// <para>
    /// The user and the machine are in the name because a terminal server runs one installation for
    /// many people, and a single <c>quickstat-yyyyMMdd.log</c> interleaves all of them into one file
    /// - which is what this port did until now. <c>FastTrak.PersonInfoSync</c> names its files the
    /// same way.
    /// </para>
    /// <para>
    /// The Windows user name is deliberately <em>not</em> redacted: it is the operator, not the
    /// patient, and <see cref="QuickStat.Diagnostics.PiiRedactor"/> says so in as many words.
    /// Redacting it inside the file while writing it on the outside would be theatre.
    /// </para>
    /// </remarks>
    public static string LogFileName() => LogFileName(Environment.UserName, Environment.MachineName);

    /// <summary>The testable half of <see cref="LogFileName()"/>.</summary>
    /// <param name="userName">Operator; sanitised for use in a file name.</param>
    /// <param name="machineName">Machine; sanitised for use in a file name.</param>
    /// <returns>The file name.</returns>
    public static string LogFileName(string? userName, string? machineName) => string.Concat(
        LogFilePrefix,
        ForFileName(userName),
        "@",
        ForFileName(machineName),
        "-",
        LogFileExtension);

    /// <summary>Reads a level name, accepting both Serilog's and Microsoft's spellings.</summary>
    /// <param name="text">The configured value, e.g. from <see cref="LevelVariable"/>.</param>
    /// <param name="level">The level it names.</param>
    /// <returns><see langword="false"/> when the value is missing or not a level name.</returns>
    /// <remarks>
    /// Both vocabularies are accepted because the port speaks Microsoft's at every call site and
    /// Serilog's in its configuration, and nobody diagnosing a problem on a user's machine should
    /// have to know which one this variable wants.
    /// </remarks>
    public static bool TryParseLevel(string? text, out LogEventLevel level)
    {
        level = LogEventLevel.Information;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        switch (text.Trim().ToUpperInvariant())
        {
            case "VERBOSE":
            case "TRACE":
                level = LogEventLevel.Verbose;

                return true;

            case "DEBUG":
                level = LogEventLevel.Debug;

                return true;

            case "INFORMATION":
            case "INFO":
                level = LogEventLevel.Information;

                return true;

            case "WARNING":
            case "WARN":
                level = LogEventLevel.Warning;

                return true;

            case "ERROR":
                level = LogEventLevel.Error;

                return true;

            case "FATAL":
            case "CRITICAL":
                level = LogEventLevel.Fatal;

                return true;

            case "OFF":
            case "NONE":
                level = LevelAlias.Off;

                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// The effective level, in words, for the start-up line - including the fact that a configured
    /// value was rejected.
    /// </summary>
    /// <param name="requestedLevel">The configured value, or <see langword="null"/>.</param>
    /// <returns>Something safe to put in a log line.</returns>
    /// <remarks>
    /// A typo in <see cref="LevelVariable"/> must not fail the start-up and must not be silent
    /// either: the one place anyone will look is the log, and the log is what would be missing the
    /// lines they expected.
    /// </remarks>
    public static string DescribeLevel(string? requestedLevel)
    {
        if (TryParseLevel(requestedLevel, out LogEventLevel level))
        {
            return level.ToString();
        }

        return string.IsNullOrWhiteSpace(requestedLevel)
            ? DefaultLevelName
            : string.Format(
                CultureInfo.InvariantCulture,
                "{0} ({1}='{2}' is not a level name)",
                DefaultLevelName,
                LevelVariable,
                requestedLevel);
    }

    /// <summary>Adds the QuickStat log file to a logging builder.</summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="logDirectory">
    /// Where to write, from <see cref="ResolveLogDirectory()"/>. <see langword="null"/> - or a
    /// directory that cannot be created - means no file, not a failed start-up.
    /// </param>
    /// <param name="requestedLevel">The value of <see cref="LevelVariable"/>, or <see langword="null"/>.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static ILoggingBuilder AddQuickStatLog(
        this ILoggingBuilder builder,
        string? logDirectory,
        string? requestedLevel)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _ = TryParseLevel(requestedLevel, out LogEventLevel level);

        LoggingLevelSwitch levelSwitch = new(level);

        // Registered so the other half of §7.5 - an [Logging] Level setting - can be bound later
        // without a restart. Nothing resolves it today.
        builder.Services.AddSingleton(levelSwitch);

        return builder.AddSerilog(Create(logDirectory, levelSwitch), dispose: true);
    }

    /// <summary>Builds the Serilog logger behind the provider. Never throws.</summary>
    /// <param name="logDirectory">Where to write, or <see langword="null"/> for no file.</param>
    /// <param name="levelSwitch">Controls the level, and can be moved at run time.</param>
    /// <returns>The logger.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="levelSwitch"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// <c>shared: true</c> because one user can start QuickStat twice, and the two processes would
    /// otherwise resolve the same file name and the second would silently log nothing.
    /// </para>
    /// <para>
    /// A log directory that cannot be written to is not a failed start-up: the application runs, and
    /// the only symptom is the missing file. A logger that throws is a classic source of unexplained
    /// crashes, especially during shutdown and especially from inside an unhandled-exception handler.
    /// </para>
    /// </remarks>
    public static Logger Create(string? logDirectory, LoggingLevelSwitch levelSwitch)
    {
        ArgumentNullException.ThrowIfNull(levelSwitch);

        LoggerConfiguration configuration = new LoggerConfiguration().MinimumLevel.ControlledBy(levelSwitch);

        if (!string.IsNullOrWhiteSpace(logDirectory))
        {
            try
            {
                Directory.CreateDirectory(logDirectory);

                configuration = configuration.WriteTo.File(
                    new QuickStatLogFormatter(),
                    Path.Combine(logDirectory, LogFileName()),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: RetainedFileCount,
                    shared: true,
                    encoding: Utf8WithBom);
            }
            catch (Exception)
            {
                // No writable directory means no file, not a failed start-up.
            }
        }

        return configuration.CreateLogger();
    }

    private static string? TryCreateDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        try
        {
            Directory.CreateDirectory(directory);

            return Path.GetFullPath(directory);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Makes a user or machine name safe to put in a file name.</summary>
    private static string ForFileName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        StringBuilder builder = new(value.Length);
        ReadOnlySpan<char> invalid = Path.GetInvalidFileNameChars();

        foreach (char current in value)
        {
            // '@' and '-' are the two separators this name is built from, so a value containing one
            // would make the name ambiguous.
            builder.Append(invalid.Contains(current) || current is '@' or '-' ? '_' : current);
        }

        return builder.ToString();
    }
}
