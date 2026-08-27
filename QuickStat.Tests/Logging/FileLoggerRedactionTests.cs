using System.IO;
using Microsoft.Extensions.Logging;
using QuickStat.Diagnostics;
using QuickStat.Logging;
using QuickStat.Tests.Diagnostics;
using Xunit;

namespace QuickStat.Tests.Logging;

/// <summary>
/// Nothing personal may reach the log file.
/// </summary>
/// <remarks>
/// <para>
/// <c>Docs/Port/01-data-access.md</c> §7.5 says to apply <see cref="PiiRedactor.ForLog"/> in the
/// logger provider, and until Phase 5 checked, nothing did: <see cref="PiiRedactor"/> was wired into
/// <c>UserNotifier</c> and <c>IniSettingsStore</c> only, so anything written through
/// <see cref="ILogger"/> directly landed on disk in the clear. That is R6, which PORT-PLAN.md §9
/// treats as release-blocking, and it is why these tests read the actual bytes of the actual file
/// rather than asserting on a formatter in isolation.
/// </para>
/// <para>
/// The identity numbers below are the suite's shared check-digit-valid test values; they are
/// structurally valid and belong to nobody.
/// </para>
/// <para>
/// <b>These five cases outlived the provider they were written against.</b> The hand-rolled
/// <c>FileLoggerProvider</c> was replaced by Serilog behind
/// <see cref="Microsoft.Extensions.Logging.ILogger"/> (PORT-PLAN.md §1.1); every assertion below is
/// unchanged, and only the four lines that build the pipeline are different. That is deliberate -
/// R6 is release-blocking, so the statement of it had to survive the change of mechanism intact
/// rather than be rewritten to describe whatever the new mechanism happens to do.
/// </para>
/// <para>
/// One of them, <see cref="AHandlebarredValueNeverReachesTheFile"/>, caught a real regression during
/// that swap: Serilog's template parser reads <c>{{</c> as an escaped <c>{</c>, so the handlebar
/// convention would have arrived at the redactor already un-doubled and silently stopped working.
/// <c>QuickStatLogFormatter</c> redacts the raw template text before it is parsed because of this
/// test.
/// </para>
/// </remarks>
public sealed class FileLoggerRedactionTests : IDisposable
{
    private readonly string _root;
    private readonly string _logDirectory;
    private readonly ILoggerFactory _factory;
    private readonly ILogger _logger;

    public FileLoggerRedactionTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "QuickStat.Tests", Guid.NewGuid().ToString("N"));
        _logDirectory = Path.Combine(_root, QuickStatLog.LogDirectoryName);

        // The real pipeline, not a formatter in isolation: these tests read the bytes that a running
        // QuickStat would actually write.
        _factory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddQuickStatLog(_logDirectory, nameof(LogLevel.Trace));
        });

        _logger = _factory.CreateLogger("RedactionTests");
    }

    public void Dispose()
    {
        _factory.Dispose();

        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void AHandlebarredValueNeverReachesTheFile()
    {
        _logger.LogInformation("Loaded patient {{Ola Nordmann}} from the cohort.");

        string written = ReadTheLog();

        Assert.DoesNotContain("Ola Nordmann", written, StringComparison.Ordinal);
        Assert.Contains(PiiRedactor.Replacement, written, StringComparison.Ordinal);

        // The surrounding sentence has to survive, or redaction has destroyed the diagnostic.
        Assert.Contains("Loaded patient", written, StringComparison.Ordinal);
        Assert.Contains("from the cohort.", written, StringComparison.Ordinal);
    }

    [Fact]
    public void AnIdentityNumberNeverReachesTheFileEvenWithoutHandlebars()
    {
        // The case that matters most: a call site that never realised it was holding one. Handlebars
        // are a convention and conventions get forgotten; the structural check does not.
        _logger.LogWarning("Lookup failed for {NationalId}.", PiiRedactorTests.ValidFodselsnummer);

        string written = ReadTheLog();

        Assert.DoesNotContain(PiiRedactorTests.ValidFodselsnummer, written, StringComparison.Ordinal);
        Assert.Contains(PiiRedactor.Replacement, written, StringComparison.Ordinal);
    }

    [Fact]
    public void AnIdentityNumberInsideAnExceptionNeverReachesTheFile()
    {
        // An exception quotes its inputs, so ToString() is a PII path in its own right - and it is
        // appended separately from the message, so redacting only the message would miss it.
        InvalidOperationException exception = new($"Rejected {PiiRedactorTests.AnotherFodselsnummer}.");

        _logger.LogError(exception, "Recovery failed.");

        string written = ReadTheLog();

        Assert.DoesNotContain(PiiRedactorTests.AnotherFodselsnummer, written, StringComparison.Ordinal);
        Assert.Contains("Recovery failed.", written, StringComparison.Ordinal);
    }

    [Fact]
    public void AnExceptionKeepsItsLineBreaks()
    {
        // Redact, not ForLog, for the exception block: collapsing whitespace would fold a stack trace
        // onto one line and destroy the only thing it is for.
        InvalidOperationException exception;
        try
        {
            throw new InvalidOperationException("boom");
        }
        catch (InvalidOperationException caught)
        {
            exception = caught;
        }

        _logger.LogError(exception, "Failed.");

        string written = ReadTheLog();

        Assert.Contains("InvalidOperationException", written, StringComparison.Ordinal);
        Assert.Contains(nameof(AnExceptionKeepsItsLineBreaks), written, StringComparison.Ordinal);
        Assert.True(
            written.Split(Environment.NewLine).Length > 3,
            "The stack trace must still span several lines.");
    }

    [Fact]
    public void AMessageCannotForgeASecondEntry()
    {
        // ForLog folds the message onto one line, so an embedded newline cannot fabricate a log
        // entry. Not the reason the redaction is there, but it falls out of it and is worth pinning.
        _logger.LogInformation("First.\r\n2099-01-01 00:00:00.000 [CRT] [T01] Forged: second.");

        string written = ReadTheLog();
        string[] lines = written.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Single(lines);
        Assert.Contains("Forged: second.", lines[0], StringComparison.Ordinal);
    }

    /// <summary>Closes the sink so the bytes are on disk, then reads them.</summary>
    /// <remarks>
    /// Disposing the factory disposes the Serilog logger, which flushes and releases the file. Each
    /// case reads once, at the end, so there is nothing to write afterwards.
    /// </remarks>
    private string ReadTheLog()
    {
        _factory.Dispose();

        string[] files = Directory.GetFiles(_logDirectory, "*.log");

        return File.ReadAllText(Assert.Single(files));
    }
}
