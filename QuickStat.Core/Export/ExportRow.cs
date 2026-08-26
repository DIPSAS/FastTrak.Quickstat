namespace QuickStat.Export;

/// <summary>One person as an export sees them.</summary>
/// <remarks>
/// The identity members are carried in full even when the selected
/// <c>QuickStat.Domain.Anonymisation.PersonIdentification</c> mode will omit them: the writer, not
/// this record, decides what reaches the file, and it decides through
/// <c>IdentificationColumns.For</c>. This mirrors the Delphi, where <c>TPersonGridRow</c> also kept
/// name, national id and date of birth in memory in every mode.
/// </remarks>
public sealed class ExportRow
{
    /// <summary>The real person id. Replaced by a pseudonym only at write time.</summary>
    public required int PersonId { get; init; }

    /// <summary>Date of birth, or <see langword="null"/> when the population did not return one.</summary>
    public DateTime? DateOfBirth { get; init; }

    /// <summary>National identity number, or <see langword="null"/>.</summary>
    public string? NationalId { get; init; }

    /// <summary><c>"Last, First"</c>, from <c>Patient.DisplayName</c>.</summary>
    public string FullName { get; init; } = "";

    /// <summary>
    /// One entry per <see cref="ExportDataset.Columns"/> entry, in the same order.
    /// </summary>
    public required IReadOnlyList<ExportCell> Cells { get; init; }
}
