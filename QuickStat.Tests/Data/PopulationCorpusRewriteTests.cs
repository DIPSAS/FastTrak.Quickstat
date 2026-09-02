using QuickStat.Data;
using Xunit;

namespace QuickStat.Tests.Data;

/// <summary>
/// The rewriter against every argument list the population catalogue actually contains
/// (PORT-PLAN.md R2).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> R2 says population SQL is the one input to this port that lives in the
/// customer's database rather than in this repository, so the scanner had been tested only against
/// shapes somebody imagined. On 2026-09-02 the catalogue was swept on two independent test
/// databases - 518 and 520 rows of <c>dbo.DbProcList WHERE ListId = 'CASE'</c>, 319 and 322 distinct
/// statements - and both reduce to the <b>same 44 distinct argument lists</b>, reproduced below.
/// Only the row count behind <c>:StudyId</c> differed (459 against 461). That agreement is why these
/// are treated as the product's vocabulary rather than one site's data.
/// </para>
/// <para>
/// <b>What the sweep found about the scanner's harder rules: nothing uses them.</b> Across both
/// catalogues, bracketed identifiers, <c>"quoted identifiers"</c>, <c>--</c> comments,
/// <c>/* */</c> comments, <c>::</c> and embedded newlines occur <b>zero</b> times, and the longest
/// statement is 70 characters. Single-quoted literals occur 9 times, and those are the
/// <see cref="AQuotedLiteralNeverBecomesAParameter"/> cases. The unit tests in
/// <see cref="SqlTextRewriterTests"/> still cover the rest, because a catalogue can gain a row.
/// </para>
/// <para>
/// <b>Only the argument lists are reproduced.</b> The real <c>SqlText</c> is
/// <c>RTRIM(ProcName + ' ' + ISNULL(ProcParams, ' '))</c>, but no procedure name in either catalogue
/// contains a colon, so the prefix cannot affect the scanner and a stand-in is faithful. Keeping
/// site procedure names out of the repository is the reason for the stand-in.
/// </para>
/// </remarks>
public class PopulationCorpusRewriteTests
{
    /// <summary>
    /// Stands in for the procedure name, which carries no colon in either catalogue.
    /// </summary>
    private const string ProcedureName = "dbo.GetCaseList ";

    /// <summary>Every distinct <c>ProcParams</c> value in the catalogue, both databases.</summary>
    private static readonly string[] Corpus =
    [
        ":StudyId",
        ":StudyId, 1",
        ":StudyId, 2",
        ":StudyId, 3",
        ":StudyId, 4",
        ":StudyId, 6",
        ":StudyId, 0",
        ":StudyId, -1",
        ":StudyId, 23",
        ":StudyId, 24",
        ":StudyId, 25",
        ":StudyId, 26",
        ":StudyId, 27",
        ":StudyId, 28",
        ":StudyId, 11779",
        ":StudyId, 11780",
        ":StudyId, 11781",
        ":StudyId, 12633",
        ":StudyId, 1, 0, 40, 79",
        ":StudyId, 1, 1, 40, 79",
        ":StudyId, 2, 0, 40, 79",
        ":StudyId, 2, 1, 40, 79",
        ":StudyId, :StartDate, :StopDate",
        ":StudyId, :StartDate,:StopDate",
        ":StudyId, :StatusId",
        ":StudyId, :AlertLevel",
        ":StudyId, :ATC",
        ":StudyId, :ATC1, :ATC2",
        ":StudyId, :DaysBack, :UserId",
        ":StudyId, :DiaType",
        ":StudyId, :FormName, :StartDate, :StopDate",
        ":StudyId, :GroupId",
        ":StudyId,:ApsType",
        ":StudyId,:DiaType",
        ":StudyId,:Year",
        ":StudyId,'Acromegaly'",
        ":StudyId,'Addison'",
        ":StudyId,'Cushing'",
        ":StudyId,'Graves','Endokrin_oftalmopati',1",
        ":StudyId,'Hyperaldosteronism'",
        ":StudyId,'Hyperparathyroidism'",
        ":StudyId,'Hyperprolactinemia'",
        ":StudyId,'Hypopituitarism'",
        ":StudyId,'Phaeochromocytoma'",
    ];

    /// <summary>
    /// The names a session can supply - <c>SessionContext.TryGetParameterValue</c> - plus the pair
    /// the period dialog fills.
    /// </summary>
    private static readonly string[] Resolvable =
        ["StudyId", "StudyName", "UserId", "SessId", "CenterId", "CaseId", "StartDate", "StopDate"];

    private static readonly ColonToAtSqlTextRewriter Rewriter = new();

    [Fact]
    public void TheSweepCoveredEveryDistinctArgumentList() =>
        // Guards against a later edit quietly dropping rows: the sweep found 44 and the count is
        // part of the evidence, not an implementation detail.
        Assert.Equal(44, Corpus.Length);

    [Fact]
    public void EveryStatementRewritesWithoutTouchingAnythingElse()
    {
        foreach (string statement in Statements())
        {
            RewrittenSql result = Rewriter.Rewrite(statement);

            // ':Name' becomes '@Name', one character for one, so a length change means the scanner
            // inserted or dropped text - the failure mode that would corrupt a production query.
            Assert.Equal(statement.Length, result.CommandText.Length);

            for (int i = 0; i < statement.Length; i++)
            {
                if (statement[i] != result.CommandText[i])
                {
                    Assert.Equal(':', statement[i]);
                    Assert.Equal('@', result.CommandText[i]);
                }
            }
        }
    }

    [Fact]
    public void NoPlaceholderIsLeftBehind()
    {
        foreach (string statement in Statements())
        {
            string rewritten = Rewriter.Rewrite(statement).CommandText;

            for (int i = 0; i + 1 < rewritten.Length; i++)
            {
                bool looksLikeAPlaceholder =
                    rewritten[i] == ':' && (char.IsAsciiLetter(rewritten[i + 1]) || rewritten[i + 1] == '_');

                Assert.False(looksLikeAPlaceholder, $"'{statement}' still carries a placeholder.");
            }
        }
    }

    [Fact]
    public void AQuotedLiteralNeverBecomesAParameter()
    {
        // The nine ENDO rows. 'Endokrin_oftalmopati' is the trap: it is a valid placeholder name
        // sitting inside a literal, so a regular expression over the raw text would have bound it.
        RewrittenSql result = Rewriter.Rewrite(ProcedureName + ":StudyId,'Graves','Endokrin_oftalmopati',1");

        Assert.Equal("dbo.GetCaseList @StudyId,'Graves','Endokrin_oftalmopati',1", result.CommandText);
        Assert.Equal<string>(["StudyId"], result.ParameterNames);
    }

    [Fact]
    public void EveryStatementBindsStudyIdAndNothingRepeats()
    {
        foreach (string statement in Statements())
        {
            RewrittenSql result = Rewriter.Rewrite(statement);

            Assert.Contains("StudyId", result.ParameterNames);
            Assert.False(result.HasRepeatedPlaceholder, statement);
        }
    }

    [Fact]
    public void TheCatalogueUsesElevenNamesNoSessionCanResolve() =>
        // Not a scanner defect and not a port regression: the Delphi's vocabulary is the same, so
        // these populations fail there too. Pinned because it is the finding the R2 sweep produced,
        // and because a shrinking list is good news somebody should notice. PORT-PLAN.md §9 R2.
        Assert.Equal<string>(
            [
                "ATC", "ATC1", "ATC2", "AlertLevel", "ApsType", "DaysBack",
                "DiaType", "FormName", "GroupId", "StatusId", "Year",
            ],
            [.. Statements()
                .SelectMany(statement => Rewriter.Rewrite(statement).ParameterNames)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(name => !Resolvable.Contains(name, StringComparer.OrdinalIgnoreCase))
                .Order(StringComparer.Ordinal)]);

    private static IEnumerable<string> Statements() => Corpus.Select(parameters => ProcedureName + parameters);
}
