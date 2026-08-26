namespace QuickStat.Collectors.Sql;

/// <summary>
/// The hand-written drug SQL from <c>EPR.QA.Collector.Drug.pas</c>, plus the two antibiotic
/// statements that live in <c>EPR.QA.SQL.pas</c>.
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
