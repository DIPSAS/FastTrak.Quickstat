using QuickStat.Domain.Anonymisation;

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

    /// <summary>All four headers, in ordinal order.</summary>
    public static IReadOnlyList<string> Headers { get; } =
        [PersonIdHeader, DateOfBirthHeader, NationalIdHeader, NameHeader];

    /// <summary>The header for one ordinal.</summary>
    /// <param name="ordinal">One of <see cref="PersonId"/>, <see cref="DateOfBirth"/>,
    /// <see cref="NationalId"/> or <see cref="Name"/>.</param>
    /// <returns>The header text.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The ordinal is not one of the four. The Delphi returned an empty string here; failing is
    /// better, because an empty header would be written into a file rather than noticed.
    /// </exception>
    public static string Header(int ordinal)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ordinal);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(ordinal, Count);

        return Headers[ordinal];
    }

    /// <summary>The ordinals a given identification mode actually emits, in order.</summary>
    /// <param name="columns">From <see cref="IdentificationColumns.For"/>.</param>
    /// <returns>The visible ordinals.</returns>
    /// <remarks>
    /// The two non-full modes <b>omit</b> the three identity columns rather than blanking them - no
    /// field and no separator (PORT-PLAN.md §6). So this list is genuinely shorter, and the header
    /// row and the data rows stay aligned because both derive from it.
    /// </remarks>
    public static IReadOnlyList<int> VisibleOrdinals(IdentificationColumns columns)
    {
        List<int> ordinals = new(Count);

        if (columns.IncludesPersonId)
        {
            ordinals.Add(PersonId);
        }

        if (columns.IncludesDateOfBirth)
        {
            ordinals.Add(DateOfBirth);
        }

        if (columns.IncludesNationalId)
        {
            ordinals.Add(NationalId);
        }

        if (columns.IncludesName)
        {
            ordinals.Add(Name);
        }

        return ordinals;
    }

    /// <summary>The headers a given identification mode actually emits, in order.</summary>
    /// <param name="columns">From <see cref="IdentificationColumns.For"/>.</param>
    /// <returns>The visible headers.</returns>
    /// <remarks>
    /// Declared here so the grid and the CSV writer read the same four Norwegian strings instead of
    /// transcribing them twice.
    /// </remarks>
    public static IReadOnlyList<string> HeadersFor(IdentificationColumns columns)
    {
        IReadOnlyList<int> ordinals = VisibleOrdinals(columns);
        List<string> headers = new(ordinals.Count);

        foreach (int ordinal in ordinals)
        {
            headers.Add(Headers[ordinal]);
        }

        return headers;
    }

    /// <summary>
    /// Whether a fixed column holds text rather than a number, and is therefore drawn left-aligned
    /// with an ellipsis.
    /// </summary>
    /// <param name="ordinal">The fixed-column ordinal.</param>
    /// <returns><see langword="true"/> for date of birth, national id and name.</returns>
    /// <remarks>
    /// Delphi <c>TPersonGrid.IsTextColumn</c> (<c>EPR.QA.GUI.Grid.pas:293-296</c>). Note that
    /// <see cref="PersonId"/> is <em>not</em> a text column: it is right-aligned like the data.
    /// </remarks>
    public static bool IsTextColumn(int ordinal) =>
        ordinal is DateOfBirth or NationalId or Name;
}
