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
/// </remarks>
public sealed class FileLoggerRedactionTests : IDisposable
{
    private readonly string _root;
    private readonly FileLoggerProvider _provider;
    private readonly ILogger _logger;

    public FileLoggerRedactionTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "QuickStat.Tests", Guid.NewGuid().ToString("N"));
        _provider = new FileLoggerProvider(Path.Combine(_root, FileLoggerProvider.LogDirectoryName));
        _logger = _provider.CreateLogger("RedactionTests");
    }

    public void Dispose()
    {
        _provider.Dispose();

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

    private string ReadTheLog()
    {
        string[] files = Directory.GetFiles(_provider.LogDirectory, "*.log");

        return File.ReadAllText(Assert.Single(files));
    }
}
