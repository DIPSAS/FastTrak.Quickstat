using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickStat.Collectors;
using QuickStat.Configuration;
using QuickStat.Data;
using QuickStat.Diagnostics;
using QuickStat.Domain.Matrix;
using QuickStat.Domain.Populations;
using QuickStat.Export;
using QuickStat.Services;
using QuickStat.ViewModels;
using Xunit;

namespace QuickStat.Tests.Ui.Shell;

/// <summary>
/// The composition root resolves. This is the test that catches a dependency cycle or a missing
/// registration before someone finds it by double-clicking the executable.
/// </summary>
/// <remarks>
/// Composed exactly as <c>App.xaml.cs</c> composes it: the seven Phase 2 extensions and then
/// <c>AddQuickStatShell</c>. <c>MainWindow</c> itself is left out - it needs an STA thread and an
/// <see cref="System.Windows.Application"/>, and everything interesting about it is in the
/// view-models it takes.
/// </remarks>
public class ShellCompositionTests
{
    /// <summary>Composes the container exactly as <c>App.xaml.cs</c> does.</summary>
    /// <param name="customise">
    /// Runs after every extension, so a registration made here <b>replaces</b> the shell's - last
    /// wins in this container. For swapping a WPF seam (dispatcher, save dialog, notification
    /// presenter) out of a test that has no window; leave it null for the graph the product uses.
    /// </param>
    /// <returns>A validated provider the caller must dispose.</returns>
    /// <remarks>
    /// Internal rather than private because <see cref="ViewInstantiationTests"/> needs the same
    /// graph to give each view the view-model the shell would give it, and a second transcription of
    /// the seven extension calls is a second thing that can drift from <c>App.xaml.cs</c>. The
    /// live-database tests need it for the same reason: what they are checking is that the product's
    /// own composition carries a national id end to end, which a bespoke graph would not show.
    /// </remarks>
    internal static ServiceProvider Build(Action<IServiceCollection>? customise = null)
    {
        ServiceCollection services = new();

        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None));

        services.AddQuickStatConfiguration();
        services.AddQuickStatData();
        services.AddQuickStatDomain();
        services.AddQuickStatCollectors();
        services.AddQuickStatMatrix();
        services.AddQuickStatExport();
        services.AddQuickStatDiagnostics();

        services.AddQuickStatShell();

        customise?.Invoke(services);

        // Validated on build, which is what turns a cycle into a readable failure here rather than a
        // stack overflow at start-up.
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    [Fact]
    public void TheWholeGraphResolves()
    {
        using ServiceProvider provider = Build();

        Assert.NotNull(provider.GetRequiredService<MainViewModel>());
    }

    [Fact]
    public void EveryShellServiceIsRegistered()
    {
        using ServiceProvider provider = Build();

        Assert.NotNull(provider.GetRequiredService<IUiDispatcher>());
        Assert.NotNull(provider.GetRequiredService<IFileDialogService>());
        Assert.NotNull(provider.GetRequiredService<IProcessLauncher>());
        Assert.NotNull(provider.GetRequiredService<IMonitorLayout>());
        Assert.NotNull(provider.GetRequiredService<IApplicationInfo>());
        Assert.NotNull(provider.GetRequiredService<IShellProgress>());
        Assert.NotNull(provider.GetRequiredService<IShellWorkspace>());
        Assert.NotNull(provider.GetRequiredService<IWindowStateService>());
        Assert.NotNull(provider.GetRequiredService<IConnectionCoordinator>());
    }

    [Fact]
    public void EveryWaveTwoViewModelIsRegistered()
    {
        // The handover: each of these is a stub with an owner named in its header comment, and each
        // is already resolvable, so a wave-2 step starts by filling one in rather than by wiring it.
        using ServiceProvider provider = Build();

        Assert.NotNull(provider.GetRequiredService<PopulationTabViewModel>());
        Assert.NotNull(provider.GetRequiredService<PopulationPickerViewModel>());
        Assert.NotNull(provider.GetRequiredService<CollectionsTabViewModel>());
        Assert.NotNull(provider.GetRequiredService<PackagesTabViewModel>());
        Assert.NotNull(provider.GetRequiredService<DatasetViewModel>());
        Assert.NotNull(provider.GetRequiredService<BusyOverlayViewModel>());
        Assert.NotNull(provider.GetRequiredService<SaveSpecViewModel>());
        Assert.NotNull(provider.GetRequiredService<PeriodViewModel>());
    }

    [Fact]
    public void TheProgressServiceHasOneInstanceUnderBothOfItsFaces()
    {
        // Core takes IProgress<OperationProgress>; the shell reads IShellProgress.  Two instances
        // would mean a banner that never moves.
        using ServiceProvider provider = Build();

        Assert.Same(
            provider.GetRequiredService<IShellProgress>(),
            provider.GetRequiredService<IProgress<OperationProgress>>());
    }

    [Fact]
    public void TheWorkspaceAndTheGridShareOneMatrix()
    {
        using ServiceProvider provider = Build();

        Assert.Same(
            provider.GetRequiredService<PersonMatrix>(),
            provider.GetRequiredService<IShellWorkspace>().Matrix);
    }

    [Fact]
    public void TheWpfNotificationPresenterReplacesTheHeadlessDefault()
    {
        using ServiceProvider provider = Build();

        Assert.IsType<WpfNotificationPresenter>(provider.GetRequiredService<IUserNotificationPresenter>());
    }

    [Fact]
    public void UserNotifierItselfIsNotReimplemented()
    {
        // PORT-PLAN.md §5: severity mapping, PII redaction and the never-fail-open rule stay in
        // QuickStat.Core, and a Core test already asserts UserNotifier is the only non-abstract
        // implementation there.  This is the App-side half of that promise.
        using ServiceProvider provider = Build();

        Assert.IsType<UserNotifier>(provider.GetRequiredService<IUserNotifier>());
    }

    [Fact]
    public void ThePeriodPromptIsRegisteredByTheShell()
    {
        // Step 2.3 declared IPeriodPrompt and deliberately left it unregistered because it shows a
        // window.  Without this the population loader would fail to resolve.
        using ServiceProvider provider = Build();

        Assert.IsType<WpfPeriodPrompt>(provider.GetRequiredService<IPeriodPrompt>());
    }

    [Fact]
    public void TheCaptionLoaderIsReachableFromTheShell()
    {
        // Nothing called it before Phase 3 (PORT-PLAN.md §8.8); IConnectionCoordinator does now.
        using ServiceProvider provider = Build();

        Assert.NotNull(provider.GetRequiredService<ICaptionLoader>());
    }

    [Fact]
    public void TheShellViewModelsAreSingletonsAndTheDialogsAreNot()
    {
        using ServiceProvider provider = Build();

        Assert.Same(provider.GetRequiredService<MainViewModel>(), provider.GetRequiredService<MainViewModel>());
        Assert.Same(provider.GetRequiredService<DatasetViewModel>(), provider.GetRequiredService<DatasetViewModel>());

        // A fresh view-model per showing is what removes the Delphi's "the form is created once and
        // reused, so the fields keep their contents" problem (05-ui-spec.md §E).
        Assert.NotSame(provider.GetRequiredService<SaveSpecViewModel>(), provider.GetRequiredService<SaveSpecViewModel>());
    }

    [Fact]
    public void TheShellExtensionIsIdempotent()
    {
        // Everything is TryAdd, so calling it twice - or after a wave-2 extension that happens to
        // register something first - must not double up or throw.
        ServiceCollection services = new();

        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None));
        services.AddQuickStatConfiguration();
        services.AddQuickStatData();
        services.AddQuickStatDomain();
        services.AddQuickStatCollectors();
        services.AddQuickStatMatrix();
        services.AddQuickStatExport();
        services.AddQuickStatDiagnostics();
        services.AddQuickStatShell();
        services.AddQuickStatShell();

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        Assert.NotNull(provider.GetRequiredService<MainViewModel>());
        Assert.Single(provider.GetServices<IShellWorkspace>());
    }

    [Fact]
    public void TheShellCanBeRegisteredBeforeDiagnosticsAndStillWins()
    {
        // Order independence is the whole point of the TryAdd convention, and Replace has to hold up
        // in both directions: called first it simply adds, and the later TryAdd then loses.
        ServiceCollection services = new();

        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None));
        services.AddQuickStatShell();
        services.AddQuickStatConfiguration();
        services.AddQuickStatData();
        services.AddQuickStatDomain();
        services.AddQuickStatCollectors();
        services.AddQuickStatMatrix();
        services.AddQuickStatExport();
        services.AddQuickStatDiagnostics();

        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        Assert.IsType<WpfNotificationPresenter>(provider.GetRequiredService<IUserNotificationPresenter>());
    }
}
