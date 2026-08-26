using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace QuickStat.Collectors;

/// <summary>Registers the collector subsystem.</summary>
/// <remarks>
/// Called from the composition root's step 2.4 anchor. The registrations use
/// <c>TryAdd</c> so that a host which has already chosen a different
/// <see cref="IPersonIdListBinder"/> keeps it.
/// </remarks>
public static class CollectorServiceCollectionExtensions
{
    /// <summary>Adds the collector registry, runner and person-id binding strategy.</summary>
    /// <param name="services">The container.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    /// <remarks>
    /// <para>
    /// <see cref="ICollectorRegistry"/> is a singleton: it holds the list for the current session
    /// and is rebuilt on every project switch, and <see cref="ICollectorRegistry.TryFind"/> has to
    /// see the same list the user is looking at.
    /// </para>
    /// <para>
    /// The registered <see cref="IPersonIdListBinder"/> is
    /// <see cref="InlineLiteralPersonIdListBinder"/>, not the table-valued one. See that type's
    /// remarks: the table type has not shipped and there is no capability probe yet, so the literal
    /// binder is the only one that works against every existing database.
    /// </para>
    /// <para>
    /// This method deliberately does not register <c>SqlOptions</c>, <c>ISqlExecutor</c> or logging
    /// - those belong to steps 2.1, 2.2 and Phase 0 respectively.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddQuickStatCollectors(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IPersonIdListBinder, InlineLiteralPersonIdListBinder>();
        services.TryAddSingleton<ICollectorRegistry, CollectorRegistry>();
        services.TryAddSingleton<ICollectorRunner, CollectorRunner>();

        return services;
    }
}
