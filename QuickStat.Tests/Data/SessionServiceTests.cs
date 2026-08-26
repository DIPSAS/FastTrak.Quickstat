using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Configuration;
using QuickStat.Data;
using QuickStat.Tests.Data.Fakes;
using Xunit;

namespace QuickStat.Tests.Data;

/// <summary>
/// Connect, run the pipeline, freeze the session, disconnect.
/// </summary>
public class SessionServiceTests
{
    private static readonly QuickStatConnection Entry = new()
    {
        Name = "Testdatabase (NDV)",
        StudyName = "NDV",
        ConnectionString = @"FILE NAME=.\FastTrak.UDL",
    };

    private static (SessionService Service, FakeSqlSession Session, List<string> Executed) Create(
        params ILoginStep[] steps)
    {
        FakeSqlSession session = new();
        List<string> executed = [];

        QuickStatDatabase database = new(
            session,
            new ColonToAtSqlTextRewriter(),
            new SqlOptions(),
            NullLogger<QuickStatDatabase>.Instance);

        SessionService service = new(
            database,
            new StubTranslator(),
            steps,
            NullLogger<SessionService>.Instance);

        foreach (ILoginStep step in steps)
        {
            if (step is RecordingStep recording)
            {
                recording.Executed = executed;
            }
        }

        return (service, session, executed);
    }

    [Fact]
    public async Task RunsTheStepsInAscendingOrderRegardlessOfRegistrationOrder()
    {
        (SessionService service, _, List<string> executed) = Create(
            new RecordingStep("third", 300),
            new RecordingStep("first", 0),
            new RecordingStep("second", 100));

        _ = await service.ConnectAsync(Entry);

        Assert.Equal<string>(["first", "second", "third"], [.. executed]);
        Assert.Equal<string>(["first", "second", "third"], [.. service.Steps.Select(step => step.Name)]);
    }

    [Fact]
    public async Task FreezesTheContextIntoTheSession()
    {
        (SessionService service, _, _) = Create(new PopulatingStep());

        SessionContext session = await service.ConnectAsync(Entry);

        Assert.Equal("NDV", session.StudyName);
        Assert.Equal(7, session.StudyId);
        Assert.Equal(99, session.SessionId);
        Assert.Equal("jdoe", session.User.UserName);
        Assert.Equal(18300, session.Database.DbVersion);
        Assert.Equal("SQL01", session.ServerName);
        Assert.Equal("EFT00028", session.DatabaseName);
        Assert.True(service.IsConnected);
        Assert.Same(session, service.Current);
    }

    [Fact]
    public async Task RaisesSessionChangedOnConnectAndOnDisconnect()
    {
        (SessionService service, _, _) = Create(new PopulatingStep());

        List<SessionContext?> observed = [];
        service.SessionChanged += (_, session) => observed.Add(session);

        _ = await service.ConnectAsync(Entry);
        await service.DisconnectAsync();

        Assert.Equal(2, observed.Count);
        Assert.NotNull(observed[0]);
        Assert.Null(observed[1]);
    }

    [Fact]
    public async Task AFailingStepLeavesTheServiceDisconnected()
    {
        // In the Delphi a failing observer aborted the login but left the ADO connection open, so
        // Connected answered true afterwards (Docs/Port/01-data-access.md §1.6).
        (SessionService service, FakeSqlSession session, _) = Create(
            new PopulatingStep(),
            new ThrowingStep());

        _ = await Assert.ThrowsAsync<SqlCommandFailedException>(() => service.ConnectAsync(Entry));

        Assert.False(service.IsConnected);
        Assert.Null(service.Current);
        Assert.False(session.IsOpen);
    }

    [Fact]
    public async Task ClosesTheSessionRowOnDisconnect()
    {
        (SessionService service, FakeSqlSession fake, _) = Create(new PopulatingStep());

        _ = await service.ConnectAsync(Entry);
        await service.DisconnectAsync();

        // Delphi TCRFStudyContext.CloseSession (CRF.Context.Session.pas:237-247) with both counters
        // at zero, because QuickStat never increments them.
        BoundSqlCommand close = fake.Commands[^1];

        Assert.Equal("EXEC dbo.CloseSession @SessId,@Updates,@Inserts", close.CommandText);
        Assert.Equal<object?>([99, 0, 0], [.. close.Parameters.Select(p => p.Value)]);
    }

    [Fact]
    public async Task DisconnectingTwiceIsHarmless()
    {
        (SessionService service, _, _) = Create(new PopulatingStep());

        _ = await service.ConnectAsync(Entry);
        await service.DisconnectAsync();
        await service.DisconnectAsync();

        Assert.False(service.IsConnected);
    }

    [Fact]
    public async Task DisconnectingWithoutASessionIsHarmless()
    {
        (SessionService service, _, _) = Create();

        await service.DisconnectAsync();

        Assert.False(service.IsConnected);
    }

    [Fact]
    public async Task AFailingCloseSessionDoesNotBlockDisconnecting()
    {
        (SessionService service, FakeSqlSession fake, _) = Create(new PopulatingStep());

        _ = await service.ConnectAsync(Entry);
        _ = fake.Throws(new SqlCommandFailedException("dbo.CloseSession is gone"));

        await service.DisconnectAsync();

        Assert.False(service.IsConnected);
    }

    [Fact]
    public async Task ConnectingAgainClosesThePreviousSessionFirst()
    {
        (SessionService service, FakeSqlSession fake, _) = Create(new PopulatingStep(), new PopulatingStep(1));

        _ = await service.ConnectAsync(Entry);
        _ = await service.ConnectAsync(Entry);

        Assert.Contains(fake.Statements, s => s.StartsWith("EXEC dbo.CloseSession", StringComparison.Ordinal));
        Assert.Equal(2, fake.OpenCount);
    }

    private sealed class StubTranslator : IConnectionStringTranslator
    {
        public ResolvedConnectionString Translate(QuickStatConnection connection) => new()
        {
            Source = connection,
            Value = "Data Source=localhost;Initial Catalog=EFT00028;Integrated Security=True",
            Redacted = "Data Source=localhost;Initial Catalog=EFT00028;Integrated Security=True",
        };
    }

    private sealed class RecordingStep : ILoginStep
    {
        public RecordingStep(string name, int order)
        {
            Name = name;
            Order = order;
        }

        public string Name { get; }

        public int Order { get; }

        public List<string> Executed { get; set; } = [];

        public Task ExecuteAsync(LoginContext context, CancellationToken cancellationToken = default)
        {
            Executed.Add(Name);
            return Task.CompletedTask;
        }
    }

    private sealed class PopulatingStep : ILoginStep
    {
        public PopulatingStep(int order = 0) => Order = order;

        public string Name => "Populate";

        public int Order { get; }

        public Task ExecuteAsync(LoginContext context, CancellationToken cancellationToken = default)
        {
            context.StudyId = 7;
            context.SessionId = 99;
            context.User = new StudyUser { UserId = 42, UserName = "jdoe" };
            context.Database = new DatabaseInfo { DbVersion = 18300 };
            context.ServerName = "SQL01";
            context.DatabaseName = "EFT00028";
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingStep : ILoginStep
    {
        public string Name => "Throw";

        public int Order => 900;

        public Task ExecuteAsync(LoginContext context, CancellationToken cancellationToken = default) =>
            throw new SqlCommandFailedException("dbo.GetDatabaseInfo does not exist");
    }
}
