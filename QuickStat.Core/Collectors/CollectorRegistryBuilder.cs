using System.Text.RegularExpressions;
using QuickStat.Collectors.Registry;

namespace QuickStat.Collectors;

/// <summary>
/// The pure half of building a registry: study name plus form classes plus database capabilities
/// in, ordered collector list out.
/// </summary>
/// <remarks>
/// <para>
/// Separated from <see cref="CollectorRegistry"/> on purpose. Everything that decides <em>which</em>
/// collectors a session gets - gate evaluation, the dynamic per-form collectors, availability
/// filtering, and above all the registration order - is a pure function of three arguments, so it
/// can be tested exhaustively with no database. The registry class does nothing but the two round
/// trips that fetch those arguments (PORT-PLAN.md R9).
/// </para>
/// <para>
/// This is <c>TQuickStatCollectors.PrepareStudy</c> and its five <c>AddCollectors*</c> procedures,
/// with the order preserved exactly.
/// </para>
/// </remarks>
public static partial class CollectorRegistryBuilder
{
    /// <summary>
    /// Every database object any collector in the catalog needs, de-duplicated - the argument list
    /// for the single availability probe.
    /// </summary>
    /// <remarks>
    /// Exactly one entry: <c>KB.AntibioticResistance2</c>, for the single collector that inner-joins
    /// it, <see cref="Registry.CollectorNames.DrugAntibioticIntermediate"/> (R7). Its neighbour
    /// <c>DRUG.RECOMMENDED</c> writes its nine ATC codes out in the statement and needs nothing
    /// (<c>EPR.QA.SQL.pas:431-443</c>).
    /// </remarks>
    public static IReadOnlyList<string> RequiredDatabaseObjects { get; } =
    [
        .. CollectorCatalog.All
            .SelectMany(collector => collector.Descriptor.Availability.RequiredDatabaseObjects)
            .Distinct(StringComparer.OrdinalIgnoreCase),
    ];

    /// <summary>Builds the ordered list of collectors for a session.</summary>
    /// <param name="studyName">Study short name; gates are evaluated against it.</param>
    /// <param name="formClasses">
    /// The rows of <c>EXEC Report.GetFormClasses :StudyId</c>, in the order the server returned
    /// them.
    /// </param>
    /// <param name="availability">
    /// Study facts and the set of database objects that resolved. The set must compare
    /// case-insensitively, because object names do.
    /// </param>
    /// <param name="onUnavailable">
    /// Called once per collector dropped by <see cref="CollectorAvailability"/>, so the caller can
    /// log the skip. Support has to be able to tell "the table is missing" from "the column is
    /// empty".
    /// </param>
    /// <returns>The collectors, in registration order.</returns>
    public static IReadOnlyList<ICollector> Build(
        string studyName,
        IEnumerable<FormClass> formClasses,
        CollectorAvailabilityContext availability,
        Action<CollectorDescriptor>? onUnavailable = null)
    {
        ArgumentNullException.ThrowIfNull(formClasses);

        StudyGate openGates = StudyGates.For(studyName);
        List<ICollector> collectors = [];

        void AddAvailable(IEnumerable<ICollector> candidates)
        {
            foreach (ICollector candidate in candidates)
            {
                if (IsAvailable(candidate.Descriptor.Availability, availability))
                {
                    collectors.Add(candidate);
                }
                else
                {
                    onUnavailable?.Invoke(candidate.Descriptor);
                }
            }
        }

        // 1. Always-on, before the dynamic collectors: PrepareStudy's own registration, then
        //    AddCollectorsBasic, then AddCollectorsLabData.
        AddAvailable(CollectorCatalog.AlwaysBeforeFormCollectors);

        // 2. AddCollectorsStudySpecific - two collectors per non-anonymous form class.
        AddAvailable(CreateFormCollectors(formClasses));

        // 3. AddCollectorsHardCoded's one ungated registration.
        AddAvailable(CollectorCatalog.AlwaysAfterFormCollectors);

        // 4. The five gate blocks, in source order. They are independent ifs, so a study can open
        //    several of them.
        foreach (GatedCollectorFamily family in CollectorCatalog.GatedFamilies)
        {
            if ((openGates & family.Gate) != StudyGate.Always)
            {
                AddAvailable(family.Collectors);
            }
        }

        return collectors;
    }

    /// <summary>
    /// The <c>2 x N</c> dynamic per-form collectors: a form-age collector and a form-data collector
    /// for every non-anonymous, not-yet-seen form class.
    /// </summary>
    /// <param name="formClasses">The rows of <c>EXEC Report.GetFormClasses :StudyId</c>.</param>
    /// <returns>The collectors, two per accepted form class, in server order.</returns>
    /// <remarks>
    /// <para>
    /// Form names matching <c>FORM\d+</c> are skipped as "anonymous forms". The match is an
    /// unanchored, case-sensitive substring test, exactly as
    /// <c>TRegEx.IsMatch( formName, 'FORM\d+' )</c> is, so <c>MYFORM12</c> is skipped too.
    /// </para>
    /// <para>
    /// Duplicates by form name are skipped. The Delphi guards with a
    /// <c>TDictionary&lt;string, string&gt;</c>, whose default comparer is ordinal and
    /// case-sensitive.
    /// </para>
    /// <para>
    /// The form-age collector's <b>name is the bare form name</b>, with no prefix - that is what
    /// <c>TFormAgeCollector.Create( formName, … )</c> passes through. Only the form-data collector
    /// gets the <c>FORM.</c> prefix.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<ICollector> CreateFormCollectors(IEnumerable<FormClass> formClasses)
    {
        ArgumentNullException.ThrowIfNull(formClasses);

        List<ICollector> collectors = [];
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (FormClass formClass in formClasses)
        {
            if (AnonymousFormPattern().IsMatch(formClass.FormName) || !seen.Add(formClass.FormName))
            {
                continue;
            }

            collectors.Add(Make.FormAge(
                formClass.FormName,
                Make.FormTitle(CollectorTitles.FormAgeTemplate, formClass.FormTitle, formClass.FormName),
                formClass.FormName));

            // The Delphi has this call twice, once commented out and once not. Register it once.
            collectors.Add(Make.FormData(
                formClass.FormName,
                Make.FormTitle(CollectorTitles.FormDataTemplate, formClass.FormTitle, formClass.FormName)));
        }

        return collectors;
    }

    /// <summary>Whether a collector's availability conditions hold.</summary>
    /// <param name="availability">The conditions.</param>
    /// <param name="context">Study facts and the objects that resolved.</param>
    /// <returns><see langword="true"/> when the collector may be registered.</returns>
    /// <remarks>Both the object list and the predicate must hold; the object list is checked first.</remarks>
    public static bool IsAvailable(CollectorAvailability availability, CollectorAvailabilityContext context)
    {
        ArgumentNullException.ThrowIfNull(availability);

        foreach (string required in availability.RequiredDatabaseObjects)
        {
            if (!context.ResolvedDatabaseObjects.Contains(required))
            {
                return false;
            }
        }

        return availability.Predicate is null || availability.Predicate(context);
    }

    [GeneratedRegex(@"FORM\d+", RegexOptions.None)]
    private static partial Regex AnonymousFormPattern();
}
