namespace QuickStat.Collectors.Sql;

/// <summary>
/// The hand-written drug SQL from <c>EPR.QA.Collector.Drug.pas</c>, plus the antibiotic statements
/// that live in <c>EPR.QA.SQL.pas</c>.
/// </summary>
/// <remarks>
/// Split out of <see cref="QaSql"/> only for readability; the transcription rules in that type's
/// remarks apply here too.
/// </remarks>
public static class DrugSql
{
    /// <summary>
    /// <c>ABS(CHECKSUM(DrugName)) % 100000</c> - a pseudo-numeric "which drug" value.
    /// </summary>
    /// <remarks>
    /// Stable per drug name and small enough for a double cell. It is not a count and it means
    /// nothing numerically; it exists so that distinct drugs render as distinct values, with the
    /// readable name in <c>Caption</c>. Downstream exports depend on it - do not "improve" it
    /// (<c>Docs/Port/03-collectors.md</c> §B.9).
    /// </remarks>
    private const string NameChecksum = "ABS(CHECKSUM(DrugName)) % 100000 AS DpValue";

    /// <summary><c>QRY_NORGEP</c>.</summary>
    public const string NorGeP = "EXEC Report.NorGeP";

    /// <summary>
    /// The one table outside <c>dbo</c> the whole collector subsystem touches.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="DrugSetAntibioticIntermediate"/> inner-joins it, and nothing else does. It is not
    /// part of the core FastTrak schema, it is absent from many customer databases, and the original
    /// author first spelled it <c>AntibioticRestistance2</c> - so it may exist under that name
    /// instead (<c>EPR.QA.SQL.pas:453</c>, corrected by commit <c>4c96c3c3b</c>).
    /// </para>
    /// <para>
    /// Because the join is an <b>inner</b> join, a missing table makes the statement fail rather
    /// than return nothing, which is why the collector carries a
    /// <see cref="CollectorAvailability"/> gate (PORT-PLAN.md R7).
    /// </para>
    /// </remarks>
    public const string AntibioticResistanceKnowledgeBase = "KB.AntibioticResistance2";

    /// <summary><c>QRY_DRUGCOUNT_BY_TYPE</c> - ongoing treatments per treatment type.</summary>
    public const string DrugCountByType =
        "SELECT PersonId, TreatType, COUNT(*) AS DpValue, MAX(CreatedAt) AS LastDate, Max(TreatId) AS MaxTreatId " +
        QaSql.FromOngoingTreatment +
        "WHERE DATALENGTH(ATC) > 4 " +
        "GROUP BY PersonId, TreatType";

    /// <summary><c>QRY_DRUGSET_ANTICHOLIN_AB</c>.</summary>
    /// <remarks>External dependency: <c>dbo.KBAnticholinDrug</c>.</remarks>
    public const string AnticholinergicsAb =
        "SELECT PersonId, 'ANTICHOLIN_AB' AS VarName, " + NameChecksum + ", StartAt, TreatId, Caption " +
        "FROM " +
        "( " +
        "  SELECT ot.PersonId, ot.DrugName, ot.StartAt, ot.TreatId, ai.AtcName AS Caption, " +
        "    RANK() OVER ( PARTITION BY ot.PersonId ORDER BY ac.AlertLevel, ot.StartAt DESC ) AS ReverseOrder " +
        QaSql.FromOngoingTreatment +
        "  JOIN dbo.KBAnticholinDrug ac ON ac.ATC = ot.ATC AND ac.AlertLevel IN ( 'A','B') " +
        QaSql.JoinAtcIndex +
        ") agg " +
        QaSql.WherePersonList +
        "AND ( ReverseOrder = 1 )";

    /// <summary><c>QRY_DRUGSET_ANTICHOLIN_N05A</c>.</summary>
    public const string AnticholinergicsN05A =
        "SELECT PersonId, 'ANTICHOLIN_N05' AS VarName, " + NameChecksum + ", StartAt, TreatId, ai.AtcName AS Caption " +
        QaSql.FromOngoingTreatment +
        QaSql.JoinAtcIndex +
        QaSql.WherePersonList +
        "AND ( ot.ATC " + QaSql.Collation + " LIKE 'N05A%' " + QaSql.Collation + " ) " +
        "AND NOT ( ( ot.ATC" + QaSql.Collation + "LIKE 'N05AH0[34]'" + QaSql.Collation + ") OR ( ot.ATC" + QaSql.Collation + "LIKE 'N05AN%'" + QaSql.Collation + ") )";

    /// <summary><c>QRY_DRUGSET_METFORMIN</c> - metformin including combination products.</summary>
    public const string Metformin =
        "SELECT PersonId, 'METFORMIN' AS VarName, " + NameChecksum + ", StartAt, TreatId, Caption " +
        "FROM " +
        "( " +
        "  SELECT ot.PersonId, ot.DrugName, ot.StartAt, ot.TreatId, ai.AtcName AS Caption, " +
        "    RANK() OVER ( PARTITION BY ot.PersonId ORDER BY ot.StartAt DESC ) AS ReverseOrder " +
        QaSql.FromOngoingTreatment +
        "  JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC AND ai.AtcName LIKE '%METFORMIN%' " +
        ") agg " +
        QaSql.WherePersonList +
        "AND ( ReverseOrder = 1 )";

    /// <summary>
    /// <c>QRY_DRUGCOUNT_BY_ATCGROUP</c> - four <c>UNION</c>ed levels of ATC truncation.
    /// </summary>
    /// <remarks>
    /// The missing space between <c>)</c> and <c>GROUP BY</c> in each branch is upstream and is
    /// preserved; SQL Server does not care and the golden files do.
    /// </remarks>
    public const string DrugCountByAtcGroup =
        "SELECT PersonId, ATC, COUNT(*) AS n, MAX(StartAt) AS LastDate, MAX(TreatId) AS MaxTreatId " +
        QaSql.FromOngoingTreatment +
        "WHERE ATC IN (" +
        "'J01XX04','M04AC01','N05CM02' )" +
        "GROUP BY PersonId, ATC " +
        "UNION " +
        "SELECT PersonId, SUBSTRING(ATC,1,5) AS ATCFragment, COUNT(*) AS n, MAX(StartAt) AS LastDate, MAX(TreatId) AS MaxTreatId " +
        QaSql.FromOngoingTreatment +
        "WHERE SUBSTRING(ATC,1,5) IN (" +
        "'A10BA','B01AE','B01AF','B03BA','B03BB','G04BD','M04AA','M04AB','N02AA','N02AB'," +
        "'N02AE','N02AG','N02AJ','N02AX','N02BA','N05BA','N05BB','N05CD','N05CF','N05CH'," +
        "'N06DA','N06DX','R03AC','R03AK','R03BB','R03DA','R06AD','R06AE','R06AX' )" +
        "GROUP BY PersonId, SUBSTRING(ATC,1,5) " +
        "UNION " +
        "SELECT PersonId, SUBSTRING(ATC,1,4) AS ATCFragment, COUNT(*) AS n, MAX(StartAt) AS LastDate, MAX(TreatId) AS MaxTreatId " +
        QaSql.FromOngoingTreatment +
        "WHERE SUBSTRING(ATC,1,4) IN (" +
        "'A02A','A02B','A06A','A10A','A10B','B01A','B01C','B03A','C01A','G04C'," +
        "'H03A','M01A','N03A','N05A','N06A','N06D','S01E' )" +
        "GROUP BY PersonId, SUBSTRING(ATC,1,4) " +
        "UNION " +
        "SELECT PersonId, SUBSTRING(ATC,1,3) AS ATCFragment, COUNT(*) AS n, MAX(StartAt) AS LastDate, MAX(TreatId) AS MaxTreatId " +
        QaSql.FromOngoingTreatment +
        "WHERE SUBSTRING(ATC,1,3) IN (" +
        "'A11','C02','C03','C07','C08','C09','H02','N04' )" +
        "GROUP BY PersonId, SUBSTRING(ATC,1,3) " +
        "ORDER BY PersonId";

    /// <summary>
    /// The ATC groups that count as resistance-driving, as one named array.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This list is release-blocking pending a protocol owner</b> (PORT-PLAN.md §8.4). It is a
    /// clinical definition, not a code detail, and "the code has been this way" is not clinical
    /// sign-off.
    /// </para>
    /// <para>
    /// <c>J01FF%</c> (lincosamides - clindamycin, lincomycin) is <b>absent</b>. Commit
    /// <c>9f4a5ed4f</c> removed it, and it is missing from all nine refs capable of building this
    /// application; it survives only on mainline, which cannot. Patients on clindamycin alone
    /// therefore produce no <c>DRUG_RESISTANCE_DRIVING</c> value and a cohort's resistance-driving
    /// count falls relative to a <c>develop_old</c> build.
    /// </para>
    /// <para>
    /// It is a named array precisely so that reversing that decision is one line plus a regenerated
    /// golden file.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<string> ResistanceDrivingAtcPatterns { get; } =
        ["J01CR%", "J01D[CDH]%", "J01MA%"];

    /// <summary><c>SpDrugsetAntibioticResistance</c>.</summary>
    /// <returns>The statement, with <see cref="QaSql.PidList"/> still in place.</returns>
    /// <remarks>
    /// Built from <see cref="ResistanceDrivingAtcPatterns"/>. Upstream lays the disjunction out two
    /// patterns per source line, and each continuation line is indented by two spaces, so the
    /// generated text reads <c>) OR ( </c> within a line and <c>) OR   ( </c> across one. That is
    /// reproduced below so the statement stays byte-comparable with a Delphi trace.
    /// </remarks>
    public static string DrugSetAntibioticResistance()
    {
        const int PatternsPerSourceLine = 2;

        string clauses = string.Join(
            "OR ",
            ResistanceDrivingAtcPatterns.Select((pattern, index) =>
                (index > 0 && index % PatternsPerSourceLine == 0 ? "  " : string.Empty) +
                "( ot.ATC" + QaSql.Collation + "LIKE " + SqlLiteral.Quote(pattern) + QaSql.Collation + ") "));

        return
            "SELECT PersonId, 'RESISTANCE_DRIVING' AS VarName, " + NameChecksum + ", StartAt, TreatId, ai.AtcName AS Caption " +
            QaSql.FromOngoingTreatment +
            QaSql.JoinAtcIndex +
            QaSql.WherePersonList +
            "AND " +
            "( " +
            "  " + clauses +
            ")";
    }

    /// <summary>
    /// The nine ATC codes that count as recommended first-line antibiotics, as one named array.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In order: <c>J01CE01</c> benzylpenicillin, <c>J01CE02</c> phenoxymethylpenicillin,
    /// <c>J01CF01</c> dicloxacillin, <c>J01CF02</c> cloxacillin, <c>J01CA08</c> pivmecillinam,
    /// <c>J01CA11</c> mecillinam, <c>J01EA01</c> trimethoprim, <c>J01EE01</c>
    /// sulfamethoxazole+trimethoprim, <c>J01XE01</c> nitrofurantoin.
    /// </para>
    /// <para>
    /// These are exact codes matched with a plain <c>IN</c>, with <b>no</b>
    /// <see cref="QaSql.Collation"/> - the only drug query in the subsystem that omits it. Order is
    /// observable in the generated list, so it is the source order
    /// (<c>EPR.QA.SQL.pas:441</c>).
    /// </para>
    /// <para>
    /// A named array for the same reason as <see cref="ResistanceDrivingAtcPatterns"/>: it is a
    /// clinical definition, and revising one is a one-line edit plus a regenerated golden file.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<string> RecommendedAntibioticAtcCodes { get; } =
        ["J01CE01", "J01CE02", "J01CF01", "J01CF02", "J01CA08", "J01CA11", "J01EA01", "J01EE01", "J01XE01"];

    /// <summary><c>SpDrugsetAntibioticRecommended</c> (<c>EPR.QA.SQL.pas:431-443</c>).</summary>
    /// <returns>The statement, with <see cref="QaSql.PidList"/> still in place.</returns>
    /// <remarks>
    /// Built from <see cref="RecommendedAntibioticAtcCodes"/>. Unlike its neighbour
    /// <see cref="DrugSetAntibioticIntermediate"/> this needs nothing from the database beyond
    /// <c>dbo</c>, so it carries no <see cref="CollectorAvailability"/> gate.
    /// </remarks>
    public static string DrugSetAntibioticRecommended() =>
        "SELECT PersonId, 'RECOMMENDED_AB' AS VarName, " + NameChecksum + ", StartAt, TreatId, ai.AtcName AS Caption " +
        QaSql.FromOngoingTreatment +
        QaSql.JoinAtcIndex +
        QaSql.WherePersonList +
        "AND ( ot.ATC IN ( " + string.Join(", ", RecommendedAntibioticAtcCodes.Select(SqlLiteral.Quote)) + " ) )";

    /// <summary><c>SpDrugsetAntibioticIntermediate</c> (<c>EPR.QA.SQL.pas:445-457</c>).</summary>
    /// <returns>The statement, with <see cref="QaSql.PidList"/> still in place.</returns>
    /// <remarks>
    /// <para>
    /// The only collector whose selection lives in the database rather than in this repository: it
    /// has no ATC clause at all and takes whatever <see cref="AntibioticResistanceKnowledgeBase"/>
    /// classifies as intermediate. That also makes it the only one that needs
    /// <see cref="CollectorAvailability"/>.
    /// </para>
    /// <para>
    /// The join sits <b>between</b> the ATC-index join and the <c>WHERE</c>, which is unusual for
    /// this file and is upstream. So is the trailing space, which comes from
    /// <see cref="QaSql.WherePersonList"/> ending the statement
    /// (<c>Docs/Port/03-collectors.md</c> §E.1).
    /// </para>
    /// </remarks>
    public static string DrugSetAntibioticIntermediate() =>
        "SELECT PersonId, 'INTERMEDIATE_AB' AS VarName, " + NameChecksum + ", StartAt, TreatId, ai.AtcName AS Caption " +
        QaSql.FromOngoingTreatment +
        QaSql.JoinAtcIndex +
        "JOIN " + AntibioticResistanceKnowledgeBase + " r2 ON r2.AtcCode = ot.ATC " +
        QaSql.WherePersonList;

    /// <summary><c>QRY_DRUGSET_BASIC</c> / <c>QRY_DRUGSET_CHECKSUM</c> for one ATC pattern.</summary>
    /// <param name="atcPattern">The ATC <c>LIKE</c> pattern, e.g. <c>C0[23789]%</c>.</param>
    /// <param name="useNameChecksum">
    /// <see langword="true"/> for <c>CreateChecksum</c> / <c>CreateForTreatType</c>, which emits the
    /// drug-name checksum; <see langword="false"/> for <c>CreateBasic</c>, which emits the literal
    /// <c>1</c>.
    /// </param>
    /// <returns>The statement, with <see cref="QaSql.PidList"/> still in place.</returns>
    /// <remarks>
    /// <c>TDrugCollector.GroupResults</c> is a class variable defaulting to <see langword="false"/>
    /// and nothing sets it, so the emitted variable name is always the split form
    /// <c>CONCAT('&lt;pattern&gt;','.',ot.TreatType)</c> - columns come out as <c>ATC_A10.F</c>,
    /// <c>ATC_A10.B</c> and so on, one per treatment type.
    /// </remarks>
    public static string DrugSet(string atcPattern, bool useNameChecksum)
    {
        string variableName = SqlLiteral.Quote(SqlLiteral.AtcPatternToVariableName(atcPattern));
        string value = useNameChecksum ? "ABS(CHECKSUM(ot.DrugName)) % 100000 AS DpValue" : "1 AS DpValue";

        return
            "SELECT ot.PersonId, CONCAT(" + variableName + ",'.',ot.TreatType) AS VarName, " + value + ", ot.StartAt, ot.TreatId, ai.AtcName AS Caption " +
            QaSql.FromOngoingTreatment +
            QaSql.JoinAtcIndex +
            QaSql.WherePersonList +
            "AND ot.ATC" + QaSql.Collation + "LIKE " + SqlLiteral.Quote(atcPattern) + QaSql.Collation;
    }
}
