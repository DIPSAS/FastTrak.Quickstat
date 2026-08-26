namespace QuickStat.Domain.Populations;

/// <summary>
/// A population's result set is missing a column the patient loader requires.
/// </summary>
/// <remarks>
/// The minimum contract for a population procedure is <c>PersonId</c> and <c>FullName</c>. The
/// Delphi read <c>FullName</c> with <c>FieldByName</c>, which raises - inside a <c>try</c> that
/// silently freed the row (<c>CRF.Patient.List.pas:316, 320-322</c>). A population that omits the
/// column therefore produced <b>zero patients and no error whatsoever</b>. PORT-PLAN.md §7.2 lists
/// that as a bug being fixed: fail loudly, naming the population and the column.
/// </remarks>
public sealed class PopulationSchemaException : Exception
{
    /// <summary>Initialises a new instance.</summary>
    public PopulationSchemaException()
    {
    }

    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">Description naming the population and the missing column.</param>
    public PopulationSchemaException(string message)
        : base(message)
    {
    }

    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">Description naming the population and the missing column.</param>
    /// <param name="innerException">The underlying failure.</param>
    public PopulationSchemaException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>The population that produced the result set.</summary>
    public int ProcId { get; init; }

    /// <summary>The population's title, for the message.</summary>
    public string? PopulationTitle { get; init; }

    /// <summary>The column that was absent.</summary>
    public string? MissingColumn { get; init; }
}
