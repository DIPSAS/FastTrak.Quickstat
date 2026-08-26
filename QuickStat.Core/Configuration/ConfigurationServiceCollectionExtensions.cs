using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace QuickStat.Configuration;

/// <summary>
/// Registers the configuration layer: the connection catalogue, the data link reader and the
/// connection-string translator.
/// </summary>
public static class ConfigurationServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="SqlOptions"/>, <see cref="IUdlReader"/>, <see cref="IConnectionCatalog"/> and
    /// <see cref="IConnectionStringTranslator"/> as singletons.
    /// </summary>
    /// <param name="services">The container being built.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <remarks>
    /// Every registration uses <c>TryAdd</c>, so a composition root that has already supplied its own
    /// - most usefully a <see cref="SqlOptions"/> built from user settings rather than the defaults -
    /// keeps it. All four are singletons: <see cref="SqlOptions"/> is immutable process-wide state,
    /// and the other three are stateless.
    /// </remarks>
    public static IServiceCollection AddQuickStatConfiguration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(new SqlOptions());
        services.TryAddSingleton<IUdlReader, UdlReader>();
        services.TryAddSingleton<IConnectionCatalog, XmlConnectionCatalog>();
        services.TryAddSingleton<IConnectionStringTranslator, OleDbConnectionStringTranslator>();

        return services;
    }
}
