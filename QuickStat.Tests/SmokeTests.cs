using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using QuickStat.Logging;
using Xunit;

namespace QuickStat.Tests;

/// <summary>
/// Phase 0 smoke tests: the three assemblies build, load and satisfy the hard constraints that
/// later phases depend on. Replaced by real coverage in Phase 5.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void CoreAssemblyIsPresentAndLoadable()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "QuickStat.Core.dll");

        Assert.True(File.Exists(path), $"Expected QuickStat.Core.dll beside the test assembly at {path}.");

        Assembly core = Assembly.LoadFrom(path);

        Assert.Equal("QuickStat.Core", core.GetName().Name);
    }

    [Fact]
    public void AppAssemblyIsNamedQuickStat()
    {
        // PORT-PLAN.md §4.1: the deployed product finds its configuration through
        // ChangeFileExt(ParamStr(0), '.config.xml'). If the assembly is renamed, every existing
        // installation stops resolving QuickStat.config.xml. This test pins that constraint.
        Assert.Equal("QuickStat", typeof(App).Assembly.GetName().Name);
    }

    [Fact]
    public void TheLogCreatesItsMissingDirectoryAndWritesTheLine()
    {
        // PORT-PLAN.md §7.2: the Delphi build never created LOGS\, silently losing every log line.
        string root = Path.Combine(Path.GetTempPath(), "QuickStat.Tests", Guid.NewGuid().ToString("N"));
        string logDirectory = Path.Combine(root, QuickStatLog.LogDirectoryName);

        try
        {
            Assert.False(Directory.Exists(logDirectory));

            using (ILoggerFactory factory = LoggerFactory.Create(
                builder => builder.AddQuickStatLog(logDirectory, nameof(LogLevel.Trace))))
            {
                Assert.True(Directory.Exists(logDirectory), "Configuring the log must create its directory.");

                factory.CreateLogger("SmokeTests").LogInformation("Smoke test line.");
            }

            string[] files = Directory.GetFiles(logDirectory, "*.log");

            Assert.Single(files);
            Assert.Contains("Smoke test line.", File.ReadAllText(files[0]), StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void TheLogSwallowsFailuresInsteadOfThrowing()
    {
        // An uncreatable log directory must degrade silently, not take the application down.
        // A regular file where the directory should be makes Directory.CreateDirectory fail.
        string root = Path.Combine(Path.GetTempPath(), "QuickStat.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        string blocker = Path.Combine(root, "blocked");
        File.WriteAllText(blocker, "not a directory");

        try
        {
            using ILoggerFactory factory = LoggerFactory.Create(
                builder => builder.AddQuickStatLog(
                    Path.Combine(blocker, QuickStatLog.LogDirectoryName),
                    nameof(LogLevel.Trace)));

            factory.CreateLogger("SmokeTests").LogError(new InvalidOperationException("boom"), "This must not throw.");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
