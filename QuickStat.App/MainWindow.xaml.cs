using System.Windows;
using Microsoft.Extensions.Logging;

namespace QuickStat;

/// <summary>
/// Placeholder shell window for the .NET port skeleton.
/// </summary>
/// <remarks>
/// Resolved from the DI container by <see cref="App"/>; Phase 3 step 3.1 replaces its contents with
/// the real shell (banner, tab host, progress, splitter).
/// </remarks>
public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;

    /// <summary>
    /// Initialises the window.
    /// </summary>
    /// <param name="logger">Logger resolved from the host's container.</param>
    public MainWindow(ILogger<MainWindow> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        InitializeComponent();

        _logger.LogInformation("Main window created.");
    }
}
