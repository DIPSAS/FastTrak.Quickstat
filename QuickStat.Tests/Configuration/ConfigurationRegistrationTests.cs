using Microsoft.Extensions.DependencyInjection;
using QuickStat.Configuration;
using Xunit;

namespace QuickStat.Tests.Configuration;

/// <summary>
/// <see cref="ConfigurationServiceCollectionExtensions"/> and the <see cref="SqlOptions"/> values
/// other Phase 2 steps read.
/// </summary>
public class ConfigurationRegistrationTests
{
    [Fact]
    public void RegistersTheWholeConfigurationLayer()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddQuickStatConfiguration()
            .BuildServiceProvider();

        Assert.IsType<UdlReader>(provider.GetRequiredService<IUdlReader>());
        Assert.IsType<XmlConnectionCatalog>(provider.GetRequiredService<IConnectionCatalog>());
        Assert.IsType<OleDbConnectionStringTranslator>(provider.GetRequiredService<IConnectionStringTranslator>());
        Assert.NotNull(provider.GetRequiredService<SqlOptions>());
    }

    [Fact]
    public void EverythingIsASingleton()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddQuickStatConfiguration()
            .BuildServiceProvider();

        Assert.Same(
            provider.GetRequiredService<IConnectionStringTranslator>(),
            provider.GetRequiredService<IConnectionStringTranslator>());
    }

    [Fact]
    public void AnAlreadyRegisteredSqlOptionsInstanceWins()
    {
        // TryAdd throughout, so the composition root can supply options built from user settings.
        SqlOptions configured = new()
        {
            ApplicationName = "Configured elsewhere",
        };

        ServiceCollection services = new();

        services.AddSingleton(configured);
        services.AddQuickStatConfiguration();

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Same(configured, provider.GetRequiredService<SqlOptions>());
    }

    [Fact]
    public void TheDefaultsOtherStepsReadAreWhatTheyExpect()
    {
        // Docs/Port/06-contracts.md §3: step 2.3 (national ids) and step 2.4 (every {IdList}
        // collector) read these three and never write them. Pinned here so a change is visible.
        SqlOptions options = new();

        Assert.Equal("Report.PersonIdList", options.PersonIdListTypeName);
        Assert.Equal("PersonId", options.PersonIdListColumnName);
        Assert.Equal(1000, options.MaxIdsPerBatch);
    }

    [Fact]
    public void TheEncryptionDefaultIsTheCompatibilityPairAndNotVerifiedTls()
    {
        // PORT-PLAN.md §8.2 / R1. TrustServerCertificate=True is deliberate and is not a security
        // improvement; changing this one string is what would break every existing installation.
        Assert.Equal("Encrypt=True;TrustServerCertificate=True", new SqlOptions().DefaultEncryptionOptions);
    }
}
