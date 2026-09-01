using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using QuickStat.Domain.Anonymisation;
using QuickStat.Export;
using Xunit;

namespace QuickStat.Tests.Export;

/// <summary>
/// One patient, one pseudonym - asserted on the bytes that leave the process, not on the map behind
/// them.
/// </summary>
/// <remarks>
/// <para>
/// <c>MatrixAnonymiserTests.PseudonymsAreUniqueWithinADataset</c> already pins the map: <c>Derive</c>
/// rejects a candidate that is already taken and both dictionaries are filled with <c>Add</c>, which
/// throws rather than overwriting. That is a strong guarantee and it is the wrong place to stop. A
/// writer that looked its pseudonym up by row <em>index</em> instead of by <c>PersonId</c>, or
/// hoisted the call out of the loop, would leave every one of those cases green while shipping a file
/// in which two patients share an id - and a shared id silently merges two people in whatever
/// analysis the file was exported for. So these cases parse the column back out of the finished CSV
/// and the finished workbook.
/// </para>
/// <para>
/// The other half is density. The unit test draws 500 pseudonyms from a space of 9 000, a load factor
/// of 5.6%; the densest cohort the Delphi's scale factor allows is <c>scale - 1</c> people, which is
/// twice that. <see cref="TheDensestCohortTheScaleAllowsStillGetsOnePidEach"/> runs there, and
/// <see cref="AnExhaustedSpaceFailsLoudlyRatherThanRepeatingAPseudonym"/> runs past the end to pin
/// which way the collision loop falls over: an exception, never a duplicate.
/// </para>
/// </remarks>
public class PseudonymUniquenessTests
{
    /// <summary>Rows per exported file. Large enough that a collision is likely if one can happen.</summary>
    /// <remarks>
    /// 200 people give a scale factor of 1 000 and a space of 9 000, so the birthday probability of
    /// at least one collision <em>without</em> the rejection loop is about 89%. A single-row fixture
    /// - which is what every other pseudonym export case uses - could not distinguish the two.
    /// </remarks>
    private const int Rows = 200;

    private static DatasetExportOptions Options(ExportFormat format = ExportFormat.Csv) =>
        new()
        {
            Identification = PersonIdentification.RandomPersonId,
            Format = format,
            Culture = ExportFixtures.Norwegian,
        };

    /// <summary>An anonymiser with a space sized the way a population load sizes it.</summary>
    private static MatrixAnonymiser ForCohort(int personCount)
    {
        MatrixAnonymiser anonymiser = new();

        anonymiser.Reset(personCount);

        return anonymiser;
    }

    /// <summary>
    /// The first field of every data row of a legacy CSV, which in this mode is the pseudonym.
    /// </summary>
    private static List<int> PidColumnOf(byte[] csv)
    {
        string[] lines = ExportFixtures.Cp1252.GetString(csv)
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        // Line 0 is the header, whose person cell is the ordinary quoted "PID" even here.
        return [.. lines.Skip(1).Select(line => int.Parse(
            line.Split(DatasetExportOptions.LegacySeparator)[0],
            CultureInfo.InvariantCulture))];
    }

    /// <summary>The same column, read out of the workbook's first column below the header.</summary>
    private static List<int> PidColumnOf(MemoryStream xlsx)
    {
        xlsx.Position = 0;

        using XLWorkbook workbook = new(xlsx);

        IXLWorksheet sheet = workbook.Worksheet(XlsxMatrixWriter.WorksheetName);

        return
        [
            .. sheet.Column(1)
                .CellsUsed()
                .Skip(1)
                .Select(cell => cell.GetValue<int>()),
        ];
    }

    private static void AssertOnePidEach(List<int> pids, int expectedRows, int scaleFactor)
    {
        Assert.Equal(expectedRows, pids.Count);
        Assert.Equal(expectedRows, pids.Distinct().Count());
        Assert.All(pids, pid => Assert.InRange(pid, scaleFactor, (10 * scaleFactor) - 1));
    }

    // -------------------------------------------------------------------------------------------
    //  What actually reaches the file.
    // -------------------------------------------------------------------------------------------

    [Fact]
    public void EveryRowOfAPseudonymisedCsvCarriesItsOwnPid()
    {
        MatrixAnonymiser anonymiser = ForCohort(Rows);

        byte[] csv = ExportFixtures.WriteCsv(ExportFixtures.Cohort(Rows), Options(), anonymiser);

        AssertOnePidEach(PidColumnOf(csv), Rows, anonymiser.ScaleFactor);
    }

    [Fact]
    public void EveryRowOfAPseudonymisedWorkbookCarriesItsOwnPid()
    {
        // The second writer, and not by reference to the first: XlsxMatrixWriter reaches the
        // anonymiser through its own loop (XlsxMatrixWriter.cs:173).
        MatrixAnonymiser anonymiser = ForCohort(Rows);

        using MemoryStream stream = new();

        XlsxMatrixWriter.Write(ExportFixtures.Cohort(Rows), stream, Options(ExportFormat.Xlsx), anonymiser);

        AssertOnePidEach(PidColumnOf(stream), Rows, anonymiser.ScaleFactor);
    }

    [Fact]
    public void ThePidInEveryRowBelongsToThatRowsPatient()
    {
        // Distinctness alone would still pass a writer that shifted the column by one row, which
        // would hand every patient somebody else's pseudonym - anonymous, unique, and wrong.
        MatrixAnonymiser anonymiser = ForCohort(Rows);

        ExportDataset dataset = ExportFixtures.Cohort(Rows);
        List<int> pids = PidColumnOf(ExportFixtures.WriteCsv(dataset, Options(), anonymiser));

        IReadOnlyDictionary<int, int> map = anonymiser.PseudonymToPersonId;

        for (int index = 0; index < Rows; index++)
        {
            Assert.Equal(dataset.Rows[index].PersonId, map[pids[index]]);
        }
    }

    [Fact]
    public void TheTwoWritersAgreeOnWhichPatientGotWhichPid()
    {
        // One anonymiser, two formats: the same loaded dataset exported twice must not produce two
        // different pseudonym assignments, whichever menu item the user reached for.
        MatrixAnonymiser anonymiser = ForCohort(Rows);

        ExportDataset dataset = ExportFixtures.Cohort(Rows);

        List<int> fromCsv = PidColumnOf(ExportFixtures.WriteCsv(dataset, Options(), anonymiser));

        using MemoryStream stream = new();

        XlsxMatrixWriter.Write(dataset, stream, Options(ExportFormat.Xlsx), anonymiser);

        Assert.Equal(fromCsv, PidColumnOf(stream));
    }

    // -------------------------------------------------------------------------------------------
    //  Density: the corner the rejection loop exists for.
    // -------------------------------------------------------------------------------------------

    [Fact]
    public void TheDensestCohortTheScaleAllowsStillGetsOnePidEach()
    {
        // 999 people is the largest cohort that still fits scale factor 1 000 - one more and
        // ScaleFactorFor moves to 10 000 - so this is the highest load factor the design ever
        // reaches: 999 in 9 000, about 11%.
        const int people = 999;

        Assert.Equal(1000, MatrixAnonymiser.ScaleFactorFor(people));
        Assert.Equal(10000, MatrixAnonymiser.ScaleFactorFor(people + 1));

        MatrixAnonymiser anonymiser = ForCohort(people);

        byte[] csv = ExportFixtures.WriteCsv(ExportFixtures.Cohort(people), Options(), anonymiser);

        AssertOnePidEach(PidColumnOf(csv), people, 1000);
    }

    [Fact]
    public void AnExhaustedSpaceFailsLoudlyRatherThanRepeatingAPseudonym()
    {
        // Nothing in the application asks for more pseudonyms than the space was sized for - the
        // load resets it to the cohort's own size - but the anonymiser cannot enforce that, and what
        // it does at the wall is the difference between a failed export and a silently corrupt one.
        // Scale factor 10 gives exactly 90 values, 10 through 99.
        MatrixAnonymiser anonymiser = ForCohort(9);

        Assert.Equal(10, anonymiser.ScaleFactor);

        HashSet<int> issued = [];

        for (int personId = 1; personId <= 90; personId++)
        {
            Assert.True(issued.Add(anonymiser.GetPseudonym(personId)), "A pseudonym was issued twice.");
        }

        Assert.Equal(90, issued.Count);

        InvalidOperationException failure =
            Assert.Throws<InvalidOperationException>(() => anonymiser.GetPseudonym(91));

        Assert.Contains("91", failure.Message, StringComparison.Ordinal);

        // And the failure left nothing behind: the map still describes exactly the 90 that worked.
        Assert.Equal(90, anonymiser.PseudonymToPersonId.Count);
    }

    // -------------------------------------------------------------------------------------------
    //  The key file, which is the only thing that can undo any of the above.
    // -------------------------------------------------------------------------------------------

    [Fact]
    public void TheKeyFileInvertsTheWholeCohortWithoutLosingARow()
    {
        // One line per patient. A duplicate pseudonym would silently shorten this file, because the
        // map is keyed by pseudonym - so this is the assertion that catches the failure from the
        // other side.
        MatrixAnonymiser anonymiser = ForCohort(Rows);

        ExportDataset dataset = ExportFixtures.Cohort(Rows);

        _ = ExportFixtures.WriteCsv(dataset, Options(), anonymiser);

        string[] lines = PseudonymKeyWriter.Render(anonymiser.PseudonymToPersonId)
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(Rows, lines.Length);

        int[] personIds = [.. lines.Select(line =>
            int.Parse(line.Split('=')[1], CultureInfo.InvariantCulture))];

        Assert.Equal(
            dataset.Rows.Select(row => row.PersonId).Order(),
            personIds.Order());
    }
}
