using System.Globalization;
using System.Text;
using QuickStat.Domain.Anonymisation;

namespace QuickStat.Export;

/// <summary>Everything that governs one export.</summary>
/// <remarks>
/// <para>
/// A record rather than a parameter list, because the Delphi's three-argument
/// <c>SaveToFile(fileName, identification, includeDates)</c> is already at the limit of what a
/// signature should carry, and the port adds format, dialect and key-file handling on top.
/// </para>
/// <para>
/// The constants on this type are the byte-level parity specification (PORT-PLAN.md §6). They are
/// constants and not literals in the writer so that the byte-comparison tests and the writer read
/// from the same place.
/// </para>
/// </remarks>
public sealed record DatasetExportOptions
{
    /// <summary>Field separator, written after <b>every</b> field including the last.</summary>
    /// <remarks>
    /// Every line therefore ends with the separator and then CRLF. This is not a quirk to tidy up -
    /// it changes the field count every consumer sees.
    /// </remarks>
    public const char LegacySeparator = ';';

    /// <summary>Code page for <see cref="CsvDialect.Legacy"/>: Windows-1252, and no byte-order mark.</summary>
    /// <remarks>
    /// .NET does not ship this encoding by default; the application must register
    /// <c>CodePagesEncodingProvider.Instance</c> during start-up or the writer cannot resolve it.
    /// </remarks>
    public const int LegacyCodePage = 1252;

    /// <summary>Appended to a variable name to form the header of its timestamp column.</summary>
    public const string TimestampColumnSuffix = ".DATE";

    /// <summary>Timestamp column format. ISO and locale-independent, unlike the date-of-birth column.</summary>
    public const string TimestampFormat = "yyyy-MM-dd";

    /// <summary>Extension of the re-identification key file, replacing the export's own.</summary>
    public const string KeyFileExtension = ".mapping.txt";

    /// <summary>
    /// How identified the export is.
    /// </summary>
    /// <remarks>
    /// Must come from <see cref="IIdentificationPolicy.Mode"/>. Do not read a control's state and
    /// do not carry a second copy: that is exactly the defect being fixed.
    /// </remarks>
    public required PersonIdentification Identification { get; init; }

    /// <summary>
    /// Which identity columns are written, derived from <see cref="Identification"/> and never
    /// decided separately.
    /// </summary>
    public IdentificationColumns Columns => IdentificationColumns.For(Identification);

    /// <summary>
    /// Write an extra timestamp field after every data field.
    /// </summary>
    /// <remarks>
    /// The <c>Export timestamp for every data element</c> check box. The header gains
    /// <c>"&lt;VarName&gt;.DATE"</c> and each data row gains an ISO date - or, for a cell with no
    /// datapoint, nothing at all followed by the separator.
    /// </remarks>
    public bool IncludeTimestamps { get; init; }

    /// <summary>File format.</summary>
    public ExportFormat Format { get; init; } = ExportFormat.Csv;

    /// <summary>CSV conventions. Ignored for <see cref="ExportFormat.Xlsx"/>.</summary>
    public CsvDialect Dialect { get; init; } = CsvDialect.Legacy;

    /// <summary>
    /// Write the pseudonym-to-person-id key file next to the export.
    /// </summary>
    /// <remarks>
    /// Off by default, and only meaningful for
    /// <see cref="PersonIdentification.RandomPersonId"/>. The Delphi wrote it unconditionally and
    /// never deleted it for temporary exports, so plaintext keys accumulate in <c>%TEMP%</c>. When
    /// this is on, warn the user and track the file for deletion.
    /// </remarks>
    public bool WriteKeyFile { get; init; }

    /// <summary>
    /// Culture for numeric formatting, or <see langword="null"/> for the dialect's default.
    /// </summary>
    /// <remarks>
    /// <see cref="CsvDialect.Legacy"/> defaults to the <b>current</b> culture, because the Delphi
    /// formats with <c>%g</c> against the global format settings - so a value of 3.5 is written
    /// <c>3,5</c> on nb-NO. <see cref="CsvDialect.Rfc4180"/> defaults to
    /// <see cref="CultureInfo.InvariantCulture"/>. This is also why
    /// <c>InvariantGlobalization</c> must stay off in the build.
    /// </remarks>
    public CultureInfo? Culture { get; init; }

    /// <summary>Text encoding, or <see langword="null"/> for the dialect's default.</summary>
    public Encoding? Encoding { get; init; }
}
