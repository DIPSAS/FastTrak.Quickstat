using System.IO;
using QuickStat.Domain.Anonymisation;
using QuickStat.Export;
using Xunit;

namespace QuickStat.Tests.Export;

/// <summary>
/// The seam between the matrix and the writers, and the guards that keep a leak from being possible.
/// </summary>
/// <remarks>
/// <see cref="ExportDataset.FromMatrix"/> itself cannot be exercised here: it reads a
/// <c>PersonMatrix</c>, which step 2.5 implements in a separate worktree and which is still a
/// <see cref="NotImplementedException"/> stub in this one. Its argument guard is testable, and the
/// projection it performs is one loop over <c>Rows</c>, <c>Columns</c> and <c>TryGetDataPoint</c>.
/// </remarks>
public class ExportDatasetTests
{
    [Fact]
    public void ProjectingANullMatrixIsRejected() =>
        Assert.Throws<ArgumentNullException>(() => ExportDataset.FromMatrix(null!));

    [Fact]
    public void WritingWithoutAnAnonymiserIsRejectedRatherThanFallingBackToRealPersonIds()
    {
        // The failure mode this guards against is the worst one available: silently exporting real
        // person ids under a header that says the data is pseudonymised.
        using var stream = new MemoryStream();
        var options = new DatasetExportOptions { Identification = PersonIdentification.RandomPersonId };

        ArgumentException failure = Assert.Throws<ArgumentException>(
            () => CsvMatrixWriter.Write(ExportFixtures.WorkedExample(), stream, options));

        Assert.Equal("anonymiser", failure.ParamName);
        Assert.Empty(stream.ToArray());
    }

    [Fact]
    public void WriterArgumentsAreValidated()
    {
        using var stream = new MemoryStream();
        var options = new DatasetExportOptions { Identification = PersonIdentification.Full };

        Assert.Throws<ArgumentNullException>(() => CsvMatrixWriter.Write(null!, stream, options));
        Assert.Throws<ArgumentNullException>(
            () => CsvMatrixWriter.Write(ExportFixtures.WorkedExample(), null!, options));
        Assert.Throws<ArgumentNullException>(
            () => CsvMatrixWriter.Write(ExportFixtures.WorkedExample(), stream, null!));
    }

    [Fact]
    public void ARowWithFewerCellsThanColumnsIsPaddedRatherThanThrowing()
    {
        // Defensive: a matrix that grew a column after a row was built would otherwise take the
        // export down mid-file, leaving a truncated CSV that looks complete.
        var dataset = new ExportDataset
        {
            Columns =
            [
                new ExportColumn { VarName = "A", Title = "A" },
                new ExportColumn { VarName = "B", Title = "B" },
            ],
            Rows = [new ExportRow { PersonId = 1, Cells = [] }],
        };

        string text = ExportFixtures.Cp1252.GetString(ExportFixtures.WriteCsv(
            dataset,
            new DatasetExportOptions { Identification = PersonIdentification.PersonIdOnly }));

        Assert.Equal("\"PID\";\"A\";\"B\";\r\n\"1\";\"\";\"\";\r\n", text);
    }

    [Fact]
    public void TheLegacyEncodingResolvesWithoutAnyStartUpRegistration()
    {
        // CP1252 is absent from .NET's default encoding set; the writer's static constructor
        // registers CodePagesEncodingProvider so nothing in the application has to remember to.
        var legacy = new DatasetExportOptions { Identification = PersonIdentification.Full };
        var modern = legacy with { Dialect = CsvDialect.Rfc4180 };

        Assert.Equal(DatasetExportOptions.LegacyCodePage, CsvMatrixWriter.ResolveEncoding(legacy).CodePage);
        Assert.Empty(CsvMatrixWriter.ResolveEncoding(legacy).GetPreamble());
        Assert.Equal(65001, CsvMatrixWriter.ResolveEncoding(modern).CodePage);
        Assert.NotEmpty(CsvMatrixWriter.ResolveEncoding(modern).GetPreamble());
    }
}
