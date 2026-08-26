using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Configuration;
using QuickStat.Data;
using QuickStat.Tests.Data.Fakes;
using Xunit;

namespace QuickStat.Tests.Data;

/// <summary>
/// The executor's policy layer: the not-connected guard, session-option ordering, retry, timeouts
/// and scalar conversion - all against <see cref="FakeSqlSession"/>, with no server.
/// </summary>
public class QuickStatDatabaseTests
{
    private static readonly ResolvedConnectionString Connection = new()
    {
        Source = new QuickStatConnection
        {
            Name = "Testdatabase (NDV)",
            StudyName = "NDV",
            ConnectionString = @"FILE NAME=.\FastTrak.UDL",
        },
        Value = "Data Source=localhost;Initial Catalog=EFT;Integrated Security=True",
        Redacted = "Data Source=localhost;Initial Catalog=EFT;Integrated Security=True",
    };

    private static (QuickStatDatabase Database, FakeSqlSession Session) Create(SqlOptions? options = null)
    {
        FakeSqlSession session = new();

        QuickStatDatabase database = new(
            session,
            new ColonToAtSqlTextRewriter(),
            options ?? new SqlOptions { RetryBaseDelay = TimeSpan.Zero },
            NullLogger<QuickStatDatabase>.Instance);

        return (database, session);
    }

    // ---------------------------------------------------------------- connection state

    [Fact]
    public async Task RaisesBeforeAnyProjectIsSelected()
    {
        (QuickStatDatabase database, _) = Create();

        await Assert.ThrowsAsync<DatabaseNotConnectedException>(
            () => database.QueryAsync(new SqlRequest { CommandText = "SELECT 1", Label = "probe" }));
    }

    [Fact]
    public async Task RaisesAfterDisconnecting()
    {
        (QuickStatDatabase database, _) = Create();

        await database.ConnectAsync(Connection);
        await database.DisconnectAsync();

        Assert.False(database.IsConnected);

        await Assert.ThrowsAsync<DatabaseNotConnectedException>(
            () => database.ExecuteAsync(new SqlRequest { CommandText = "SELECT 1" }));
    }

    [Fact]
    public async Task AppliesTheSessionOptionsBeforeTheFirstUserQuery()
    {
        // PORT-PLAN.md §7.2. In the Delphi, SET DATEFORMAT ymd ran in login observer #2, after
        // TSimpleDatabase.Connect had already issued SELECT @@SERVERNAME, DB_NAME() and after
        // observer #1's EXEC dbo.GetStudyAndUser (Emetra.Database.Info.pas:147).
        (QuickStatDatabase database, FakeSqlSession session) = Create();

        await database.ConnectAsync(Connection);
        _ = await database.QueryAsync(new SqlRequest { CommandText = "EXEC dbo.GetStudyAndUser :StudyName", Values = ["NDV"] });

        Assert.Equal(SqlClientSession.SessionOptionsBatch, session.Statements[0]);
        Assert.Contains("SET DATEFORMAT ymd", session.Statements[0], StringComparison.Ordinal);
        Assert.Equal("EXEC dbo.GetStudyAndUser @StudyName", session.Statements[1]);
    }

    [Fact]
    public async Task ReAppliesTheSessionOptionsWhenTheConnectionIsReplaced()
    {
        (QuickStatDatabase database, FakeSqlSession session) = Create();

        await database.ConnectAsync(Connection);
        await database.ConnectAsync(Connection);

        Assert.Equal(2, session.OpenCount);
        Assert.Equal(2, session.Statements.Count(s => s == SqlClientSession.SessionOptionsBatch));
    }

    // ---------------------------------------------------------------- binding and rewriting

    [Fact]
    public async Task RewritesAndBindsBeforeReachingTheSession()
    {
        (QuickStatDatabase database, FakeSqlSession session) = Create();

        await database.ConnectAsync(Connection);

        _ = await database.QueryAsync(new SqlRequest
        {
            CommandText = "SELECT * FROM dbo.Study WHERE StudName = :StudyName",
            Values = ["NDV"],
        });

        BoundSqlCommand command = Assert.Single(session.Commands);

        Assert.Equal("SELECT * FROM dbo.Study WHERE StudName = @StudyName", command.CommandText);
        Assert.Equal("StudyName", Assert.Single(command.Parameters).Name);
        Assert.Equal("NDV", command.Parameters[0].Value);
    }

    [Fact]
    public async Task ABindingFailureNeverReachesTheSession()
    {
        (QuickStatDatabase database, FakeSqlSession session) = Create();

        await database.ConnectAsync(Connection);

        await Assert.ThrowsAsync<SqlParameterBindingException>(
            () => database.QueryAsync(new SqlRequest { CommandText = "SELECT :A, :B", Values = [1] }));

        Assert.Empty(session.Commands);
    }

    // ---------------------------------------------------------------- results

    [Fact]
    public async Task ReturnsTheMaterialisedResultSet()
    {
        (QuickStatDatabase database, FakeSqlSession session) = Create();

        _ = session.Returns(SqlResultSet.Create(["StudyId"], [7]));

        await database.ConnectAsync(Connection);

        SqlResultSet result = await database.QueryAsync(new SqlRequest { CommandText = "SELECT StudyId" });

        Assert.Equal(7, result[0].GetInt32(0));
    }

    [Fact]
    public async Task ReturnsTheRealRowsAffected()
    {
        // The Delphi always returned the literal 1 (Emetra.Database.Simple.pas:493).
        (QuickStatDatabase database, FakeSqlSession session) = Create();

        _ = session.Returns(37);

        await database.ConnectAsync(Connection);

        Assert.Equal(37, await database.ExecuteAsync(new SqlRequest { CommandText = "DELETE FROM t" }));
    }

    [Theory]
    [InlineData(7, 7)]
    [InlineData("7", 7)]
    [InlineData(7L, 7)]
    public async Task ConvertsScalarResults(object value, int expected)
    {
        (QuickStatDatabase database, FakeSqlSession session) = Create();

        _ = session.Returns(value);

        await database.ConnectAsync(Connection);

        Assert.Equal(expected, await database.ScalarAsync<int>(new SqlRequest { CommandText = "SELECT 7" }));
    }

    [Fact]
    public void AScalarNullBecomesTheDefault()
    {
        Assert.Equal(0, QuickStatDatabase.ConvertScalar<int>(null));
        Assert.Equal(0, QuickStatDatabase.ConvertScalar<int>(DBNull.Value));
        Assert.Null(QuickStatDatabase.ConvertScalar<int?>(DBNull.Value));
        Assert.Null(QuickStatDatabase.ConvertScalar<string>(null));
    }

    // ---------------------------------------------------------------- retry

    [Fact]
    public async Task RetriesATransientFailureOnAnIdempotentRead()
    {
        (QuickStatDatabase database, FakeSqlSession session) = Create();

        _ = session
            .Throws(new SqlCommandFailedException("transport") { Number = 10054 })
            .Returns(SqlResultSet.Create(["Value"], [1]));

        await database.ConnectAsync(Connection);

        SqlResultSet result = await database.QueryAsync(new SqlRequest
        {
            CommandText = "SELECT 1",
            IsIdempotent = true,
        });

        Assert.Equal(1, result[0].GetInt32(0));
        Assert.Equal(2, session.Commands.Count);
    }

    [Fact]
    public async Task DoesNotRetryANonIdempotentCommand()
    {
        (QuickStatDatabase database, FakeSqlSession session) = Create();

        _ = session.Throws(new SqlCommandFailedException("transport") { Number = 10054 });

        await database.ConnectAsync(Connection);

        _ = await Assert.ThrowsAsync<SqlCommandFailedException>(() => database.ExecuteAsync(new SqlRequest
        {
            CommandText = "EXEC Report.AddSelectionMember :SelectionId, :PersonId",
            Values = [1, 2],
            IsIdempotent = false,
        }));

        Assert.Single(session.Commands);
    }

    [Fact]
    public async Task DoesNotRetryARealFailure()
    {
        (QuickStatDatabase database, FakeSqlSession session) = Create();

        _ = session.Throws(new SqlPrivilegeException("denied") { Number = 229 });

        await database.ConnectAsync(Connection);

        _ = await Assert.ThrowsAsync<SqlPrivilegeException>(
            () => database.QueryAsync(new SqlRequest { CommandText = "SELECT 1", IsIdempotent = true }));

        Assert.Single(session.Commands);
    }

    [Fact]
    public async Task GivesUpAfterTheConfiguredAttemptCount()
    {
        (QuickStatDatabase database, FakeSqlSession session) =
            Create(new SqlOptions { MaxRetryAttempts = 3, RetryBaseDelay = TimeSpan.Zero });

        for (int i = 0; i < 5; i++)
        {
            _ = session.Throws(new SqlCommandFailedException("transport") { Number = 10054 });
        }

        await database.ConnectAsync(Connection);

        _ = await Assert.ThrowsAsync<SqlCommandFailedException>(
            () => database.QueryAsync(new SqlRequest { CommandText = "SELECT 1", IsIdempotent = true }));

        Assert.Equal(3, session.Commands.Count);
    }

    [Fact]
    public async Task ReconnectsBeforeRetryingWhenTheConnectionDidNotSurvive()
    {
        // The half Emetra.Database.Simple.pas:662 was missing: it disconnected and hoped the
        // provider would re-open implicitly, which would also have dropped the session options.
        (QuickStatDatabase database, FakeSqlSession session) = Create();

        _ = session
            .Throws(new SqlCommandFailedException("transport") { Number = 10054 })
            .Returns(SqlResultSet.Empty);

        await database.ConnectAsync(Connection);
        session.IsUsable = false;

        _ = await database.QueryAsync(new SqlRequest { CommandText = "SELECT 1", IsIdempotent = true });

        Assert.Equal(1, session.ReopenCount);
        Assert.Equal(2, session.Statements.Count(s => s == SqlClientSession.SessionOptionsBatch));
    }

    [Fact]
    public async Task DoesNotReconnectWhenTheConnectionIsStillGood()
    {
        (QuickStatDatabase database, FakeSqlSession session) = Create();

        _ = session
            .Throws(new SqlCommandFailedException("timeout") { Number = -2 })
            .Returns(SqlResultSet.Empty);

        await database.ConnectAsync(Connection);

        _ = await database.QueryAsync(new SqlRequest { CommandText = "SELECT 1", IsIdempotent = true });

        Assert.Equal(1, session.UsabilityProbeCount);
        Assert.Equal(0, session.ReopenCount);
    }

    // ---------------------------------------------------------------- cancellation

    [Fact]
    public async Task PropagatesCancellationAndVerifiesTheConnection()
    {
        (QuickStatDatabase database, FakeSqlSession session) = Create();

        _ = session.Throws(new OperationCanceledException());

        await database.ConnectAsync(Connection);

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => database.QueryAsync(new SqlRequest { CommandText = "SELECT 1", IsIdempotent = true }));

        Assert.Equal(1, session.UsabilityProbeCount);
    }

    [Fact]
    public async Task ReleasesTheGateAfterAFailureSoLaterCallsStillWork()
    {
        (QuickStatDatabase database, FakeSqlSession session) = Create();

        _ = session
            .Throws(new SqlCommandFailedException("bad") { Number = 208 })
            .Returns(SqlResultSet.Create(["Ok"], [1]));

        await database.ConnectAsync(Connection);

        _ = await Assert.ThrowsAsync<SqlCommandFailedException>(
            () => database.QueryAsync(new SqlRequest { CommandText = "SELECT 1" }));

        SqlResultSet result = await database.QueryAsync(new SqlRequest { CommandText = "SELECT 2" });

        Assert.Equal(1, result[0].GetInt32(0));
    }

    // ---------------------------------------------------------------- serialisation

    [Fact]
    public async Task SerialisesConcurrentCallers()
    {
        // The Delphi could not produce two live result sets because FastQuery returned one shared
        // TADOQuery; every consumer was written against that. Concurrency has to serialise here
        // rather than corrupt the connection.
        FakeSqlSession session = new();
        SemaphoreSlim release = new(0, 1);
        int concurrent = 0;
        int peak = 0;

        _ = session.Answers(command =>
        {
            int now = Interlocked.Increment(ref concurrent);
            peak = Math.Max(peak, now);
            release.Wait();
            Interlocked.Decrement(ref concurrent);
            return SqlResultSet.Empty;
        });

        _ = session.Answers(command =>
        {
            int now = Interlocked.Increment(ref concurrent);
            peak = Math.Max(peak, now);
            Interlocked.Decrement(ref concurrent);
            return SqlResultSet.Empty;
        });

        QuickStatDatabase database = new(
            session,
            new ColonToAtSqlTextRewriter(),
            new SqlOptions(),
            NullLogger<QuickStatDatabase>.Instance);

        await database.ConnectAsync(Connection);

        Task<SqlResultSet> first = Task.Run(() => database.QueryAsync(new SqlRequest { CommandText = "SELECT 1" }));

        // Let the first caller get inside the gate before the second one arrives.
        for (int i = 0; Volatile.Read(ref concurrent) == 0 && i < 500; i++)
        {
            await Task.Delay(10);
        }

        Assert.Equal(1, Volatile.Read(ref concurrent));

        Task<SqlResultSet> second = Task.Run(() => database.QueryAsync(new SqlRequest { CommandText = "SELECT 2" }));

        await Task.Delay(20);
        _ = release.Release();

        _ = await Task.WhenAll(first, second);

        Assert.Equal(1, peak);
        Assert.Equal(2, session.Commands.Count);
    }
}
