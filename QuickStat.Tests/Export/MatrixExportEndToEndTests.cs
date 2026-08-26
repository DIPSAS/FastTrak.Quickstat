using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using QuickStat.Collectors;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.DataPoints;
using QuickStat.Domain.Matrix;
using QuickStat.Domain.Patients;
using QuickStat.Export;
using Xunit;

namespace QuickStat.Tests.Export;

/// <summary>
/// The export driven by a real <see cref="PersonMatrix"/> rather than a hand-built dataset.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ExportDataset.FromMatrix"/> was written against a stubbed matrix, so this is where a
/// disagreement between step 2.5's model and step 2.6's projection would actually surface. Every
/// matrix here is built the way the application builds one: prepare the population, accumulate a
/// <see cref="VariableNameSet"/>, add the columns, feed rows through
/// <see cref="ICollectorResultSink"/>, lock.
/// </para>
/// <para>
/// The anchor test is <see cref="AMatrixProducesTheSameBytesAsTheHandBuiltFixture"/>: if the
/// projection and the fixtures ever disagree, every other byte-parity assertion in this suite is
/// worthless, so that one is asserted first and by byte comparison.
/// </para>
/// </remarks>
public class MatrixExportEndToEndTests
{
    private static readonly DateTime Observed = new(2019, 8, 14, 9, 30, 0, DateTimeKind.Unspecified);

    private static CollectorResultRow Row(
        int personId,
        string varName,
        double value,
        int rowId,
        string? caption = null,
        DateTime? timestamp = null) =>
        new()
        {
            PersonId = personId,
            VarName = varName,
            Value = value,
            Timestamp = timestamp ?? Observed,
            RowId = rowId,
            Caption = caption,
        };

    private static Patient OlaHansen() => new()
    {
        PersonId = 8,
        DateOfBirth = new DateTime(1922, 3, 12, 0, 0, 0, DateTimeKind.Unspecified),
        FirstName = "Ola",
        LastName = "Hansen",
        NationalId = "12032212345",
    };

    /// <summary>Builds the §5.2 worked example through the real matrix.</summary>
    private static PersonMatrix WorkedExampleMatrix(
        IDataPointFactory? factory = null,
        bool secondValueMissing = false,
        ColumnOrder columnOrder = ColumnOrder.FirstSeen)
    {
        PersonMatrix matrix = new(factory ?? new DataPointFactory()) { ColumnOrder = columnOrder };

        matrix.PreparePopulation([OlaHansen()]);

        VariableNameSet names = matrix.CreateVariableNameSet();
        names.Add("AGE");
        names.Add("YOB");
        matrix.AddColumns(names);

        matrix.Add("AGE", Row(8, "AGE", 97, 1));

        if (!secondValueMissing)
        {
            matrix.Add("YOB", Row(8, "YOB", 1922, 2));
        }

        matrix.Lock();

        return matrix;
    }

    private static DatasetExportOptions Legacy(
        PersonIdentification identification,
        bool includeTimestamps = false) =>
        new()
        {
            Identification = identification,
            IncludeTimestamps = includeTimestamps,
            Culture = ExportFixtures.Norwegian,
        };

    private static byte[] WriteCsv(PersonMatrix matrix, DatasetExportOptions options, IAnonymiser? anonymiser = null)
    {
        using MemoryStream stream = new();
        CsvMatrixWriter.Write(ExportDataset.FromMatrix(matrix), stream, options, anonymiser);

        return stream.ToArray();
    }

    [Theory]
    [InlineData(PersonIdentification.Full, false)]
    [InlineData(PersonIdentification.Full, true)]
    [InlineData(PersonIdentification.PersonIdOnly, false)]
    [InlineData(PersonIdentification.PersonIdOnly, true)]
    public void AMatrixProducesTheSameBytesAsTheHandBuiltFixture(
        PersonIdentification identification,
        bool includeTimestamps)
    {
        DatasetExportOptions options = Legacy(identification, includeTimestamps);

        byte[] fromMatrix = WriteCsv(WorkedExampleMatrix(), options);
        byte[] fromFixture = ExportFixtures.WriteCsv(ExportFixtures.WorkedExample(), options);

        Assert.Equal(ExportFixtures.Hex(fromFixture), ExportFixtures.Hex(fromMatrix));
    }

    [Fact]
    public void AMissingCellIsTheSameThroughTheMatrixAsThroughTheFixture()
    {
        DatasetExportOptions options = Legacy(PersonIdentification.PersonIdOnly, includeTimestamps: true);

        byte[] fromMatrix = WriteCsv(WorkedExampleMatrix(secondValueMissing: true), options);

        Assert.Equal(
            "\"PID\";\"AGE\";\"AGE.DATE\";\"YOB\";\"YOB.DATE\";\r\n\"8\";\"97\";\"2019-08-14\";\"\";;\r\n",
            ExportFixtures.Cp1252.GetString(fromMatrix));
    }

    [Fact]
    public void ColumnsKeepTheOrderTheCollectorsProducedThem()
    {
        // ColumnOrder.FirstSeen is value zero and is what ships. Alphabetical would reorder every
        // existing export (PORT-PLAN.md §8.5), so the projection must not quietly sort.
        PersonMatrix matrix = new(new DataPointFactory());
        matrix.PreparePopulation([OlaHansen()]);

        VariableNameSet names = matrix.CreateVariableNameSet();
        names.Add("ZULU");
        names.Add("ALPHA");
        names.Add("MIKE");
        matrix.AddColumns(names);
        matrix.Lock();

        ExportDataset dataset = ExportDataset.FromMatrix(matrix);

        Assert.Equal(new[] { "ZULU", "ALPHA", "MIKE" }, dataset.Columns.Select(column => column.VarName));
        Assert.StartsWith(
            "\"PID\";\"ZULU\";\"ALPHA\";\"MIKE\";\r\n",
            ExportFixtures.Cp1252.GetString(WriteCsv(matrix, Legacy(PersonIdentification.PersonIdOnly))),
            StringComparison.Ordinal);
    }

    [Fact]
    public void TheAlphabeticalPolicyReachesTheFileToo()
    {
        PersonMatrix matrix = new(new DataPointFactory()) { ColumnOrder = ColumnOrder.Alphabetical };
        matrix.PreparePopulation([OlaHansen()]);

        VariableNameSet names = matrix.CreateVariableNameSet();
        names.Add("ZULU");
        names.Add("ALPHA");
        names.Add("MIKE");
        matrix.AddColumns(names);
        matrix.Lock();

        Assert.StartsWith(
            "\"PID\";\"ALPHA\";\"MIKE\";\"ZULU\";\r\n",
            ExportFixtures.Cp1252.GetString(WriteCsv(matrix, Legacy(PersonIdentification.PersonIdOnly))),
            StringComparison.Ordinal);
    }

    [Fact]
    public void TwoCollectorsEmittingTheSameVariableStillProduceOneColumn()
    {
        // The Delphi produced two identical columns here, because ContainsVariable existed and was
        // never called. The de-duplication is 2.5's; this checks it survives the projection.
        PersonMatrix matrix = new(new DataPointFactory());
        matrix.PreparePopulation([OlaHansen()]);

        VariableNameSet first = matrix.CreateVariableNameSet();
        first.Add("AGE");
        matrix.AddColumns(first);

        VariableNameSet second = matrix.CreateVariableNameSet();
        second.Add("AGE");
        second.Add("YOB");
        matrix.AddColumns(second);

        matrix.Add("AGE", Row(8, "AGE", 97, 1));
        matrix.Lock();

        Assert.Equal(2, ExportDataset.FromMatrix(matrix).Columns.Count);
        Assert.StartsWith(
            "\"PID\";\"AGE\";\"YOB\";\r\n",
            ExportFixtures.Cp1252.GetString(WriteCsv(matrix, Legacy(PersonIdentification.PersonIdOnly))),
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PersonIdentification.Full)]
    [InlineData(PersonIdentification.PersonIdOnly)]
    [InlineData(PersonIdentification.RandomPersonId)]
    public void TheIdentityFieldsAreTheOnesFixedColumnsSaysAndTheyReadAsTheGridRendersThem(
        PersonIdentification identification)
    {
        // Both halves of the anonymity fix in one assertion: the file emits exactly
        // FixedColumns.VisibleOrdinals, and each of those fields carries the same text
        // PersonMatrix.GetFixedCell gives the grid. If the two ever diverge, the screen and the file
        // are describing different people.
        PersonMatrix matrix = WorkedExampleMatrix();
        IdentificationColumns columns = IdentificationColumns.For(identification);
        IReadOnlyList<int> ordinals = FixedColumns.VisibleOrdinals(columns);

        MatrixAnonymiser anonymiser = new();
        anonymiser.Reset(matrix.Rows.Count);

        string[] lines = ExportFixtures.Cp1252
            .GetString(WriteCsv(matrix, Legacy(identification), anonymiser))
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        string[] headerFields = lines[0].Split(';');
        string[] dataFields = lines[1].Split(';');

        Assert.Equal(
            FixedColumns.HeadersFor(columns),
            headerFields.Take(ordinals.Count).Select(field => field.Trim('"')).ToArray());

        for (int index = 0; index < ordinals.Count; index++)
        {
            int ordinal = ordinals[index];
            string written = dataFields[index].Trim('"');

            if (ordinal == FixedColumns.PersonId && columns.UsesPseudonyms)
            {
                // The one field that must NOT match the grid: it is a pseudonym.
                Assert.NotEqual(matrix.GetFixedCell(0, ordinal, ExportFixtures.Norwegian).Text, written);
                Assert.Equal(anonymiser.GetPseudonym(8).ToString(CultureInfo.InvariantCulture), written);

                continue;
            }

            Assert.Equal(matrix.GetFixedCell(0, ordinal, ExportFixtures.Norwegian).Text, written);
        }
    }

    [Fact]
    public void ANationalIdFilledInAfterLoadingReachesAFullExport()
    {
        // The AddNationalIds path: MatrixRow.NationalId is settable because the Delphi fills it after
        // the row exists, and Phase 4 restores the call this repository has commented out.
        PersonMatrix matrix = new(new DataPointFactory());
        matrix.PreparePopulation([new Patient { PersonId = 8, FirstName = "Ola", LastName = "Hansen" }]);

        VariableNameSet names = matrix.CreateVariableNameSet();
        names.Add("AGE");
        matrix.AddColumns(names);
        matrix.Lock();

        Assert.DoesNotContain(
            "12032212345",
            ExportFixtures.Cp1252.GetString(WriteCsv(matrix, Legacy(PersonIdentification.Full))),
            StringComparison.Ordinal);

        matrix.Rows[0].NationalId = "12032212345";

        Assert.Contains(
            "\"12032212345\"",
            ExportFixtures.Cp1252.GetString(WriteCsv(matrix, Legacy(PersonIdentification.Full))),
            StringComparison.Ordinal);
    }

    [Fact]
    public void RowsArriveInPersonIdOrderWhateverOrderTheyWereLoadedIn()
    {
        PersonMatrix matrix = new(new DataPointFactory());

        matrix.PreparePopulation(
        [
            new Patient { PersonId = 30, LastName = "C" },
            new Patient { PersonId = 10, LastName = "A" },
            new Patient { PersonId = 20, LastName = "B" },
        ]);

        VariableNameSet names = matrix.CreateVariableNameSet();
        names.Add("AGE");
        matrix.AddColumns(names);
        matrix.Lock();

        ExportDataset dataset = ExportDataset.FromMatrix(matrix);

        Assert.Equal(new[] { 10, 20, 30 }, dataset.Rows.Select(row => row.PersonId));
    }

    [Fact]
    public void ARowForSomebodyOutsideTheCohortNeverReachesTheFile()
    {
        // PidBinding.None collectors scan the whole database and the cohort filter is PersonMatrix.Add
        // returning false. Nothing of the rejected person may appear.
        PersonMatrix matrix = new(new DataPointFactory());
        matrix.PreparePopulation([OlaHansen()]);

        VariableNameSet names = matrix.CreateVariableNameSet();
        names.Add("AGE");
        matrix.AddColumns(names);

        Assert.True(matrix.Add("AGE", Row(8, "AGE", 97, 1)));
        Assert.False(matrix.Add("AGE", Row(999, "AGE", 55, 2)));

        matrix.Lock();

        string text = ExportFixtures.Cp1252.GetString(WriteCsv(matrix, Legacy(PersonIdentification.PersonIdOnly)));

        Assert.Equal("\"PID\";\"AGE\";\r\n\"8\";\"97\";\r\n", text);
    }

    [Fact]
    public void AnUnlockedMatrixIsRefusedRatherThanExportedAsNotReady()
    {
        PersonMatrix matrix = new(new DataPointFactory());
        matrix.PreparePopulation([OlaHansen()]);

        Assert.False(matrix.IsLocked);
        Assert.Throws<InvalidOperationException>(() => ExportDataset.FromMatrix(matrix));
    }

    [Fact]
    public void TheExportWritesTheWholeCaptionWhileTheGridTruncatesIt()
    {
        // The clearest place display and export are meant to differ: TDataPoint.CellText does
        // Copy(fCaption, 1, 6) and TPersonGridData.GetCellText does not.
        const string caption = "Ingen funn ved undersøkinga";

        PersonMatrix matrix = new(new DataPointFactory());
        matrix.PreparePopulation([OlaHansen()]);

        VariableNameSet names = matrix.CreateVariableNameSet();
        names.Add("FORM.NOTE");
        matrix.AddColumns(names);
        matrix.Add("FORM.NOTE", Row(8, "FORM.NOTE", 42, 1, caption));
        matrix.Lock();

        Assert.Equal(
            caption[..DataPointRule.DefaultCaptionLength],
            matrix.GetCell(0, 0).Text);

        Assert.Equal(
            $"\"PID\";\"FORM.NOTE\";\r\n\"8\";\"{caption}\";\r\n",
            ExportFixtures.Cp1252.GetString(WriteCsv(matrix, Legacy(PersonIdentification.PersonIdOnly))));
    }

    [Fact]
    public void ADisplayOnlyFormatOverrideDoesNotReachTheFile()
    {
        // BMI renders as %.1f on screen and as %g in the file. That surprises users and it is
        // correct for analysis, so it is pinned rather than fixed.
        //
        // The culture scope is load-bearing: BodyMassIndex.FormatValue formats against
        // CultureInfo.CurrentCulture, so the *display* half of this assertion is "23,5" on a
        // Norwegian machine and "23.5" on an English one. The export half is culture-pinned through
        // the options and would be right either way.
        using IDisposable culture = ExportFixtures.UseNorwegianCulture();

        PersonMatrix matrix = new(new DataPointFactory());
        matrix.PreparePopulation([OlaHansen()]);

        VariableNameSet names = matrix.CreateVariableNameSet();
        names.Add(StandardDataPointRules.BodyMassIndexVarName);
        matrix.AddColumns(names);
        matrix.Add(StandardDataPointRules.BodyMassIndexVarName,
            Row(8, StandardDataPointRules.BodyMassIndexVarName, 23.456, 1));
        matrix.Lock();

        Assert.Equal("23,5", matrix.GetCell(0, 0).Text);
        Assert.Contains(
            "\"23,456\"",
            ExportFixtures.Cp1252.GetString(WriteCsv(matrix, Legacy(PersonIdentification.PersonIdOnly))),
            StringComparison.Ordinal);
    }

    [Fact]
    public void ColoursAreNotCollectedForACsv()
    {
        PersonMatrix matrix = WorkedExampleMatrix();

        ExportCell plain = ExportDataset.FromMatrix(matrix).Rows[0].Cells[0];
        ExportCell painted = ExportDataset.FromMatrix(matrix, includeAppearance: true).Rows[0].Cells[0];

        Assert.Null(plain.Background);
        Assert.Equal(RiskPalette.NoRisk, painted.Background);
    }

    [Fact]
    public void TheWorkbookCarriesTheThresholdLadderColours()
    {
        // NPU01566 is total cholesterol: > 8 is clGraveRisk, and 3 is below every band but zero, so
        // it lands on clNoRisk - white, which is deliberately left unfilled.
        PersonMatrix matrix = new(new DataPointFactory());

        matrix.PreparePopulation(
        [
            new Patient { PersonId = 1, LastName = "Høg" },
            new Patient { PersonId = 2, LastName = "Låg" },
        ]);

        VariableNameSet names = matrix.CreateVariableNameSet();
        names.Add(StandardDataPointRules.TotalCholesterolVarName);
        matrix.AddColumns(names);
        matrix.Add(StandardDataPointRules.TotalCholesterolVarName,
            Row(1, StandardDataPointRules.TotalCholesterolVarName, 9, 1));
        matrix.Add(StandardDataPointRules.TotalCholesterolVarName,
            Row(2, StandardDataPointRules.TotalCholesterolVarName, 3, 2));
        matrix.Lock();

        using XLWorkbook workbook = RoundTrip(matrix);
        IXLWorksheet sheet = workbook.Worksheet(XlsxMatrixWriter.WorksheetName);

        IXLCell grave = sheet.Cell(2, 2);
        IXLCell white = sheet.Cell(3, 2);

        Assert.Equal(RiskPalette.GraveRisk, ToRgb(grave.Style.Fill.BackgroundColor));
        Assert.Equal(XLFillPatternValues.Solid, grave.Style.Fill.PatternType);
        Assert.Equal(XLFillPatternValues.None, white.Style.Fill.PatternType);

        // The value is still a number, and still the raw one.
        Assert.Equal(XLDataType.Number, grave.DataType);
        Assert.Equal(9, grave.GetDouble());
    }

    [Fact]
    public void TheWorkbookCarriesTheFontColourAndTheAlignment()
    {
        Rgb ink = new(0, 0, 128);

        DataPointFactory factory = new(new Dictionary<string, DataPointRule>(StringComparer.Ordinal)
        {
            ["INK"] = new DataPointRule { FontColor = _ => ink },
        });

        PersonMatrix matrix = new(factory);
        matrix.PreparePopulation([OlaHansen()]);

        VariableNameSet names = matrix.CreateVariableNameSet();
        names.Add("INK");
        names.Add("TEXT");
        matrix.AddColumns(names);
        matrix.Add("INK", Row(8, "INK", 5, 1));
        matrix.Add("TEXT", Row(8, "TEXT", 0, 2, "Ja"));
        matrix.Lock();

        using XLWorkbook workbook = RoundTrip(matrix);
        IXLWorksheet sheet = workbook.Worksheet(XlsxMatrixWriter.WorksheetName);

        Assert.Equal(ink, ToRgb(sheet.Cell(2, 2).Style.Font.FontColor));

        // A number is right-aligned and a captioned cell is left-aligned, exactly as the grid draws
        // them - MatrixCell.AlignLeft is true when a caption is present.
        Assert.Equal(XLAlignmentHorizontalValues.Right, sheet.Cell(2, 2).Style.Alignment.Horizontal);
        Assert.Equal(XLAlignmentHorizontalValues.Left, sheet.Cell(2, 3).Style.Alignment.Horizontal);
        Assert.Equal("Ja", sheet.Cell(2, 3).GetString());
    }

    [Fact]
    public void AWorkbookOfAnAnonymousMatrixLeaksNothing()
    {
        PersonMatrix matrix = WorkedExampleMatrix();
        MatrixAnonymiser anonymiser = new();
        anonymiser.Reset(matrix.Rows.Count);

        using MemoryStream stream = new();
        XlsxMatrixWriter.Write(
            ExportDataset.FromMatrix(matrix, includeAppearance: true),
            stream,
            new DatasetExportOptions
            {
                Identification = PersonIdentification.RandomPersonId,
                Format = ExportFormat.Xlsx,
            },
            anonymiser);

        stream.Position = 0;

        using XLWorkbook workbook = new(stream);
        string everything = string.Concat(
            workbook.Worksheet(XlsxMatrixWriter.WorksheetName)
                .CellsUsed()
                .Select(cell => cell.GetFormattedString()));

        Assert.DoesNotContain("Hansen", everything, StringComparison.Ordinal);
        Assert.DoesNotContain("12032212345", everything, StringComparison.Ordinal);
        Assert.DoesNotContain("1922-03-12", everything, StringComparison.Ordinal);
        Assert.Contains(
            anonymiser.GetPseudonym(8).ToString(CultureInfo.InvariantCulture),
            everything,
            StringComparison.Ordinal);
    }

    private static XLWorkbook RoundTrip(PersonMatrix matrix)
    {
        using MemoryStream stream = new();

        XlsxMatrixWriter.Write(
            ExportDataset.FromMatrix(matrix, includeAppearance: true),
            stream,
            new DatasetExportOptions
            {
                Identification = PersonIdentification.PersonIdOnly,
                Format = ExportFormat.Xlsx,
            });

        stream.Position = 0;

        return new XLWorkbook(stream);
    }

    private static Rgb ToRgb(XLColor colour) => new(colour.Color.R, colour.Color.G, colour.Color.B);
}
