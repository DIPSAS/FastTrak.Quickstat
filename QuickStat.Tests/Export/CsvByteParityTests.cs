using System.Globalization;
using System.Text;
using QuickStat.Domain.Anonymisation;
using QuickStat.Export;
using Xunit;

namespace QuickStat.Tests.Export;

/// <summary>
/// Byte-for-byte parity of <see cref="CsvDialect.Legacy"/> with the Delphi build.
/// </summary>
/// <remarks>
/// PORT-PLAN.md R4: drift in the encoding, the decimal separator or the trailing separator breaks
/// downstream R/SPSS/Stata scripts. These assert <em>bytes</em> rather than strings on purpose - a
/// string comparison cannot see a byte-order mark, cannot tell CP1252 from UTF-8 for <c>æøå</c>, and
/// cannot tell CRLF from LF.
/// </remarks>
public class CsvByteParityTests
{
    private static DatasetExportOptions Legacy(
        PersonIdentification identification,
        bool includeTimestamps = false) =>
        new()
        {
            Identification = identification,
            IncludeTimestamps = includeTimestamps,
            Culture = ExportFixtures.Norwegian,
        };

    [Fact]
    public void PersonIdOnlyProducesExactlyTheseBytes()
    {
        // Docs/Port/04-matrix-export.md §5.2, second worked example:
        //     "PID";"AGE";"YOB";
        //     "8";"97";"1922";
        // Written out byte by byte so nothing about the expectation is derived from the code under
        // test - not the encoding, not the quoting and not the line ending.
        string expected = string.Join(
            '-',
            "22-50-49-44-22-3B",                   // "PID";
            "22-41-47-45-22-3B",                   // "AGE";
            "22-59-4F-42-22-3B",                   // "YOB";
            "0D-0A",                               // CRLF
            "22-38-22-3B",                         // "8";
            "22-39-37-22-3B",                      // "97";
            "22-31-39-32-32-22-3B",                // "1922";
            "0D-0A");                              // CRLF

        byte[] actual = ExportFixtures.WriteCsv(
            ExportFixtures.WorkedExample(),
            Legacy(PersonIdentification.PersonIdOnly));

        Assert.Equal(expected, ExportFixtures.Hex(actual));
    }

    [Fact]
    public void NorwegianTextIsWindows1252AndNumbersUseTheDecimalComma()
    {
        // "PID";"Født";"Fødselsnummer";"Navn";"ÆØÅ";
        // "7";"";"";"Sætre, Bjørn";"3,5";
        string expected = string.Join(
            '-',
            "22-50-49-44-22-3B",                                     // "PID";
            "22-46-F8-64-74-22-3B",                                  // "Født";   ø = 0xF8
            "22-46-F8-64-73-65-6C-73-6E-75-6D-6D-65-72-22-3B",       // "Fødselsnummer";
            "22-4E-61-76-6E-22-3B",                                  // "Navn";
            "22-C6-D8-C5-22-3B",                                     // "ÆØÅ";    Æ Ø Å
            "0D-0A",
            "22-37-22-3B",                                           // "7";
            "22-22-3B",                                              // "";       no date of birth
            "22-22-3B",                                              // "";       no national id
            "22-53-E6-74-72-65-2C-20-42-6A-F8-72-6E-22-3B",          // "Sætre, Bjørn";  æ ø
            "22-33-2C-35-22-3B",                                     // "3,5";
            "0D-0A");

        byte[] actual = ExportFixtures.WriteCsv(
            ExportFixtures.NorwegianText(),
            Legacy(PersonIdentification.Full));

        Assert.Equal(expected, ExportFixtures.Hex(actual));
    }

    [Fact]
    public void ThereIsNoByteOrderMark()
    {
        byte[] actual = ExportFixtures.WriteCsv(
            ExportFixtures.NorwegianText(),
            Legacy(PersonIdentification.Full));

        Assert.Empty(ExportFixtures.Cp1252.GetPreamble());
        Assert.Equal((byte)'"', actual[0]);
        Assert.NotEqual(0xEF, actual[0]);   // UTF-8
        Assert.NotEqual(0xFF, actual[0]);   // UTF-16 LE
        Assert.NotEqual(0xFE, actual[0]);   // UTF-16 BE
    }

    [Fact]
    public void EveryLineEndsWithASeparatorThenCrlf()
    {
        byte[] actual = ExportFixtures.WriteCsv(
            ExportFixtures.WorkedExample(),
            Legacy(PersonIdentification.Full, includeTimestamps: true));

        // Three bytes, in this order, at the end of every line: ';' CR LF.
        for (int index = 0; index < actual.Length; index++)
        {
            if (actual[index] != (byte)'\n')
            {
                continue;
            }

            Assert.Equal((byte)'\r', actual[index - 1]);
            Assert.Equal((byte)';', actual[index - 2]);
        }

        Assert.Equal((byte)'\n', actual[^1]);
    }

    [Fact]
    public void QuotesInsideAFieldAreDoubled()
    {
        var dataset = new ExportDataset
        {
            Columns = [new ExportColumn { VarName = "A\"B", Title = "x" }],
            Rows =
            [
                new ExportRow
                {
                    PersonId = 1,
                    FullName = "O'Hara, \"Bill\"",
                    NationalId = "1;2",
                    DateOfBirth = null,
                    Cells = [default],
                },
            ],
        };

        string text = ExportFixtures.Cp1252.GetString(
            ExportFixtures.WriteCsv(dataset, Legacy(PersonIdentification.Full)));

        Assert.Equal(
            "\"PID\";\"Født\";\"Fødselsnummer\";\"Navn\";\"A\"\"B\";\r\n" +
            "\"1\";\"\";\"1;2\";\"O'Hara, \"\"Bill\"\"\";\"\";\r\n",
            text);
    }

    [Fact]
    public void HeaderCarriesTheVariableNameNotTheTitle()
    {
        string text = ExportFixtures.Cp1252.GetString(
            ExportFixtures.WriteCsv(
                ExportFixtures.WorkedExample(),
                Legacy(PersonIdentification.PersonIdOnly)));

        Assert.StartsWith("\"PID\";\"AGE\";\"YOB\";\r\n", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Alder", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Fødselsår", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FullIdentificationMatchesTheWorkedExample()
    {
        string text = ExportFixtures.Cp1252.GetString(
            ExportFixtures.WriteCsv(ExportFixtures.WorkedExample(), Legacy(PersonIdentification.Full)));

        Assert.Equal(
            "\"PID\";\"Født\";\"Fødselsnummer\";\"Navn\";\"AGE\";\"YOB\";\r\n" +
            "\"8\";\"12.03.1922\";\"12032212345\";\"Hansen, Ola\";\"97\";\"1922\";\r\n",
            text);
    }

    [Fact]
    public void TimestampColumnsMatchTheWorkedExample()
    {
        // Docs/Port/04-matrix-export.md §5.2, fourth worked example. The missing cell writes a
        // quoted empty value and then an *unquoted* empty timestamp - ";;" rather than ";\"\";".
        string text = ExportFixtures.Cp1252.GetString(
            ExportFixtures.WriteCsv(
                ExportFixtures.WorkedExample(secondValueMissing: true),
                Legacy(PersonIdentification.PersonIdOnly, includeTimestamps: true)));

        Assert.Equal(
            "\"PID\";\"AGE\";\"AGE.DATE\";\"YOB\";\"YOB.DATE\";\r\n" +
            "\"8\";\"97\";\"2019-08-14\";\"\";;\r\n",
            text);
    }

    [Fact]
    public void TimestampsAreIsoEvenThoughTheDateOfBirthIsNot()
    {
        string text = ExportFixtures.Cp1252.GetString(
            ExportFixtures.WriteCsv(
                ExportFixtures.WorkedExample(),
                Legacy(PersonIdentification.Full, includeTimestamps: true)));

        Assert.Contains("\"12.03.1922\"", text, StringComparison.Ordinal);
        Assert.Contains("\"2019-08-14\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CaptionTextReplacesTheNumberAsItDoesUpstream()
    {
        // EPR.QA.Matrix.pas:242-246 on origin/tarmscreening/develop, added by 8486b3d09
        // "#489525: QuickStat skal kunne vise og eksportere tekstdata fra skjema". The copy of the
        // library in this repository is develop_old, which predates it and writes the number.
        var dataset = new ExportDataset
        {
            Columns = [new ExportColumn { VarName = "FORM.NOTE", Title = "Notat" }],
            Rows =
            [
                new ExportRow
                {
                    PersonId = 5,
                    Cells = [new ExportCell { HasValue = true, Value = 42, Caption = "Ingen funn" }],
                },
            ],
        };

        string text = ExportFixtures.Cp1252.GetString(
            ExportFixtures.WriteCsv(dataset, Legacy(PersonIdentification.PersonIdOnly)));

        Assert.Equal("\"PID\";\"FORM.NOTE\";\r\n\"5\";\"Ingen funn\";\r\n", text);
    }

    [Fact]
    public void ThePseudonymIsTheOneUnquotedField()
    {
        var anonymiser = new MatrixAnonymiser();
        anonymiser.Reset(1);

        byte[] bytes = ExportFixtures.WriteCsv(
            ExportFixtures.WorkedExample(),
            Legacy(PersonIdentification.RandomPersonId),
            anonymiser);

        string text = ExportFixtures.Cp1252.GetString(bytes);
        int pseudonym = anonymiser.GetPseudonym(8);

        // The header cell is still the quoted "PID": the Delphi's pseudonym branch is guarded by
        // rowNo > FixedRows - 1, so it never applies to row zero.
        Assert.Equal(
            FormattableString.Invariant($"\"PID\";\"AGE\";\"YOB\";\r\n{pseudonym};\"97\";\"1922\";\r\n"),
            text);
    }

    [Fact]
    public void AnEmptyPopulationWritesTheHeaderAndNoPhantomRow()
    {
        // The Delphi's RowCount was 1 + max(DataRows, 1), so a cohort of nobody still produced a
        // row of "nil" (Docs/Port/04-matrix-export.md §5.2). Not reproduced: R-10 in that document
        // treats it as a defect, and it is the only place the port departs from the byte format.
        var dataset = new ExportDataset
        {
            Columns = [new ExportColumn { VarName = "AGE", Title = "Alder" }],
            Rows = [],
        };

        string text = ExportFixtures.Cp1252.GetString(
            ExportFixtures.WriteCsv(dataset, Legacy(PersonIdentification.PersonIdOnly)));

        Assert.Equal("\"PID\";\"AGE\";\r\n", text);
    }

    [Fact]
    public void Rfc4180IsUtf8WithABomAndNoTrailingSeparator()
    {
        var options = new DatasetExportOptions
        {
            Identification = PersonIdentification.Full,
            Dialect = CsvDialect.Rfc4180,
        };

        byte[] bytes = ExportFixtures.WriteCsv(ExportFixtures.NorwegianText(), options);

        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        Assert.Equal(
            "PID,Født,Fødselsnummer,Navn,ÆØÅ\r\n7,,,\"Sætre, Bjørn\",3.5\r\n",
            new UTF8Encoding(false).GetString(bytes[3..]));
    }

    [Fact]
    public void TheDefaultDialectIsLegacyAndTheDefaultModeIsPersonIdOnly()
    {
        var options = new DatasetExportOptions { Identification = default };

        Assert.Equal(CsvDialect.Legacy, options.Dialect);
        Assert.Equal(ExportFormat.Csv, options.Format);
        Assert.False(options.WriteKeyFile);
        Assert.False(options.IncludeTimestamps);

        // PersonIdOnly is the checked radio button in the Delphi, but it is enum value 1, so
        // default(PersonIdentification) is Full. Anyone building options must say which they mean.
        Assert.Equal(PersonIdentification.Full, options.Identification);
        Assert.Equal(PersonIdentification.PersonIdOnly, new IdentificationPolicy().Mode);
    }

    [Fact]
    public void TheParityConstantsAreWhatTheSpecificationSays()
    {
        Assert.Equal(';', DatasetExportOptions.LegacySeparator);
        Assert.Equal(1252, DatasetExportOptions.LegacyCodePage);
        Assert.Equal(".DATE", DatasetExportOptions.TimestampColumnSuffix);
        Assert.Equal("yyyy-MM-dd", DatasetExportOptions.TimestampFormat);
        Assert.Equal(".mapping.txt", DatasetExportOptions.KeyFileExtension);
        Assert.Equal("windows-1252", ExportFixtures.Cp1252.WebName);
        Assert.Equal(CultureInfo.CurrentCulture, CsvMatrixWriter.ResolveCulture(
            new DatasetExportOptions { Identification = PersonIdentification.Full }));
    }
}
