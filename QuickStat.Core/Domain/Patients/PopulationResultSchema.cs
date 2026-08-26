using QuickStat.Domain.Populations;

namespace QuickStat.Domain.Patients;

/// <summary>
/// The minimum a population's result set has to contain before it can be turned into a cohort.
/// </summary>
/// <remarks>
/// PORT-PLAN.md §7.2. <c>CRF.Patient.List.pas:316</c> read <c>FullName</c> with <c>FieldByName</c>,
/// which raises, inside a <c>try</c> whose handler silently freed the row
/// (<c>CRF.Patient.List.pas:320-322</c>). A population that omitted the column therefore produced
/// <b>zero patients and no message</b> - indistinguishable from a genuinely empty cohort, in a tool
/// whose whole purpose is to tell a researcher who is in their cohort.
/// </remarks>
internal static class PopulationResultSchema
{
    /// <summary>
    /// The column the Delphi read strictly, and the one PORT-PLAN.md §7.2 names.
    /// </summary>
    public const string FullNameColumn = "FullName";

    /// <summary>
    /// The identity column. Also required: without it every patient reads as <c>-1</c> and the
    /// de-duplication in <see cref="IPatientRepository.LoadPopulationAsync"/> collapses the whole
    /// cohort to a single row - the same class of silent, total data loss.
    /// <see cref="PopulationSchemaException"/> names both columns as the minimum contract.
    /// </summary>
    public const string PersonIdColumn = "PersonId";

    /// <summary>Checks a result set's columns and throws when the cohort cannot be built.</summary>
    /// <param name="columnNames">Column names as returned, matched case-insensitively.</param>
    /// <param name="procId">The population, for the message. Zero for a statement that is not one.</param>
    /// <param name="populationTitle">The population's title, or another label naming the statement.</param>
    /// <exception cref="PopulationSchemaException">A required column is absent.</exception>
    public static void Validate(IEnumerable<string> columnNames, int procId, string? populationTitle)
    {
        ArgumentNullException.ThrowIfNull(columnNames);

        HashSet<string> present = new(columnNames, StringComparer.OrdinalIgnoreCase);

        // FullName first: it is the column PORT-PLAN.md §7.2 names, so when a population is missing
        // both the message points at the one a reader will be looking for.
        if (!present.Contains(FullNameColumn))
        {
            throw Missing(procId, populationTitle, FullNameColumn);
        }

        if (!present.Contains(PersonIdColumn))
        {
            throw Missing(procId, populationTitle, PersonIdColumn);
        }
    }

    /// <summary>Builds the exception for one missing column.</summary>
    /// <param name="procId">The population.</param>
    /// <param name="populationTitle">The population's title.</param>
    /// <param name="column">The absent column.</param>
    /// <returns>The exception to throw.</returns>
    public static PopulationSchemaException Missing(int procId, string? populationTitle, string column)
    {
        string where = string.IsNullOrEmpty(populationTitle)
            ? $"Population {procId}"
            : $"Population {procId} (\"{populationTitle}\")";

        return new PopulationSchemaException(
            $"{where} does not return the required column \"{column}\", so no patients can be loaded from it.")
        {
            ProcId = procId,
            PopulationTitle = populationTitle,
            MissingColumn = column,
        };
    }
}
