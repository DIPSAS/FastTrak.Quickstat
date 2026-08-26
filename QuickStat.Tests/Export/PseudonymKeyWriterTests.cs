using System.Globalization;
using System.IO;
using QuickStat.Domain.Anonymisation;
using QuickStat.Export;
using Xunit;

namespace QuickStat.Tests.Export;

/// <summary>The re-identification key file: its name, its bytes, and its content.</summary>
public class PseudonymKeyWriterTests
{
    [Theory]
    [InlineData(@"C:\temp\export.csv", @"C:\temp\export.mapping.txt")]
    [InlineData(@"C:\temp\export.xlsx", @"C:\temp\export.mapping.txt")]
    [InlineData(@"C:\temp\a.b.csv", @"C:\temp\a.b.mapping.txt")]
    [InlineData("export.csv", "export.mapping.txt")]
    public void TheKeyFileSitsBesideTheExportWithTheExtensionReplaced(string export, string expected) =>
        Assert.Equal(expected, PseudonymKeyWriter.KeyFilePathFor(export));

    [Fact]
    public void LinesAreSortedPseudonymEqualsPersonIdAndCrlfTerminated()
    {
        var map = new Dictionary<int, int>
        {
            [473] = 8,
            [120] = 9,
            [999] = 10,
        };

        Assert.Equal("120=9\r\n473=8\r\n999=10\r\n", PseudonymKeyWriter.Render(map));
    }

    [Fact]
    public void TheFileIsWindows1252WithoutABom()
    {
        using var stream = new MemoryStream();
        PseudonymKeyWriter.Write(new Dictionary<int, int> { [12] = 3 }, stream);

        Assert.Equal("31-32-3D-33-0D-0A", ExportFixtures.Hex(stream.ToArray()));
    }

    [Fact]
    public void AnEmptyMapProducesAnEmptyFile()
    {
        using var stream = new MemoryStream();
        PseudonymKeyWriter.Write(new Dictionary<int, int>(), stream);

        Assert.Empty(stream.ToArray());
    }

    [Fact]
    public void EveryPseudonymInOneExportHasTheSameWidthSoTheTextSortIsAlsoNumeric()
    {
        var anonymiser = new MatrixAnonymiser();
        anonymiser.Reset(120);

        for (int personId = 1; personId <= 120; personId++)
        {
            _ = anonymiser.GetPseudonym(personId);
        }

        string[] lines = PseudonymKeyWriter.Render(anonymiser.PseudonymToPersonId)
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(120, lines.Length);

        int previous = 0;

        foreach (string line in lines)
        {
            int pseudonym = int.Parse(line.Split('=')[0], CultureInfo.InvariantCulture);

            Assert.InRange(pseudonym, 1000, 9999);
            Assert.True(pseudonym > previous, "The textual sort was not also numeric.");
            previous = pseudonym;
        }
    }
}
