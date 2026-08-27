using System.IO;
using System.Text.RegularExpressions;
using QuickStat.Collectors;
using QuickStat.Collectors.Registry;
using QuickStat.Tests.Configuration;
using Xunit;

namespace QuickStat.Tests.Collectors;

/// <summary>
/// The golden-file corpus: one <c>Golden/&lt;NAME&gt;.sql</c> per static collector, compared against
/// what <see cref="ICollector.BuildSql"/> emits.
/// </summary>
/// <remarks>
/// <para>
/// This is the mitigation PORT-PLAN.md R3 names for "the 131-entry collector registry is transcribed
/// by hand". Its value rests entirely on <b>where the golden files came from</b>: they were derived
/// in Phase 5 from the Delphi Pascal at the pinned baseline
/// (<c>C:\work\FastTrak-tarmscreening</c>), by readers that were not allowed to look at
/// <c>QuickStat.Core\Collectors\</c>. A corpus regenerated from this project's own output would
/// pin regressions but could never detect a transcription error, because it would agree with the
/// error by construction. <b>If a golden file ever needs to change, change it from the Pascal.</b>
/// </para>
/// <para>
/// Comparison is whitespace-normalised - runs of whitespace collapse to one space, ends trimmed -
/// because indentation is not a property of the statement and an independent derivation cannot be
/// expected to reproduce it. Everything else is ordinal and case-sensitive, which is what catches a
/// wrong id, a dropped predicate, a renamed column or a changed ATC pattern.
/// </para>
/// <para>
/// Static collectors only. The per-form collectors <c>CollectorRegistryBuilder.CreateFormCollectors</c>
/// builds depend on <c>Report.GetFormClasses</c>, so their statements vary per database and are
/// covered by <c>DynamicFormCollectorTests</c> instead.
/// </para>
/// </remarks>
public partial class CollectorGoldenSqlTests
{
    private static readonly CollectorSqlContext Context = CollectorTestContext.SqlContext;

    /// <summary>Every static collector's name, as xunit theory data.</summary>
    public static TheoryData<string> CollectorNames
    {
        get
        {
            TheoryData<string> data = [];
            foreach (ICollector collector in CollectorCatalog.All)
            {
                data.Add(collector.Descriptor.Name);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(CollectorNames))]
    public void TheGeneratedStatementMatchesTheStatementDerivedFromTheDelphi(string collectorName)
    {
        ICollector collector = CollectorTestContext.ByName(CollectorCatalog.All, collectorName);
        string path = GoldenPath(collectorName);

        Assert.True(
            File.Exists(path),
            $"{collectorName} has no golden file. Derive one from the Delphi Pascal and write it to {path}.");

        Assert.Equal(Normalise(File.ReadAllText(path)), Normalise(collector.BuildSql(Context)));
    }

    [Fact]
    public void EveryGoldenFileBelongsToACollector()
    {
        // The other direction: a renamed collector leaves its old golden file behind, and an orphan
        // is indistinguishable from coverage unless something looks for it.
        HashSet<string> expected = new(StringComparer.Ordinal);
        foreach (ICollector collector in CollectorCatalog.All)
        {
            expected.Add(FileName(collector.Descriptor.Name));
        }

        List<string> orphans = [];
        foreach (string file in Directory.EnumerateFiles(RepositoryFiles.CollectorGoldenDirectory, "*.sql"))
        {
            string name = Path.GetFileName(file);
            if (!expected.Contains(name))
            {
                orphans.Add(name);
            }
        }

        Assert.Empty(orphans);
    }

    [Fact]
    public void TheCorpusCoversTheWholeCatalog()
    {
        int files = Directory.GetFiles(RepositoryFiles.CollectorGoldenDirectory, "*.sql").Length;

        Assert.Equal(CollectorTestContext.DistinctNameCount, files);
    }

    [Fact]
    public void NoGoldenFileIsEmpty()
    {
        // A zero-byte file normalises to the empty string, which would silently match a collector
        // whose BuildSql returned nothing. Neither is legitimate, so say so directly.
        foreach (string file in Directory.EnumerateFiles(RepositoryFiles.CollectorGoldenDirectory, "*.sql"))
        {
            Assert.False(
                Normalise(File.ReadAllText(file)).Length == 0,
                $"{Path.GetFileName(file)} is empty.");
        }
    }

    private static string GoldenPath(string collectorName) =>
        Path.Combine(RepositoryFiles.CollectorGoldenDirectory, FileName(collectorName));

    /// <summary>
    /// A collector name maps to a file name by replacing every dot, and nothing else - the case is
    /// the collector's own, so <c>DRUG.NorGEP</c> is <c>DRUG_NorGEP.sql</c>.
    /// </summary>
    /// <param name="collectorName">The collector name.</param>
    /// <returns>The file name, with extension.</returns>
    private static string FileName(string collectorName) => collectorName.Replace('.', '_') + ".sql";

    private static string Normalise(string sql) => WhitespaceRuns().Replace(sql, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRuns();
}
