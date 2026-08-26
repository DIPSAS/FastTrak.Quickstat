using QuickStat.Collectors;
using QuickStat.Collectors.Registry;
using QuickStat.Collectors.Sql;
using Xunit;

namespace QuickStat.Tests.Collectors;

/// <summary>
/// Generated SQL. <see cref="ICollector.BuildSql"/> is a pure function, so the whole subsystem is
/// testable without a database (PORT-PLAN.md R9) - and it has to <em>stay</em> pure, because
/// Phase 5's golden files compare its output byte for byte (R3).
/// </summary>
public class CollectorSqlTests
{
    private static readonly CollectorSqlContext Context = CollectorTestContext.SqlContext;

    [Fact]
    public void BuildSqlIsDeterministic()
    {
        foreach (ICollector collector in CollectorCatalog.All)
        {
            string first = collector.BuildSql(Context);
            string second = collector.BuildSql(Context);

            Assert.Equal(first, second);
        }
    }

    [Fact]
    public void BuildSqlNeverLeavesAPlaceholderBehind()
    {
        foreach (ICollector collector in CollectorCatalog.All)
        {
            string sql = collector.BuildSql(Context);

            Assert.DoesNotContain(QaSql.PidList, sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(QaSql.ItemList, sql, StringComparison.Ordinal);
            Assert.DoesNotContain(QaSql.LabList, sql, StringComparison.Ordinal);
            Assert.DoesNotContain(QaSql.FormName, sql, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void PidBindingAgreesWithTheGeneratedStatement()
    {
        // The descriptor's binding is derived from the SQL rather than typed out per collector, and
        // this is the assertion that keeps the derivation honest end to end.
        List<ICollector> all = [.. CollectorCatalog.All, .. CollectorRegistryBuilder.CreateFormCollectors([new FormClass("BARTHEL", "Barthel")])];

        foreach (ICollector collector in all)
        {
            string sql = collector.BuildSql(Context);
            string name = collector.Descriptor.Name;

            switch (collector.Descriptor.PidBinding)
            {
                case PidBinding.IdList:
                    Assert.True(sql.Contains(CollectorTestContext.IdListToken, StringComparison.Ordinal), $"{name} claims IdList but its statement has no person-id fragment.");
                    break;

                case PidBinding.SinglePerson:
                    Assert.True(sql.Contains(QaSql.PersonIdParameter, StringComparison.Ordinal), $"{name} claims SinglePerson but its statement has no :PersonId.");
                    break;

                case PidBinding.None:
                    Assert.False(sql.Contains(CollectorTestContext.IdListToken, StringComparison.Ordinal), $"{name} claims None but its statement takes a person-id fragment.");
                    Assert.False(sql.Contains(QaSql.PersonIdParameter, StringComparison.Ordinal), $"{name} claims None but its statement takes :PersonId.");
                    break;

                default:
                    Assert.Fail($"{name} has an unknown PidBinding.");
                    break;
            }
        }
    }

    [Fact]
    public void OnlyTheFormInstanceCollectorTakesOneRoundTripPerPatient()
    {
        // TFormDataCollector joined it upstream until SpSnapshotFormDataAll replaced its query;
        // PORT-PLAN.md §8.5 takes that change, so the form-instance collector is the last one left.
        List<string> singlePerson =
        [
            .. System.Linq.Enumerable.Select(
                System.Linq.Enumerable.Where(CollectorCatalog.All, c => c.Descriptor.PidBinding == PidBinding.SinglePerson),
                c => c.Descriptor.Name),
        ];

        Assert.Equal([CollectorNames.FormFrequency], singlePerson);
        Assert.Equal(1, CollectorTestContext.ByName(CollectorCatalog.All, CollectorNames.FormFrequency).Descriptor.BatchSize);
    }

    [Fact]
    public void MostWholeCohortCollectorsCarryNoPersonIdFragmentAtAll()
    {
        // PORT-PLAN.md R10: preserved for parity, recorded as a performance follow-up. The
        // exceptions are the drug-set collectors built on SQL_WHERE_PERSON_LIST.
        List<string> wholeCohortWithIdList =
        [
            .. System.Linq.Enumerable.Select(
                System.Linq.Enumerable.Where(
                    CollectorCatalog.All,
                    c => c.Descriptor.BatchSize == int.MaxValue && c.Descriptor.PidBinding == PidBinding.IdList),
                c => c.Descriptor.Name),
        ];

        Assert.Equal(
            new[]
            {
                CollectorNames.DrugMetformin,
                CollectorNames.DrugAnticholinergicN05,
                CollectorNames.DrugAnticholinergicAb,
                CollectorNames.DrugAntibioticResistance,
                CollectorNames.DrugAntibioticIntermediate,
                CollectorNames.DrugAntibioticRecommended,
            },
            wholeCohortWithIdList);
    }

    [Fact]
    public void EveryStatementProjectsSomething()
    {
        foreach (ICollector collector in CollectorCatalog.All)
        {
            string sql = collector.BuildSql(Context).TrimStart();

            Assert.True(
                sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) || sql.StartsWith("EXEC", StringComparison.OrdinalIgnoreCase),
                $"{collector.Descriptor.Name} generated a statement that is neither a SELECT nor an EXEC: {sql}");
        }
    }

    [Fact]
    public void DemographicsStatementIsVerbatim() =>
        AssertSql(
            CollectorNames.PatientAge,
            "SELECT PersonId,'AGE' AS VarName, DATEDIFF(YYYY,DOB,GETDATE()) AS DpValue, GETDATE() AS VarDate, PersonId AS ResultId " +
            "FROM dbo.Person WHERE (PersonId IN (/*PIDS*/))");

    [Fact]
    public void StudyScopedStatementFoldsInTheStudyId() =>
        AssertSql(
            CollectorNames.StudyStatus,
            "SELECT sc.PersonId, 'StatusId' AS VarName, sc.FinState AS DpValue, GETDATE(), sc.StudCaseId AS RowId " +
            "FROM dbo.StudCase sc " +
            "WHERE sc.StudyId = 42");

    [Fact]
    public void DrugChecksumStatementIsVerbatimIncludingItsTrailingSpace() =>
        AssertSql(
            "DRUG.A10",
            "SELECT ot.PersonId, CONCAT('A10','.',ot.TreatType) AS VarName, ABS(CHECKSUM(ot.DrugName)) % 100000 AS DpValue, ot.StartAt, ot.TreatId, ai.AtcName AS Caption " +
            "FROM dbo.OngoingTreatment ot " +
            "LEFT JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC " +
            "WHERE ( PersonId IN (/*PIDS*/) ) " +
            "AND ot.ATC COLLATE Latin1_General_CI_AI LIKE 'A10%' COLLATE Latin1_General_CI_AI ");

    [Fact]
    public void ResistanceDrivingStatementHasThreeAtcGroupsAndNoJ01Ff()
    {
        // PORT-PLAN.md §8.4. Release-blocking for this collector until a protocol owner confirms,
        // but the evidence is that J01FF is absent from all nine baselines capable of building the
        // application. Pinned as an exact string so that changing it back is a visible diff.
        AssertSql(
            CollectorNames.DrugAntibioticResistance,
            "SELECT PersonId, 'RESISTANCE_DRIVING' AS VarName, ABS(CHECKSUM(DrugName)) % 100000 AS DpValue, StartAt, TreatId, ai.AtcName AS Caption " +
            "FROM dbo.OngoingTreatment ot " +
            "LEFT JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC " +
            "WHERE ( PersonId IN (/*PIDS*/) ) " +
            "AND (   ( ot.ATC COLLATE Latin1_General_CI_AI LIKE 'J01CR%' COLLATE Latin1_General_CI_AI ) " +
            "OR ( ot.ATC COLLATE Latin1_General_CI_AI LIKE 'J01D[CDH]%' COLLATE Latin1_General_CI_AI ) " +
            "OR   ( ot.ATC COLLATE Latin1_General_CI_AI LIKE 'J01MA%' COLLATE Latin1_General_CI_AI ) )");

        Assert.Equal(
            new[] { "J01CR%", "J01D[CDH]%", "J01MA%" },
            DrugSql.ResistanceDrivingAtcPatterns);
    }

    [Fact]
    public void IntermediateAntibioticStatementDelegatesItsSelectionToTheKnowledgeBase() =>
        // Docs/Port/03-collectors.md §E.1, resolved verbatim. Note the order - the KB join goes
        // between the ATC-index join and the WHERE - and the trailing space, both upstream.
        AssertSql(
            CollectorNames.DrugAntibioticIntermediate,
            "SELECT PersonId, 'INTERMEDIATE_AB' AS VarName, ABS(CHECKSUM(DrugName)) % 100000 AS DpValue, StartAt, TreatId, ai.AtcName AS Caption " +
            "FROM dbo.OngoingTreatment ot " +
            "LEFT JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC " +
            "JOIN KB.AntibioticResistance2 r2 ON r2.AtcCode = ot.ATC " +
            "WHERE ( PersonId IN (/*PIDS*/) ) ");

    [Fact]
    public void RecommendedAntibioticStatementListsNineExactCodesWithNoCollation()
    {
        // Docs/Port/03-collectors.md §E.2, resolved verbatim. The plain IN is the point: every other
        // drug query wraps its comparison in COLLATE Latin1_General_CI_AI and this one does not.
        AssertSql(
            CollectorNames.DrugAntibioticRecommended,
            "SELECT PersonId, 'RECOMMENDED_AB' AS VarName, ABS(CHECKSUM(DrugName)) % 100000 AS DpValue, StartAt, TreatId, ai.AtcName AS Caption " +
            "FROM dbo.OngoingTreatment ot " +
            "LEFT JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC " +
            "WHERE ( PersonId IN (/*PIDS*/) ) " +
            "AND ( ot.ATC IN ( 'J01CE01', 'J01CE02', 'J01CF01', 'J01CF02', 'J01CA08', 'J01CA11', 'J01EA01', 'J01EE01', 'J01XE01' ) )");

        Assert.Equal(
            new[] { "J01CE01", "J01CE02", "J01CF01", "J01CF02", "J01CA08", "J01CA11", "J01EA01", "J01EE01", "J01XE01" },
            DrugSql.RecommendedAntibioticAtcCodes);

        Assert.DoesNotContain(
            QaSql.Collation,
            CollectorTestContext.ByName(CollectorCatalog.All, CollectorNames.DrugAntibioticRecommended).BuildSql(Context),
            StringComparison.Ordinal);
    }

    [Fact]
    public void MetenamineStatementEmitsTheLiteralOneAndMatchesOneExactCode()
    {
        // Docs/Port/03-collectors.md §E.2.2. TDrugCollector.CreateBasic, so the value column is the
        // literal 1 rather than a drug-name checksum, and the pattern carries no % - an exact match
        // written with LIKE so the collation behaviour is identical to every other drug query.
        AssertSql(
            CollectorNames.DrugJ01Xx05,
            "SELECT ot.PersonId, CONCAT('J01XX05','.',ot.TreatType) AS VarName, 1 AS DpValue, ot.StartAt, ot.TreatId, ai.AtcName AS Caption " +
            "FROM dbo.OngoingTreatment ot " +
            "LEFT JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC " +
            "WHERE ( PersonId IN (/*PIDS*/) ) " +
            "AND ot.ATC COLLATE Latin1_General_CI_AI LIKE 'J01XX05' COLLATE Latin1_General_CI_AI ");

        Assert.Equal("J01XX05", AtcPatterns.J01Xx05);
        Assert.Equal("DRUG.J01XX05", CollectorNames.DrugPrefix + SqlLiteral.AtcPatternToVariableName(AtcPatterns.J01Xx05));
    }

    [Fact]
    public void OnlyTheIntermediateAntibioticStatementTouchesTheKbSchema()
    {
        // The KB schema is the one non-dbo dependency in the whole subsystem, and it is the reason
        // CollectorAvailability exists. A second one appearing silently would be a regression.
        List<string> withKb =
        [
            .. CollectorCatalog.All
                .Where(collector => collector.BuildSql(Context).Contains("KB.", StringComparison.Ordinal))
                .Select(collector => collector.Descriptor.Name),
        ];

        Assert.Equal([CollectorNames.DrugAntibioticIntermediate], withKb);
    }

    [Fact]
    public void LabCountStatementNamesItsOwnWindow() =>
        AssertSql(
            CollectorNames.LabCount24M,
            "SELECT PersonId,'LABCOUNT24M' AS VarName, COUNT(*) AS n, MAX(LabDate) AS MaxLabDate, MAX(ResultId) AS MaxResultId " +
            "FROM LabData " +
            "WHERE DATEDIFF(MM,LabDate,GETDATE()) < 24 " +
            "GROUP BY PersonId");

    [Fact]
    public void KidneyLabSetResolvesTheDelphiSetToAscendingOrdinals()
    {
        // LABSET_KIDNEY is the only set expressed as a Delphi "set of TLabTest". Docs/Port §B.7's
        // ordinal table is one too high for the kidney members; 49 / 50 / 53 / 54 are corroborated
        // by LABCLASSES_KIDNEY and by the eGFR colour registrations in QuickStat.Collectors.pas.
        Assert.Equal(new[] { 3, 4, 5, 6, 7, 49, 50, 53, 54, 90, 91 }, LabClassSets.Kidney);
        AssertContains(CollectorNames.LabKidney, "la.LabClassId IN (3, 4, 5, 6, 7, 49, 50, 53, 54, 90, 91)");
    }

    [Fact]
    public void RoasBaseCarriesAll68ItemIdsInTheDelphiOrder()
    {
        // Docs/Port/03-collectors.md §E.3, count-verified against EPR.QA.Definitions.pas:103-105.
        // Order is observable in the generated IN ( … ) list, so it is asserted rather than the set.
        Assert.Equal(68, ItemSets.RoasBase.Count);
        Assert.Equal(ItemSets.RoasBase.Count, ItemSets.RoasBase.Distinct().Count());

        Assert.Equal(
            new[]
            {
                4255, 6314, 3486, 6312, 6323, 6313, 6324, 6299, 6089, 6090,
                6321, 6332, 3410, 6328, 6317, 6327, 6316, 6326, 8594, 8595,
                6318, 6334, 6329, 3411, 6330, 6320, 6331, 6322, 6333, 8543,
                8544, 6669, 6670, 6671, 6607, 5069, 3982, 6633, 6634, 6635,
                6636, 6637, 6638, 6639, 6640, 6808, 6641, 5170, 9996, 3983,
                7135, 4002, 6682, 3985, 8797, 6605, 2143, 9477, 10643, 3846,
                3981, 6804, 6805, 6802, 6803, 7977, 7979, 6807,
            },
            ItemSets.RoasBase);

        // TVarSetCollector.CreateForNumeric: SpSnapshotVarset( itNumeric, … ), prefix '', batch 100.
        ICollector roasBase = CollectorTestContext.ByName(CollectorCatalog.All, CollectorNames.RoasBase);

        Assert.Equal(string.Empty, roasBase.Descriptor.VarPrefix);
        Assert.Equal(CollectorKind.VarSet, roasBase.Descriptor.Kind);
        Assert.Equal(100, roasBase.Descriptor.BatchSize);

        AssertContains(CollectorNames.RoasBase, "cdp.Quantity AS DpValue");
        AssertContains(CollectorNames.RoasBase, "ISNULL(cdp.Quantity,-1) <> -1");
        AssertContains(
            CollectorNames.RoasBase,
            "AND ( cdp.ItemId IN ( " +
            "4255, 6314, 3486, 6312, 6323, 6313, 6324, 6299, 6089, 6090, " +
            "6321, 6332, 3410, 6328, 6317, 6327, 6316, 6326, 8594, 8595, " +
            "6318, 6334, 6329, 3411, 6330, 6320, 6331, 6322, 6333, 8543, " +
            "8544, 6669, 6670, 6671, 6607, 5069, 3982, 6633, 6634, 6635, " +
            "6636, 6637, 6638, 6639, 6640, 6808, 6641, 5170, 9996, 3983, " +
            "7135, 4002, 6682, 3985, 8797, 6605, 2143, 9477, 10643, 3846, " +
            "3981, 6804, 6805, 6802, 6803, 7977, 7979, 6807 ) )");
    }

    [Fact]
    public void ThresholdIsRenderedWithAnInvariantDecimalSeparator()
    {
        // SpSnapshotQuantityIfBelowThreshold passes explicit en-US format settings so that a
        // Norwegian machine does not emit "120,0".
        AssertContains(CollectorNames.GbdLowBp, "WHERE v.Quantity < 120");
        AssertContains(CollectorNames.GbdLowBp, "dbo.GetLastQuantityTable( 3556, NULL )");
        // Ordinal, and it is not a formality. Without it this binds to the culture-sensitive
        // overload, and a collation that treats "," as ignorable punctuation matches ",0" against
        // the ", 0" in "v.EventTime, 0 AS RowId" - so the test fails while the SQL is perfectly
        // correct. The same confusion in the other direction is what §8.8(i) warns about for the two
        // list filters: a byte scan and a collation are different questions.
        Assert.DoesNotContain(
            ",0",
            CollectorTestContext.ByName(CollectorCatalog.All, CollectorNames.GbdLowBp).BuildSql(Context),
            StringComparison.Ordinal);
    }

    [Fact]
    public void DoctorNoteGroupExpandsToFourQuotedFormNames() =>
        AssertContains(
            CollectorNames.GbdDoctorNotes3M,
            "UPPER('GBDLEGE') AS VarName");

    [Fact]
    public void DoctorNoteGroupListsItsFourForms() =>
        AssertContains(
            CollectorNames.GbdDoctorNotes3M,
            "mf.FormName IN ( 'GBD_NOTAT_LEGE','GBD_STATUS_PRESENS','GBD_INFECTION','GBD_BESLUTNINGER' )");

    [Fact]
    public void DiagnosePatternBecomesBothTheNameAndTheEmittedVariable()
    {
        ICollector stroke = CollectorTestContext.ByName(CollectorCatalog.All, "DX.I6x01234");

        Assert.Equal("DX.", stroke.Descriptor.VarPrefix);
        AssertContains("DX.I6x01234", "'I6x01234' AS VarName");
        AssertContains("DX.I6x01234", "mni.ItemCode LIKE 'I6[01234]%'");
    }

    [Fact]
    public void DruidPatternsSurviveTheDelphiFormatEscaping()
    {
        // 'DRUID#%%' goes through Format and comes out as 'DRUID#%'; 'DRUID%' does not.
        AssertContains(CollectorNames.DruidSpecific, "AlertClass LIKE 'DRUID#%'");
        AssertContains(CollectorNames.DruidCount, "AlertClass LIKE 'DRUID%'");
    }

    [Fact]
    public void NumericVarSetDiscardsMinusOneAsWellAsNull() =>
        // Deliberate defect, reproduced: a genuine quantity of exactly -1 is dropped.
        AssertContains(CollectorNames.GbdScores, "ISNULL(cdp.Quantity,-1) <> -1");

    [Fact]
    public void TextVarSetMeasuresTheLengthOfTheText() =>
        AssertContains(CollectorNames.GbdPrimaryContact, "DATALENGTH(cdp.TextVal) AS DpValue");

    [Fact]
    public void FormDataQueryCarriesTheDeterministicTieBreaker()
    {
        // PORT-PLAN.md §8.5: take RANK -> ROW_NUMBER "with a deterministic tie-breaker". Upstream
        // orders by ce.EventNum DESC alone, so which row wins among same-event duplicates is
        // arbitrary and two runs can disagree. This is the port's only change to that statement.
        string sql = CollectorRegistryBuilder.CreateFormCollectors([new FormClass("BARTHEL", "Barthel")])[1]
            .BuildSql(Context);

        Assert.Contains("ROW_NUMBER() OVER ( PARTITION BY ce.PersonId, mi.ItemId ORDER BY ce.EventNum DESC, dp.RowId DESC )", sql, StringComparison.Ordinal);
        Assert.Contains("dp.TextVal AS Caption", sql, StringComparison.Ordinal);
        Assert.Contains("ORDER BY agg.OrderNumber", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void TheIdListFragmentIsSubstitutedCaseInsensitivelyAndEverywhere()
    {
        // Delphi: StringReplace( …, [rfIgnoreCase, rfReplaceAll] ).
        const string Template = "A {IdList} B {idlist} C {IDLIST}";

        Assert.Equal("A X B X C X", Make.BindIdList(Template, new CollectorSqlContext(1, "X")));
    }

    [Theory]
    // Docs/Port/03-collectors.md §0.6.
    [InlineData("C0[23789]%", "C0x23789")]
    [InlineData("E1[014]%", "E1x014")]
    [InlineData("A10%", "A10")]
    [InlineData("A10BA02", "A10BA02")]
    [InlineData("I6[01234]%", "I6x01234")]
    [InlineData("F[123456789]%", "Fx123456789")]
    [InlineData("J01D[CDH]%", "J01DxCDH")]
    public void AtcPatternToVariableNameMatchesTheDelphiRegexes(string pattern, string expected) =>
        Assert.Equal(expected, SqlLiteral.AtcPatternToVariableName(pattern));

    [Fact]
    public void ListSeparatorIsACommaAndASpace()
    {
        Assert.Equal("3224, 3225, 3310", SqlLiteral.List([3224, 3225, 3310]));
        Assert.Equal("4771", SqlLiteral.List([4771]));
        Assert.Equal(string.Empty, SqlLiteral.List([]));
    }

    [Fact]
    public void QuoteDoublesEmbeddedApostrophes()
    {
        Assert.Equal("'BARTHEL'", SqlLiteral.Quote("BARTHEL"));
        Assert.Equal("'O''BRIEN'", SqlLiteral.Quote("O'BRIEN"));
        Assert.Equal("''''", SqlLiteral.Quote("'"));
    }

    private static void AssertSql(string collectorName, string expected) =>
        Assert.Equal(expected, CollectorTestContext.ByName(CollectorCatalog.All, collectorName).BuildSql(Context));

    private static void AssertContains(string collectorName, string expected) =>
        Assert.Contains(expected, CollectorTestContext.ByName(CollectorCatalog.All, collectorName).BuildSql(Context), StringComparison.Ordinal);
}
