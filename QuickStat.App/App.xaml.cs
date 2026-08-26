using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickStat.Collectors;
using QuickStat.Configuration;
using QuickStat.Data;
using QuickStat.Diagnostics;
using QuickStat.Domain.Matrix;
using QuickStat.Domain.Populations;
using QuickStat.Export;
using QuickStat.Logging;
using QuickStat.Services;

namespace QuickStat;

/// <summary>
/// Application entry point and composition root.
/// </summary>
/// <remarks>
/// The generic host owns object lifetimes: <see cref="OnStartup"/> builds an
/// <see cref="IHost"/>, starts it, resolves the main window from the container and shows it;
/// <see cref="OnExit"/> stops and disposes the host. There is deliberately no
/// <c>StartupUri</c> in <c>App.xaml</c>.
/// </remarks>
public partial class App : Application
{
    private IHost? _host;
    private ILogger? _logger;

    /// <summary>
    /// ===================================================================================
    ///  COMPOSITION ROOT - LATER PHASES REGISTER THEIR SERVICES HERE.
    /// ===================================================================================
    /// <para>
    /// This is the single, intentional extension point for dependency-injection registrations.
    /// Each phase has its own anchor comment below; add registrations under your own anchor only,
    /// so parallel steps do not collide on the same lines.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    private static void ConfigureServices(IServiceCollection services)
    {
        // --- Phase 0: skeleton -----------------------------------------------------------
        services.AddSingleton<MainWindow>();

        // Each Phase 2 step contributes one extension method from a folder it owned, so seven
        // parallel agents never had to edit this file. Every one of them registers with TryAdd,
        // so the order below does not matter and a later Replace wins.
        services.AddQuickStatConfiguration();   // 2.1  config file, UDL, OLE DB -> ADO.NET
        services.AddQuickStatData();            // 2.2  SQL execution, login pipeline
        services.AddQuickStatDomain();          // 2.3  populations, patients, packages
        services.AddQuickStatCollectors();      // 2.4  collector framework + registry
        services.AddQuickStatMatrix();          // 2.5  matrix, datapoints, colouring
        services.AddQuickStatExport();          // 2.6  anonymisation, CSV/xlsx export
        services.AddQuickStatDiagnostics();     // 2.7  settings store, user notifier

        // --- Phase 3: views and view models ----------------------------------------------
        // (QuickStat.Views, QuickStat.ViewModels)
        //
        // Same shape as Phase 2: one AddQuickStat* extension per step, in a file that step owns, so
        // no two agents edit these lines.  Wave 2 (steps 3.2, 3.3, 3.4, 3.6) adds its own below.
        //
        // AddQuickStatShell also installs the two WPF seams Phase 2 left open:
        //   IUserNotificationPresenter -> WpfNotificationPresenter, through Replace, because
        //     AddQuickStatDiagnostics has already TryAdd-ed the headless default.  IUserNotifier
        //     itself is NOT reimplemented - severity mapping, PII redaction and the never-fail-open
        //     rule stay in QuickStat.Core.
        //   IPeriodPrompt -> WpfPeriodPrompt, which step 2.3 declared and deliberately left
        //     unregistered because it shows a window.
        services.AddQuickStatShell();       // 3.1  theme, shell, dataset tab, shared contracts
    }

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // An empty builder rather than Host.CreateApplicationBuilder: QuickStat reads its own
        // deployed QuickStat.config.xml (PORT-PLAN.md §1.1), so the appsettings.json / environment
        // machinery and the default console + EventLog providers would all be dead weight.
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            ApplicationName = "QuickStat",

            // Executable directory, not the working directory: launching from a shortcut sets an
            // arbitrary CWD (PORT-PLAN.md §4.1).
            ContentRootPath = AppContext.BaseDirectory,
        });

        // A WPF process is not a console application. The default ConsoleLifetime writes
        // "Application started. Press Ctrl+C to shut down." into QuickStat's log file and installs
        // an AppDomain.ProcessExit handler that blocks waiting for the host to stop. WPF owns the
        // application lifetime here.
        builder.Services.Replace(ServiceDescriptor.Singleton<IHostLifetime, WpfHostLifetime>());

        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddDebug();
        builder.Logging.AddFile();

        ConfigureServices(builder.Services);

        _host = builder.Build();
        _host.Start();

        _logger = _host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("QuickStat.App");
        _logger.LogInformation(
            "QuickStat starting. Base directory: {BaseDirectory}. Log directory: {LogDirectory}.",
            AppContext.BaseDirectory,
            FileLoggerProvider.DefaultLogDirectory);

        MainWindow window = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        window.Show();
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

        _logger?.LogInformation("QuickStat exiting with code {ExitCode}.", e.ApplicationExitCode);

        if (_host is not null)
        {
            // Both of these run on the thread pool rather than inline. This is the WPF UI thread
            // and its dispatcher is shutting down, so a continuation that captures the
            // synchronisation context would deadlock rather than fail - a hang on exit, which is
            // worse than a crash because it leaves the process holding the SQL connection.
            RunOnPool(() => _host.StopAsync(TimeSpan.FromSeconds(5)), "Host did not stop cleanly.");

            // Dispose asynchronously when the host supports it. ServiceProvider.Dispose() throws
            // InvalidOperationException for any singleton implementing ONLY IAsyncDisposable, so
            // the synchronous path turns every clean exit into a crash as soon as such a service
            // is registered - and QuickStat.Core registers several. Found by step 2.2's DI test.
            if (_host is IAsyncDisposable asyncDisposable)
            {
                RunOnPool(() => asyncDisposable.DisposeAsync().AsTask(), "Host did not dispose cleanly.");
            }
            else
            {
                try
                {
                    _host.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Host did not dispose cleanly.");
                }
            }

            _host = null;
        }

        _logger = null;

        base.OnExit(e);
    }

    /// <summary>
    /// Runs a shutdown step on the thread pool with a hard ceiling, logging rather than throwing.
    /// </summary>
    /// <remarks>
    /// Nothing during shutdown may propagate: an exception out of <see cref="OnExit"/> replaces a
    /// clean exit with a crash dialog after the user has already closed the window. The timeout is
    /// a backstop for a step that never completes at all.
    /// </remarks>
    private void RunOnPool(Func<Task> step, string failureMessage)
    {
        try
        {
            if (!Task.Run(step).Wait(TimeSpan.FromSeconds(10)))
            {
                _logger?.LogError("{FailureMessage} It timed out.", failureMessage);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "{FailureMessage}", failureMessage);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Report("Unhandled exception on the UI thread.", e.Exception);

        // Left unhandled on purpose. Phase 3 owns the recovery policy and the real error dialog;
        // until then a fault should be loud rather than quietly swallowed.
        e.Handled = false;
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Report(
            e.IsTerminating ? "Unhandled exception; the process is terminating." : "Unhandled exception.",
            e.ExceptionObject as Exception);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Report("Unobserved task exception.", e.Exception);

        // Observing it keeps a background fault from tearing down the process on its own schedule;
        // it is already on disk by this point.
        e.SetObserved();
    }

    /// <summary>
    /// Logs a failure and shows a bare placeholder dialog. Phase 3 step 3.6 replaces the dialog.
    /// </summary>
    private void Report(string headline, Exception? exception)
    {
        try
        {
            if (_logger is not null)
            {
                _logger.LogCritical(exception, "{Headline}", headline);
            }

            MessageBox.Show(
                string.Concat(headline, Environment.NewLine, Environment.NewLine, exception?.Message ?? "(no exception detail)"),
                "QuickStat",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception)
        {
            // Never fail while reporting a failure.
        }
    }

    /// <summary>
    /// An <see cref="IHostLifetime"/> that does nothing, because WPF - not the host - decides when
    /// this process starts and stops.
    /// </summary>
    private sealed class WpfHostLifetime : IHostLifetime
    {
        public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
