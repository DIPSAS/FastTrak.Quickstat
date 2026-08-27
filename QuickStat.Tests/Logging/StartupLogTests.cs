using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;
using QuickStat.Data;
using QuickStat.Logging;
using Xunit;

namespace QuickStat.Tests.Logging;

/// <summary>
/// The two lines that answer "which build, pointed at what?".
/// </summary>
/// <remarks>
/// PORT-PLAN.md R11, R13 and R14 are one mistake in three costumes: reasoning about a build, a
/// branch or a working tree that is not the one in play. Phase 5's parity pass is done by hand
/// against a deployed <c>22.12.21.547</c>, and these lines are what make a log from the field
/// self-describing. Written through the real pipeline and read back off disk, because a start-up
/// line that is not in the file is not a start-up line.
/// </remarks>
public sealed class StartupLogTests : IDisposable
{
    private const string OlasNationalId = "12032212345";

    private readonly string _root;
    private readonly string _logDirectory;

    public StartupLogTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "QuickStat.Tests", Guid.NewGuid().ToString("N"));
        _logDirectory = Path.Combine(_root, QuickStatLog.LogDirectoryName);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void TheStartupLineNamesTheBuildTheConfigAndTheLocale()
    {
        string configFile = Path.Combine(_root, "QuickStat.config.xml");

        Directory.CreateDirectory(_root);
        File.WriteAllText(configFile, "<QuickStat />");

        string written = Write(logger =>
            logger.LogStartupEnvironment("26.0.0.0", configFile, _logDirectory, "Verbose"));

        Assert.Contains("QuickStat 26.0.0.0 starting.", written, StringComparison.Ordinal);
        Assert.Contains(Environment.UserName, written, StringComparison.Ordinal);
        Assert.Contains(Environment.MachineName, written, StringComparison.Ordinal);
        Assert.Contains(configFile, written, StringComparison.Ordinal);
        Assert.Contains(StartupLog.ConfigFileFound, written, StringComparison.Ordinal);
        Assert.Contains("level Verbose", written, StringComparison.Ordinal);

        // The one that earns its place: InvariantGlobalization is false and CSV export formats
        // numbers with the OS decimal separator for byte-parity with the Delphi (PORT-PLAN.md §6,
        // R4). A "the separators are wrong" report is a locale report, and this answers it.
        Assert.Contains(CultureInfo.CurrentCulture.Name, written, StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingConfigFileIsCalledOut()
    {
        // "QuickStat has no projects in the list" and "QuickStat could not find its configuration
        // file" look identical from the user's side, and only one of them is a QuickStat bug.
        string written = Write(logger => logger.LogStartupEnvironment(
            "26.0.0.0",
            Path.Combine(_root, "absent.config.xml"),
            _logDirectory,
            "Information"));

        Assert.Contains(StartupLog.ConfigFileMissing, written, StringComparison.Ordinal);
        Assert.DoesNotContain(StartupLog.ConfigFileFound, written, StringComparison.Ordinal);
    }

    [Fact]
    public void TheStartupLineSurvivesHavingNothingToSay()
    {
        string written = Write(logger => logger.LogStartupEnvironment(null, null, null, null));

        Assert.Contains("(unknown)", written, StringComparison.Ordinal);
        Assert.Contains(StartupLog.NoLogDirectory, written, StringComparison.Ordinal);
        Assert.Contains(QuickStatLog.DefaultLevelName, written, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSessionLineNamesTheServerAndTheDatabase()
    {
        string written = Write(logger => logger.LogSession("NDV produksjon", NewSession()));

        Assert.Contains("NDV produksjon", written, StringComparison.Ordinal);
        Assert.Contains("server sql01", written, StringComparison.Ordinal);
        Assert.Contains("database FastTrak", written, StringComparison.Ordinal);
        Assert.Contains("study 931 'Tarmscreening'", written, StringComparison.Ordinal);
        Assert.Contains("schema version 18200", written, StringComparison.Ordinal);
    }

    [Fact]
    public void NeitherLineCanCarryACredential()
    {
        // The session line reads SessionContext's own fields, so there is no connection string in
        // scope to leak one out of - and the redactor still runs over whatever does get written.
        SessionContext session = NewSession();

        string written = Write(logger =>
        {
            logger.LogStartupEnvironment("26.0.0.0", "C:\\deployed\\QuickStat.config.xml", _logDirectory, "Information");
            logger.LogSession("NDV", session);
        });

        Assert.DoesNotContain("Password", written, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Trusted_Connection", written, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(OlasNationalId, written, StringComparison.Ordinal);
    }

    private static SessionContext NewSession() => new()
    {
        StudyName = "Tarmscreening",
        StudyId = 931,
        SessionId = 42,
        User = new StudyUser { UserId = 7, UserName = "chs" },
        Database = new DatabaseInfo { DbVersion = 18200 },
        ServerName = "sql01",
        DatabaseName = "FastTrak",
    };

    private string Write(Action<ILogger> log)
    {
        using (ILoggerFactory factory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddQuickStatLog(_logDirectory, nameof(LogLevel.Trace));
        }))
        {
            log(factory.CreateLogger("StartupLogTests"));
        }

        return File.ReadAllText(Assert.Single(Directory.GetFiles(_logDirectory, "*.log")));
    }
}
