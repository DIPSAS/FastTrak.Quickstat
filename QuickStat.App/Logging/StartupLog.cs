using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using QuickStat.Data;

namespace QuickStat.Logging;

/// <summary>
/// The two lines that say which build, which configuration and which database a run actually used.
/// </summary>
/// <remarks>
/// <para>
/// Borrowed from <c>FastTrak.PersonInfoSync</c>, which opens every run by logging its whole resolved
/// configuration with the secrets replaced by <c>[Redacted]</c> and the connection string decomposed
/// into server and database. It is the most useful thing in that project's log file, because the
/// first question about any report from the field is "which build, pointed at what?" - and the log
/// is usually all you get.
/// </para>
/// <para>
/// It earns its place here for a specific reason. PORT-PLAN.md R11, R13 and R14 are all one mistake
/// in different clothes: reasoning about a build, a branch or a working tree that is not the one in
/// play. Phase 5's parity pass compares this port against a deployed <c>22.12.21.547</c> by hand,
/// and a single line naming the version, the config file and the database removes a whole class of
/// "which one was that?" from it.
/// </para>
/// <para>
/// <b>No secret goes in either line.</b> The start-up line carries no connection string at all, and
/// the session line reads server and database from <see cref="SessionContext"/>, which already holds
/// them as separate fields - so there is no connection string to parse and no credential that could
/// leak out of one. Both lines still pass through
/// <see cref="QuickStat.Diagnostics.PiiRedactor"/> on the way to the file like everything else.
/// </para>
/// </remarks>
public static class StartupLog
{
    /// <summary>Written when the configuration file is where it is expected to be.</summary>
    public const string ConfigFileFound = "found";

    /// <summary>Written when it is not, which is a first-run or a broken installation.</summary>
    public const string ConfigFileMissing = "MISSING";

    /// <summary>Written in place of the log directory when no writable one could be found.</summary>
    public const string NoLogDirectory = "(none)";

    /// <summary>Records the build, the environment and the configuration this run resolved.</summary>
    /// <param name="logger">Where to write it.</param>
    /// <param name="version">From <see cref="QuickStat.Services.IApplicationInfo.Version"/>.</param>
    /// <param name="configFilePath">
    /// From <see cref="QuickStat.Configuration.IConnectionCatalog.DefaultConfigFilePath"/>. Whether
    /// it exists is checked here and said out loud, because "QuickStat has no projects in the list"
    /// and "QuickStat could not find its configuration file" look identical from the user's side.
    /// </param>
    /// <param name="logDirectory">From <see cref="QuickStatLog.ResolveLogDirectory()"/>.</param>
    /// <param name="logLevel">From <see cref="QuickStatLog.DescribeLevel"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <b><see cref="CultureInfo.CurrentCulture"/> is in here on purpose.</b> QuickStat sets
    /// <c>InvariantGlobalization=false</c> and formats exported numbers with the operating system's
    /// decimal separator, deliberately, for byte-parity with the Delphi build (PORT-PLAN.md §6 and
    /// R4). A CSV that arrives at a downstream consumer with full stops where commas were expected
    /// is a locale report, and this is the line that answers it.
    /// </remarks>
    public static void LogStartupEnvironment(
        this ILogger logger,
        string? version,
        string? configFilePath,
        string? logDirectory,
        string? logLevel)
    {
        ArgumentNullException.ThrowIfNull(logger);

        logger.LogInformation(
            "QuickStat {Version} starting. User {User} on {Machine}; {OperatingSystem}; .NET {Runtime}; "
            + "culture {Culture} / UI {UiCulture}; base {BaseDirectory}; "
            + "config {ConfigFile} ({ConfigFileState}); log {LogDirectory} at level {LogLevel}.",
            version ?? "(unknown)",
            Environment.UserName,
            Environment.MachineName,
            RuntimeInformation.OSDescription,
            RuntimeInformation.FrameworkDescription,
            CultureInfo.CurrentCulture.Name,
            CultureInfo.CurrentUICulture.Name,
            AppContext.BaseDirectory,
            configFilePath ?? "(unknown)",
            DescribeConfigFile(configFilePath),
            logDirectory ?? NoLogDirectory,
            logLevel ?? QuickStatLog.DefaultLevelName);
    }

    /// <summary>Records which database a connection actually reached.</summary>
    /// <param name="logger">Where to write it.</param>
    /// <param name="connectionName">The <c>&lt;Connection&gt;</c> entry the user picked.</param>
    /// <param name="session">The established session.</param>
    /// <exception cref="ArgumentNullException">Either argument is <see langword="null"/>.</exception>
    public static void LogSession(this ILogger logger, string? connectionName, SessionContext session)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(session);

        logger.LogInformation(
            "Session on {Connection}: server {Server}, database {Database}, "
            + "study {StudyId} '{StudyName}', schema version {DbVersion}, session {SessionId}, user {UserName}.",
            connectionName ?? "(unnamed)",
            session.ServerName,
            session.DatabaseName,
            session.StudyId,
            session.StudyName,
            session.Database.DbVersion,
            session.SessionId,
            session.User.UserName);
    }

    private static string DescribeConfigFile(string? configFilePath)
    {
        if (string.IsNullOrWhiteSpace(configFilePath))
        {
            return ConfigFileMissing;
        }

        try
        {
            return File.Exists(configFilePath) ? ConfigFileFound : ConfigFileMissing;
        }
        catch (Exception)
        {
            // An unreadable path is not worth failing a start-up line over.
            return ConfigFileMissing;
        }
    }
}
