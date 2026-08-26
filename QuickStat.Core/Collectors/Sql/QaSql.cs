namespace QuickStat.Collectors.Sql;

/// <summary>
/// The collector SQL library: one pure function per <c>Sp*</c> / <c>QRY_*</c> in
/// <c>EPR.QA.SQL.pas</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Transcribed verbatim, whitespace included.</b> Every string below reproduces the Delphi
/// concatenation fragment for fragment, so that a reviewer can diff generated SQL against a Delphi
/// trace and Phase 5's golden files can be seeded from either side. Several fragments carry odd
/// spacing - a double space after <c>FROM</c>, a missing space before <c>GROUP BY</c>, a literal
/// tab - and those are preserved on purpose, not tidied. Reformatting is a separate commit with a
/// regenerated golden diff (<c>Docs/Port/03-collectors.md</c> §G.2).
/// </para>
/// <para>
/// Source of truth is the pinned library worktree <c>C:\work\FastTrak-tarmscreening</c>
/// (<c>origin/tarmscreening/develop</c>, <c>249ac2d16</c>), which is the lineage every shipped
/// QuickStat binary was built from (PORT-PLAN.md §2.1). It is <em>not</em> this repository's
/// <c>FastTrak\</c> copies, which are the <c>develop_old</c> extraction.
/// </para>
/// <para>
/// String concatenation rather than raw string literals or interpolation is deliberate: the
/// placeholders are literally <c>{IdList}</c>, <c>{ItemList}</c>, <c>{LabList}</c> and
/// <c>{FormName}</c>, so any brace-sensitive formatting mechanism would need them escaped and one
/// missed escape would be a silent SQL defect.
/// </para>
/// </remarks>
public static class QaSql
{
    /// <summary>Replaced per batch with the person-id fragment. Delphi <c>PID_LIST_PLACEHOLDER</c>.</summary>
    public const string PidList = "{IdList}";

    /// <summary>Replaced once at construction. Delphi <c>ITEM_LIST_PLACEHOLDER</c>.</summary>
    public const string ItemList = "{ItemList}";

    /// <summary>Replaced once at construction. Delphi <c>LAB_LIST_PLACEHOLDER</c>.</summary>
    public const string LabList = "{LabList}";

    /// <summary>Replaced once at construction. Delphi <c>FORM_NAME_PLACEHOLDER</c>.</summary>
    public const string FormName = "{FormName}";

    /// <summary>The <c>:PersonId</c> parameter used by the two one-round-trip-per-patient collectors.</summary>
    public const string PersonIdParameter = ":PersonId";

    /// <summary>
    /// <c>SQL_COLLATION</c>. Note the leading <b>and</b> trailing space - they are part of the
    /// constant and removing either breaks the generated statement.
    /// </summary>
    /// <remarks>
    /// The comment in <c>EPR.QA.SQL.pas:121</c> explains why both sides of every ATC <c>LIKE</c>
    /// carry it: "Collation issues can be dangerous when matching ATCs with LIKE because 'A%' will
    /// not match 'AA'".
    /// </remarks>
    public const string Collation = " COLLATE Latin1_General_CI_AI ";

    /// <summary><c>SQL_WHERE_PERSON_LIST</c>, trailing space included.</summary>
    public const string WherePersonList = "WHERE ( PersonId IN " + PidList + " ) ";

    /// <summary><c>SQL_JOIN_ATC_INDEX</c>, trailing space included.</summary>
    public const string JoinAtcIndex = "LEFT JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC ";

    /// <summary><c>SQL_FROM_ONGOING_TREATMENT</c>, trailing space included.</summary>
    public const string FromOngoingTreatment = "FROM dbo.OngoingTreatment ot ";

    /// <summary><c>QRY_FORM_CLASSES</c> - drives the <c>2 x N</c> dynamic per-form collectors.</summary>
    public const string FormClasses = "EXEC Report.GetFormClasses :StudyId";

    /// <summary>Column of <see cref="FormClasses"/> carrying the form name. Delphi <c>FLD_FORM_NAME</c>.</summary>
    public const string FormNameColumn = "FormName";

    /// <summary>Column of <see cref="FormClasses"/> carrying the form title. Delphi <c>FLD_FORM_TITLE</c>.</summary>
    public const string FormTitleColumn = "FormTitle";

    /// <summary><c>QRY_FORM_INSTANCES</c> - one round trip per patient.</summary>
    public const string FormInstances = "EXEC Report.GetFormInstances " + PersonIdParameter;

    /// <summary><c>QRY_TVANGSVEDTAK</c>, ProcId 9001.</summary>
    public const string GbdTvangsvedtak = "EXEC Report.ColGbdTvangsvedtak";

    /// <summary><c>QRY_DEMOGRAPHICS</c>, one row per person out of <c>dbo.Person</c>.</summary>
    /// <param name="varName">Emitted <c>VarName</c>, e.g. <c>AGE</c>.</param>
    /// <param name="varSpec">The expression producing the value, e.g. <c>DATEPART(YYYY,DOB)</c>.</param>
    /// <returns>The statement, with <see cref="PidList"/> still in place.</returns>
    public static string Demographics(string varName, string varSpec) =>
        "SELECT PersonId,'" + varName + "' AS VarName, " + varSpec + " AS DpValue, GETDATE() AS VarDate, PersonId AS ResultId " +
        "FROM dbo.Person WHERE (PersonId IN " + PidList + ")";

    /// <summary><c>SpStudCaseFields</c> - a single column of <c>dbo.StudCase</c> for one study.</summary>
    /// <param name="varName">Emitted <c>VarName</c>, quoted by this method.</param>
    /// <param name="fieldName">Column of <c>dbo.StudCase</c>.</param>
    /// <param name="studyId">Current study.</param>
    /// <returns>The statement. Carries no <see cref="PidList"/>: it scans the whole study.</returns>
    public static string StudCaseFields(string varName, string fieldName, int studyId) =>
        "SELECT sc.PersonId, " + SqlLiteral.Quote(varName) + " AS VarName, sc." + fieldName + " AS DpValue, GETDATE(), sc.StudCaseId AS RowId " +
        "FROM dbo.StudCase sc " +
        "WHERE sc.StudyId = " + SqlLiteral.Int(studyId);

    /// <summary><c>SpStudyCenter</c>.</summary>
    /// <param name="studyId">Current study.</param>
    /// <returns>The statement.</returns>
    public static string StudyCenter(int studyId) =>
        "SELECT sc.PersonId, 'CenterId' AS VarName, sg.CenterId AS DpValue, GETDATE(), sc.StudCaseId AS RowId " +
        "FROM dbo.StudCase sc " +
        "JOIN dbo.StudyGroup sg ON sg.StudyId = sc.StudyId AND sg.GroupId = sc.GroupId " +
        "WHERE sc.StudyId = " + SqlLiteral.Int(studyId);

    /// <summary><c>SpStudyGroupDeath</c> - the group a deceased patient last moved to.</summary>
    /// <param name="studyId">Current study.</param>
    /// <returns>The statement.</returns>
    /// <remarks>
    /// The <c>LEFT JOIN dbo.StudyGroup sg</c> is never referenced by the projection. Harmless, and
    /// kept for fidelity (<c>Docs/Port/03-collectors.md</c> §B.1).
    /// </remarks>
    public static string StudyGroupDeath(int studyId) =>
        "SELECT PersonId, 'DEATH_GROUP' AS VarName, NewGroupId AS DpValue, DeceasedDate AS VarDate, StudCaseLogId AS RowId  " +
        "FROM " +
        "( " +
        "  SELECT p.PersonId, p.DeceasedDate, scl.NewGroupId, scl.StudCaseLogId, " +
        "  ROW_NUMBER() OVER (PARTITION BY scl.StudCaseId ORDER BY scl.StudCaseLogId desc ) AS ReverseOrder " +
        "  FROM dbo.Person p " +
        "  JOIN dbo.StudCase sc ON sc.PersonId = p.PersonId AND sc.StudyId = " + SqlLiteral.Int(studyId) + " " +
        "  JOIN dbo.StudCaseLog scl ON scl.StudCaseId = sc.StudCaseId AND scl.ChangedAt < p.DeceasedDate " +
        "  LEFT JOIN dbo.StudyGroup sg ON sg.StudyId = sc.StudyId AND sg.GroupId = scl.NewGroupId " +
        "  WHERE NOT DeceasedDate IS NULL " +
        ") agg " +
        "WHERE agg.ReverseOrder = 1";

    /// <summary><c>SpStudyCenterDeath</c> - the centre a deceased patient last belonged to.</summary>
    /// <param name="studyId">Current study.</param>
    /// <returns>The statement.</returns>
    public static string StudyCenterDeath(int studyId) =>
        "SELECT PersonId, 'DEATH_CENTER' AS VarName, CenterId AS DpValue, DeceasedDate AS VarDate, StudCaseLogId AS RowId  " +
        "FROM " +
        "( " +
        "  SELECT p.PersonId, p.DeceasedDate, sg.CenterId, scl.StudCaseLogId, " +
        "  ROW_NUMBER() OVER (PARTITION BY scl.StudCaseId ORDER BY scl.StudCaseLogId desc ) AS ReverseOrder " +
        "  FROM dbo.Person p " +
        "  JOIN dbo.StudCase sc ON sc.PersonId = p.PersonId AND sc.StudyId = " + SqlLiteral.Int(studyId) + " " +
        "  JOIN dbo.StudCaseLog scl ON scl.StudCaseId = sc.StudCaseId AND scl.ChangedAt < p.DeceasedDate " +
        "  LEFT JOIN dbo.StudyGroup sg ON sg.StudyId = sc.StudyId AND sg.GroupId = scl.NewGroupId " +
        "  WHERE NOT DeceasedDate IS NULL " +
        ") agg " +
        "WHERE agg.ReverseOrder = 1";

    /// <summary><c>SpSnapshotFormDataAll</c> - every item of the newest instance of one form.</summary>
    /// <param name="formName">Form class name; quoted by this method.</param>
    /// <returns>The statement.</returns>
    /// <remarks>
    /// <para>
    /// This replaced <c>EXEC Report.GetFormData :PersonId, '&lt;form&gt;'</c> on the tarmscreening
    /// lineage (commit <c>8486b3d09</c>, "#489525") and is what ships: free-text answers arrive as
    /// <c>dp.TextVal AS Caption</c>, and the batch size goes from 1 to 200. PORT-PLAN.md §8.5 settles
    /// taking it - declining would remove a live feature.
    /// </para>
    /// <para>
    /// Two behaviours to record rather than fix: the query has no <c>cf.DeletedAt IS NULL</c>
    /// predicate and no <c>mi.ItemType IN (1,2,5)</c> filter, so whatever
    /// <c>Report.GetFormData</c> did about deleted forms and item types is not reproduced. That is
    /// pre-existing production behaviour, not a port regression.
    /// </para>
    /// <para>
    /// The <c>, dp.RowId DESC</c> tie-breaker is <em>added</em> by the port and is the only
    /// deviation from the upstream text in this method. PORT-PLAN.md §8.5 settles taking
    /// <c>RANK</c> to <c>ROW_NUMBER</c> "with a deterministic tie-breaker", and
    /// <c>Docs/Port/03-collectors.md</c> §F.3 spells that tie-breaker out - but §F.3's diff covers
    /// <c>SpSnapshotFormDataNumeric</c>, which feeds <c>TFormDataNumericCollector</c>, a class
    /// nothing instantiates. Applying it only there would have been the literal reading and the
    /// wrong one: <c>ROW_NUMBER</c> with no tie-breaker picks arbitrarily among rows that share an
    /// <c>EventNum</c>, so two runs of the <em>live</em> collector can put different values in a
    /// cell and Phase 5's golden files would not be stable. Determinism is the entire point of the
    /// §8.5 decision, so it is applied where it can actually be observed. Five characters of
    /// divergence buys a reproducible export.
    /// </para>
    /// </remarks>
    public static string SnapshotFormDataAll(string formName) =>
        "SELECT agg.* FROM " +
        "( " +
        "  SELECT ce.PersonId, mi.VarName, ISNULL(dp.Quantity,DATEDIFF(DD,'1899-12-30',dp.DTVal)) AS DataValue, ce.EventTime, dp.RowId, dp.TextVal AS Caption, mfi.OrderNumber, " +
        "    ROW_NUMBER() OVER ( PARTITION BY ce.PersonId, mi.ItemId ORDER BY ce.EventNum DESC, dp.RowId DESC ) AS OrderBy " +
        "  FROM dbo.ClinDatapoint dp " +
        "    JOIN dbo.ClinEvent ce ON ce.EventId = dp.EventId " +
        "    JOIN dbo.ClinForm cf ON cf.EventId = ce.EventId " +
        "    JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId " +
        "    JOIN dbo.MetaItem mi ON mi.ItemId = dp.ItemId " +
        "    JOIN dbo.MetaFormItem mfi ON mfi.FormId = cf.FormId AND mfi.ItemId = mi.ItemId " +
        "  WHERE ( mf.FormName = " + SqlLiteral.Quote(formName) + " ) " +
        "  AND ( ce.PersonId IN " + PidList + " )" +
        ") agg " +
        "WHERE agg.OrderBy = 1 ORDER BY agg.OrderNumber";

    /// <summary><c>SpRecentFormCountAll</c> - instances per form type inside a month window.</summary>
    /// <param name="monthCount">Window length in months, exclusive.</param>
    /// <returns>The statement. Carries no <see cref="PidList"/>.</returns>
    public static string RecentFormCountAll(int monthCount) =>
        "SELECT ce.PersonId, UPPER(mf.FormName) AS VarName, COUNT(*) AS DpValue, MAX(ce.EventTime) AS VarDate, MAX(cf.ClinFormId) AS MaxClinFormId " +
        "FROM dbo.ClinForm cf " +
        "JOIN dbo.ClinEvent ce ON ce.EventId = cf.EventId " +
        "JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId " +
        "WHERE ( DATEDIFF( MM, ce.EventTime, GETDATE() ) < " + SqlLiteral.Int(monthCount) + " ) AND ( cf.DeletedAt IS NULL ) " +
        "GROUP BY ce.PersonId, mf.FormName";

    /// <summary><c>SpRecentFormCountSingle</c> - instances of one form inside a month window.</summary>
    /// <param name="formName">Form class name; quoted by this method.</param>
    /// <param name="monthCount">Window length in months, exclusive.</param>
    /// <returns>The statement. Carries no <see cref="PidList"/>.</returns>
    public static string RecentFormCountSingle(string formName, int monthCount) =>
        "SELECT ce.PersonId, UPPER(mf.FormName) AS VarName, COUNT(*) AS DpValue, MAX(ce.EventTime) AS VarDate, MAX(cf.ClinFormId) AS MaxClinFormId " +
        "FROM dbo.ClinForm cf " +
        "JOIN dbo.ClinEvent ce ON ce.EventId = cf.EventId " +
        "JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId AND mf.FormName=" + SqlLiteral.Quote(formName) + " " +
        "WHERE ( DATEDIFF( MM, ce.EventTime, GETDATE() ) < " + SqlLiteral.Int(monthCount) + " ) AND ( cf.DeletedAt IS NULL ) " +
        "GROUP BY ce.PersonId, mf.FormName";

    /// <summary><c>SpRecentFormGroupCount</c> - instances of several forms counted as one variable.</summary>
    /// <param name="quotedVarName">Emitted <c>VarName</c>, <b>already quoted</b> by the caller.</param>
    /// <param name="formNames">Form class names, in order.</param>
    /// <param name="monthCount">Window length in months, exclusive.</param>
    /// <returns>The statement. Carries no <see cref="PidList"/>.</returns>
    /// <remarks>
    /// <paramref name="quotedVarName"/> keeps the Delphi's asymmetry: the caller passes
    /// <c>QuotedStr( VAR_GBD_LEGENOTATER )</c> while the form list is quoted inside the function
    /// (<c>EPR.QA.SQL.pas:243-262</c>).
    /// </remarks>
    public static string RecentFormGroupCount(string quotedVarName, IReadOnlyList<string> formNames, int monthCount) =>
        "SELECT ce.PersonId, UPPER(" + quotedVarName + ") AS VarName, COUNT(*) AS DpValue, MAX(ce.EventTime) AS MaxEventTime, MAX(cf.ClinFormId) AS MaxClinFormId " +
        "FROM dbo.ClinForm cf " +
        "JOIN dbo.ClinEvent ce ON ce.EventId = cf.EventId " +
        "JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId AND mf.FormName IN ( " + string.Join(",", formNames.Select(SqlLiteral.Quote)) + " ) " +
        "WHERE ( DATEDIFF( MM, ce.EventTime, GETDATE() ) < " + SqlLiteral.Int(monthCount) + " ) AND ( cf.DeletedAt IS NULL ) " +
        "GROUP BY ce.PersonId";

    /// <summary>The four form classes that count as a doctor's note. Delphi <c>GBD_LEGENOTATER</c>.</summary>
    public static IReadOnlyList<string> GbdDoctorNoteForms { get; } =
        ["GBD_NOTAT_LEGE", "GBD_STATUS_PRESENS", "GBD_INFECTION", "GBD_BESLUTNINGER"];

    /// <summary><c>SpRecentFormGroupLege3m</c>.</summary>
    /// <returns>The statement, emitting the single variable <c>GBDLEGE</c>.</returns>
    public static string RecentFormGroupLege3M() =>
        RecentFormGroupCount(SqlLiteral.Quote("GBDLEGE"), GbdDoctorNoteForms, 3);

    /// <summary><c>SpRecentFormCompleteness</c> - the worst recent completeness of one form.</summary>
    /// <param name="formName">Form class name; quoted by this method.</param>
    /// <param name="months">Window length in months, exclusive.</param>
    /// <returns>The statement. Carries no <see cref="PidList"/>.</returns>
    /// <remarks>
    /// <c>ORDER BY cf.FormComplete, ce.EventNum DESC</c> ranks the <em>lowest</em> completeness
    /// first, so the cell shows the worst instance in the window, not the newest.
    /// </remarks>
    public static string RecentFormCompleteness(string formName, int months) =>
        "SELECT PersonId, VarName, DpValue, EventTime, ClinFormId " +
        "FROM " +
        "( " +
        "  SELECT ce.PersonId, mf.FormName AS VarName, cf.FormComplete AS DpValue, ce.EventTime, cf.ClinFormId, " +
        "  RANK() OVER (Partition by ce.PersonId ORDER BY cf.FormComplete, ce.EventNum DESC) AS rnk " +
        "  FROM dbo.ClinForm cf " +
        "  JOIN dbo.ClinEvent ce ON ce.EventId = cf.EventId " +
        "  JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId " +
        "  WHERE mf.FormName = " + SqlLiteral.Quote(formName) + " AND cf.FormComplete > 0 AND cf.DeletedAt IS NULL " +
        "  AND DATEDIFF( MM, ce.EventTime, GETDATE() ) < " + SqlLiteral.Int(months) + " " +
        ") agg " +
        "WHERE agg.rnk = 1";

    /// <summary><c>SpDruidIndividualInteractions</c> - one column per common interaction class.</summary>
    /// <param name="minCount">Only classes seen more than this many times get a column.</param>
    /// <returns>The statement. Carries no <see cref="PidList"/>.</returns>
    /// <remarks>
    /// The Delphi writes <c>'DRUID#%%'</c> because the string goes through <c>Format</c>; the
    /// emitted pattern is <c>'DRUID#%'</c>.
    /// </remarks>
    public static string DruidIndividualInteractions(int minCount) =>
        "SELECT a.PersonId, REPLACE(agg.AlertClass,'#','') AS VarName, AlertLevel AS DpValue,CreatedAt, a.AlertId, a.AlertHeader AS Caption " +
        "FROM " +
        "( " +
        "  SELECT AlertClass, COUNT(*) AS n FROM dbo.Alert " +
        "   WHERE AlertClass LIKE 'DRUID#%'" +
        "  GROUP BY AlertClass " +
        ") agg " +
        "JOIN dbo.Alert a ON a.AlertClass = agg.AlertClass " +
        "WHERE agg.n > " + SqlLiteral.Int(minCount) + " " +
        "ORDER BY PersonId";

    /// <summary><c>SpDruidCountByLevel</c> - interaction counts as GREEN / YELLOW / ORANGE / RED.</summary>
    /// <returns>The statement. Carries no <see cref="PidList"/>.</returns>
    public static string DruidCountByLevel() =>
        "SELECT PersonId, " +
        "  CASE AlertLevel WHEN 1 THEN 'GREEN' WHEN 2 THEN 'YELLOW' WHEN 3 THEN 'ORANGE' WHEN 4 THEN 'RED' END AS DruidLevel, " +
        "  n, MaxAlertDate, MaxAlertId " +
        "FROM " +
        "( " +
        "  SELECT PersonId, AlertLevel, MAX(CreatedAt) AS MaxAlertDate, COUNT(*) AS n, MAX(AlertId) AS MaxAlertId FROM dbo.Alert " +
        "  WHERE ( AlertClass LIKE 'DRUID%' ) AND ( AlertLevel > 0 ) " +
        "  GROUP BY PersonId, AlertLevel " +
        ") agg " +
        "ORDER BY PersonId";

    /// <summary><c>SpDiagnoseDetailsByLevel</c> - problem counts per ICD-10 code prefix.</summary>
    /// <param name="level">Number of leading characters of the code, 1 to 5.</param>
    /// <returns>The statement. Carries no <see cref="PidList"/>.</returns>
    /// <remarks>
    /// The widest collector in the application: level 5 can add hundreds of columns, and it reads
    /// every problem row in the database to do it (PORT-PLAN.md R10).
    /// </remarks>
    public static string DiagnoseDetailsByLevel(int level) =>
        "SELECT PersonId, SUBSTRING(ItemCode,1," + SqlLiteral.Int(level) + ") AS VarName, COUNT(*) AS DpValue, MIN(CreatedAt) AS MinCreatedAt, MIN(ProbId) AS MinProbId FROM  " +
        "( " +
        "  SELECT PersonId, mni.ItemCode, cp.ListItem, cp.CreatedAt, cp.ProbId " +
        "  FROM dbo.ClinProblem cp  " +
        "  JOIN dbo.MetaProblemType mp ON mp.ProbType = cp.ProbType AND mp.ProbActive = 1 " +
        "  JOIN dbo.MetaNomListItem li ON li.ListItem = cp.ListItem " +
        "  JOIN dbo.MetaNomItem mni ON mni.ItemId = li.ItemId " +
        ") pro " +
        "GROUP BY PersonId, SUBSTRING(ItemCode,1," + SqlLiteral.Int(level) + ") ";

    /// <summary><c>SpDiagnoseByPattern</c> - the earliest problem matching an ICD-10 pattern.</summary>
    /// <param name="pattern">An ICD-10 <c>LIKE</c> pattern, e.g. <c>I1[012345]%</c>.</param>
    /// <returns>The statement. Carries no <see cref="PidList"/>.</returns>
    /// <remarks>
    /// The value is <c>cp.ListItem</c>, a nomenclature list-item id - not a count and not a 0/1
    /// flag. <c>Caption</c> carries the ICD-10 code.
    /// </remarks>
    public static string DiagnoseByPattern(string pattern) =>
        "SELECT PersonId, VarName, ListItem, CreatedAt, ProbId, Caption " +
        "FROM ( " +
        "  SELECT cp.PersonId, " + SqlLiteral.Quote(SqlLiteral.AtcPatternToVariableName(pattern)) + " AS VarName, cp.ListItem, cp.CreatedAt, cp.ProbId, mni.ItemCode AS Caption, " +
        "  RANK() OVER ( PARTITION BY cp.PersonId ORDER BY cp.CreatedAt ) AS OrderNo " +
        "  FROM dbo.ClinProblem cp " +
        "  JOIN dbo.MetaProblemType pt ON pt.ProbType = cp.ProbType AND pt.ProbActive = 1 " +
        "  JOIN dbo.MetaNomListItem mnli ON mnli.ListItem = cp.ListItem " +
        "  JOIN dbo.MetaNomItem mni ON mni.ItemId = mnli.ItemId " +
        "  WHERE ( mni.ItemCode LIKE " + SqlLiteral.Quote(pattern) + " ) " +
        ") agg WHERE OrderNo = 1 ";

    /// <summary><c>SpDiagnoseDementiaAndAlzheimers</c>.</summary>
    /// <returns>The statement. Carries no <see cref="PidList"/>.</returns>
    /// <remarks>
    /// The ICD filter sits on the <c>JOIN</c> rather than in a <c>WHERE</c> - same result for an
    /// inner join - and the registered title says <c>F0[123]+G03</c> while the SQL matches
    /// <c>F0[0123]</c> and <c>G30</c>. The SQL is right; the title is wrong and is preserved.
    /// </remarks>
    public static string DiagnoseDementiaAndAlzheimers() =>
        "SELECT PersonId, VarName, ListItem, CreatedAt, ProbId, Caption " +
        "FROM ( " +
        "  SELECT cp.PersonId, 'DEMENTIA' AS VarName, cp.ListItem, cp.CreatedAt, cp.ProbId, mni.ItemCode AS Caption, " +
        "  RANK() OVER ( PARTITION BY cp.PersonId ORDER BY cp.CreatedAt ) AS OrderNo " +
        "  FROM dbo.ClinProblem cp " +
        "  JOIN dbo.MetaProblemType pt ON pt.ProbType = cp.ProbType AND pt.ProbActive = 1 " +
        "  JOIN dbo.MetaNomListItem mnli ON mnli.ListItem = cp.ListItem " +
        "  JOIN dbo.MetaNomItem mni ON mni.ItemId = mnli.ItemId " +
        "  AND ( mni.ItemCode LIKE 'F0[0123]%' OR mni.ItemCode LIKE 'G30%' ) " +
        ") agg WHERE OrderNo = 1 ";

    /// <summary><c>SpDrugWithoutDiagnose</c> - patients on a drug class with no matching diagnosis.</summary>
    /// <param name="varName">Emitted <c>VarName</c>; quoted by this method.</param>
    /// <param name="drugPattern">ATC <c>LIKE</c> pattern; quoted by this method.</param>
    /// <param name="diagnosisPattern">ICD-10 <c>LIKE</c> pattern; quoted by this method.</param>
    /// <returns>The statement. Carries no <see cref="PidList"/>.</returns>
    /// <remarks>External dependency: the view or table <c>Diagnose.ICD10</c>.</remarks>
    public static string DrugWithoutDiagnose(string varName, string drugPattern, string diagnosisPattern) =>
        "SELECT rx.PersonId, " + SqlLiteral.Quote(varName) + " AS VarName, rxn AS DpValue, MaxCreatedAt, MaxTreatId FROM ( " +
        "   SELECT PersonId, MAX(CreatedAt) AS MaxCreatedAt, MAX(TreatId) AS MaxTreatId, COUNT(*) AS rxn " +
        "   FROM dbo.OngoingTreatment " +
        "   WHERE ATC LIKE " + SqlLiteral.Quote(drugPattern) + " " +
        "   GROUP BY PersonId " +
        ") rx " +
        "LEFT JOIN " +
        "  ( " +
        "    SELECT PersonId, COUNT(*) AS n FROM Diagnose.ICD10 " +
        "    WHERE ItemCode LIKE " + SqlLiteral.Quote(diagnosisPattern) + " AND ProbActive = 1 " +
        "    GROUP BY PersonId " +
        "  ) agg ON agg.PersonId = rx.PersonId " +
        "WHERE ( agg.n IS NULL )" +
        "ORDER BY PersonId";

    /// <summary><c>SpDrugHypertensionWithLowBp</c>, ProcId-backed.</summary>
    /// <param name="lowBpThreshold">Systolic threshold, exclusive.</param>
    /// <returns>The statement. Carries no <see cref="PidList"/>.</returns>
    public static string DrugHypertensionWithLowBp(int lowBpThreshold) =>
        "EXEC Report.ColAntiHypertensivesLowBP " + SqlLiteral.Int(lowBpThreshold);

    /// <summary><c>SpDrugAndRenalFunction</c>, ProcId-backed.</summary>
    /// <param name="drugPattern">ATC <c>LIKE</c> pattern; quoted by this method.</param>
    /// <param name="lowGfrValueThreshold">GFR threshold, exclusive.</param>
    /// <returns>The statement. Carries no <see cref="PidList"/>.</returns>
    public static string DrugAndRenalFunction(string drugPattern, int lowGfrValueThreshold) =>
        "EXEC Report.ColDrugAndRenalFunction " + SqlLiteral.Quote(drugPattern) + ", " + SqlLiteral.Int(lowGfrValueThreshold);

    /// <summary><c>SpDrugCountNoAtc</c>.</summary>
    /// <returns>The statement. Carries no <see cref="PidList"/>.</returns>
    /// <remarks>
    /// Position 1 is an <b>unaliased</b> literal in the Delphi
    /// (<c>SELECT PersonId, 'NOATC', COUNT(*) …</c>), and the missing space before
    /// <c>GROUP BY</c> is also upstream. Both are preserved: the positional contract still holds,
    /// and aliasing it would be a silent divergence from the golden files.
    /// </remarks>
    public static string DrugCountNoAtc() =>
        "SELECT PersonId, 'NOATC', COUNT(*) AS DpValue, MAX(StartAt) AS LastDate, MAX(TreatId) AS MaxTreatId " +
        FromOngoingTreatment +
        "WHERE ISNULL(ATC,'') = ''" +
        "GROUP BY PersonId";

    /// <summary><c>SpSnapshotVarset</c> - the newest value of each of a fixed set of items.</summary>
    /// <param name="variableDataType">Which column supplies the value.</param>
    /// <param name="itemIds">The item ids, in registration order.</param>
    /// <returns>The statement, with <see cref="PidList"/> still in place.</returns>
    public static string SnapshotVarSet(CrfVarType variableDataType, IReadOnlyList<int> itemIds)
    {
        (string valueFragment, string qualifyFragment) = variableDataType switch
        {
            // Quantities and enums are both represented by Quantity.
            CrfVarType.Numeric => ("cdp.Quantity", "ISNULL(cdp.Quantity,-1) <> -1"),
            // Dates are represented as Excel dates.
            CrfVarType.Date => ("DATEDIFF(DD,'1899-12-30',cdp.DTVal)", "NOT cdp.DTVal IS NULL"),
            // Text is represented as the length of the text.
            CrfVarType.Text => ("DATALENGTH(cdp.TextVal)", "NOT cdp.TextVal IS NULL"),
            _ => throw new ArgumentOutOfRangeException(nameof(variableDataType)),
        };

        return
            "SELECT a.* FROM ( " +
            "  SELECT ce.PersonId, mi.VarName, " + valueFragment + " AS DpValue, ce.EventTime AS VarDate, cdp.RowId, mi.ItemId, " +
            "  RANK() OVER (PARTITION BY ce.PersonId, mi.ItemId ORDER BY ce.EventNum DESC ) AS OrderBy " +
            "  FROM dbo.ClinDataPoint cdp " +
            "  JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId " +
            "  JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId " +
            "  WHERE ( ce.PersonId IN " + PidList + " ) " +
            "    AND ( " + qualifyFragment + " ) " +
            "    AND ( cdp.ItemId IN ( " + SqlLiteral.List(itemIds) + " ) )" +
            " ) a " +
            " WHERE a.OrderBy = 1 " +
            "ORDER BY PersonId";
    }

    /// <summary><c>SpSnapshotVarsetAge</c> - days since each item was last recorded.</summary>
    /// <param name="itemIds">The item ids, in registration order.</param>
    /// <returns>The statement, with <see cref="PidList"/> still in place.</returns>
    public static string SnapshotVarSetAge(IReadOnlyList<int> itemIds) =>
        "SELECT a.* FROM " +
        "(" +
        "  SELECT ce.PersonId, mi.VarName, DATEDIFF(dd,ce.EventTime,GETDATE()) AS DpValue, ce.EventTime AS VarDate, cdp.RowId, mi.ItemId, " +
        "  RANK() OVER (PARTITION BY ce.PersonId,mi.ItemId ORDER BY ce.EventTime DESC ) AS OrderBy " +
        "  FROM dbo.ClinDataPoint cdp " +
        "  JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId " +
        "  JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId " +
        "  WHERE ( ce.PersonId IN " + PidList + " ) " +
        "    AND NOT ( cdp.Quantity IS NULL AND cdp.DTVal IS NULL AND cdp.TextVal IS NULL ) " +
        "    AND ( cdp.ItemId IN ( " + SqlLiteral.List(itemIds) + " ) )" +
        " ) a " +
        " WHERE a.OrderBy = 1";

    /// <summary><c>SpMaximumQuantityVarset</c> - the highest value of each of a set of items.</summary>
    /// <param name="itemIds">The item ids, in registration order.</param>
    /// <returns>The statement, with <see cref="PidList"/> still in place.</returns>
    /// <remarks>The leading space and the lower-case <c>select</c> / <c>where</c> are upstream.</remarks>
    public static string MaximumQuantityVarSet(IReadOnlyList<int> itemIds) =>
        " SELECT a.* FROM  " +
        "(" +
        "  select ce.PersonId, mi.VarName, cdp.Quantity AS DpValue, ce.EventTime AS VarDate, cdp.RowId, cdp.ItemId, " +
        "  RANK() OVER ( PARTITION BY ce.PersonId, cdp.ItemId ORDER BY Quantity DESC, cdp.RowId DESC ) AS rnk " +
        "  FROM dbo.ClinDataPoint cdp " +
        "  JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId " +
        "  JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId " +
        "  WHERE ( ce.PersonId IN " + PidList + " ) " +
        "    AND ( cdp.ItemId IN ( " + SqlLiteral.List(itemIds) + " ) )" +
        " ) a " +
        "where a.rnk = 1";

    /// <summary><c>SpSnapshotLabset</c> - the newest result in each of a set of lab classes.</summary>
    /// <param name="labClassIds">The lab-class ids, in registration order.</param>
    /// <returns>The statement, with <see cref="PidList"/> still in place.</returns>
    /// <remarks>
    /// The emitted <c>VarName</c> is the NLK code (e.g. <c>NPU01566</c>) or, when the lab class has
    /// none, the scalar function <c>Report.LabClassName(id)</c>.
    /// </remarks>
    public static string SnapshotLabSet(IReadOnlyList<int> labClassIds) =>
        "SELECT agg.* FROM " +
        "( " +
        "  SELECT ld.PersonId, ISNULL(la.NLK, Report.LabClassName(lc.LabClassId)) AS VarName, ld.NumResult, ld.LabDate, ld.ResultId, " +
        "  RANK() OVER ( PARTITION BY ld.PersonId,lc.LabClassId ORDER BY ld.LabDate DESC ) AS OrderBy " +
        "  FROM dbo.LabData ld " +
        "  JOIN dbo.LabCode lc ON lc.LabCodeId = ld.LabCodeId " +
        "  JOIN dbo.LabClass la ON la.LabClassId = lc.LabClassId " +
        "  WHERE ( ld.PersonId IN " + PidList + " ) AND ( la.LabClassId IN (" + SqlLiteral.List(labClassIds) + ") AND ( ld.NumResult >= 0 ) ) " +
        " ) agg " +
        " WHERE agg.OrderBy = 1 ORDER BY agg.PersonId, agg.VarName";

    /// <summary><c>SpSnapshotLabdataByTrustLevel</c>.</summary>
    /// <param name="trustLevel">3 = high, 2 = medium, 1 = low.</param>
    /// <returns>The statement, with <see cref="PidList"/> still in place.</returns>
    /// <remarks>
    /// The window partitions by <c>PersonId</c> <em>only</em>, not by lab class, so this returns the
    /// single most recent lab row per patient plus same-date ties - almost certainly not what was
    /// intended. Shipping behaviour, reproduced as-is and raised as a question
    /// (<c>Docs/Port/03-collectors.md</c> §B.7).
    /// </remarks>
    public static string SnapshotLabDataByTrustLevel(int trustLevel) =>
        "SELECT a.* FROM " +
        "( " +
        "   SELECT ld.PersonId, ISNULL(la.NLK, Report.LabClassName( la.LabClassId)) AS VarName, ld.NumResult, ld.LabDate, ld.ResultId, " +
        "   RANK() OVER ( PARTITION BY PersonId ORDER BY LabDate DESC ) AS OrderBy " +
        "   FROM dbo.LabData ld " +
        "     JOIN dbo.LabCode lc ON lc.LabCodeId = ld.LabCodeId " +
        "     JOIN dbo.LabClass la ON la.LabClassId = lc.LabClassId " +
        "   WHERE ( la.TrustLevel = " + SqlLiteral.Int(trustLevel) + " )  AND ( ld.PersonId IN " + PidList + " ) " +
        ") a " +
        "WHERE a.OrderBy = 1";

    /// <summary><c>SpRecentLabdataPresent</c> - how many lab results inside a month window.</summary>
    /// <param name="monthsAgo">Window length in months, exclusive.</param>
    /// <returns>The statement. Carries no <see cref="PidList"/>.</returns>
    public static string RecentLabDataPresent(int monthsAgo) =>
        "SELECT PersonId,'LABCOUNT" + SqlLiteral.Int(monthsAgo) + "M' AS VarName, COUNT(*) AS n, MAX(LabDate) AS MaxLabDate, MAX(ResultId) AS MaxResultId " +
        "FROM LabData " +
        "WHERE DATEDIFF(MM,LabDate,GETDATE()) < " + SqlLiteral.Int(monthsAgo) + " " +
        "GROUP BY PersonId";

    /// <summary><c>SpRecentQuantityPresent</c> - every value of one item inside a month window.</summary>
    /// <param name="itemId">The item.</param>
    /// <param name="monthsAgo">Window length in months, exclusive.</param>
    /// <returns>The statement. Carries no <see cref="PidList"/>.</returns>
    /// <remarks>
    /// Returns <em>every</em> matching datapoint, not just the newest. The sink rejects a duplicate
    /// variable name, so the first row wins, and <c>ORDER BY ce.EventNum</c> ascending means the
    /// <b>oldest</b> value in the window is what lands in the cell. Reproduce the ordering exactly.
    /// </remarks>
    public static string RecentQuantityPresent(int itemId, int monthsAgo) =>
        "SELECT ce.PersonId, mi.VarName, cdp.Quantity, ce.EventTime, cdp.RowId " +
        "FROM dbo.ClinDataPoint cdp " +
        "JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId " +
        "JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId " +
        "WHERE ( cdp.ItemId = " + SqlLiteral.Int(itemId) + " ) " +
        "AND ( NOT cdp.Quantity IS NULL ) " +
        "AND DATEDIFF( MM, ce.EventTime, GETDATE()) < " + SqlLiteral.Int(monthsAgo) + " " +
        "ORDER BY ce.EventNum";

    /// <summary><c>SpSnapshotQuantityIfBelowThreshold</c>.</summary>
    /// <param name="itemId">The item.</param>
    /// <param name="value">Exclusive upper bound.</param>
    /// <returns>The statement. Carries no <see cref="PidList"/>.</returns>
    /// <remarks>
    /// External dependency: the table-valued function <c>dbo.GetLastQuantityTable</c>. The threshold
    /// is rendered with an invariant decimal separator, matching the Delphi's explicit
    /// <c>en-US</c> format settings.
    /// </remarks>
    public static string SnapshotQuantityIfBelowThreshold(int itemId, double value) =>
        "SELECT v.PersonId, mi.VarName, v.Quantity AS DpValue, v.EventTime, 0 AS RowId " +
        "FROM dbo.GetLastQuantityTable( " + SqlLiteral.Int(itemId) + ", NULL ) v " +
        "JOIN dbo.MetaItem mi ON mi.ItemId = " + SqlLiteral.Int(itemId) + " " +
        "WHERE v.Quantity < " + SqlLiteral.General(value);

    /// <summary><c>SpFormAgeSingle</c> - days since one form class was last filled in.</summary>
    /// <param name="formName">Form class name; quoted by this method.</param>
    /// <returns>The statement, with <see cref="PidList"/> still in place.</returns>
    /// <remarks>The double space before the form-name literal is upstream.</remarks>
    public static string FormAgeSingle(string formName) =>
        "SELECT a.* FROM ( " +
        "  SELECT ce.PersonId, mf.FormName AS VarName, DATEDIFF(dd,ce.EventTime,GETDATE()) AS DpValue, ce.EventTime AS VarDate, cf.ClinFormId, " +
        "  RANK() OVER (PARTITION BY ce.PersonId,mf.FormName ORDER BY ce.EventTime DESC ) AS OrderBy " +
        "  FROM dbo.ClinForm cf " +
        "  JOIN dbo.ClinEvent ce ON ce.EventId = cf.EventId " +
        "  JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId AND mf.FormName =  " + SqlLiteral.Quote(formName) +
        "  WHERE ( ce.PersonId IN " + PidList + " ) AND ( cf.DeletedAt IS NULL )" +
        " ) a " +
        " WHERE a.OrderBy = 1";

    /// <summary><c>SpFlackerKileyDeath</c> - Flacker-Kiely score and days lived after it.</summary>
    /// <returns>The statement. Carries no <see cref="PidList"/>.</returns>
    /// <remarks>
    /// Produces two variables per patient through an <c>UNPIVOT</c>: <c>FK_SCORE</c> and
    /// <c>FK_DAYS_LIVED</c>. The literal tab is in the Delphi source and is kept so the generated
    /// text stays byte-comparable.
    /// </remarks>
    public static string FlackerKielyDeath() =>
        "SELECT PersonId, VarName, DataValue, EventTime, RowId, ReverseOrder  " +
        "FROM " +
        "( " +
        "  SELECT ce.PersonId, cdp.Quantity AS FK_SCORE," +
        "\t CONVERT(DECIMAL(18,4),DATEDIFF(DAY, ce.EventTime, p.DeceasedDate )) AS FK_DAYS_LIVED," +
        "    ce.EventTime, cdp.RowId," +
        "  ROW_NUMBER() OVER (PARTITION BY ce.PersonId ORDER BY ce.EventTime DESC ) AS ReverseOrder " +
        "  FROM dbo.ClinEvent ce " +
        "  JOIN dbo.ClinDataPoint cdp ON cdp.EventId = ce.EventId " +
        "  JOIN dbo.Person p ON p.PersonId = ce.PersonId " +
        "  WHERE cdp.ItemId = 1128 " +
        ") AS SourceTable " +
        "UNPIVOT " +
        "( DataValue FOR VarName IN ( FK_SCORE, FK_DAYS_LIVED ) ) AS DestTable " +
        "WHERE ReverseOrder = 1";
}
