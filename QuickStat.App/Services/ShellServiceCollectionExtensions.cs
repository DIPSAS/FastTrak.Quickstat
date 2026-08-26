using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuickStat.Diagnostics;
using QuickStat.Domain.Populations;
using QuickStat.ViewModels;

namespace QuickStat.Services;

/// <summary>Registers step 3.1: the shell services, the view-models, and the two WPF seams.</summary>
/// <remarks>
/// <para>
/// One extension method in a file step 3.1 owns, called from the Phase 3 anchor in
/// <c>App.xaml.cs</c> - the same shape as the seven Phase 2 extensions, so parallel steps never edit
/// the same lines. <b>Wave-2 steps add their own <c>AddQuickStat*</c> method rather than editing
/// this one.</b>
/// </para>
/// <para>
/// Everything is <c>TryAdd</c>, so order does not matter and a later <c>Replace</c> wins - with one
/// deliberate exception, <see cref="IUserNotificationPresenter"/>, which <em>is</em> a
/// <c>Replace</c>: <c>AddQuickStatDiagnostics</c> has already registered the headless default, and
/// <c>TryAdd</c> would silently lose to it. <c>Replace</c> also works when nothing is there yet, so
/// this method is still order-independent.
/// </para>
/// <para>
/// The view-models are singletons because the shell has exactly one of each and they hold state a
/// second instance would fork - the same argument as <c>PersonMatrix</c>. The two dialog view-models
/// are transient: a dialog gets a fresh one per showing, which is what removes the Delphi's
/// "the form is created once and reused, so the fields keep their contents" problem (§E).
/// </para>
/// </remarks>
public static class ShellServiceCollectionExtensions
{
    /// <summary>Adds the shell.</summary>
    /// <param name="services">The container being configured.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddQuickStatShell(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // --- platform seams ---------------------------------------------------------------
        services.TryAddSingleton<IUiDispatcher, WpfUiDispatcher>();
        services.TryAddSingleton<IFileDialogService, WpfFileDialogService>();
        services.TryAddSingleton<IProcessLauncher, ShellProcessLauncher>();
        services.TryAddSingleton<IMonitorLayout, SystemMonitorLayout>();
        services.TryAddSingleton<IApplicationInfo, AssemblyApplicationInfo>();

        // --- shell state ------------------------------------------------------------------
        services.TryAddSingleton<ShellProgress>();
        services.TryAddSingleton<IShellProgress>(static provider => provider.GetRequiredService<ShellProgress>());

        // Registered under IProgress<OperationProgress> as well, because that is the type
        // QuickStat.Core's login pipeline and collector runner take.  One instance, two faces: a
        // wave-2 step can inject whichever it needs and they cannot disagree.
        services.TryAddSingleton<IProgress<OperationProgress>>(
            static provider => provider.GetRequiredService<ShellProgress>());

        services.TryAddSingleton<IShellWorkspace, ShellWorkspace>();
        services.TryAddSingleton<IWindowStateService, WindowStateService>();
        services.TryAddSingleton<IConnectionCoordinator, ConnectionCoordinator>();

        // --- the two seams Phase 2 left for the UI ----------------------------------------
        services.Replace(ServiceDescriptor.Singleton<IUserNotificationPresenter, WpfNotificationPresenter>());
        services.TryAddSingleton<IPeriodPrompt, WpfPeriodPrompt>();

        // --- view-models ------------------------------------------------------------------
        services.TryAddSingleton<MainViewModel>();
        services.TryAddSingleton<DatasetViewModel>();
        services.TryAddSingleton<PopulationTabViewModel>();
        services.TryAddSingleton<PopulationPickerViewModel>();
        services.TryAddSingleton<CollectionsTabViewModel>();
        services.TryAddSingleton<PackagesTabViewModel>();
        services.TryAddSingleton<BusyOverlayViewModel>();

        services.TryAddTransient<SaveSpecViewModel>();
        services.TryAddTransient<PeriodViewModel>();

        return services;
    }
}
