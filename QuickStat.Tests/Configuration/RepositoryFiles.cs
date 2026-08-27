using System.IO;

namespace QuickStat.Tests.Configuration;

/// <summary>
/// Locates real files in the repository that tests must read in place: the deployed
/// <c>QuickStat.config.xml</c>, the <c>FastTrak.UDL</c> it points at, and the collector golden-SQL
/// corpus.
/// </summary>
/// <remarks>
/// Found by walking up from the test output directory to the folder holding <c>QuickStat.slnx</c>,
/// rather than by copying them into the output. Copying would test a copy; PORT-PLAN.md §6 requires
/// that an <em>existing</em> configuration file works untouched, so the test reads the shipped one.
/// The same walk serves the golden corpus, which is reviewed as source and must not drift from a
/// build-output copy.
/// </remarks>
internal static class RepositoryFiles
{
    private const string SolutionFileName = "QuickStat.slnx";

    /// <summary>The repository root.</summary>
    internal static string Root { get; } = FindRoot();

    /// <summary>The shipped <c>QuickStat.config.xml</c>.</summary>
    internal static string ConfigFile => Path.Combine(Root, "QuickStat.config.xml");

    /// <summary>The shipped <c>FastTrak.UDL</c>, UTF-16 LE with a byte-order mark.</summary>
    internal static string UdlFile => Path.Combine(Root, "FastTrak.UDL");

    /// <summary>One <c>.sql</c> file per static collector; see <c>CollectorGoldenSqlTests</c>.</summary>
    internal static string CollectorGoldenDirectory =>
        Path.Combine(Root, "QuickStat.Tests", "Collectors", "Golden");

    private static string FindRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not find {SolutionFileName} above {AppContext.BaseDirectory}.");
    }
}
