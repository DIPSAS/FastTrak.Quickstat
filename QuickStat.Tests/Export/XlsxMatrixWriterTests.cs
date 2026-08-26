using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.Matrix;
using QuickStat.Export;
using Xunit;

namespace QuickStat.Tests.Export;

/// <summary>
/// The xlsx writer, whose point is that nothing goes through a locale-formatted string.
/// </summary>
/// <remarks>
/// PORT-PLAN.md §7.3. <c>Open this dataset in Excel</c> writes a CSV to <c>%TEMP%</c> today, so
/// every number is formatted with the ambient decimal separator and re-parsed by Excel, and a
/// national id starting with a zero loses it. A workbook has neither problem, and these tests read
/// the values back as values.
/// </remarks>
public class XlsxMatrixWriterTests
{
    private static XLWorkbook RoundTrip(ExportDataset dataset, DatasetExportOptions options)
    {
        using var stream = new MemoryStream();
        XlsxMatrixWriter.Write(dataset, stream, options);

        stream.Position = 0;

        return new XLWorkbook(stream);
    }

    private static IXLWorksheet Sheet(XLWorkbook workbook) =>
        workbook.Worksheet(XlsxMatrixWriter.WorksheetName);

    [Fact]
    public void NumbersAreNumbersAndNotLocaleFormattedText()
    {
        var dataset = new ExportDataset
        {
            Columns = [new ExportColumn { VarName = "BMI", Title = "Kroppsmasseindeks" }],
            Rows =
            [
                new ExportRow
                {
                    PersonId = 1,
                    Cells = [new ExportCell { HasValue = true, Value = 23.456 }],
                },
            ],
        };

        using XLWorkbook workbook = RoundTrip(
            dataset,
            new DatasetExportOptions { Identification = PersonIdentification.PersonIdOnly });

        IXLCell cell = Sheet(workbook).Cell(2, 2);

        Assert.Equal(XLDataType.Number, cell.DataType);
        Assert.Equal(23.456, cell.GetDouble(), 10);
    }

    [Fact]
    public void DatesAreDatesWithAnIsoNumberFormat()
    {
        using XLWorkbook workbook = RoundTrip(
            ExportFixtures.WorkedExample(),
            new DatasetExportOptions
            {
                Identification = PersonIdentification.Full,
                IncludeTimestamps = true,
            });

        IXLWorksheet sheet = Sheet(workbook);

        IXLCell dateOfBirth = sheet.Cell(2, 2);
        Assert.Equal(XLDataType.DateTime, dateOfBirth.DataType);
        Assert.Equal(new DateTime(1922, 3, 12, 0, 0, 0, DateTimeKind.Unspecified), dateOfBirth.GetDateTime());
        Assert.Equal(XlsxMatrixWriter.DateNumberFormat, dateOfBirth.Style.DateFormat.Format);

        // Column 5 is AGE, column 6 is AGE.DATE - the timestamp lands beside its value, exactly as
        // in the CSV, and it lands as a date rather than as the string "2019-08-14".
        IXLCell timestamp = sheet.Cell(2, 6);
        Assert.Equal("AGE.DATE", sheet.Cell(1, 6).GetString());
        Assert.Equal(XLDataType.DateTime, timestamp.DataType);
        Assert.Equal(new DateTime(2019, 8, 14, 0, 0, 0, DateTimeKind.Unspecified), timestamp.GetDateTime());
    }

    [Fact]
    public void ANationalIdKeepsItsLeadingZero()
    {
        var dataset = new ExportDataset
        {
            Columns = [],
            Rows =
            [
                new ExportRow
                {
                    PersonId = 1,
                    NationalId = "01029912345",
                    FullName = "Ås, Kari",
                    Cells = [],
                },
            ],
        };

        using XLWorkbook workbook = RoundTrip(
            dataset,
            new DatasetExportOptions { Identification = PersonIdentification.Full });

        IXLCell cell = Sheet(workbook).Cell(2, 3);

        Assert.Equal(XLDataType.Text, cell.DataType);
        Assert.Equal("01029912345", cell.GetString());
    }

    [Fact]
    public void CaptionTextWinsOverTheNumberHereToo()
    {
        var dataset = new ExportDataset
        {
            Columns = [new ExportColumn { VarName = "FORM.NOTE", Title = "Notat" }],
            Rows =
            [
                new ExportRow
                {
                    PersonId = 1,
                    Cells = [new ExportCell { HasValue = true, Value = 42, Caption = "Ingen funn" }],
                },
            ],
        };

        using XLWorkbook workbook = RoundTrip(
            dataset,
            new DatasetExportOptions { Identification = PersonIdentification.PersonIdOnly });

        Assert.Equal("Ingen funn", Sheet(workbook).Cell(2, 2).GetString());
    }

    [Fact]
    public void HeadersAreVariableNamesAndThePanesAreFrozen()
    {
        using XLWorkbook workbook = RoundTrip(
            ExportFixtures.WorkedExample(),
            new DatasetExportOptions { Identification = PersonIdentification.Full });

        IXLWorksheet sheet = Sheet(workbook);

        Assert.Equal(
            new[]
            {
                FixedColumns.PersonIdHeader,
                FixedColumns.DateOfBirthHeader,
                FixedColumns.NationalIdHeader,
                FixedColumns.NameHeader,
                "AGE",
                "YOB",
            },
            Enumerable.Range(1, 6).Select(column => sheet.Cell(1, column).GetString()).ToArray());

        Assert.Equal(1, sheet.SheetView.SplitRow);
        Assert.Equal(FixedColumns.Count, sheet.SheetView.SplitColumn);
        Assert.True(sheet.Row(1).Style.Font.Bold);
    }

    [Fact]
    public void ACellWithNoDatapointStaysEmptyRatherThanBecomingZero()
    {
        using XLWorkbook workbook = RoundTrip(
            ExportFixtures.WorkedExample(secondValueMissing: true),
            new DatasetExportOptions
            {
                Identification = PersonIdentification.PersonIdOnly,
                IncludeTimestamps = true,
            });

        IXLWorksheet sheet = Sheet(workbook);

        Assert.True(sheet.Cell(2, 4).IsEmpty());   // YOB
        Assert.True(sheet.Cell(2, 5).IsEmpty());   // YOB.DATE
    }

    [Fact]
    public void TooManyColumnsIsAnExplicitFailureRatherThanASilentTruncation()
    {
        // Docs/Port/04-matrix-export.md R-15: Excel stops at 16 384 columns, so with timestamps on
        // the ceiling is about 8 190 variables.
        var columns = new List<ExportColumn>();

        for (int index = 0; index < 8_200; index++)
        {
            columns.Add(new ExportColumn
            {
                VarName = index.ToString(CultureInfo.InvariantCulture),
                Title = "",
            });
        }

        var dataset = new ExportDataset { Columns = columns, Rows = [] };
        var options = new DatasetExportOptions
        {
            Identification = PersonIdentification.PersonIdOnly,
            IncludeTimestamps = true,
        };

        using var stream = new MemoryStream();

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(
            () => XlsxMatrixWriter.Write(dataset, stream, options));

        Assert.Contains("16384", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RandomPersonIdNeedsAnAnonymiserHereTooRatherThanSilentlyLeaking()
    {
        using var stream = new MemoryStream();
        var options = new DatasetExportOptions { Identification = PersonIdentification.RandomPersonId };

        Assert.Throws<ArgumentException>(
            () => XlsxMatrixWriter.Write(ExportFixtures.WorkedExample(), stream, options));
    }

    [Fact]
    public void TheTextRendererAgreesWithTheCsv()
    {
        var cell = new ExportCell { HasValue = true, Value = 3.5 };

        Assert.Equal("3,5", XlsxMatrixWriter.AsText(cell, ExportFixtures.Norwegian));
        Assert.Equal(string.Empty, XlsxMatrixWriter.AsText(default, ExportFixtures.Norwegian));
        Assert.Equal(
            "Ja",
            XlsxMatrixWriter.AsText(cell with { Caption = "Ja" }, ExportFixtures.Norwegian));
    }
}
