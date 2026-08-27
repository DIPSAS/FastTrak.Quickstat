using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Configuration;
using QuickStat.Data;
using Xunit;

namespace QuickStat.Tests.Data;

/// <summary>
/// The one registration entry point the composition root calls.
/// </summary>
/// <remarks>
/// Step 2.2 owns this so that no Phase 3 agent has to edit <c>App.xaml.cs</c> to change how the data
/// layer is wired, and so the seven parallel Phase 2 steps never touch the same lines.
/// </remarks>
public class DataServiceCollectionExtensionsTests
{
    private static ServiceProvider Build(Action<IServiceCollection>? extra = null)
    {
        ServiceCollection services = new();

        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IConnectionStringTranslator, StubTranslator>();

        extra?.Invoke(services);

        _ = services.AddQuickStatData();

        return services.BuildServiceProvider(validateScopes: true);
    }

    [Fact]
    public void ResolvesTheExecutor()
    {
        using ServiceProvider provider = Build();

        Assert.NotNull(provider.GetRequiredService<ISqlExecutor>());
    }

    [Fact]
    public void ResolvesTheRewriter()
    {
        using ServiceProvider provider = Build();

        Assert.IsType<ColonToAtSqlTextRewriter>(provider.GetRequiredService<ISqlTextRewriter>());
    }

    [Fact]
    public void ResolvesTheSessionService()
    {
        using ServiceProvider provider = Build();

        Assert.NotNull(provider.GetRequiredService<ISessionService>());
    }

    [Fact]
    public void TheExecutorAndTheSessionServiceShareOneConnection()
    {
        using ServiceProvider provider = Build();

        ISqlExecutor executor = provider.GetRequiredService<ISqlExecutor>();
        ISessionService session = provider.GetRequiredService<ISessionService>();

        Assert.Same(executor, provider.GetRequiredService<ISqlExecutor>());
        Assert.Same(session, provider.GetRequiredService<ISessionService>());
    }

    [Fact]
    public void RegistersTheFourLoginStepsInOrder()
    {
        using ServiceProvider provider = Build();

        IEnumerable<ILoginStep> steps = provider.GetServices<ILoginStep>();

        Assert.Equal<string>(
            ["Session options", "Database information", "Active user", "Study session"],
            [.. steps.OrderBy(step => step.Order).Select(step => step.Name)]);
    }

    [Fact]
    public void ACallerCanPreRegisterItsOwnSqlOptions()
    {
        // TryAdd throughout, so the seven parallel Phase 2 registrations compose in any order.
        SqlOptions mine = new() { MaxRetryAttempts = 9 };

        using ServiceProvider provider = Build(services => services.AddSingleton(mine));

        Assert.Same(mine, provider.GetRequiredService<SqlOptions>());
    }

    [Fact]
    public void RegisteringTwiceIsHarmless()
    {
        ServiceCollection services = new();

        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IConnectionStringTranslator, StubTranslator>();

        _ = services.AddQuickStatData();
        _ = services.AddQuickStatData();

        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Equal(4, provider.GetServices<ILoginStep>().Count());
    }

    [Fact]
    public void TheContainerCanBeDisposedSynchronously()
    {
        // ServiceProvider.Dispose() throws InvalidOperationException for any singleton that
        // implements only IAsyncDisposable, and App.OnExit stops and disposes the host
        // synchronously - so an async-only disposal chain here would turn every clean exit into an
        // unhandled exception. This test is the guard on that; it caught it once already.
        ServiceProvider provider = Build();

        _ = provider.GetRequiredService<ISqlExecutor>();
        _ = provider.GetRequiredService<ISessionService>();

        provider.Dispose();
    }

    [Fact]
    public void TheSessionIsClosedBeforeTheConnectionIsDisposed()
    {
        // THE RESOLUTION ORDER BELOW IS THE TEST. Do not tidy it.
        //
        // ServiceProvider disposes in reverse order of capture and captures once per descriptor, so
        // an alias to a disposable singleton books a second disposal slot at the moment the alias is
        // first resolved. This is the app's order: the shell resolves ISessionService to connect, and
        // only afterwards does a repository ask for ISqlExecutor. With the registrations the other
        // way round that late alias was disposed first, the connection died before the session that
        // owns it, and dbo.CloseSession never ran - measured against a live server, where it left
        // every dbo.UserLog row open and logged "The connection did not close cleanly" on every
        // clean exit (PORT-PLAN.md §8.11).
        //
        // The two tests above resolve ISqlExecutor first, which is why neither of them saw it.
        RecordingSession session = new();
        ServiceProvider provider = Build(services => services.AddSingleton<ISqlSession>(session));

        _ = provider.GetRequiredService<ISessionService>();
        _ = provider.GetRequiredService<ISqlExecutor>();

        provider.Dispose();

        Assert.Equal<string>(["CloseAsync", "Dispose"], session.Calls);
    }

    [Fact]
    public void NeitherSingletonIsTornDownTwice()
    {
        // Both are captured twice - once as the interface, once as the concrete alias - so both
        // dispose paths really do run. Idempotency is what keeps that from being a second teardown,
        // and for the session service a second dbo.CloseSession for a session already closed.
        RecordingSession session = new();
        ServiceProvider provider = Build(services => services.AddSingleton<ISqlSession>(session));

        _ = provider.GetRequiredService<ISessionService>();
        _ = provider.GetRequiredService<ISqlExecutor>();

        provider.Dispose();

        Assert.Single(session.Calls, call => call == "Dispose");
        Assert.Single(session.Calls, call => call == "CloseAsync");
    }

    [Fact]
    public async Task TheContainerCanAlsoBeDisposedAsynchronously()
    {
        ServiceProvider provider = Build();

        _ = provider.GetRequiredService<ISqlExecutor>();
        _ = provider.GetRequiredService<ISessionService>();

        await provider.DisposeAsync();
    }

    [Fact]
    public void RejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => DataServiceCollectionExtensions.AddQuickStatData(null!));

    private sealed class StubTranslator : IConnectionStringTranslator
    {
        public ResolvedConnectionString Translate(QuickStatConnection connection) => new()
        {
            Source = connection,
            Value = "Data Source=localhost;Initial Catalog=EFT;Integrated Security=True",
            Redacted = "Data Source=localhost;Initial Catalog=EFT;Integrated Security=True",
        };
    }

    /// <summary>
    /// Records shutdown calls in order, which is the only thing the two disposal tests look at.
    /// </summary>
    /// <remarks>
    /// Separate from <c>Fakes/FakeSqlSession</c> on purpose: that one counts, and a count cannot tell
    /// "closed then disposed" from "disposed then closed", which is the whole question here.
    /// </remarks>
    private sealed class RecordingSession : ISqlSession
    {
        public List<string> Calls { get; } = [];

        public bool IsOpen { get; private set; }

        public Task OpenAsync(string connectionString, CancellationToken cancellationToken)
        {
            IsOpen = true;
            Calls.Add("OpenAsync");
            return Task.CompletedTask;
        }

        public Task ReopenAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> IsUsableAsync(CancellationToken cancellationToken) => Task.FromResult(true);

        public Task CloseAsync()
        {
            IsOpen = false;
            Calls.Add("CloseAsync");
            return Task.CompletedTask;
        }

        public Task<SqlResultSet> QueryAsync(BoundSqlCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<int> ExecuteAsync(BoundSqlCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<object?> ScalarAsync(BoundSqlCommand command, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Dispose()
        {
            IsOpen = false;
            Calls.Add("Dispose");
        }

        public ValueTask DisposeAsync()
        {
            IsOpen = false;
            Calls.Add("DisposeAsync");
            return ValueTask.CompletedTask;
        }
    }
}
