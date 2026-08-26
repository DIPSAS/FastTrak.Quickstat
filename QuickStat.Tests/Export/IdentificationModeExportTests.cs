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
/// The privacy regression suite PORT-PLAN.md R6 calls for, and treats as release-blocking.
/// </summary>
/// <remarks>
/// Two distinct claims are tested, because they fail independently:
/// <list type="number">
///   <item><description>
///     The date-of-birth, national-id and name columns are <b>absent</b> in the two non-full modes -
///     no field, no separator, no placeholder. A blank column is not an absent one, and a consumer
///     counting columns notices.
///   </description></item>
///   <item><description>
///     No identifying value reaches the file <em>by any route</em>. Asserting on the column layout
///     alone would pass a writer that leaked a name into a data cell, a header or a comment, so the
///     whole byte stream is searched for the identifying strings.
///   </description></item>
/// </list>
/// <para>
/// Most cases here drive a hand-built <see cref="ExportDataset"/>, because the writers are pure.
/// <see cref="ANationalIdRecoveredAfterTheLoadStillCannotLeave"/> is the exception and starts from a
/// real <see cref="PersonMatrix"/>: Phase 4 restored
/// <see cref="QuickStat.Domain.Patients.NationalIdRecovery.EnsureNationalIdsAsync"/>
/// (<c>MainQuickStat.pas:536-540</c>), which fills
/// <see cref="QuickStat.Domain.Patients.Patient.NationalId"/> at load time <b>whatever the
/// identification mode is</b>, so from now on <see cref="MatrixRow.NationalId"/> really is populated
/// while an anonymous export runs. That is the field this suite exists to keep inside the process.
/// </para>
/// </remarks>
public class IdentificationModeExportTests
{
    private const string LeakedName = "Hansen, Ola";
    private const string LeakedNationalId = "12032212345";
    private const string LeakedDateOfBirth = "12.03.1922";

    private static DatasetExportOptions Options(
        PersonIdentification identification,
        bool includeTimestamps = false,
        ExportFormat format = ExportFormat.Csv) =>
        new()
        {
            Identification = identification,
            IncludeTimestamps = includeTimestamps,
            Format = format,
            Culture = ExportFixtures.Norwegian,
        };

    private static string WriteText(PersonIdentification identification, bool includeTimestamps = false)
    {
        var anonymiser = new MatrixAnonymiser();
        anonymiser.Reset(1);

        return ExportFixtures.Cp1252.GetString(ExportFixtures.WriteCsv(
            ExportFixtures.WorkedExample(),
            Options(identification, includeTimestamps),
            anonymiser));
    }

    [Theory]
    [InlineData(PersonIdentification.PersonIdOnly)]
    [InlineData(PersonIdentification.RandomPersonId)]
    public void NonFullModesOmitTheIdentityColumnsEntirely(PersonIdentification identification)
    {
        string text = WriteText(identification);
        string[] lines = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("\"PID\";\"AGE\";\"YOB\";", lines[0]);

        // Three fields per line, therefore three separators - not seven with four of them empty.
        Assert.All(lines, line => Assert.Equal(3, line.Count(character => character == ';')));

        Assert.DoesNotContain(FixedColumns.DateOfBirthHeader, text, StringComparison.Ordinal);
        Assert.DoesNotContain(FixedColumns.NationalIdHeader, text, StringComparison.Ordinal);
        Assert.DoesNotContain(FixedColumns.NameHeader, text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PersonIdentification.PersonIdOnly, false)]
    [InlineData(PersonIdentification.PersonIdOnly, true)]
    [InlineData(PersonIdentification.RandomPersonId, false)]
    [InlineData(PersonIdentification.RandomPersonId, true)]
    public void NoIdentifyingValueSurvivesInANonFullExport(
        PersonIdentification identification,
        bool includeTimestamps)
    {
        string text = WriteText(identification, includeTimestamps);

        Assert.DoesNotContain(LeakedName, text, StringComparison.Ordinal);
        Assert.DoesNotContain("Hansen", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Ola", text, StringComparison.Ordinal);
        Assert.DoesNotContain(LeakedNationalId, text, StringComparison.Ordinal);
        Assert.DoesNotContain(LeakedDateOfBirth, text, StringComparison.Ordinal);
        Assert.DoesNotContain("1922-03-12", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RandomPersonIdAlsoRemovesTheRealPersonId()
    {
        var anonymiser = new MatrixAnonymiser();
        anonymiser.Reset(1);

        string text = ExportFixtures.Cp1252.GetString(ExportFixtures.WriteCsv(
            ExportFixtures.WorkedExample(),
            Options(PersonIdentification.RandomPersonId),
            anonymiser));

        int pseudonym = anonymiser.GetPseudonym(8);

        Assert.DoesNotContain("\"8\"", text, StringComparison.Ordinal);
        Assert.Contains(pseudonym.ToString(CultureInfo.InvariantCulture), text, StringComparison.Ordinal);
        Assert.NotEqual(8, pseudonym);
    }

    [Fact]
    public void FullIdentificationKeepsEverything()
    {
        string text = WriteText(PersonIdentification.Full);

        Assert.Contains(LeakedName, text, StringComparison.Ordinal);
        Assert.Contains(LeakedNationalId, text, StringComparison.Ordinal);
        Assert.Contains(LeakedDateOfBirth, text, StringComparison.Ordinal);
        Assert.Contains("\"8\"", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PersonIdentification.Full, false, 6)]
    [InlineData(PersonIdentification.Full, true, 8)]
    [InlineData(PersonIdentification.PersonIdOnly, false, 3)]
    [InlineData(PersonIdentification.PersonIdOnly, true, 5)]
    [InlineData(PersonIdentification.RandomPersonId, false, 3)]
    [InlineData(PersonIdentification.RandomPersonId, true, 5)]
    public void TheColumnCountIsWhatEachModeImplies(
        PersonIdentification identification,
        bool includeTimestamps,
        int expected)
    {
        var options = Options(identification, includeTimestamps);

        Assert.Equal(expected, CsvMatrixWriter.CountColumns(ExportFixtures.WorkedExample(), options));

        string text = WriteText(identification, includeTimestamps);
        string header = text.Split("\r\n")[0];

        Assert.Equal(expected, header.Count(character => character == ';'));
    }

    [Theory]
    [InlineData(PersonIdentification.PersonIdOnly)]
    [InlineData(PersonIdentification.RandomPersonId)]
    public void TheWorkbookOmitsTheSameColumnsAsTheCsv(PersonIdentification identification)
    {
        var anonymiser = new MatrixAnonymiser();
        anonymiser.Reset(1);

        using var stream = new MemoryStream();
        XlsxMatrixWriter.Write(
            ExportFixtures.WorkedExample(),
            stream,
            Options(identification, format: ExportFormat.Xlsx),
            anonymiser);

        stream.Position = 0;

        using var workbook = new XLWorkbook(stream);
        IXLWorksheet sheet = workbook.Worksheet(XlsxMatrixWriter.WorksheetName);

        List<string> headers = [.. Enumerable.Range(1, 3).Select(column => sheet.Cell(1, column).GetString())];

        Assert.Equal(new[] { FixedColumns.PersonIdHeader, "AGE", "YOB" }, headers);
        Assert.True(sheet.Cell(1, 4).IsEmpty());

        string everything = string.Concat(
            sheet.CellsUsed().Select(cell => cell.GetFormattedString()));

        Assert.DoesNotContain("Hansen", everything, StringComparison.Ordinal);
        Assert.DoesNotContain(LeakedNationalId, everything, StringComparison.Ordinal);
        Assert.DoesNotContain("1922-03-12", everything, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PersonIdentification.PersonIdOnly, ExportFormat.Csv)]
    [InlineData(PersonIdentification.PersonIdOnly, ExportFormat.Xlsx)]
    [InlineData(PersonIdentification.RandomPersonId, ExportFormat.Csv)]
    [InlineData(PersonIdentification.RandomPersonId, ExportFormat.Xlsx)]
    public void ANationalIdRecoveredAfterTheLoadStillCannotLeave(
        PersonIdentification identification,
        ExportFormat format)
    {
        // The hazard Phase 4 introduces: the recovery query runs unconditionally, so MatrixRow
        // .NationalId is now filled in even when the mode will never show it.  R6 is about what
        // leaves the process, and this is the test that says so - both writers derive their identity
        // columns from FixedColumns.VisibleOrdinals and neither can emit the field.
        PersonMatrix matrix = RecoveredCohortMatrix();

        var anonymiser = new MatrixAnonymiser();
        anonymiser.Reset(matrix.Rows.Count);

        ExportDataset dataset = ExportDataset.FromMatrix(matrix, includeAppearance: format == ExportFormat.Xlsx);

        // It really is in the matrix and in the projection: the assertions below are about the file.
        Assert.Equal(LeakedNationalId, Assert.Single(matrix.Rows).NationalId);
        Assert.Equal(LeakedNationalId, Assert.Single(dataset.Rows).NationalId);

        string written = format == ExportFormat.Csv
            ? ExportFixtures.Cp1252.GetString(
                ExportFixtures.WriteCsv(dataset, Options(identification), anonymiser))
            : WorkbookText(dataset, Options(identification, format: ExportFormat.Xlsx), anonymiser);

        Assert.DoesNotContain(LeakedNationalId, written, StringComparison.Ordinal);
        Assert.DoesNotContain(FixedColumns.NationalIdHeader, written, StringComparison.Ordinal);
        Assert.DoesNotContain("Hansen", written, StringComparison.Ordinal);
    }

    /// <summary>
    /// A cohort whose national id was <em>not</em> returned by the population procedure and was
    /// filled in afterwards, which is what <c>EnsureNationalIdsAsync</c> does to every load.
    /// </summary>
    private static PersonMatrix RecoveredCohortMatrix()
    {
        Patient patient = new()
        {
            PersonId = 8,
            FirstName = "Ola",
            LastName = "Hansen",
            DateOfBirth = new DateTime(1922, 3, 12, 0, 0, 0, DateTimeKind.Unspecified),
        };

        Assert.Null(patient.NationalId);

        // NationalIdRecovery.EnsureNationalIdsAsync, in one line: the id lands on the patient before
        // PreparePopulation copies it onto the row.
        patient.NationalId = LeakedNationalId;

        PersonMatrix matrix = new(new DataPointFactory());

        matrix.PreparePopulation([patient]);

        VariableNameSet names = matrix.CreateVariableNameSet();

        names.Add("AGE");
        matrix.AddColumns(names);

        matrix.Add("AGE", new CollectorResultRow
        {
            PersonId = 8,
            VarName = "AGE",
            Value = 97,
            Timestamp = new DateTime(2019, 8, 14, 9, 30, 0, DateTimeKind.Unspecified),
            RowId = 1,
        });

        matrix.Lock();

        return matrix;
    }

    private static string WorkbookText(
        ExportDataset dataset,
        DatasetExportOptions options,
        IAnonymiser anonymiser)
    {
        using var stream = new MemoryStream();

        XlsxMatrixWriter.Write(dataset, stream, options, anonymiser);

        stream.Position = 0;

        using var workbook = new XLWorkbook(stream);

        return string.Concat(
            workbook.Worksheet(XlsxMatrixWriter.WorksheetName)
                .CellsUsed()
                .Select(cell => cell.GetFormattedString()));
    }

    [Fact]
    public void ExportAndDisplayCannotDisagreeBecauseBothDeriveFromOnePolicy()
    {
        // PORT-PLAN.md §7.2. There is one instance of the mode and one derivation from it; nothing
        // in the export path reads a control or keeps a second copy.
        var policy = new IdentificationPolicy { Mode = PersonIdentification.RandomPersonId };
        var options = new DatasetExportOptions { Identification = policy.Mode };

        Assert.Equal(policy.Columns, options.Columns);
        Assert.Equal(IdentificationColumns.For(policy.Mode), options.Columns);

        policy.Mode = PersonIdentification.Full;

        // The options record captured the old mode by value, which is correct: an export in flight
        // must not change identification underneath itself. Rebuilding from the policy tracks it.
        Assert.NotEqual(policy.Columns, options.Columns);
        Assert.Equal(
            policy.Columns,
            new DatasetExportOptions { Identification = policy.Mode }.Columns);
    }
}
