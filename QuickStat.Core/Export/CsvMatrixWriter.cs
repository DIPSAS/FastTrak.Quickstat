using System.Globalization;
using System.Text;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.Matrix;

namespace QuickStat.Export;

/// <summary>
/// Writes a dataset as delimited text. <see cref="CsvDialect.Legacy"/> is byte-for-byte the Delphi.
/// </summary>
/// <remarks>
/// <para>
/// The reference is <c>TPersonGridData.SaveToFile</c> (<c>EPR.QA.Matrix.pas:445-497</c>) together
/// with <c>GetCellText</c> (<c>:222-278</c>), both read from the pinned tarmscreening tip. Every
/// element of the legacy format is load-bearing for someone's R/SPSS/Stata script, so it is
/// reproduced rather than improved (PORT-PLAN.md §6, R4):
/// </para>
/// <list type="bullet">
///   <item><description>
///     <c>;</c> after <b>every</b> field <em>including the last</em>, so each line ends with a
///     separator and then CRLF. The Delphi wrote <c>write(F, field, ';')</c> per field and
///     <c>WriteLn(F)</c> per row - there is no join, so there is no last-field special case.
///   </description></item>
///   <item><description>
///     <c>AnsiQuotedStr(s, '"')</c>: every field is wrapped in double quotes and embedded quotes are
///     doubled. The single exception is the pseudonym, written by
///     <c>write(F, &lt;integer&gt;, ';')</c> and therefore bare.
///   </description></item>
///   <item><description>
///     Windows-1252, no byte-order mark, CRLF - a classic <c>AssignFile</c>/<c>Rewrite</c> text
///     file.
///   </description></item>
///   <item><description>
///     Numbers through <see cref="DelphiNumberFormat.Format"/>, i.e. <c>%g</c> with the locale
///     decimal separator: <c>3,5</c> on nb-NO.
///   </description></item>
///   <item><description>
///     Header cells carry the <c>VarName</c>, not the title.
///   </description></item>
///   <item><description>
///     With timestamps on, each data column is followed by a second field: <c>"&lt;VarName&gt;.DATE"</c>
///     in the header, an ISO date where there is a datapoint, and - where there is not - nothing at
///     all, still followed by its separator.
///   </description></item>
/// </list>
/// <para>
/// Which identity columns appear is <b>never</b> decided here. It comes from
/// <see cref="IdentificationColumns.For"/>, through <see cref="DatasetExportOptions.Columns"/>, so
/// the grid and the file cannot disagree (PORT-PLAN.md §7.2).
/// </para>
/// </remarks>
public static class CsvMatrixWriter
{
    /// <summary>Field separator for <see cref="CsvDialect.Rfc4180"/>.</summary>
    public const char Rfc4180Separator = ',';

    /// <summary>Line terminator. Both dialects use CRLF; RFC 4180 mandates it.</summary>
    public const string LineTerminator = "\r\n";

    /// <summary>Date format for <see cref="CsvDialect.Rfc4180"/>, which is ISO throughout.</summary>
    public const string Rfc4180DateFormat = "yyyy-MM-dd";

    static CsvMatrixWriter()
    {
        // Windows-1252 is not in .NET's default encoding set, so Encoding.GetEncoding(1252) throws
        // NotSupportedException until this runs. Registering from a static constructor here rather
        // than from application start-up keeps the writer self-contained: a unit test, a future
        // console tool and the WPF app all get the same bytes without anyone remembering a line of
        // bootstrap. The provider ships in the .NET 10 shared framework - do NOT add the
        // System.Text.Encoding.CodePages package, which raises NU1510 in this repository.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>Windows-1252, which has a zero-length preamble and therefore writes no BOM.</summary>
    public static Encoding LegacyEncoding => Encoding.GetEncoding(DatasetExportOptions.LegacyCodePage);

    /// <summary>The encoding an export will actually use.</summary>
    /// <param name="options">The export options.</param>
    /// <returns>
    /// <see cref="DatasetExportOptions.Encoding"/> when set, otherwise the dialect's default:
    /// Windows-1252 without a BOM for <see cref="CsvDialect.Legacy"/>, UTF-8 with one for
    /// <see cref="CsvDialect.Rfc4180"/>.
    /// </returns>
    /// <remarks>
    /// Call this rather than <c>Encoding.GetEncoding(1252)</c> directly: touching this type is what
    /// registers the code-pages provider.
    /// </remarks>
    public static Encoding ResolveEncoding(DatasetExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.Encoding
            ?? (options.Dialect == CsvDialect.Legacy
                ? LegacyEncoding
                : new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    /// <summary>The culture an export will actually use.</summary>
    /// <param name="options">The export options.</param>
    /// <returns>
    /// <see cref="DatasetExportOptions.Culture"/> when set, otherwise
    /// <see cref="CultureInfo.CurrentCulture"/> for <see cref="CsvDialect.Legacy"/> - the Delphi
    /// formatted against the process-wide <c>FormatSettings</c> - and
    /// <see cref="CultureInfo.InvariantCulture"/> for <see cref="CsvDialect.Rfc4180"/>.
    /// </returns>
    public static CultureInfo ResolveCulture(DatasetExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.Culture
            ?? (options.Dialect == CsvDialect.Legacy
                ? CultureInfo.CurrentCulture
                : CultureInfo.InvariantCulture);
    }

    /// <summary>Number of fields a row of this dataset will contain.</summary>
    /// <param name="dataset">The dataset.</param>
    /// <param name="options">The export options.</param>
    /// <returns>Identity columns plus data columns plus, when enabled, one timestamp column each.</returns>
    public static int CountColumns(ExportDataset dataset, DatasetExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentNullException.ThrowIfNull(options);

        int identityColumns = FixedColumns.VisibleOrdinals(options.Columns).Count;

        return identityColumns + (dataset.Columns.Count * (options.IncludeTimestamps ? 2 : 1));
    }

    /// <summary>Writes the dataset to a stream.</summary>
    /// <param name="dataset">The dataset.</param>
    /// <param name="stream">Destination. Left open; the caller owns it.</param>
    /// <param name="options">Identification, timestamps and dialect.</param>
    /// <param name="anonymiser">
    /// Required when <see cref="IdentificationColumns.UsesPseudonyms"/> is set, ignored otherwise.
    /// It must already have a pseudonym space.
    /// </param>
    /// <param name="cancellationToken">Cancels between rows.</param>
    /// <exception cref="ArgumentException">
    /// <see cref="PersonIdentification.RandomPersonId"/> without an anonymiser.
    /// </exception>
    public static void Write(
        ExportDataset dataset,
        Stream stream,
        DatasetExportOptions options,
        IAnonymiser? anonymiser = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(options);

        IdentificationColumns columns = options.Columns;

        if (columns.UsesPseudonyms && anonymiser is null)
        {
            throw new ArgumentException(
                $"{PersonIdentification.RandomPersonId} needs an anonymiser.",
                nameof(anonymiser));
        }

        bool legacy = options.Dialect == CsvDialect.Legacy;
        char separator = legacy ? DatasetExportOptions.LegacySeparator : Rfc4180Separator;
        CultureInfo culture = ResolveCulture(options);

        // leaveOpen: the caller supplied the stream and decides its lifetime. StreamWriter emits a
        // preamble only when the encoding has one, and Windows-1252's is zero-length, so Legacy gets
        // no BOM.
        using var writer = new StreamWriter(stream, ResolveEncoding(options), bufferSize: -1, leaveOpen: true)
        {
            NewLine = LineTerminator,
            AutoFlush = false,
        };

        var fields = new List<CsvField>(CountColumns(dataset, options));

        BuildHeaderRow(fields, dataset, columns, options.IncludeTimestamps);
        WriteRow(writer, fields, separator, legacy);

        foreach (ExportRow row in dataset.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            BuildDataRow(fields, dataset, row, columns, options, culture, legacy, anonymiser);
            WriteRow(writer, fields, separator, legacy);
        }

        writer.Flush();
    }

    private static void BuildHeaderRow(
        List<CsvField> fields,
        ExportDataset dataset,
        IdentificationColumns columns,
        bool includeTimestamps)
    {
        fields.Clear();

        // One derivation, shared with the grid and with the data rows below, so a header can never
        // end up describing a column the data rows do not write. Even in RandomPersonId the person
        // cell here is the ordinary quoted "PID": the Delphi's pseudonym branch is guarded by
        // rowNo > FixedRows - 1 (EPR.QA.Matrix.pas:466).
        foreach (string header in FixedColumns.HeadersFor(columns))
        {
            fields.Add(new CsvField(header));
        }

        foreach (ExportColumn column in dataset.Columns)
        {
            fields.Add(new CsvField(column.VarName));

            if (includeTimestamps)
            {
                fields.Add(new CsvField(column.VarName + DatasetExportOptions.TimestampColumnSuffix));
            }
        }
    }

    private static void BuildDataRow(
        List<CsvField> fields,
        ExportDataset dataset,
        ExportRow row,
        IdentificationColumns columns,
        DatasetExportOptions options,
        CultureInfo culture,
        bool legacy,
        IAnonymiser? anonymiser)
    {
        fields.Clear();

        // The same ordinal list the header row used, so the two cannot fall out of step.
        foreach (int ordinal in FixedColumns.VisibleOrdinals(columns))
        {
            if (ordinal == FixedColumns.PersonId && columns.UsesPseudonyms)
            {
                // The one unquoted field in the whole format: write(F, <integer>, ';').
                int pseudonym = anonymiser!.GetPseudonym(row.PersonId);

                fields.Add(new CsvField(pseudonym.ToString(CultureInfo.InvariantCulture), Quoted: false));

                continue;
            }

            fields.Add(new CsvField(ordinal switch
            {
                FixedColumns.PersonId => row.PersonId.ToString(CultureInfo.InvariantCulture),
                FixedColumns.DateOfBirth => FormatDateOfBirth(row.DateOfBirth, culture, legacy),
                FixedColumns.NationalId => row.NationalId ?? string.Empty,
                _ => row.FullName,
            }));
        }

        for (int columnIndex = 0; columnIndex < dataset.Columns.Count; columnIndex++)
        {
            ExportCell cell = columnIndex < row.Cells.Count ? row.Cells[columnIndex] : default;

            fields.Add(new CsvField(FormatCell(cell, culture)));

            if (!options.IncludeTimestamps)
            {
                continue;
            }

            if (cell.HasValue)
            {
                fields.Add(new CsvField(cell.Timestamp.ToString(
                    DatasetExportOptions.TimestampFormat,
                    CultureInfo.InvariantCulture)));
            }
            else
            {
                // write(F, EmptyStr, ';') - empty and unquoted, unlike the value field beside it,
                // which is a quoted empty string.
                fields.Add(new CsvField(string.Empty, Quoted: false));
            }
        }
    }

    /// <summary>Reproduces the datapoint half of <c>GetCellText</c>.</summary>
    private static string FormatCell(ExportCell cell, CultureInfo culture)
    {
        if (!cell.HasValue)
        {
            return string.Empty;
        }

        // EPR.QA.Matrix.pas:242-246 on the tarmscreening tip: the caption wins when it is set.
        // Added by 8486b3d09, "QuickStat skal kunne vise og eksportere tekstdata fra skjema".
        return string.IsNullOrEmpty(cell.Caption)
            ? DelphiNumberFormat.Format(cell.Value, culture)
            : cell.Caption;
    }

    private static string FormatDateOfBirth(DateTime? dateOfBirth, CultureInfo culture, bool legacy)
    {
        if (dateOfBirth is null)
        {
            return string.Empty;
        }

        // DateToStr, i.e. the locale short date - "14.08.2019" on nb-NO. RFC 4180 is ISO throughout.
        return legacy
            ? dateOfBirth.Value.ToString("d", culture)
            : dateOfBirth.Value.ToString(Rfc4180DateFormat, CultureInfo.InvariantCulture);
    }

    private static void WriteRow(TextWriter writer, List<CsvField> fields, char separator, bool legacy)
    {
        for (int index = 0; index < fields.Count; index++)
        {
            CsvField field = fields[index];

            if (legacy)
            {
                if (field.Quoted)
                {
                    WriteQuoted(writer, field.Text);
                }
                else
                {
                    writer.Write(field.Text);
                }

                // After every field, including the last one.
                writer.Write(separator);
            }
            else
            {
                if (index > 0)
                {
                    writer.Write(separator);
                }

                if (NeedsQuoting(field.Text, separator))
                {
                    WriteQuoted(writer, field.Text);
                }
                else
                {
                    writer.Write(field.Text);
                }
            }
        }

        writer.Write(LineTerminator);
    }

    /// <summary><c>AnsiQuotedStr(s, '"')</c>: wrap in quotes, double the embedded ones.</summary>
    private static void WriteQuoted(TextWriter writer, string text)
    {
        writer.Write('"');

        foreach (char character in text)
        {
            if (character == '"')
            {
                writer.Write('"');
            }

            writer.Write(character);
        }

        writer.Write('"');
    }

    private static bool NeedsQuoting(string text, char separator) =>
        text.Contains(separator) ||
        text.Contains('"', StringComparison.Ordinal) ||
        text.Contains('\r', StringComparison.Ordinal) ||
        text.Contains('\n', StringComparison.Ordinal);

    /// <param name="Text">The field's text.</param>
    /// <param name="Quoted">
    /// Whether the legacy dialect quotes it. False only for the pseudonym and for the empty
    /// timestamp of a cell with no datapoint - the two places the Delphi bypassed
    /// <c>AnsiQuotedStr</c>. <see cref="CsvDialect.Rfc4180"/> ignores it and quotes on need.
    /// </param>
    private readonly record struct CsvField(string Text, bool Quoted = true);
}
