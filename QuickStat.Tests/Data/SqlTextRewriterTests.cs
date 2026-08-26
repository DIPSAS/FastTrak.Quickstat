using QuickStat.Data;
using Xunit;

namespace QuickStat.Tests.Data;

/// <summary>
/// The <c>:Name</c> to <c>@Name</c> scanner (PORT-PLAN.md R2).
/// </summary>
/// <remarks>
/// This gets more tests than anything else in step 2.2 because it is the one component whose input
/// is not in this repository: population SQL lives in <c>dbo.DbProcList</c> and is arbitrary T-SQL
/// written by users over many years. Every test below is a shape that would silently corrupt a
/// production query if the scanner got it wrong.
/// </remarks>
public class SqlTextRewriterTests
{
    private static readonly ColonToAtSqlTextRewriter Rewriter = new();

    private static RewrittenSql Rewrite(string sql) => Rewriter.Rewrite(sql);

    // ---------------------------------------------------------------- the happy path

    [Fact]
    public void RewritesASinglePlaceholder()
    {
        RewrittenSql result = Rewrite("EXEC dbo.GetStudyAndUser :StudyName");

        Assert.Equal("EXEC dbo.GetStudyAndUser @StudyName", result.CommandText);
        Assert.Equal<string>(["StudyName"], result.ParameterNames);
        Assert.False(result.HasRepeatedPlaceholder);
    }

    [Fact]
    public void ReportsPlaceholdersInFirstAppearanceOrder()
    {
        RewrittenSql result = Rewrite("EXEC dbo.AddSession :StudyId,:CompName,:CompUser,:CompTime,:AppVer");

        Assert.Equal(
            "EXEC dbo.AddSession @StudyId,@CompName,@CompUser,@CompTime,@AppVer",
            result.CommandText);

        Assert.Equal<string>(["StudyId", "CompName", "CompUser", "CompTime", "AppVer"], result.ParameterNames);
    }

    [Fact]
    public void LeavesTextWithoutPlaceholdersExactlyAsItWas()
    {
        const string Sql = "SELECT\tStudyId\r\nFROM dbo.Study\r\nWHERE StudName = 'NDV'";

        RewrittenSql result = Rewrite(Sql);

        Assert.Equal(Sql, result.CommandText);
        Assert.Empty(result.ParameterNames);
        Assert.False(result.HasRepeatedPlaceholder);
    }

    [Theory]
    [InlineData(":a", "@a")]
    [InlineData(":_", "@_")]
    [InlineData(":_x1", "@_x1")]
    [InlineData(":X9_y", "@X9_y")]
    public void AcceptsIdentifierStartAndPartCharacters(string sql, string expected) =>
        Assert.Equal(expected, Rewrite(sql).CommandText);

    [Theory]
    [InlineData("SELECT :Id, 1")]
    [InlineData("SELECT :Id)")]
    [InlineData("SELECT :Id;")]
    [InlineData("SELECT :Id\r\n")]
    [InlineData("WHERE a=:Id AND b=2")]
    public void StopsTheNameAtTheFirstNonIdentifierCharacter(string sql)
    {
        RewrittenSql result = Rewrite(sql);

        Assert.Equal<string>(["Id"], result.ParameterNames);
        Assert.Equal(sql.Replace(":Id", "@Id", StringComparison.Ordinal), result.CommandText);
    }

    // ---------------------------------------------------------------- not a placeholder

    [Theory]
    [InlineData("SELECT * FROM ::fn_helpcollations()")]
    [InlineData("SELECT a::b FROM t")]
    [InlineData("SELECT 1 -- trailing colon :")]
    [InlineData("SELECT 12:30")]
    [InlineData("SELECT :")]
    [InlineData("SELECT :9x")]
    public void LeavesColonsThatCannotStartAPlaceholderAlone(string sql)
    {
        RewrittenSql result = Rewrite(sql);

        Assert.Equal(sql, result.CommandText);
        Assert.Empty(result.ParameterNames);
    }

    [Fact]
    public void TreatsTheThirdColonOfARunAsAPlaceholderMarker()
    {
        // ':::Name' is ':' ':' then ':Name'. The pair is consumed as scope resolution and the
        // remaining colon is a marker. Documented rather than defended: no real statement has this
        // shape, and both readings are defensible.
        RewrittenSql result = Rewrite(":::Name");

        Assert.Equal("::@Name", result.CommandText);
        Assert.Equal<string>(["Name"], result.ParameterNames);
    }

    // ---------------------------------------------------------------- single-quoted literals

    [Theory]
    [InlineData("SELECT '23:59'")]
    [InlineData("SELECT 'the value is :Name'")]
    [InlineData("SELECT N'unicode :Name'")]
    [InlineData("SELECT 'it''s :Name, honest'")]
    [InlineData("SELECT '''  :Name  '''")]
    public void SkipsSingleQuotedLiterals(string sql)
    {
        RewrittenSql result = Rewrite(sql);

        Assert.Equal(sql, result.CommandText);
        Assert.Empty(result.ParameterNames);
    }

    [Fact]
    public void RewritesAfterAClosedLiteral()
    {
        RewrittenSql result = Rewrite("WHERE Code = 'a:b' AND StudyId = :StudyId");

        Assert.Equal("WHERE Code = 'a:b' AND StudyId = @StudyId", result.CommandText);
        Assert.Equal<string>(["StudyId"], result.ParameterNames);
    }

    [Fact]
    public void RewritesBetweenTwoLiterals()
    {
        RewrittenSql result = Rewrite("SELECT 'a:1', :Mid, 'b:2'");

        Assert.Equal("SELECT 'a:1', @Mid, 'b:2'", result.CommandText);
        Assert.Equal<string>(["Mid"], result.ParameterNames);
    }

    [Fact]
    public void TreatsAnUnterminatedLiteralAsRunningToTheEnd()
    {
        // The statement cannot execute either way; inventing a parameter out of quoted text would
        // be the worse failure.
        const string Sql = "SELECT 'oops :Name";

        Assert.Equal(Sql, Rewrite(Sql).CommandText);
        Assert.Empty(Rewrite(Sql).ParameterNames);
    }

    [Fact]
    public void DoesNotSeeCommentMarkersInsideALiteral()
    {
        RewrittenSql result = Rewrite("SELECT '-- /* :Hidden', :Real");

        Assert.Equal("SELECT '-- /* :Hidden', @Real", result.CommandText);
        Assert.Equal<string>(["Real"], result.ParameterNames);
    }

    // ---------------------------------------------------------------- bracketed identifiers

    [Theory]
    [InlineData("SELECT [a:b] FROM t")]
    [InlineData("SELECT [x]]:y] FROM t")]
    [InlineData("SELECT [-- :Name] FROM t")]
    public void SkipsBracketedIdentifiers(string sql)
    {
        RewrittenSql result = Rewrite(sql);

        Assert.Equal(sql, result.CommandText);
        Assert.Empty(result.ParameterNames);
    }

    [Fact]
    public void RewritesAfterABracketedIdentifier()
    {
        RewrittenSql result = Rewrite("SELECT [My:Column] FROM t WHERE Id = :Id");

        Assert.Equal("SELECT [My:Column] FROM t WHERE Id = @Id", result.CommandText);
        Assert.Equal<string>(["Id"], result.ParameterNames);
    }

    // ---------------------------------------------------------------- quoted identifiers

    [Theory]
    [InlineData("SELECT \"a:b\" FROM t")]
    [InlineData("SELECT \"a\"\"b:c\" FROM t")]
    public void SkipsDoubleQuotedIdentifiers(string sql)
    {
        RewrittenSql result = Rewrite(sql);

        Assert.Equal(sql, result.CommandText);
        Assert.Empty(result.ParameterNames);
    }

    [Fact]
    public void RewritesAfterAQuotedIdentifier()
    {
        RewrittenSql result = Rewrite("SELECT \"od:d\" FROM t WHERE Id=:Id");

        Assert.Equal("SELECT \"od:d\" FROM t WHERE Id=@Id", result.CommandText);
        Assert.Equal<string>(["Id"], result.ParameterNames);
    }

    // ---------------------------------------------------------------- line comments

    [Fact]
    public void SkipsLineComments()
    {
        const string Sql = "SELECT 1 -- and :Name is only a note";

        Assert.Equal(Sql, Rewrite(Sql).CommandText);
        Assert.Empty(Rewrite(Sql).ParameterNames);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    public void EndsALineCommentAtTheLineBreak(string lineBreak)
    {
        string sql = $"-- :Hidden{lineBreak}SELECT :Real";

        RewrittenSql result = Rewrite(sql);

        Assert.Equal($"-- :Hidden{lineBreak}SELECT @Real", result.CommandText);
        Assert.Equal<string>(["Real"], result.ParameterNames);
    }

    [Fact]
    public void DoesNotOpenALiteralFromAnApostropheInsideALineComment()
    {
        // The nastiest realistic shape: a Norwegian comment with an apostrophe in it. Treating that
        // apostrophe as a string opener would swallow the rest of the statement and lose every
        // later placeholder.
        RewrittenSql result = Rewrite("-- pasientens 'siste' verdi, se :Ignored\r\nSELECT :StudyId");

        Assert.Equal("-- pasientens 'siste' verdi, se :Ignored\r\nSELECT @StudyId", result.CommandText);
        Assert.Equal<string>(["StudyId"], result.ParameterNames);
    }

    [Fact]
    public void ASingleHyphenIsNotAComment()
    {
        RewrittenSql result = Rewrite("SELECT 1 - :Offset");

        Assert.Equal("SELECT 1 - @Offset", result.CommandText);
        Assert.Equal<string>(["Offset"], result.ParameterNames);
    }

    // ---------------------------------------------------------------- block comments

    [Fact]
    public void SkipsBlockComments()
    {
        const string Sql = "SELECT /* :Name */ 1";

        Assert.Equal(Sql, Rewrite(Sql).CommandText);
        Assert.Empty(Rewrite(Sql).ParameterNames);
    }

    [Fact]
    public void SkipsNestedBlockComments()
    {
        // T-SQL nests block comments, so a scanner that stops at the first '*/' would resume inside
        // the outer comment and rewrite whatever followed.
        RewrittenSql result = Rewrite("/* outer /* inner :Hidden */ still outer :AlsoHidden */ SELECT :Real");

        Assert.Equal(
            "/* outer /* inner :Hidden */ still outer :AlsoHidden */ SELECT @Real",
            result.CommandText);

        Assert.Equal<string>(["Real"], result.ParameterNames);
    }

    [Fact]
    public void SkipsBlockCommentsContainingStars()
    {
        RewrittenSql result = Rewrite("/* a ** b * / :Hidden */ SELECT :Real");

        Assert.Equal("/* a ** b * / :Hidden */ SELECT @Real", result.CommandText);
        Assert.Equal<string>(["Real"], result.ParameterNames);
    }

    [Fact]
    public void TreatsAnUnterminatedBlockCommentAsRunningToTheEnd()
    {
        const string Sql = "SELECT 1 /* :Name and no close";

        Assert.Equal(Sql, Rewrite(Sql).CommandText);
        Assert.Empty(Rewrite(Sql).ParameterNames);
    }

    [Fact]
    public void ASoloSlashIsNotACommentOpener()
    {
        RewrittenSql result = Rewrite("SELECT 10 / :Divisor");

        Assert.Equal("SELECT 10 / @Divisor", result.CommandText);
        Assert.Equal<string>(["Divisor"], result.ParameterNames);
    }

    [Fact]
    public void DoesNotOpenALiteralFromAnApostropheInsideABlockComment()
    {
        RewrittenSql result = Rewrite("/* don't rewrite :Hidden */ SELECT :Real");

        Assert.Equal("/* don't rewrite :Hidden */ SELECT @Real", result.CommandText);
        Assert.Equal<string>(["Real"], result.ParameterNames);
    }

    // ---------------------------------------------------------------- repetition

    [Fact]
    public void ReportsARepeatedPlaceholderOnceAndFlagsIt()
    {
        RewrittenSql result = Rewrite("SELECT * FROM t WHERE a = :Id OR b = :Id");

        Assert.Equal("SELECT * FROM t WHERE a = @Id OR b = @Id", result.CommandText);
        Assert.Equal<string>(["Id"], result.ParameterNames);
        Assert.True(result.HasRepeatedPlaceholder);
    }

    [Fact]
    public void NormalisesTheCasingOfARepeatedPlaceholderToItsFirstAppearance()
    {
        // One SqlParameter is emitted, so both references have to spell it the same way; otherwise
        // a case-sensitive server collation would reject the second one.
        RewrittenSql result = Rewrite("WHERE a = :Id AND b = :ID AND c = :id");

        Assert.Equal("WHERE a = @Id AND b = @Id AND c = @Id", result.CommandText);
        Assert.Equal<string>(["Id"], result.ParameterNames);
        Assert.True(result.HasRepeatedPlaceholder);
    }

    [Fact]
    public void DoesNotFlagDistinctNamesThatMerelyShareAPrefix()
    {
        RewrittenSql result = Rewrite("WHERE a=:Id AND b=:IdList AND c=:Id2");

        Assert.Equal<string>(["Id", "IdList", "Id2"], result.ParameterNames);
        Assert.False(result.HasRepeatedPlaceholder);
    }

    // ---------------------------------------------------------------- what step 2.3 needs

    [Fact]
    public void DetectsThePeriodPair()
    {
        // Emetra.Database.ParameterDictionary.pas:96-98 - the period dialog is shown only when both
        // names are present, and step 2.3 asks this rewriter.
        RewrittenSql result = Rewrite(
            "SELECT PersonId, FullName FROM dbo.StudCase WHERE Created >= :StartDate AND Created < :StopDate");

        Assert.Contains("StartDate", result.ParameterNames);
        Assert.Contains("StopDate", result.ParameterNames);
    }

    [Fact]
    public void DoesNotDetectThePeriodPairInsideAComment()
    {
        RewrittenSql result = Rewrite("-- :StartDate :StopDate were removed\r\nSELECT 1");

        Assert.Empty(result.ParameterNames);
    }

    // ---------------------------------------------------------------- everything at once

    [Fact]
    public void HandlesAPopulationQueryThatUsesEverySkippedConstruct()
    {
        const string Sql = """
            /* Utvalg: pasienter i perioden.
               NB: /* nøstet kommentar med :Ikke_en_parameter */
               Skrevet 2019. */
            SELECT  sc.PersonId,
                    p.FullName,
                    [Tid:Punkt]      AS [Tid:Punkt],   -- kolonnen heter faktisk dette, :Ikke_heller
                    "Rar:Kolonne"    AS Alias
            FROM    dbo.StudCase sc
            JOIN    dbo.Person   p ON p.PersonId = sc.PersonId
            WHERE   sc.StudyId  = :StudyId
              AND   sc.Created >= :StartDate
              AND   sc.Created <  :StopDate
              AND   CONVERT(varchar(5), sc.Created, 108) <> '23:59'
              AND   sc.Note NOT LIKE '%:%'
              AND   sc.Owner = :StudyId
            OPTION (RECOMPILE)
            """;

        RewrittenSql result = Rewrite(Sql);

        Assert.Equal<string>(["StudyId", "StartDate", "StopDate"], result.ParameterNames);
        Assert.True(result.HasRepeatedPlaceholder);

        Assert.Contains("sc.StudyId  = @StudyId", result.CommandText, StringComparison.Ordinal);
        Assert.Contains("sc.Owner = @StudyId", result.CommandText, StringComparison.Ordinal);

        // Nothing outside the three real placeholders may have moved.
        Assert.Contains(":Ikke_en_parameter", result.CommandText, StringComparison.Ordinal);
        Assert.Contains(":Ikke_heller", result.CommandText, StringComparison.Ordinal);
        Assert.Contains("[Tid:Punkt]", result.CommandText, StringComparison.Ordinal);
        Assert.Contains("\"Rar:Kolonne\"", result.CommandText, StringComparison.Ordinal);
        Assert.Contains("'23:59'", result.CommandText, StringComparison.Ordinal);
        Assert.Contains("'%:%'", result.CommandText, StringComparison.Ordinal);

        Assert.Equal(
            Sql.Length,
            result.CommandText.Length);
    }

    // ---------------------------------------------------------------- mechanics

    [Fact]
    public void IsStableAcrossRepeatedCalls()
    {
        const string Sql = "EXEC dbo.GetPopulations :StudyId";

        RewrittenSql first = Rewrite(Sql);
        RewrittenSql second = Rewrite(Sql);

        Assert.Equal(first.CommandText, second.CommandText);
        Assert.Equal(first.ParameterNames, second.ParameterNames);
        Assert.Equal(first.HasRepeatedPlaceholder, second.HasRepeatedPlaceholder);
    }

    [Fact]
    public void SurvivesFillingItsCache()
    {
        ColonToAtSqlTextRewriter rewriter = new();

        for (int i = 0; i < (ColonToAtSqlTextRewriter.CacheCapacity * 2) + 5; i++)
        {
            RewrittenSql result = rewriter.Rewrite($"SELECT {i} WHERE Id = :Id{i}");

            Assert.Equal<string>([$"Id{i}"], result.ParameterNames);
        }

        Assert.Equal("SELECT 0 WHERE Id = @Id0", rewriter.Rewrite("SELECT 0 WHERE Id = :Id0").CommandText);
    }

    [Fact]
    public void RejectsNull() => Assert.Throws<ArgumentNullException>(() => Rewriter.Rewrite(null!));

    [Fact]
    public void HandlesTheEmptyStatement()
    {
        RewrittenSql result = Rewrite("");

        Assert.Equal("", result.CommandText);
        Assert.Empty(result.ParameterNames);
    }
}
