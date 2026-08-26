using QuickStat.Configuration;
using QuickStat.Data;
using QuickStat.Domain.Patients;
using Xunit;

namespace QuickStat.Tests.Domain.Patients;

/// <summary>
/// National-id recovery: the table-valued parameter that replaces the upstream string-concatenated
/// <c>IN ( %s )</c> list, and the chunked fallback for a database without the table type.
/// </summary>
/// <remarks>
/// PORT-PLAN.md §7.3 and R2. The upstream implementation is quoted in
/// <c>Docs/Port/02-populations-patients.md</c> §5.2, together with its two latent bugs.
/// </remarks>
public class NationalIdRequestTests
{
    private static readonly SqlOptions TableValued = new();

    private static readonly SqlOptions ChunkedFallback = new()
    {
        PersonIdListTypeName = null,
        MaxIdsPerBatch = 1000,
    };

    [Fact]
    public void AnEmptyCohortProducesNoStatementAtAll()
    {
        // Upstream built "WHERE PersonId IN (  )", which SQL Server rejects outright (bug B1).
        Assert.Empty(PatientSql.NationalIdRequests([], TableValued));
    }

    [Fact]
    public void TheTableValuedPathIsASingleStatement()
    {
        SqlRequest request = Assert.Single(PatientSql.NationalIdRequests([4711, 88, 3], TableValued));

        Assert.Equal(
            "SELECT p.PersonId, p.NationalId FROM dbo.Person p JOIN :Ids i ON i.[PersonId] = p.PersonId " +
            "WHERE p.NationalId IS NOT NULL",
            request.CommandText);
        Assert.True(request.IsIdempotent);
    }

    [Fact]
    public void TheIdsTravelInTheTableValuedParameterAndNeverInTheText()
    {
        SqlRequest request = Assert.Single(PatientSql.NationalIdRequests([4711, 88, 3], TableValued));

        SqlTableParameter table = Assert.Single(request.TableParameters);
        Assert.Equal("Ids", table.Name);
        Assert.Equal("Report.PersonIdList", table.TypeName);
        Assert.Equal("PersonId", table.ColumnName);
        Assert.Equal([4711, 88, 3], table.Values);

        Assert.DoesNotContain("4711", request.CommandText, StringComparison.Ordinal);
        Assert.Null(request.NamedValues);
    }

    [Fact]
    public void TheTableTypeAndColumnComeFromConfiguration()
    {
        SqlOptions options = new() { PersonIdListTypeName = "dbo.IntIdList", PersonIdListColumnName = "Id" };

        SqlRequest request = Assert.Single(PatientSql.NationalIdRequests([1], options));

        Assert.Contains("i.[Id] = p.PersonId", request.CommandText, StringComparison.Ordinal);
        Assert.Equal("dbo.IntIdList", Assert.Single(request.TableParameters).TypeName);
    }

    [Fact]
    public void AConfiguredColumnNameCannotBreakOutOfItsIdentifier()
    {
        SqlOptions options = new() { PersonIdListColumnName = "Id] ; DROP TABLE dbo.Person --" };

        SqlRequest request = Assert.Single(PatientSql.NationalIdRequests([1], options));

        Assert.Contains("i.[Id]] ; DROP TABLE dbo.Person --]", request.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public void ACohortLargerThanTheParameterLimitIsStillOneStatement()
    {
        // The reason the TVP exists: 2 100 parameters is Microsoft.Data.SqlClient's hard ceiling, and
        // real protocols have tens of thousands of cases.
        int[] ids = [.. Enumerable.Range(1, 5000)];

        SqlRequest request = Assert.Single(PatientSql.NationalIdRequests(ids, TableValued));

        Assert.Equal(5000, Assert.Single(request.TableParameters).Values.Count);
    }

    [Fact]
    public void DuplicateIdsCollapse()
    {
        // scList.Add upstream throws on a duplicate PersonId; a population procedure returning the
        // same patient twice must not be able to crash this.
        SqlRequest request = Assert.Single(PatientSql.NationalIdRequests([7, 7, 9, 7], TableValued));

        Assert.Equal([7, 9], Assert.Single(request.TableParameters).Values);
    }

    [Fact]
    public void TheFallbackChunksAtTheConfiguredBatchSize()
    {
        int[] ids = [.. Enumerable.Range(1, 2500)];

        IReadOnlyList<SqlRequest> requests = PatientSql.NationalIdRequests(ids, ChunkedFallback);

        Assert.Equal(3, requests.Count);
        Assert.Equal(1000, requests[0].NamedValues!.Count);
        Assert.Equal(1000, requests[1].NamedValues!.Count);
        Assert.Equal(500, requests[2].NamedValues!.Count);
        Assert.All(requests, request => Assert.Empty(request.TableParameters));
    }

    [Fact]
    public void EveryChunkStaysUnderTheParameterLimit()
    {
        int[] ids = [.. Enumerable.Range(1, 6000)];

        IReadOnlyList<SqlRequest> requests = PatientSql.NationalIdRequests(ids, ChunkedFallback);

        Assert.All(requests, request => Assert.InRange(request.NamedValues!.Count, 1, 2099));
        Assert.Equal(6000, requests.Sum(request => request.NamedValues!.Count));
    }

    [Fact]
    public void TheFallbackParameterisesEveryIdRatherThanInterpolatingIt()
    {
        SqlRequest request = Assert.Single(PatientSql.NationalIdRequests([4711, 88], ChunkedFallback));

        Assert.Equal(
            "SELECT p.PersonId, p.NationalId FROM dbo.Person p WHERE p.NationalId IS NOT NULL " +
            "AND p.PersonId IN (:p0, :p1)",
            request.CommandText);
        Assert.Equal(4711, request.NamedValues!["p0"]);
        Assert.Equal(88, request.NamedValues["p1"]);
        Assert.DoesNotContain("4711", request.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public void ABatchSizeOfZeroDoesNotProduceAnInfiniteLoop()
    {
        SqlOptions broken = new() { PersonIdListTypeName = null, MaxIdsPerBatch = 0 };

        IReadOnlyList<SqlRequest> requests = PatientSql.NationalIdRequests([1, 2, 3], broken);

        Assert.Equal(3, requests.Count);
    }

    [Fact]
    public void AnEmptyTableTypeNameFallsBackToChunking()
    {
        SqlOptions blank = new() { PersonIdListTypeName = "  ", MaxIdsPerBatch = 2 };

        IReadOnlyList<SqlRequest> requests = PatientSql.NationalIdRequests([1, 2, 3], blank);

        Assert.Equal(2, requests.Count);
        Assert.All(requests, request => Assert.Empty(request.TableParameters));
    }

    [Fact]
    public void TheStatementKeepsTheUpstreamNullFilter()
    {
        // "AND NOT NationalId IS NULL" upstream: patients without a registered national id are simply
        // absent, so an existing value is never overwritten with an empty one.
        foreach (SqlRequest request in PatientSql.NationalIdRequests([1], TableValued)
            .Concat(PatientSql.NationalIdRequests([1], ChunkedFallback)))
        {
            Assert.Contains("p.NationalId IS NOT NULL", request.CommandText, StringComparison.Ordinal);
        }
    }
}
