using System.IO;
using QuickStat.Export;
using Xunit;

namespace QuickStat.Tests.Export;

/// <summary>
/// The tracker that has to include the key file, which the Delphi's never did.
/// </summary>
public class TempFileTrackerTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "QuickStat.Tests",
        Guid.NewGuid().ToString("N"));

    private bool _disposed;

    public TempFileTrackerTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        GC.SuppressFinalize(this);

        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private string NewFile(string name)
    {
        string path = Path.Combine(_directory, name);
        File.WriteAllText(path, "x");
        return path;
    }

    [Fact]
    public void DisposingDeletesEverythingTracked()
    {
        string csv = NewFile("a.csv");
        string key = NewFile("a.mapping.txt");

        using (var tracker = new TempFileTracker())
        {
            tracker.Track(csv);
            tracker.Track(key);
        }

        Assert.False(File.Exists(csv));
        Assert.False(File.Exists(key));
    }

    [Fact]
    public void TrackingTheSamePathTwiceLeavesOneEntry()
    {
        using var tracker = new TempFileTracker();
        string path = NewFile("a.csv");

        tracker.Track(path);
        tracker.Track(path);
        tracker.Track(path.ToUpperInvariant());

        Assert.Single(tracker.TrackedPaths);
    }

    [Fact]
    public void DeletingOneFileForgetsIt()
    {
        using var tracker = new TempFileTracker();
        string first = NewFile("a.csv");
        string second = NewFile("b.csv");

        tracker.Track(first);
        tracker.Track(second);

        Assert.True(tracker.Delete(first));
        Assert.False(File.Exists(first));
        Assert.Equal(new[] { second }, tracker.TrackedPaths.ToArray());
    }

    [Fact]
    public void DeleteAllReportsHowManyWentAndClearsTheList()
    {
        using var tracker = new TempFileTracker();

        tracker.Track(NewFile("a.csv"));
        tracker.Track(NewFile("b.csv"));

        Assert.Equal(2, tracker.DeleteAll());
        Assert.Empty(tracker.TrackedPaths);
    }

    [Fact]
    public void AFileHeldOpenIsReportedRatherThanThrowing()
    {
        // Excel keeps the temporary CSV open for as long as it is showing it. The Delphi swallowed
        // the failure; so does this, but it says so.
        using var tracker = new TempFileTracker();
        string path = NewFile("locked.csv");

        using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            tracker.Track(path);

            Assert.Equal(0, tracker.DeleteAll());
            Assert.True(File.Exists(path));
        }
    }

    [Fact]
    public void DisposingTwiceIsHarmless()
    {
        var tracker = new TempFileTracker();
        tracker.Track(NewFile("a.csv"));

        tracker.Dispose();
        tracker.Dispose();

        Assert.Empty(tracker.TrackedPaths);
    }

    [Fact]
    public void ABlankPathIsRejected()
    {
        using var tracker = new TempFileTracker();

        Assert.Throws<ArgumentException>(() => tracker.Track("  "));
        Assert.Throws<ArgumentNullException>(() => tracker.Track(null!));
    }
}
