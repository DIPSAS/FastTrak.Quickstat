using System.IO;
using QuickStat.Configuration.Settings;
using Xunit;

namespace QuickStat.Tests.Configuration.Settings;

/// <summary>
/// PORT-PLAN.md §4.1: paths resolve from the executable directory, never from the working directory,
/// and never through <c>Assembly.Location</c>.
/// </summary>
/// <remarks>
/// This is not hypothetical. QuickStat is launched from shortcuts, and a shortcut sets whatever
/// working directory it likes. The Delphi resolved its settings root from
/// <c>ExtractFilePath(ParamStr(0))</c> and got this right; the relative UDL path in the same code
/// base did not, which is the bug §4.1 exists to avoid repeating.
/// </remarks>
public class SettingsPathTests
{
    [Fact]
    public void TheExecutableDirectoryIsTheAssemblyBaseDirectory()
    {
        Assert.Equal(
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppContext.BaseDirectory)),
            SettingsPath.ExecutableDirectory);
    }

    [Fact]
    public void EveryPathIsFullyQualified()
    {
        Assert.True(Path.IsPathFullyQualified(SettingsPath.ExecutableDirectory));
        Assert.True(Path.IsPathFullyQualified(SettingsPath.PortableFilePath));
        Assert.True(Path.IsPathFullyQualified(SettingsPath.RoamingFilePath));
        Assert.True(Path.IsPathFullyQualified(SettingsPath.Resolve()));
    }

    [Fact]
    public void ThePortableFileSitsBesideTheExecutable()
    {
        Assert.Equal(
            Path.Combine(SettingsPath.ExecutableDirectory, SettingsPath.PortableFolderName, SettingsPath.FileName),
            SettingsPath.PortableFilePath);
    }

    [Fact]
    public void TheRoamingFileSitsUnderTheRoamingProfile()
    {
        string roaming = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);

        Assert.False(string.IsNullOrEmpty(roaming), "This test needs a roaming profile.");
        Assert.Equal(
            Path.Combine(roaming, SettingsPath.RoamingFolderName, SettingsPath.FileName),
            SettingsPath.RoamingFilePath);
    }

    [Fact]
    public void TheWorkingDirectoryDoesNotAffectAnyPath()
    {
        // The whole point of §4.1. Every other test in this suite uses absolute paths, so moving the
        // working directory briefly is safe.
        string original = Environment.CurrentDirectory;

        string executable = SettingsPath.ExecutableDirectory;
        string portable = SettingsPath.PortableFilePath;
        string roaming = SettingsPath.RoamingFilePath;
        string resolved = SettingsPath.Resolve();

        try
        {
            Environment.CurrentDirectory = Path.GetTempPath();

            Assert.Equal(executable, SettingsPath.ExecutableDirectory);
            Assert.Equal(portable, SettingsPath.PortableFilePath);
            Assert.Equal(roaming, SettingsPath.RoamingFilePath);
            Assert.Equal(resolved, SettingsPath.Resolve());
        }
        finally
        {
            Environment.CurrentDirectory = original;
        }
    }

    [Fact]
    public void ResolvePicksTheRoamingFileWhenThereIsNoPortableOne()
    {
        // The test assembly's own directory has no Settings\QuickStat.ini, and nothing creates one.
        Assert.False(File.Exists(SettingsPath.PortableFilePath));
        Assert.Equal(SettingsPath.RoamingFilePath, SettingsPath.Resolve());
    }

    [Fact]
    public void ResolvePicksThePortableFileWhenSomebodyPutOneThere()
    {
        string portable = SettingsPath.PortableFilePath;
        string? directory = Path.GetDirectoryName(portable);

        Assert.NotNull(directory);

        bool directoryExisted = Directory.Exists(directory);

        Directory.CreateDirectory(directory);

        try
        {
            File.WriteAllText(portable, "; portable\r\n");

            Assert.Equal(portable, SettingsPath.Resolve());
        }
        finally
        {
            File.Delete(portable);

            if (!directoryExisted)
            {
                Directory.Delete(directory);
            }
        }
    }

    [Fact]
    public void NothingHereGoesThroughAssemblyLocation()
    {
        // Assembly.Location is an empty string under single-file publish, which PORT-PLAN.md §8.7
        // leaves on the table. An empty anchor would turn every path above into a relative one -
        // exactly the failure mode this class exists to prevent.
        //
        // This has to be checked against the source, because a runtime assertion cannot see the
        // difference: in a normal build Path.GetDirectoryName(Assembly.Location) and
        // AppContext.BaseDirectory are the same string, and they only diverge in the published
        // configuration where the test suite no longer runs.
        string code = StripComments(ReadSettingsPathSource());

        Assert.DoesNotContain("Assembly.Location", code, StringComparison.Ordinal);
        Assert.DoesNotContain("GetExecutingAssembly", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Environment.CurrentDirectory", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Directory.GetCurrentDirectory", code, StringComparison.Ordinal);
        Assert.Contains("AppContext.BaseDirectory", code, StringComparison.Ordinal);
    }

    /// <summary>Removes line and documentation comments, so a prohibition can be discussed in prose.</summary>
    private static string StripComments(string source)
    {
        IEnumerable<string> lines = source
            .Split('\n')
            .Select(line => line.TrimStart())
            .Where(line => !line.StartsWith("//", StringComparison.Ordinal));

        return string.Join('\n', lines);
    }

    /// <summary>
    /// Finds <c>SettingsPath.cs</c> by walking up from the test binaries to the repository root.
    /// </summary>
    private static string ReadSettingsPathSource()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(
                directory.FullName,
                "QuickStat.Core",
                "Configuration",
                "Settings",
                "SettingsPath.cs");

            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not find SettingsPath.cs above {AppContext.BaseDirectory}. "
            + $"({typeof(SettingsPath).Assembly.GetName().Name})");
    }
}
