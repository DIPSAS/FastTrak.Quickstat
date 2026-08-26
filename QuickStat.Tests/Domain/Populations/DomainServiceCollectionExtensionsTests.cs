using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Configuration;
using QuickStat.Data;
using QuickStat.Domain.Packages;
using QuickStat.Domain.Patients;
using QuickStat.Domain.Populations;
using Xunit;

namespace QuickStat.Tests.Domain.Populations;

/// <summary>
/// The step 2.3 registrations must produce a usable container from the dependencies steps 2.1, 2.2
/// and Phase 3 supply.
/// </summary>
public class DomainServiceCollectionExtensionsTests
{
    private static ServiceProvider BuildProvider(Action<IServiceCollection>? configure = null)
    {
        ServiceCollection services = new();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ISqlExecutor>(new RecordingSqlExecutor());
        services.AddSingleton<ISqlTextRewriter>(new StubSqlTextRewriter());
        services.AddSingleton<ISessionService>(new StubSessionService());
        services.AddSingleton<IPeriodPrompt>(new StubPeriodPrompt());

        configure?.Invoke(services);
        services.AddQuickStatDomain();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void EveryStep23ServiceResolves()
    {
        using ServiceProvider provider = BuildProvider();

        Assert.NotNull(provider.GetRequiredService<IPopulationRepository>());
        Assert.NotNull(provider.GetRequiredService<IQueryParameterResolver>());
        Assert.NotNull(provider.GetRequiredService<IPatientRepository>());
        Assert.NotNull(provider.GetRequiredService<IPackageRepository>());
    }

    [Fact]
    public void TheServicesAreSingletons()
    {
        using ServiceProvider provider = BuildProvider();

        Assert.Same(
            provider.GetRequiredService<IPatientRepository>(),
            provider.GetRequiredService<IPatientRepository>());
    }

    [Fact]
    public void SqlOptionsIsSuppliedOnlyWhenNobodyElseDid()
    {
        // SqlOptions belongs to step 2.1. Registering it defensively must not override theirs.
        SqlOptions theirs = new() { PersonIdListTypeName = "Other.IdList" };

        using ServiceProvider provider = BuildProvider(services => services.AddSingleton(theirs));

        Assert.Same(theirs, provider.GetRequiredService<SqlOptions>());
    }

    [Fact]
    public void SqlOptionsHasADefaultSoTheContainerIsUsableAlone()
    {
        using ServiceProvider provider = BuildProvider();

        Assert.NotNull(provider.GetRequiredService<SqlOptions>());
    }

    [Fact]
    public void ThePeriodPromptIsNotRegisteredHere()
    {
        // It shows a window; the UI layer owns it. Registered above only so the resolver can be built.
        ServiceCollection services = new();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddQuickStatDomain();

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IPeriodPrompt));
    }

    [Fact]
    public void NullServicesAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => DomainServiceCollectionExtensions.AddQuickStatDomain(null!));
    }
}
