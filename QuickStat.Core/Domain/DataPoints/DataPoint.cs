using System.Globalization;
using System.Text;

namespace QuickStat.Domain.DataPoints;

/// <summary>One value in one cell of the matrix.</summary>
/// <remarks>
/// <para>
/// Delphi: <c>TDataPoint</c> (<c>EPR.QA.DataPoint.pas:12-22</c>) and its eighteen subclasses. The
/// subclasses exist only to override display text and cell colour, which is data rather than
/// behaviour, so here they become <see cref="DataPointRule"/> and the class is sealed.
/// </para>
/// <para>
/// Every value is a <see cref="double"/>. The matrix has no other field type
/// (<c>EPR.QA.Matrix.pas:542</c> returns <c>ftFloat</c> unconditionally), and dates arrive already
/// converted to a day count.
/// </para>
/// </remarks>
public sealed class DataPoint
{
    /// <summary>The matrix column, prefix included.</summary>
    public required string VarName { get; init; }

    /// <summary>The numeric value. This, not the display text, is what the CSV exports.</summary>
    public double Value { get; private set; }

    /// <summary>When the underlying observation was made.</summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>Identity of the source row, from ordinal 4 of the collector result.</summary>
    public int RowId { get; private set; }

    /// <summary>Optional <c>ItemId</c>; zero when the collector did not project one.</summary>
    public int ItemId { get; set; }

    /// <summary>
    /// Optional free text - a form answer or an ATC name.
    /// </summary>
    /// <remarks>
    /// When present the cell is drawn left-aligned and shows the caption truncated to <b>six</b>
    /// characters instead of the number (<c>EPR.QA.DataPoint.pas:86-92</c>). The export writes the
    /// caption too, but <b>in full</b> — truncation is a display concern only
    /// (<c>EPR.QA.Matrix.pas:242-246</c>).
    /// </remarks>
    /// <remarks>
    /// An earlier revision of this remark said the export always writes the raw value. That
    /// described the <c>develop_old</c> copy in this repository, not the parity baseline: commit
    /// <c>8486b3d09</c> (2022-05-06, "#489525: QuickStat skal kunne vise og eksportere tekstdata fra
    /// skjema") added the caption branch, and it is present on <b>both</b> tarmscreening refs, so
    /// this behaviour does not depend on how PORT-PLAN.md R12 was decided.
    /// </remarks>
    public string? Caption { get; set; }

    /// <summary>
    /// How many times <see cref="Update"/> has been called, shown in the hint panel.
    /// </summary>
    public int UpdateCount { get; private set; }

    /// <summary>Assigns the value and increments <see cref="UpdateCount"/>.</summary>
    /// <param name="value">The value.</param>
    /// <param name="timestamp">Observation time.</param>
    /// <param name="rowId">Source row identity.</param>
    public void Update(double value, DateTime timestamp, int rowId)
    {
        Value = value;
        Timestamp = timestamp;
        RowId = rowId;
        UpdateCount++;
    }

    /// <summary>The multi-line text the floating hint panel shows for this cell.</summary>
    /// <returns>
    /// <c>VarName = value</c>, timestamp, row id and update count, plus item id and caption when
    /// they are set.
    /// </returns>
    /// <remarks>
    /// Delphi: <c>TDataPoint.AsString</c> (<c>EPR.QA.DataPoint.pas:72-79</c>). The number uses
    /// <c>%g</c> with the locale decimal separator and the date uses the locale short date, so on
    /// nb-NO the hint reads <c>3,5</c> and <c>14.08.2019</c> - unlike the timestamp columns in the
    /// export, which are ISO.
    /// </remarks>
    public string Describe() => Describe(null);

    /// <summary>The multi-line text the floating hint panel shows for this cell.</summary>
    /// <param name="formatProvider">
    /// Supplies the decimal separator and the short date pattern; <see langword="null"/> means
    /// <see cref="CultureInfo.CurrentCulture"/>, which is what the Delphi's global
    /// <c>FormatSettings</c> amount to.
    /// </param>
    /// <returns>The hint text.</returns>
    /// <remarks>
    /// Line breaks are bare <c>LF</c>, not <c>CRLF</c>: the Delphi builds the string with <c>#10</c>.
    /// </remarks>
    public string Describe(IFormatProvider? formatProvider)
    {
        CultureInfo culture = formatProvider as CultureInfo ?? CultureInfo.CurrentCulture;

        StringBuilder text = new();

        text.Append(VarName)
            .Append(" = ")
            .Append(NumericFormat.G(Value, culture))
            .Append('\n')
            .Append("TimeStamp = ")
            .Append(Timestamp.ToString("d", culture))
            .Append('\n')
            .Append("RowId = ")
            .Append(RowId.ToString(culture))
            .Append('\n')
            .Append("Updates = ")
            .Append(UpdateCount.ToString(culture));

        if (ItemId > 0)
        {
            text.Append('\n').Append("ItemId = ").Append(ItemId.ToString(culture));
        }

        if (!string.IsNullOrEmpty(Caption))
        {
            text.Append('\n').Append("Caption =\"").Append(Caption).Append('"');
        }

        return text.ToString();
    }
}
