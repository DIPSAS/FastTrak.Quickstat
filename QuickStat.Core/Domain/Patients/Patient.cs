namespace QuickStat.Domain.Patients;

/// <summary>
/// One patient in a loaded population - the subset of <c>TStudyCase</c> that QuickStat actually
/// reads.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>TStudyCase</c> built by <c>TPatientList.Query</c>
/// (<c>CRF.Patient.List.pas:286-333</c>) out of a chain of four <c>Load</c> overrides. All but one
/// of the columns are optional; a population that fails to return <c>FullName</c> is a
/// <see cref="QuickStat.Domain.Populations.PopulationSchemaException"/>.
/// </para>
/// <para>
/// <see cref="FirstName"/> and <see cref="LastName"/> are stored split rather than as a single
/// <c>FullName</c>, because the Delphi's parse is lossy in a way that shows on screen: it splits on
/// a comma with <c>StrictDelimiter</c>, so <c>"Nordmann, Ola"</c> splits correctly but
/// <c>"Ola Nordmann"</c> becomes a last name of <c>"Ola Nordmann"</c> and an empty first name
/// (<c>Emetra.Person.pas:328-361</c>). Step 2.3 owns that parse; the split result is the contract.
/// </para>
/// </remarks>
public sealed class Patient
{
    /// <summary><c>dbo.Person.PersonId</c>. The identity, and the grid's sort key.</summary>
    public required int PersonId { get; init; }

    /// <summary>Date of birth, or <see langword="null"/> when the population did not return it.</summary>
    public DateTime? DateOfBirth { get; init; }

    /// <summary>Given name; empty when the population's <c>FullName</c> had no comma.</summary>
    public string FirstName { get; init; } = "";

    /// <summary>Family name, or the whole unsplittable name.</summary>
    public string LastName { get; init; } = "";

    /// <summary>
    /// National identity number, or <see langword="null"/>/empty when unknown.
    /// </summary>
    /// <remarks>
    /// Settable because most population procedures do not return it and it is filled afterwards by
    /// <see cref="IPatientRepository.GetNationalIdsAsync"/>. The recovery query filters
    /// <c>NationalId IS NOT NULL</c>, so patients without one are simply absent from its result:
    /// never write an empty value back over an existing one.
    /// </remarks>
    public string? NationalId { get; set; }

    /// <summary>Raw <c>GenderId</c> as returned.</summary>
    public int GenderId { get; init; }

    /// <summary>Interpreted sex.</summary>
    public Sex Sex { get; init; }

    /// <summary>Group / ward id.</summary>
    public int GroupId { get; init; }

    /// <summary>Group / ward name; shown by some collectors.</summary>
    public string GroupName { get; init; } = "";

    /// <summary>Study-case status id.</summary>
    public int StatusId { get; init; }

    /// <summary>
    /// Status text. Overwritten by the population's optional <c>InfoText</c> column when present
    /// (<c>CRF.Patient.List.pas:317-318</c>).
    /// </summary>
    public string StatusText { get; init; } = "";

    /// <summary>Whether the study case is flagged as a test case.</summary>
    public bool IsTestCase { get; init; }

    /// <summary>
    /// <c>"Last, First"</c> - exactly what the grid and every export show.
    /// </summary>
    /// <remarks>
    /// Implemented here rather than stubbed because the format <em>is</em> the contract
    /// (<c>EPR.QA.Matrix.Row.pas:90-97</c>) and step 2.5 must not be able to render it differently
    /// from step 2.6. Note the grid deliberately does not use <c>TCRFPerson.Get_FullName</c>, which
    /// would have produced <c>"First Middle Last"</c>.
    /// </remarks>
    public string DisplayName => $"{LastName}, {FirstName}";
}
