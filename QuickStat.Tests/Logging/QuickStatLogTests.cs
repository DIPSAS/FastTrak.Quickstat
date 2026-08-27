using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;
using QuickStat.Logging;
using Serilog.Events;
using Xunit;

namespace QuickStat.Tests.Logging;

/// <summary>
/// Where the log goes, how loud it is, and how long it is kept.
/// </summary>
/// <remarks>
/// The four gaps <c>Docs/Port/01-data-access.md</c> §7.5 specified and the port had never
/// implemented: the user in the file name, retention, the level override, and the fallback
/// directory. Redaction has its own file, <c>FileLoggerRedactionTests</c>, because R6 is
/// release-blocking and deserves to be found on its own.
/// </remarks>
public sealed class QuickStatLogTests : IDisposable
{
    private readonly string _root;

    public QuickStatLogTests() =>
        _root = Path.Combine(Path.GetTempPath(), "QuickStat.Tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    // ---------------------------------------------------------------------------------------
    //  The file name.  One installation, many people: a terminal server.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void TheFileNameCarriesTheUserAndTheMachine()
    {
        // Until now every user of a shared installation interleaved into one quickstat-yyyyMMdd.log.
        Assert.Equal("quickstat-chs@BOX-.log", QuickStatLog.LogFileName("chs", "BOX"));
    }

    [Theory]
    [InlineData("dips\\chs", "quickstat-dips_chs@BOX-.log")]
    [InlineData("a b", "quickstat-a b@BOX-.log")]
    [InlineData("chs-1", "quickstat-chs_1@BOX-.log")]
    [InlineData("a@b", "quickstat-a_b@BOX-.log")]
    [InlineData("", "quickstat-unknown@BOX-.log")]
    public void TheFileNameSurvivesAwkwardAccountNames(string userName, string expected)
    {
        // A domain-qualified account name carries a backslash, which would silently redirect the log
        // into a subdirectory - or fail the open outright. The two separators this name is built
        // from are neutralised too, so the name cannot become ambiguous.
        Assert.Equal(expected, QuickStatLog.LogFileName(userName, "BOX"));
    }

    [Fact]
    public void TheWrittenFileCarriesTheDate()
    {
        string directory = Write(logger => logger.LogInformation("Line."));

        string name = Path.GetFileName(Assert.Single(Directory.GetFiles(directory, "*.log")));
        string today = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        Assert.StartsWith(QuickStatLog.LogFilePrefix, name, StringComparison.Ordinal);
        Assert.EndsWith(today + QuickStatLog.LogFileExtension, name, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------------
    //  The level.  "You cannot turn on tracing on a user's machine" was the gap.
    // ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData("Verbose", LogEventLevel.Verbose)]
    [InlineData("Trace", LogEventLevel.Verbose)]
    [InlineData("debug", LogEventLevel.Debug)]
    [InlineData("INFORMATION", LogEventLevel.Information)]
    [InlineData(" info ", LogEventLevel.Information)]
    [InlineData("Warning", LogEventLevel.Warning)]
    [InlineData("Error", LogEventLevel.Error)]
    [InlineData("Critical", LogEventLevel.Fatal)]
    [InlineData("Fatal", LogEventLevel.Fatal)]
    public void BothVocabulariesAreAccepted(string text, LogEventLevel expected)
    {
        // Every call site speaks Microsoft's names and the configuration speaks Serilog's; nobody
        // diagnosing a problem on a user's machine should have to know which one this wants.
        Assert.True(QuickStatLog.TryParseLevel(text, out LogEventLevel level));
        Assert.Equal(expected, level);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("loud")]
    public void AnUnusableLevelIsRejectedRatherThanGuessed(string? text)
    {
        Assert.False(QuickStatLog.TryParseLevel(text, out LogEventLevel level));
        Assert.Equal(LogEventLevel.Information, level);
    }

    [Fact]
    public void ATypoInTheVariableIsSaidOutLoudRatherThanIgnored()
    {
        // The one place anyone will look is the log, and the log is what would be missing the lines
        // they expected - so the start-up line has to say why.
        Assert.Equal("Information", QuickStatLog.DescribeLevel(null));
        Assert.Equal("Verbose", QuickStatLog.DescribeLevel("Verbose"));

        string described = QuickStatLog.DescribeLevel("loud");

        Assert.Contains("Information", described, StringComparison.Ordinal);
        Assert.Contains(QuickStatLog.LevelVariable, described, StringComparison.Ordinal);
        Assert.Contains("loud", described, StringComparison.Ordinal);
    }

    [Fact]
    public void DebugIsSilentByDefaultAndAppearsWhenAsked()
    {
        // This is the whole point of the override: turning on tracing in the field.
        Assert.DoesNotContain(
            "Chatter.",
            Write(logger => logger.LogDebug("Chatter."), requestedLevel: null, read: true),
            StringComparison.Ordinal);

        Assert.Contains(
            "Chatter.",
            Write(logger => logger.LogDebug("Chatter."), requestedLevel: "Debug", read: true),
            StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------------
    //  Retention and the fallback directory.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void OnlyTenDailyFilesAreKept()
    {
        // Delphi MaxFile = 10. The hand-rolled provider had no retention at all, so a machine that
        // ran QuickStat daily accumulated files forever.
        string directory = Path.Combine(_root, QuickStatLog.LogDirectoryName);

        Directory.CreateDirectory(directory);

        string prefix = QuickStatLog.LogFileName()[..^QuickStatLog.LogFileExtension.Length];

        for (int day = 1; day <= 20; day++)
        {
            DateTime stamp = DateTime.Now.AddDays(-day);

            File.WriteAllText(
                Path.Combine(directory, prefix + stamp.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + QuickStatLog.LogFileExtension),
                "old");
        }

        Assert.Equal(20, Directory.GetFiles(directory, "*.log").Length);

        WriteTo(directory, logger => logger.LogInformation("Today."), requestedLevel: null);

        Assert.Equal(QuickStatLog.RetainedFileCount, Directory.GetFiles(directory, "*.log").Length);
    }

    [Fact]
    public void AnUnwritablePreferredDirectoryFallsBackInsteadOfLosingTheLog()
    {
        // Docs/Port/01-data-access.md §7.5. A per-machine installation under Program Files is not
        // writable by an ordinary user, and the Delphi's answer was to discard every line in silence.
        Directory.CreateDirectory(_root);

        string blocker = Path.Combine(_root, "blocked");

        File.WriteAllText(blocker, "not a directory");

        string fallback = Path.Combine(_root, "fallback");

        Assert.Equal(
            Path.GetFullPath(fallback),
            QuickStatLog.ResolveLogDirectory(Path.Combine(blocker, "LOGS"), fallback));
    }

    [Fact]
    public void WithNowhereToWriteThereIsNoLogAndNoCrash()
    {
        Directory.CreateDirectory(_root);

        string blocker = Path.Combine(_root, "blocked");

        File.WriteAllText(blocker, "not a directory");

        Assert.Null(QuickStatLog.ResolveLogDirectory(Path.Combine(blocker, "a"), Path.Combine(blocker, "b")));

        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddQuickStatLog(null, null));

        factory.CreateLogger("Nowhere").LogError(new InvalidOperationException("boom"), "Must not throw.");
    }

    // ---------------------------------------------------------------------------------------
    //  The entry itself.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void AnEntryCarriesItsLevelThreadCategoryAndMessage()
    {
        string written = Write(logger => logger.LogWarning("Something."), requestedLevel: null, read: true);

        Assert.Contains("[WRN]", written, StringComparison.Ordinal);
        Assert.Contains("[T", written, StringComparison.Ordinal);
        Assert.Contains("QuickStatLogTests: Something.", written, StringComparison.Ordinal);
    }

    [Fact]
    public void AnEventIdIsRenderedWhenThereIsOne()
    {
        // Serilog.Extensions.Logging carries the EventId as a structure rather than a scalar, so the
        // formatter has to dig it out; this pins that it actually does.
        string written = Write(
            logger => logger.LogInformation(new EventId(4711), "With an id."),
            requestedLevel: null,
            read: true);

        Assert.Contains("(4711): With an id.", written, StringComparison.Ordinal);
    }

    [Fact]
    public void AnEntryWithNoCategoryStillReads()
    {
        string directory = Path.Combine(_root, QuickStatLog.LogDirectoryName);

        using (ILoggerFactory factory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddQuickStatLog(directory, nameof(LogLevel.Trace));
        }))
        {
            factory.CreateLogger(string.Empty).LogInformation("Uncategorised.");
        }

        Assert.Contains(
            "(none): Uncategorised.",
            File.ReadAllText(Assert.Single(Directory.GetFiles(directory, "*.log"))),
            StringComparison.Ordinal);
    }

    private string Write(Action<ILogger> log) => Write(log, requestedLevel: null, read: false);

    /// <summary>Logs through the real pipeline into a fresh directory and hands back what was written.</summary>
    /// <returns>The directory when <paramref name="read"/> is false, otherwise the file's text.</returns>
    private string Write(Action<ILogger> log, string? requestedLevel, bool read)
    {
        string directory = Path.Combine(_root, Guid.NewGuid().ToString("N"), QuickStatLog.LogDirectoryName);

        WriteTo(directory, log, requestedLevel);

        if (!read)
        {
            return directory;
        }

        string[] files = Directory.GetFiles(directory, "*.log");

        return files.Length == 0 ? "" : File.ReadAllText(files[0]);
    }

    private static void WriteTo(string directory, Action<ILogger> log, string? requestedLevel)
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder =>
        {
            // Trace, exactly as App.xaml.cs does it: the level is decided by QuickStatLog's switch
            // and nowhere else, so this proves the switch and not the builder's floor.
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddQuickStatLog(directory, requestedLevel);
        });

        log(factory.CreateLogger<QuickStatLogTests>());
    }
}
