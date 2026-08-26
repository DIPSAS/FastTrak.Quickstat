using System.Globalization;
using System.IO;
using System.Text;
using QuickStat.Export;

namespace QuickStat.Tests.Export;

/// <summary>
/// Datasets and byte helpers shared by the export tests.
/// </summary>
/// <remarks>
/// The fixtures reproduce the worked examples in <c>Docs/Port/04-matrix-export.md</c> §5.2, which
/// were derived from <c>TPersonGridData.SaveToFile</c>. Building an <see cref="ExportDataset"/> by
/// hand rather than through a <c>PersonMatrix</c> is deliberate: the writers are pure, and the byte
/// parity tests that PORT-PLAN.md R4 and R6 require must not wait for step 2.5.
/// </remarks>
internal static class ExportFixtures
{
    /// <summary>Norwegian Bokmål, the locale the Delphi build formats against in the field.</summary>
    internal static CultureInfo Norwegian => CultureInfo.GetCultureInfo("nb-NO");

    /// <summary>Windows-1252, resolved through the writer so the provider is registered.</summary>
    internal static Encoding Cp1252 => CsvMatrixWriter.LegacyEncoding;

    /// <summary>
    /// The §5.2 worked example: person 8, born 1922-03-12, fnr 12032212345, "Hansen, Ola",
    /// AGE 97 and YOB 1922, both observed 2019-08-14.
    /// </summary>
    /// <param name="secondValueMissing">
    /// Drop the <c>YOB</c> datapoint, which is the variant §5.2 uses to show what a cell with no
    /// value writes when timestamps are on.
    /// </param>
    /// <returns>The dataset.</returns>
    internal static ExportDataset WorkedExample(bool secondValueMissing = false)
    {
        var observed = new DateTime(2019, 8, 14, 9, 30, 0, DateTimeKind.Unspecified);

        return new ExportDataset
        {
            Columns =
            [
                new ExportColumn { VarName = "AGE", Title = "Alder" },
                new ExportColumn { VarName = "YOB", Title = "Fødselsår" },
            ],
            Rows =
            [
                new ExportRow
                {
                    PersonId = 8,
                    DateOfBirth = new DateTime(1922, 3, 12, 0, 0, 0, DateTimeKind.Unspecified),
                    NationalId = "12032212345",
                    FullName = "Hansen, Ola",
                    Cells =
                    [
                        new ExportCell { HasValue = true, Value = 97, Timestamp = observed },
                        secondValueMissing
                            ? default
                            : new ExportCell { HasValue = true, Value = 1922, Timestamp = observed },
                    ],
                },
            ],
        };
    }

    /// <summary>A dataset whose every text field exercises Windows-1252 and the decimal comma.</summary>
    /// <returns>The dataset.</returns>
    internal static ExportDataset NorwegianText() =>
        new()
        {
            Columns = [new ExportColumn { VarName = "ÆØÅ", Title = "Vekt" }],
            Rows =
            [
                new ExportRow
                {
                    PersonId = 7,
                    DateOfBirth = null,
                    NationalId = null,
                    FullName = "Sætre, Bjørn",
                    Cells = [new ExportCell { HasValue = true, Value = 3.5 }],
                },
            ],
        };

    /// <summary>A cohort large enough to make pseudonyms three digits wide.</summary>
    /// <param name="personCount">Number of people. Person ids are 1001, 1002, …</param>
    /// <returns>The dataset.</returns>
    internal static ExportDataset Cohort(int personCount)
    {
        var rows = new List<ExportRow>(personCount);

        for (int index = 0; index < personCount; index++)
        {
            rows.Add(new ExportRow
            {
                PersonId = 1001 + index,
                DateOfBirth = new DateTime(1950, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).AddDays(index),
                NationalId = (10101000000L + index).ToString(CultureInfo.InvariantCulture),
                FullName = $"Etternavn{index}, Fornavn{index}",
                Cells = [new ExportCell { HasValue = true, Value = index, Timestamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Unspecified) }],
            });
        }

        return new ExportDataset
        {
            Columns = [new ExportColumn { VarName = "VAL", Title = "Verdi" }],
            Rows = rows,
        };
    }

    /// <summary>Writes a dataset and returns the raw bytes.</summary>
    /// <param name="dataset">The dataset.</param>
    /// <param name="options">The options.</param>
    /// <param name="anonymiser">Optional anonymiser.</param>
    /// <returns>Exactly what would have reached the file.</returns>
    internal static byte[] WriteCsv(
        ExportDataset dataset,
        DatasetExportOptions options,
        QuickStat.Domain.Anonymisation.IAnonymiser? anonymiser = null)
    {
        using var stream = new MemoryStream();
        CsvMatrixWriter.Write(dataset, stream, options, anonymiser);
        return stream.ToArray();
    }

    /// <summary>Renders bytes as <c>50-49-44</c>, so an assertion failure is readable.</summary>
    /// <param name="bytes">The bytes.</param>
    /// <returns>Uppercase hyphen-separated hex.</returns>
    internal static string Hex(byte[] bytes) => Convert.ToHexString(bytes).Length == 0
        ? string.Empty
        : string.Join('-', Convert.ToHexString(bytes).Chunk(2).Select(pair => new string(pair)));

    /// <summary>Parses <c>50-49-44</c> back into bytes, for expected values written by hand.</summary>
    /// <param name="hex">Hyphen- or space-separated hex, comments stripped by the caller.</param>
    /// <returns>The bytes.</returns>
    internal static byte[] FromHex(string hex) =>
        Convert.FromHexString(hex.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal));
}
