using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using QuickStat.Configuration.Settings;
using QuickStat.Diagnostics;
using Xunit;

namespace QuickStat.Tests.Diagnostics;

/// <summary>
/// What step 2.7 puts in the container, and - the point of the exercise - what the container answers
/// when nobody has configured anything.
/// </summary>
public class DiagnosticsServiceCollectionExtensionsTests
{
    [Fact]
    public async Task ADefaultContainerAnswersConfirmationsWithNo()
    {
        // The single most important assertion in this file. If a Phase 3 view model resolves
        // IUserNotifier before anyone has wired up a dialog, the delete-package confirmation must
        // still come back no.
        using ServiceProvider provider = new ServiceCollection()
            .AddQuickStatDiagnostics()
            .BuildServiceProvider();

        IUserNotifier notifier = provider.GetRequiredService<IUserNotifier>();

        Assert.False(await notifier.ConfirmAsync("Do you really want to delete this package?"));
    }

    [Fact]
    public void EverythingIsResolvableWithoutALoggingProvider()
    {
        // QuickStat.Core references only Logging.Abstractions, so a test host that never called
        // AddLogging must still be able to compose the step.
        using ServiceProvider provider = new ServiceCollection()
            .AddQuickStatDiagnostics()
            .BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IUserNotifier>());
        Assert.NotNull(provider.GetRequiredService<IUserNotificationPresenter>());
        Assert.NotNull(provider.GetRequiredService<ISettingsStore>());
    }

    [Fact]
    public void ItDoesNotStealTheOpenGenericLoggerRegistration()
    {
        // Registering NullLogger<> for ILogger<> would have been the easy way to make the previous
        // test pass, and it would silently disable logging for every service registered afterwards.
        ServiceCollection services = new();

        services.AddQuickStatDiagnostics();

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(ILogger<>));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(ILoggerFactory));
    }

    [Fact]
    public void TheDefaultsAreSingletons()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddQuickStatDiagnostics()
            .BuildServiceProvider();

        Assert.Same(provider.GetRequiredService<IUserNotifier>(), provider.GetRequiredService<IUserNotifier>());
        Assert.Same(provider.GetRequiredService<ISettingsStore>(), provider.GetRequiredService<ISettingsStore>());
    }

    [Fact]
    public void TheDefaultPresenterIsTheHeadlessOne()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddQuickStatDiagnostics()
            .BuildServiceProvider();

        Assert.IsType<HeadlessNotificationPresenter>(provider.GetRequiredService<IUserNotificationPresenter>());
    }

    [Fact]
    public async Task Phase3ReplacesThePresenterAndKeepsTheGuarantee()
    {
        // The documented seam. Replacing the presenter changes where the dialog is drawn; it does
        // not change who owns the never-fail-open rule.
        ServiceCollection services = new();

        services.AddQuickStatDiagnostics();
        services.Replace(ServiceDescriptor.Singleton<IUserNotificationPresenter>(
            _ => HeadlessNotificationPresenter.Answering(true)));

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsType<UserNotifier>(provider.GetRequiredService<IUserNotifier>());
        Assert.True(await provider.GetRequiredService<IUserNotifier>().ConfirmAsync("Ask a real user."));
    }

    [Fact]
    public void CallingItTwiceIsHarmless()
    {
        ServiceCollection services = new();

        services.AddQuickStatDiagnostics();
        services.AddQuickStatDiagnostics();

        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(IUserNotifier)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(IUserNotificationPresenter)));
        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(ISettingsStore)));
    }

    [Fact]
    public void AHostThatHasAlreadyChosenKeepsItsChoice()
    {
        ServiceCollection services = new();

        services.AddSingleton<IUserNotificationPresenter>(_ => HeadlessNotificationPresenter.Answering(true));
        services.AddQuickStatDiagnostics();

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Equal(1, services.Count(descriptor => descriptor.ServiceType == typeof(IUserNotificationPresenter)));
    }

    [Fact]
    public void ANullServiceCollectionIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => { _ = DiagnosticsServiceCollectionExtensions.AddQuickStatDiagnostics(null!); });
    }

    [Fact]
    public void TheSettingsStoreResolvesToTheUsersFileAndIsNotCreatedByResolvingIt()
    {
        // Resolving must not have the side effect of writing to a roaming profile.
        using ServiceProvider provider = new ServiceCollection()
            .AddQuickStatDiagnostics()
            .BuildServiceProvider();

        IniSettingsStore store = Assert.IsType<IniSettingsStore>(provider.GetRequiredService<ISettingsStore>());

        Assert.Equal(SettingsPath.Resolve(), store.FilePath);
        Assert.False(store.HasUnsavedChanges);
    }
}
