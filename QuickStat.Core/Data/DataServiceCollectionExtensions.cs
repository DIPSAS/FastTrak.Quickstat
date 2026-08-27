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

        // ORDER MATTERS, and it is not obvious. ISqlExecutor is the *primary* registration and the
        // concrete type is the alias, not the other way round.
        //
        // ServiceProvider disposes singletons in reverse order of *capture*, and it captures once per
        // descriptor that yields the instance - a factory alias to a disposable singleton therefore
        // registers a second disposal slot at whatever moment the alias is first resolved. With the
        // registrations the other way round, SessionService took the concrete type (capturing it
        // early), some repository later asked for ISqlExecutor (capturing the same instance again,
        // late), and the late slot was disposed first: the database was torn down *before* the
        // session that owns it. Phase 5 measured that against a live server - dbo.CloseSession never
        // ran, every UserLog row was left open, and every clean exit logged "The connection did not
        // close cleanly" twice (PORT-PLAN.md §8.11).
        //
        // This way round, SessionService cannot be constructed without resolving the alias, which
        // resolves the primary, so the database is always captured before the session regardless of
        // who asks for what first. The instance is still captured twice; both types make disposal
        // idempotent, which is what makes that harmless rather than a second teardown.
        services.TryAddSingleton<ISqlExecutor, QuickStatDatabase>();
        services.TryAddSingleton(provider => (QuickStatDatabase)provider.GetRequiredService<ISqlExecutor>());

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
