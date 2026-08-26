using QuickStat.Diagnostics;
using Xunit;

namespace QuickStat.Tests.Diagnostics;

/// <summary>
/// <see cref="OperationProgress"/> is owned by step 2.7 by placement only - it is produced by 2.2
/// and 2.4 and consumed by Phase 3 (<c>Docs/Port/06-contracts.md</c> §3). These tests pin the shape
/// its real consumers depend on and nothing more.
/// </summary>
public class OperationProgressTests
{
    [Fact]
    public void ItIsACheapValue()
    {
        // Reported from inside a collect loop that runs once per batch per collector, so it must not
        // allocate.
        Assert.True(typeof(OperationProgress).IsValueType);
    }

    [Fact]
    public void PercentIsOptional()
    {
        OperationProgress indeterminate = new("Progress", "Connecting to Testdatabase (NDV) ...", null);

        Assert.Null(indeterminate.Percent);
        Assert.Equal("Progress", indeterminate.Header);
        Assert.Equal("Connecting to Testdatabase (NDV) ...", indeterminate.Info);
    }

    [Fact]
    public void EqualityIsByValue()
    {
        // So a view model can suppress a redundant update without writing a comparer.
        Assert.Equal(new OperationProgress("H", "I", 50d), new OperationProgress("H", "I", 50d));
        Assert.NotEqual(new OperationProgress("H", "I", 50d), new OperationProgress("H", "I", 51d));
    }

    [Fact]
    public void ItSurvivesBeingReportedThroughIProgress()
    {
        List<OperationProgress> received = [];
        IProgress<OperationProgress> progress = new Progress<OperationProgress>(received.Add);

        Assert.NotNull(progress);

        // Progress<T> marshals asynchronously, so this only asserts the contract compiles and
        // accepts the type; the marshalling itself belongs to Phase 3.
        progress.Report(new OperationProgress("Progress", "Collecting", 10d));
    }
}
