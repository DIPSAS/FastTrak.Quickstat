using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Configuration.Settings;

namespace QuickStat.Diagnostics;

/// <summary>
/// Registers step 2.7: the settings store and the notification service.
/// </summary>
/// <remarks>
/// <para>
/// One extension for both halves of the step, because they ship together and a caller that wants
/// one always wants the other - the notifier is how the application reports that the settings file
/// could not be written.
/// </para>
/// <para>
/// Logging is optional here. Each service is created through a factory that falls back to
/// <c>NullLogger</c> when no <see cref="ILoggerFactory"/> has been registered, so
/// <c>QuickStat.Core</c> can be composed in a test without dragging in a logging provider. This is
/// deliberately <em>not</em> done by registering <c>NullLogger&lt;&gt;</c> for the open generic: that
/// would win over a real <c>AddLogging</c> that ran afterwards and silently disable all logging.
/// </para>
/// </remarks>
public static class DiagnosticsServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="ISettingsStore"/>, <see cref="IUserNotifier"/> and the default
    /// <see cref="IUserNotificationPresenter"/>.
    /// </summary>
    /// <param name="services">The container being configured.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// All three are registered with <c>TryAdd</c>, so a host that has already made its own choice
    /// keeps it. The presenter is the one Phase 3 replaces:
    /// </para>
    /// <code>
    /// services.AddQuickStatDiagnostics();
    /// services.Replace(ServiceDescriptor.Singleton&lt;IUserNotificationPresenter, WpfNotificationPresenter&gt;());
    /// </code>
    /// <para>
    /// <see cref="IUserNotifier"/> itself should not be replaced; see <see cref="UserNotifier"/> for
    /// what would be lost. Note that the safe outcome is also the default outcome: if nothing is
    /// replaced, confirmations are logged and answered no rather than failing or assuming yes.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddQuickStatDiagnostics(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IUserNotificationPresenter>(_ => new HeadlessNotificationPresenter());

        services.TryAddSingleton<IUserNotifier>(provider => new UserNotifier(
            provider.GetRequiredService<IUserNotificationPresenter>(),
            provider.GetService<ILogger<UserNotifier>>() ?? NullLogger<UserNotifier>.Instance));

        services.TryAddSingleton<ISettingsStore>(provider => IniSettingsStore.OpenDefault(
            provider.GetService<ILogger<IniSettingsStore>>()));

        return services;
    }
}
