using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuickStat.Configuration;

namespace QuickStat.Data;

/// <summary>
/// Registers the SQL execution surface and the login pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The composition root calls <see cref="AddQuickStatData"/>; it does not know any of the concrete
/// types, all but one of which are internal. Step 2.2 owns this file, so no other step and no
/// phase-3 agent has to open <c>App.xaml.cs</c> to change how the data layer is wired.
/// </para>
/// <para>
/// Everything uses <c>TryAdd</c>, so the seven parallel Phase 2 registrations compose in any order
/// and a caller can pre-register its own <see cref="SqlOptions"/> or its own
/// <see cref="ISqlTextRewriter"/> and win.
/// </para>
/// </remarks>
public static class DataServiceCollectionExtensions
{
    /// <summary>Adds the data layer.</summary>
    /// <param name="services">The container being built.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <remarks>
    /// <see cref="ISessionService"/> additionally needs an
    /// <see cref="IConnectionStringTranslator"/>, which belongs to step 2.1 and is registered by
    /// that step.
    /// </remarks>
    public static IServiceCollection AddQuickStatData(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<SqlOptions>();
        services.TryAddSingleton<ISqlTextRewriter, ColonToAtSqlTextRewriter>();

        services.TryAddSingleton<ISqlSession, SqlClientSession>();
        services.TryAddSingleton<QuickStatDatabase>();
        services.TryAddSingleton<ISqlExecutor>(provider => provider.GetRequiredService<QuickStatDatabase>());

        // Registered as an unordered set; SessionService sorts by ILoginStep.Order, so a later phase
        // adds a stage with one line here and nothing else changes.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoginStep, SessionOptionsStep>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoginStep, DatabaseInfoStep>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoginStep, ActiveUserStep>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoginStep, StudySessionStep>());

        services.TryAddSingleton<SessionService>();
        services.TryAddSingleton<ISessionService>(provider => provider.GetRequiredService<SessionService>());

        return services;
    }
}
