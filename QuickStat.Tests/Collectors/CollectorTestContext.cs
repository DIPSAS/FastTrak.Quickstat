using QuickStat.Collectors;
using QuickStat.Collectors.Registry;

namespace QuickStat.Tests.Collectors;

/// <summary>
/// Shared helpers for the step 2.4 tests.
/// </summary>
/// <remarks>
/// No database is available to the test suite (PORT-PLAN.md R9), and none is needed: everything the
/// registry decides is a pure function of the study name, the study's form classes and the set of
/// database objects that resolved, so the tests drive
/// <see cref="CollectorRegistryBuilder"/> directly.
/// </remarks>
internal static class CollectorTestContext
{
    /// <summary>
    /// The fixed <c>{IdList}</c> replacement used everywhere a statement is asserted on.
    /// </summary>
    /// <remarks>
    /// A token rather than a real list is what makes generated SQL deterministic and therefore
    /// golden-file-able (PORT-PLAN.md R3).
    /// </remarks>
    public const string IdListToken = "(/*PIDS*/)";

    /// <summary>A fixed study id, so study-scoped statements are deterministic too.</summary>
    public const int StudyId = 42;

    /// <summary>Distinct collector names in the catalog. PORT-PLAN.md §10.3.</summary>
    public const int DistinctNameCount = 131;

    /// <summary>Always-on static collectors - the ones no gate guards.</summary>
    public const int AlwaysCount = 37;

    /// <summary>
    /// Static collectors a fully gated study registers on a database that resolves every object the
    /// catalog asks for. PORT-PLAN.md §10.4.
    /// </summary>
    public const int FullyGatedCount = 124;

    /// <summary>
    /// The same study on a database that resolves none of them.
    /// </summary>
    /// <remarks>
    /// The difference is <see cref="CollectorNames.DrugAntibioticIntermediate"/> and nothing else -
    /// pinned by <c>CollectorRegistryCountTests</c> rather than asserted here, so that adding a
    /// second optional collector fails a test instead of quietly changing a constant.
    /// </remarks>
    public const int FullyGatedWithoutOptionalObjects = FullyGatedCount - 1;

    /// <summary>The SQL context every test builds statements with.</summary>
    public static CollectorSqlContext SqlContext { get; } = new(StudyId, IdListToken);

    /// <summary>
    /// Every database object the catalog asks for - the probe outcome of a fully provisioned
    /// database.
    /// </summary>
    public static string[] EveryDatabaseObject { get; } = [.. CollectorRegistryBuilder.RequiredDatabaseObjects];

    /// <summary>Builds an availability context.</summary>
    /// <param name="studyName">Study short name.</param>
    /// <param name="resolvedObjects">Database objects that exist, or none.</param>
    /// <returns>The context.</returns>
    public static CollectorAvailabilityContext Availability(string studyName, params string[] resolvedObjects) =>
        new()
        {
            StudyName = studyName,
            StudyId = StudyId,
            ResolvedDatabaseObjects = new HashSet<string>(resolvedObjects, StringComparer.OrdinalIgnoreCase),
        };

    /// <summary>
    /// Builds the registry for a study against a database that resolves <b>no</b> optional object.
    /// </summary>
    /// <param name="studyName">Study short name.</param>
    /// <param name="formClasses">Rows of <c>Report.GetFormClasses</c>.</param>
    /// <returns>The collectors, in registration order.</returns>
    /// <remarks>
    /// The pessimistic probe outcome, because it is the one a customer without the <c>KB</c> schema
    /// gets. Counts taken from this overload are
    /// <see cref="FullyGatedWithoutOptionalObjects"/>, not <see cref="FullyGatedCount"/>.
    /// </remarks>
    public static IReadOnlyList<ICollector> Build(string studyName, params FormClass[] formClasses) =>
        CollectorRegistryBuilder.Build(studyName, formClasses, Availability(studyName));

    /// <summary>
    /// Builds the registry for a study against a database that resolves <b>every</b> object the
    /// catalog asks for.
    /// </summary>
    /// <param name="studyName">Study short name.</param>
    /// <param name="formClasses">Rows of <c>Report.GetFormClasses</c>.</param>
    /// <returns>The collectors, in registration order.</returns>
    public static IReadOnlyList<ICollector> BuildComplete(string studyName, params FormClass[] formClasses) =>
        CollectorRegistryBuilder.Build(studyName, formClasses, Availability(studyName, EveryDatabaseObject));

    /// <summary>The names of a collector list, in order.</summary>
    /// <param name="collectors">The collectors.</param>
    /// <returns>The names, in a list that supports <c>IndexOf</c>.</returns>
    public static List<string> Names(IEnumerable<ICollector> collectors) =>
        [.. collectors.Select(collector => collector.Descriptor.Name)];

    /// <summary>Finds one collector by name, failing the test when it is absent or duplicated.</summary>
    /// <param name="collectors">The collectors.</param>
    /// <param name="name">The name to find.</param>
    /// <returns>The collector.</returns>
    public static ICollector ByName(IEnumerable<ICollector> collectors, string name) =>
        collectors.Single(collector => collector.Descriptor.Name == name);
}
