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
}
