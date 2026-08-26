using System.Globalization;
using QuickStat.Collectors.Sql;

namespace QuickStat.Collectors.Registry;

/// <summary>
/// One factory helper per Delphi collector constructor, so that every registration in the catalog
/// files is a single readable line.
/// </summary>
/// <remarks>
/// <para>
/// This is the most important readability decision in step 2.4: 126 registrations spread over six
/// files are only reviewable if each one shows nothing but the facts that differ between
/// collectors. Everything a Delphi constructor did implicitly - the variable prefix, the batch
/// size, the title suffix - happens here, once.
/// </para>
/// <para>
/// <see cref="CollectorDescriptor.PidBinding"/> is <b>derived</b> from the generated SQL rather
/// than typed out per collector. That removes 126 chances of getting it wrong and it reproduces the
/// Delphi rule exactly, which was itself implicit: <c>TDataCollector.SQL</c> substitutes
/// <c>{IdList}</c> only when the batch size exceeds one, and <c>RunBatch</c> takes the
/// <c>:PersonId</c> path only when it does not.
/// </para>
/// </remarks>
internal static class Make
{
    /// <summary>Delphi <c>maxint</c> - one statement for the whole cohort.</summary>
    internal const int WholeCohort = int.MaxValue;

    /// <summary>The batch size every <c>{IdList}</c> collector except form data uses.</summary>
    internal const int DefaultBatchSize = 100;

    /// <summary>
    /// <c>TFormDataCollector</c>'s batch size on the shipping lineage (PORT-PLAN.md §8.5).
    /// </summary>
    internal const int FormDataBatchSize = 200;

    /// <summary>Builds a descriptor and pairs it with a constant SQL template.</summary>
    /// <param name="name">Collector name.</param>
    /// <param name="title">Displayed title, suffixes already applied.</param>
    /// <param name="kind">Family.</param>
    /// <param name="varPrefix">Prepended to every returned <c>VarName</c>.</param>
    /// <param name="sql">The finished statement, with placeholders except <c>{IdList}</c> resolved.</param>
    /// <param name="batchSize">People per statement.</param>
    /// <param name="availability">
    /// What the database must provide, or <see langword="null"/> for
    /// <see cref="CollectorAvailability.Always"/>.
    /// </param>
    /// <returns>The collector.</returns>
    private static Collector Create(
        string name,
        string title,
        CollectorKind kind,
        string varPrefix,
        string sql,
        int batchSize,
        CollectorAvailability? availability = null)
    {
        CollectorDescriptor descriptor = new()
        {
            Name = name,
            Title = title,
            Kind = kind,
            VarPrefix = varPrefix,
            PidBinding = BindingFor(sql),
            BatchSize = batchSize,
            Availability = availability ?? CollectorAvailability.Always,
        };

        return new Collector(descriptor, context => BindIdList(sql, context));
    }

    /// <summary>Substitutes the per-batch person-id fragment.</summary>
    /// <param name="sql">The template.</param>
    /// <param name="context">Study id and the fragment.</param>
    /// <returns>The statement to execute.</returns>
    /// <remarks>
    /// Case-insensitive and global, matching the Delphi's
    /// <c>StringReplace( …, [rfIgnoreCase, rfReplaceAll] )</c>. A statement without the placeholder
    /// passes through untouched, which is what makes this safe to apply to every collector.
    /// </remarks>
    internal static string BindIdList(string sql, CollectorSqlContext context) =>
        sql.Replace(QaSql.PidList, context.IdListFragment ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    /// <summary>Derives how the person list reaches the server from the statement itself.</summary>
    /// <param name="sql">The statement, before <c>{IdList}</c> substitution.</param>
    /// <returns>The binding.</returns>
    internal static PidBinding BindingFor(string sql) =>
        sql.Contains(QaSql.PidList, StringComparison.OrdinalIgnoreCase) ? PidBinding.IdList
        : sql.Contains(QaSql.PersonIdParameter, StringComparison.Ordinal) ? PidBinding.SinglePerson
        : PidBinding.None;

    // ---- Demographics ---------------------------------------------------------------------------

    /// <summary><c>TDemographicsCollector</c> and its six subclasses.</summary>
    /// <param name="name">Collector name.</param>
    /// <param name="title">Displayed title, verbatim.</param>
    /// <param name="varName">Emitted <c>VarName</c>.</param>
    /// <param name="varSpec">Expression producing the value.</param>
    /// <returns>The collector.</returns>
    public static Collector Demographics(string name, string title, string varName, string varSpec) =>
        Create(name, title, CollectorKind.Demographics, string.Empty, QaSql.Demographics(varName, varSpec), DefaultBatchSize);

    /// <summary><c>TGlobalCollector</c> - a study-scoped query built from the current study id.</summary>
    /// <param name="name">Collector name.</param>
    /// <param name="title">Displayed title, verbatim.</param>
    /// <param name="sql">Builds the statement from the study id.</param>
    /// <returns>The collector.</returns>
    /// <remarks>
    /// The five of these are the only collectors whose SQL is not fully known at construction: the
    /// Delphi overrides <c>function SQL</c> rather than setting <c>FSQL</c>, because
    /// <c>fStudyId</c> is only assigned in <c>RunBatch</c>. None of them contains <c>{IdList}</c>,
    /// so the binding is <see cref="PidBinding.None"/> by construction rather than by inspection.
    /// </remarks>
    public static Collector StudyScoped(string name, string title, Func<int, string> sql)
    {
        CollectorDescriptor descriptor = new()
        {
            Name = name,
            Title = title,
            Kind = CollectorKind.StudyCase,
            VarPrefix = string.Empty,
            PidBinding = PidBinding.None,
            BatchSize = WholeCohort,
        };

        return new Collector(descriptor, context => BindIdList(sql(context.StudyId), context));
    }

    // ---- TCustomDataCollector -------------------------------------------------------------------

    /// <summary><c>TCustomDataCollector</c> - a finished statement and nothing else.</summary>
    /// <param name="name">Collector name.</param>
    /// <param name="title">Displayed title, verbatim.</param>
    /// <param name="varPrefix">Prepended to every returned <c>VarName</c>.</param>
    /// <param name="sql">The finished statement.</param>
    /// <param name="kind">Family, for grouping and golden-file naming.</param>
    /// <param name="availability">
    /// What the database must provide, or <see langword="null"/> for
    /// <see cref="CollectorAvailability.Always"/>.
    /// </param>
    /// <returns>The collector.</returns>
    /// <remarks>
    /// The batch size is always <see cref="WholeCohort"/>, as in
    /// <c>EPR.QA.Collector.Base.pas:226</c>. Whether the statement actually restricts to the cohort
    /// depends on whether it contains <c>{IdList}</c>; most do not (PORT-PLAN.md R10).
    /// </remarks>
    public static Collector Custom(
        string name,
        string title,
        string varPrefix,
        string sql,
        CollectorKind kind = CollectorKind.Custom,
        CollectorAvailability? availability = null) =>
        Create(name, title, kind, varPrefix, sql, WholeCohort, availability);

    // ---- Var sets -------------------------------------------------------------------------------

    /// <summary><c>TVarSetCollector.CreateForNumeric</c> - appends <c>' (siste)'</c>.</summary>
    /// <param name="name">Collector name.</param>
    /// <param name="title">Title <b>without</b> the suffix.</param>
    /// <param name="itemIds">The item ids, in registration order.</param>
    /// <returns>The collector.</returns>
    public static Collector VarSetNumeric(string name, string title, IReadOnlyList<int> itemIds) =>
        VarSet(name, title, itemIds, CrfVarType.Numeric);

    /// <summary><c>TVarSetCollector.CreateForText</c> - appends <c>' (siste)'</c>.</summary>
    /// <param name="name">Collector name.</param>
    /// <param name="title">Title <b>without</b> the suffix.</param>
    /// <param name="itemIds">The item ids, in registration order.</param>
    /// <returns>The collector.</returns>
    public static Collector VarSetText(string name, string title, IReadOnlyList<int> itemIds) =>
        VarSet(name, title, itemIds, CrfVarType.Text);

    private static Collector VarSet(string name, string title, IReadOnlyList<int> itemIds, CrfVarType type) =>
        Create(
            name,
            CollectorTitle.WithLastSuffix(title),
            CollectorKind.VarSet,
            string.Empty,
            QaSql.SnapshotVarSet(type, itemIds),
            DefaultBatchSize);

    /// <summary><c>TVarSetAgeCollector</c> - appends <c>' (siste)'</c>, prefix <c>ITEMAGE.</c>.</summary>
    /// <param name="name">Collector name.</param>
    /// <param name="title">Title <b>without</b> the suffix.</param>
    /// <param name="itemIds">The item ids, in registration order.</param>
    /// <returns>The collector.</returns>
    public static Collector VarSetAge(string name, string title, IReadOnlyList<int> itemIds) =>
        Create(
            name,
            CollectorTitle.WithLastSuffix(title),
            CollectorKind.VarSetAge,
            CollectorNames.ItemAgeVariablePrefix,
            QaSql.SnapshotVarSetAge(itemIds),
            DefaultBatchSize);

    /// <summary><c>TVarSetMaxCollector</c> - appends <c>' (høyeste)'</c>, prefix <c>ITEMMAX.</c>.</summary>
    /// <param name="name">Collector name.</param>
    /// <param name="title">Title <b>without</b> the suffix.</param>
    /// <param name="itemIds">The item ids, in registration order.</param>
    /// <returns>The collector.</returns>
    public static Collector VarSetMax(string name, string title, IReadOnlyList<int> itemIds) =>
        Create(
            name,
            CollectorTitle.WithMaxSuffix(title),
            CollectorKind.VarSetMax,
            CollectorNames.ItemMaxVariablePrefix,
            QaSql.MaximumQuantityVarSet(itemIds),
            DefaultBatchSize);

    // ---- Lab --------------------------------------------------------------------------------------

    /// <summary><c>TLabSetCollector</c> - wraps the group name unless it already has a colon.</summary>
    /// <param name="name">Collector name.</param>
    /// <param name="groupName">Group name, <b>not</b> the finished title.</param>
    /// <param name="labClassIds">The lab-class ids, in registration order.</param>
    /// <returns>The collector.</returns>
    public static Collector LabSet(string name, string groupName, IReadOnlyList<int> labClassIds) =>
        Create(
            name,
            CollectorTitle.ForLabSet(groupName),
            CollectorKind.LabSet,
            CollectorNames.LabVariablePrefix,
            QaSql.SnapshotLabSet(labClassIds),
            DefaultBatchSize);

    /// <summary><c>TLab{High,Medium,Low}TrustCollector</c>.</summary>
    /// <param name="name">Collector name.</param>
    /// <param name="title">Displayed title, verbatim.</param>
    /// <param name="trustLevel">3 = high, 2 = medium, 1 = low.</param>
    /// <returns>The collector.</returns>
    public static Collector LabTrust(string name, string title, int trustLevel) =>
        Create(
            name,
            title,
            CollectorKind.LabTrust,
            CollectorNames.LabVariablePrefix,
            QaSql.SnapshotLabDataByTrustLevel(trustLevel),
            DefaultBatchSize);

    /// <summary>Lab sample counts over a recent window.</summary>
    /// <param name="name">Collector name.</param>
    /// <param name="title">Displayed title, verbatim.</param>
    /// <param name="months">Window length in months, exclusive.</param>
    /// <returns>The collector.</returns>
    public static Collector LabCount(string name, string title, int months) =>
        Custom(name, title, string.Empty, QaSql.RecentLabDataPresent(months), CollectorKind.LabCount);

    // ---- Forms ------------------------------------------------------------------------------------

    /// <summary><c>TFormInstanceCollector</c> - one round trip per patient.</summary>
    /// <param name="name">Collector name.</param>
    /// <param name="title">Displayed title, verbatim.</param>
    /// <returns>The collector.</returns>
    public static Collector FormInstances(string name, string title) =>
        Create(name, title, CollectorKind.FormInstance, CollectorNames.FormPrefix, QaSql.FormInstances, 1);

    /// <summary><c>TFormAgeCollector</c> - appends <c>' (siste)'</c>, prefix <c>FORMAGE.</c>.</summary>
    /// <param name="name">Collector name. For the dynamic collectors this is the bare form name.</param>
    /// <param name="title">Title <b>without</b> the suffix.</param>
    /// <param name="formName">Form class name.</param>
    /// <returns>The collector.</returns>
    public static Collector FormAge(string name, string title, string formName) =>
        Create(
            name,
            CollectorTitle.WithLastSuffix(title),
            CollectorKind.FormAge,
            CollectorNames.FormAgeVariablePrefix,
            QaSql.FormAgeSingle(formName),
            DefaultBatchSize);

    /// <summary><c>TFormDataCollector</c> - every item of the newest instance of one form.</summary>
    /// <param name="formName">Form class name; also the name suffix and the variable prefix.</param>
    /// <param name="title">Displayed title, verbatim - no suffix is appended.</param>
    /// <returns>The collector.</returns>
    /// <remarks>
    /// The collector name is <c>FORM.</c> + the form name, the variable prefix is the form name
    /// followed by a dot, and the batch size is <see cref="FormDataBatchSize"/>.
    /// </remarks>
    public static Collector FormData(string formName, string title) =>
        Create(
            CollectorNames.FormPrefix + formName,
            title,
            CollectorKind.FormData,
            formName + ".",
            QaSql.SnapshotFormDataAll(formName),
            FormDataBatchSize);

    /// <summary>Instance counts per form type over a recent window.</summary>
    /// <param name="name">Collector name.</param>
    /// <param name="title">Displayed title, verbatim.</param>
    /// <param name="varPrefix">Prepended to every returned form name.</param>
    /// <param name="months">Window length in months, exclusive.</param>
    /// <returns>The collector.</returns>
    public static Collector FormCountAll(string name, string title, string varPrefix, int months) =>
        Custom(name, title, varPrefix, QaSql.RecentFormCountAll(months), CollectorKind.FormCount);

    /// <summary>Instance count of one form type over a recent window.</summary>
    /// <param name="name">Collector name.</param>
    /// <param name="title">Displayed title, verbatim.</param>
    /// <param name="varPrefix">Prepended to the returned form name.</param>
    /// <param name="formName">Form class name.</param>
    /// <param name="months">Window length in months, exclusive.</param>
    /// <returns>The collector.</returns>
    public static Collector FormCountSingle(string name, string title, string varPrefix, string formName, int months) =>
        Custom(name, title, varPrefix, QaSql.RecentFormCountSingle(formName, months), CollectorKind.FormCount);

    /// <summary>Worst recent completeness of one form type.</summary>
    /// <param name="name">Collector name.</param>
    /// <param name="title">Displayed title, verbatim.</param>
    /// <param name="varPrefix">Prepended to the returned form name.</param>
    /// <param name="formName">Form class name.</param>
    /// <param name="months">Window length in months, exclusive.</param>
    /// <returns>The collector.</returns>
    public static Collector FormCompleteness(string name, string title, string varPrefix, string formName, int months) =>
        Custom(name, title, varPrefix, QaSql.RecentFormCompleteness(formName, months), CollectorKind.FormCompleteness);

    // ---- Diagnosis ----------------------------------------------------------------------------------

    /// <summary><c>TDiagnoseCollector</c> - name and variable name both derive from the pattern.</summary>
    /// <param name="title">Displayed title, verbatim.</param>
    /// <param name="pattern">ICD-10 <c>LIKE</c> pattern.</param>
    /// <returns>The collector.</returns>
    public static Collector Diagnose(string title, string pattern) =>
        Custom(
            CollectorNames.DiagnosePrefix + SqlLiteral.AtcPatternToVariableName(pattern),
            title,
            CollectorNames.DiagnosePrefix,
            QaSql.DiagnoseByPattern(pattern),
            CollectorKind.Diagnose);

    /// <summary><c>TDementiaCollector</c>.</summary>
    /// <param name="title">Displayed title, verbatim.</param>
    /// <returns>The collector.</returns>
    public static Collector Dementia(string title) =>
        Custom(
            CollectorNames.DiagnoseDementia,
            title,
            CollectorNames.DiagnosePrefix,
            QaSql.DiagnoseDementiaAndAlzheimers(),
            CollectorKind.Diagnose);

    /// <summary>Problem counts per ICD-10 code prefix.</summary>
    /// <param name="name">Collector name.</param>
    /// <param name="title">Displayed title, verbatim.</param>
    /// <param name="level">Number of leading characters of the code.</param>
    /// <returns>The collector.</returns>
    public static Collector DiagnoseCount(string name, string title, int level) =>
        Custom(
            name,
            title,
            CollectorNames.DiagnoseCountPrefix,
            QaSql.DiagnoseDetailsByLevel(level),
            CollectorKind.DiagnoseCount);

    // ---- Drug -----------------------------------------------------------------------------------------

    /// <summary><c>TDrugCollector</c> - name and variable name both derive from the ATC pattern.</summary>
    /// <param name="title">Displayed title, verbatim.</param>
    /// <param name="atcPattern">ATC <c>LIKE</c> pattern.</param>
    /// <param name="useNameChecksum">
    /// <see langword="true"/> for <c>CreateChecksum</c> and <c>CreateForTreatType</c>;
    /// <see langword="false"/> for <c>CreateBasic</c>.
    /// </param>
    /// <returns>The collector.</returns>
    /// <remarks>
    /// <c>CreateForTreatType( …, ttAnyTreatType, … )</c> is behaviourally identical to
    /// <c>CreateChecksum</c>: <c>ttAnyTreatType</c> adds no <c>TreatType</c> clause and keeps the
    /// <c>DRUG.</c> name prefix. The <c>ttLongTerm</c> and <c>ttAsNeeded</c> variants exist upstream
    /// but QuickStat never uses them, so they are not ported.
    /// </remarks>
    public static Collector Drug(string title, string atcPattern, bool useNameChecksum = true) =>
        Create(
            CollectorNames.DrugPrefix + SqlLiteral.AtcPatternToVariableName(atcPattern),
            title,
            CollectorKind.Drug,
            CollectorNames.DrugVariablePrefix,
            DrugSql.DrugSet(atcPattern, useNameChecksum),
            DefaultBatchSize);

    /// <summary>A named drug set with hand-written SQL.</summary>
    /// <param name="name">Collector name.</param>
    /// <param name="title">Displayed title, verbatim.</param>
    /// <param name="varPrefix">Prepended to every returned <c>VarName</c>.</param>
    /// <param name="sql">The finished statement.</param>
    /// <param name="availability">
    /// What the database must provide, or <see langword="null"/> for
    /// <see cref="CollectorAvailability.Always"/>. Only
    /// <see cref="CollectorNames.DrugAntibioticIntermediate"/> passes anything here.
    /// </param>
    /// <returns>The collector.</returns>
    public static Collector DrugSetCollector(
        string name,
        string title,
        string varPrefix,
        string sql,
        CollectorAvailability? availability = null) =>
        Custom(name, title, varPrefix, sql, CollectorKind.DrugSet, availability);

    /// <summary>A DRUID drug-drug interaction collector.</summary>
    /// <param name="name">Collector name.</param>
    /// <param name="title">Displayed title, verbatim.</param>
    /// <param name="varPrefix">Prepended to every returned <c>VarName</c>.</param>
    /// <param name="sql">The finished statement.</param>
    /// <returns>The collector.</returns>
    public static Collector DrugInteraction(string name, string title, string varPrefix, string sql) =>
        Custom(name, title, varPrefix, sql, CollectorKind.DrugInteraction);

    /// <summary>Formats one of the two dynamic form-collector title templates.</summary>
    /// <param name="template">
    /// <see cref="CollectorTitles.FormAgeTemplate"/> or <see cref="CollectorTitles.FormDataTemplate"/>.
    /// </param>
    /// <param name="formTitle">Human-readable form title from <c>Report.GetFormClasses</c>.</param>
    /// <param name="formName">Form class name.</param>
    /// <returns>The title, before any class-applied suffix.</returns>
    public static string FormTitle(string template, string formTitle, string formName) =>
        string.Format(CultureInfo.InvariantCulture, template, formTitle, formName);
}
