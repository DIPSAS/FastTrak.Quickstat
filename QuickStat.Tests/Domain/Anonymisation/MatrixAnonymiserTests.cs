using QuickStat.Domain.Anonymisation;
using Xunit;

namespace QuickStat.Tests.Domain.Anonymisation;

/// <summary>
/// The two security properties <see cref="IAnonymiser"/> promises, plus the Delphi's digit widths.
/// </summary>
/// <remarks>
/// PORT-PLAN.md §7.2 lists the unseeded RNG as a bug: identical across every run and every machine,
/// yet different between two exports in one session. Both halves are asserted here, in the
/// direction the fix requires.
/// </remarks>
public class MatrixAnonymiserTests
{
    [Theory]
    // Docs/Port/04-matrix-export.md §4.2. The Delphi passed RowCount = 1 + max(N, 1) and multiplied
    // ten until it reached it, so 17 people give three-digit ids in 100-999.
    [InlineData(0, 10)]
    [InlineData(1, 10)]
    [InlineData(9, 10)]
    [InlineData(10, 100)]
    [InlineData(17, 100)]
    [InlineData(99, 100)]
    [InlineData(100, 1000)]
    [InlineData(999, 1000)]
    [InlineData(1000, 10000)]
    [InlineData(9999, 10000)]
    public void ScaleFactorMatchesTheDelphi(int personCount, int expected) =>
        Assert.Equal(expected, MatrixAnonymiser.ScaleFactorFor(personCount));

    [Fact]
    public void PseudonymsHaveTheDelphiDigitWidth()
    {
        var anonymiser = new MatrixAnonymiser();
        anonymiser.Reset(17);

        Assert.Equal(100, anonymiser.ScaleFactor);

        for (int personId = 1; personId <= 17; personId++)
        {
            int pseudonym = anonymiser.GetPseudonym(personId);

            Assert.InRange(pseudonym, 100, 999);
        }
    }

    [Fact]
    public void PseudonymsAreUniqueWithinADataset()
    {
        var anonymiser = new MatrixAnonymiser();
        anonymiser.Reset(500);

        var pseudonyms = new HashSet<int>();

        for (int personId = 1; personId <= 500; personId++)
        {
            Assert.True(pseudonyms.Add(anonymiser.GetPseudonym(personId)));
        }

        Assert.Equal(500, pseudonyms.Count);
        Assert.All(pseudonyms, pseudonym => Assert.InRange(pseudonym, 1000, 9999));
    }

    [Fact]
    public void APseudonymIsStableForTheLifetimeOfTheDataset()
    {
        // The Delphi failed this: a second SaveToFile in one process built a fresh anonymiser and
        // continued the RNG stream, so the same patient changed pseudonym between two exports.
        var anonymiser = new MatrixAnonymiser();
        anonymiser.Reset(50);

        int first = anonymiser.GetPseudonym(4711);

        for (int repeat = 0; repeat < 100; repeat++)
        {
            _ = anonymiser.GetPseudonym(repeat);
        }

        Assert.Equal(first, anonymiser.GetPseudonym(4711));
        Assert.False(anonymiser.EnsureSpaceFor(50));
        Assert.Equal(first, anonymiser.GetPseudonym(4711));
    }

    [Fact]
    public void EnsureSpaceForDoesNotDisturbAWideEnoughSpace()
    {
        var anonymiser = new MatrixAnonymiser();

        Assert.True(anonymiser.EnsureSpaceFor(50));

        int scaleFactor = anonymiser.ScaleFactor;
        int pseudonym = anonymiser.GetPseudonym(1);

        Assert.False(anonymiser.EnsureSpaceFor(50));
        Assert.False(anonymiser.EnsureSpaceFor(5));
        Assert.Equal(scaleFactor, anonymiser.ScaleFactor);
        Assert.Equal(pseudonym, anonymiser.GetPseudonym(1));

        // A cohort that needs more digits cannot be served by the old space, so it is replaced.
        Assert.True(anonymiser.EnsureSpaceFor(5000));
        Assert.Equal(10000, anonymiser.ScaleFactor);
        Assert.Empty(anonymiser.PseudonymToPersonId);
    }

    [Fact]
    public void TheSamePatientIsUnlinkableAcrossDatasets()
    {
        // The core privacy property. Reset draws a fresh 256-bit HMAC key, so the pseudonym a
        // patient receives in one dataset says nothing about the one they receive in another;
        // joining two anonymised exports must not re-identify anybody.
        const int patient = 4711;
        const int datasets = 200;

        var anonymiser = new MatrixAnonymiser();
        var seen = new HashSet<int>();

        for (int dataset = 0; dataset < datasets; dataset++)
        {
            anonymiser.Reset(50);
            seen.Add(anonymiser.GetPseudonym(patient));
        }

        // 200 independent draws from 900 values: about 179 distinct in expectation. Anything below
        // 50 means the derivation is correlated across resets, which is the defect being tested for.
        Assert.True(
            seen.Count > 50,
            $"Expected the pseudonym to vary across datasets; saw {seen.Count} distinct values in {datasets}.");
    }

    [Fact]
    public void TwoAnonymisersDoNotShareASequence()
    {
        // The Delphi's Random stream started from RandSeed = 0 in every process, so two runs - and
        // two machines - produced the identical pseudonym list for cohorts of the same size.
        const int people = 40;

        var first = new MatrixAnonymiser();
        var second = new MatrixAnonymiser();

        first.Reset(people);
        second.Reset(people);

        int[] left = [.. Enumerable.Range(1, people).Select(first.GetPseudonym)];
        int[] right = [.. Enumerable.Range(1, people).Select(second.GetPseudonym)];

        Assert.NotEqual(left, right);
        Assert.True(
            left.Zip(right).Count(pair => pair.First == pair.Second) < people / 2,
            "Two independently keyed anonymisers produced suspiciously similar sequences.");
    }

    [Fact]
    public void TwoDifferentCohortsOfTheSameSizeDoNotShareAPseudonymList()
    {
        // Docs/Port/04-matrix-export.md §4.2: "Two 'anonymised' exports of two different populations
        // of the same size share the same pseudonym list; joining them re-identifies by position."
        const int people = 40;

        var anonymiser = new MatrixAnonymiser();

        anonymiser.Reset(people);
        int[] cohortA = [.. Enumerable.Range(1, people).Select(anonymiser.GetPseudonym)];

        anonymiser.Reset(people);
        int[] cohortB = [.. Enumerable.Range(5000, people).Select(anonymiser.GetPseudonym)];

        Assert.NotEqual(cohortA, cohortB);
    }

    [Fact]
    public void ResetForgetsTheMapCompletely()
    {
        var anonymiser = new MatrixAnonymiser();
        anonymiser.Reset(10);

        _ = anonymiser.GetPseudonym(1);
        _ = anonymiser.GetPseudonym(2);

        Assert.Equal(2, anonymiser.PseudonymToPersonId.Count);

        anonymiser.Reset(10);

        Assert.Empty(anonymiser.PseudonymToPersonId);
    }

    [Fact]
    public void TheMapIsASnapshotAndCannotBeMutatedThroughTheInterface()
    {
        var anonymiser = new MatrixAnonymiser();
        anonymiser.Reset(10);

        _ = anonymiser.GetPseudonym(1);

        IReadOnlyDictionary<int, int> before = anonymiser.PseudonymToPersonId;

        _ = anonymiser.GetPseudonym(2);

        Assert.Single(before);
        Assert.Equal(2, anonymiser.PseudonymToPersonId.Count);
    }

    [Fact]
    public void TheMapInvertsExactly()
    {
        var anonymiser = new MatrixAnonymiser();
        anonymiser.Reset(30);

        for (int personId = 100; personId < 130; personId++)
        {
            _ = anonymiser.GetPseudonym(personId);
        }

        foreach (KeyValuePair<int, int> entry in anonymiser.PseudonymToPersonId)
        {
            Assert.Equal(entry.Key, anonymiser.GetPseudonym(entry.Value));
        }
    }

    [Fact]
    public void UsingTheAnonymiserBeforeAnyResetFails()
    {
        var anonymiser = new MatrixAnonymiser();

        Assert.False(anonymiser.HasPseudonymSpace);
        Assert.Equal(0, anonymiser.ScaleFactor);
        Assert.Throws<InvalidOperationException>(() => anonymiser.GetPseudonym(1));
    }

    [Fact]
    public void ANegativeCohortIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MatrixAnonymiser.ScaleFactorFor(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MatrixAnonymiser().Reset(-1));
    }

    [Fact]
    public void AnAbsurdlyLargeCohortIsRejectedRatherThanOverflowing()
    {
        // The Delphi's fScaleFactor was an integer and would have wrapped.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => MatrixAnonymiser.ScaleFactorFor(int.MaxValue));
    }
}
