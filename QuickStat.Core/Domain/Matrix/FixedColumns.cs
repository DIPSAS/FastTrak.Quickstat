namespace QuickStat.Domain.Matrix;

/// <summary>
/// The four leading identity columns, which are the same in the grid and in every export.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>EPR.QA.Matrix.Interfaces.pas:153-163</c>. The headers are Norwegian and are parity
/// that must not drift (PORT-PLAN.md §6); the ordinals are the frozen leading columns of the grid
/// (<c>FixedCols = 4</c>).
/// </para>
/// <para>
/// Shared by step 2.5, which renders them, and step 2.6, which writes them, so they are declared
/// once rather than transcribed twice.
/// </para>
/// </remarks>
public static class FixedColumns
{
    /// <summary>Number of leading identity columns. Delphi <c>FixedCols</c>.</summary>
    public const int Count = 4;

    /// <summary>Number of header rows. Delphi <c>FixedRows</c>.</summary>
    public const int HeaderRowCount = 1;

    /// <summary>Ordinal of the person-id column. Always present, in every identification mode.</summary>
    public const int PersonId = 0;

    /// <summary>Ordinal of the date-of-birth column.</summary>
    public const int DateOfBirth = 1;

    /// <summary>Ordinal of the national-id column.</summary>
    public const int NationalId = 2;

    /// <summary>Ordinal of the name column.</summary>
    public const int Name = 3;

    /// <summary>Header text for <see cref="PersonId"/>. Delphi <c>HDR_PID</c>.</summary>
    public const string PersonIdHeader = "PID";

    /// <summary>Header text for <see cref="DateOfBirth"/>. Delphi <c>HDR_BORN</c>.</summary>
    public const string DateOfBirthHeader = "Født";

    /// <summary>Header text for <see cref="NationalId"/>. Delphi <c>HDR_NATIONAL_ID</c>.</summary>
    public const string NationalIdHeader = "Fødselsnummer";

    /// <summary>Header text for <see cref="Name"/>. Delphi <c>HDR_NAME</c>.</summary>
    public const string NameHeader = "Navn";
}
