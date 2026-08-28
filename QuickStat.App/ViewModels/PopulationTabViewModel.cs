using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using QuickStat.Configuration;
using QuickStat.Diagnostics;
using QuickStat.Services;

namespace QuickStat.ViewModels;

/// <summary>The <c>Population</c> tab: the database combo box and the embedded picker.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.2.</b> <c>05-ui-spec.md</c> §B.1. The tab owns one control with behaviour -
/// <c>cbProject</c> - and hosts <see cref="Picker"/>, which owns everything else.
/// </para>
/// <para>
/// Picking a project is the whole of the Delphi's <c>SelectConnection</c>
/// (<c>MainQuickStat.pas:495-519</c>), and that sequence lives behind
/// <see cref="IConnectionCoordinator"/>, not here: it does the status text, the busy scope, the
/// disconnect, the login <em>and</em> <see cref="QuickStat.Domain.Matrix.ICaptionLoader"/>, which
/// nothing else in the application calls.
/// </para>
/// </remarks>
public sealed partial class PopulationTabViewModel : ObservableObject
{
    /// <summary>Teal header above the combo box.</summary>
    public const string DatabaseHeader = "Select database";

    /// <summary>Teal header above the picker.</summary>
    public const string PopulationHeader = "Select population";

    /// <summary>
    /// How <c>cbProject</c> is ordered.
    /// </summary>
    /// <remarks>
    /// <c>cbProject.Sorted := true</c> (<c>MainQuickStat.pas:399</c>, <c>05-ui-spec.md</c> §G.5).
    /// <c>TStringList.Sorted</c> with the default <c>CaseSensitive = False</c> compares with
    /// <c>AnsiCompareText</c>, which is case-insensitive and locale-aware - so this, and not
    /// <c>StringComparer.Ordinal</c>. The check list uses ordinal for a specific reason that does not
    /// apply here: it has to keep the <c>^ </c>-prefixed demographic collectors first (PORT-PLAN.md
    /// §6). Connection names carry no such hack, and ordinal would sort a lower-case name after every
    /// upper-case one.
    /// </remarks>
    public static readonly StringComparer ProjectOrder = StringComparer.CurrentCultureIgnoreCase;

    private readonly IConnectionCoordinator _connections;
    private readonly IWindowStateService _windowState;
    private readonly IUserNotifier _notifier;
    private readonly ILogger<PopulationTabViewModel> _logger;

    [ObservableProperty]
    private QuickStatConnection? _selectedProject;

    /// <summary>Creates the tab's view-model and reads the connection catalogue.</summary>
    /// <param name="picker">The embedded population picker.</param>
    /// <param name="catalogue">Reads the deployed <c>QuickStat.config.xml</c>.</param>
    /// <param name="connections">Connects, and loads the captions. The whole of <c>SelectConnection</c>.</param>
    /// <param name="windowState">Remembers the last database chosen.</param>
    /// <param name="notifier">Reports a failed connect to the user.</param>
    /// <param name="logger">Log.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public PopulationTabViewModel(
        PopulationPickerViewModel picker,
        IConnectionCatalog catalogue,
        IConnectionCoordinator connections,
        IWindowStateService windowState,
        IUserNotifier notifier,
        ILogger<PopulationTabViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(picker);
        ArgumentNullException.ThrowIfNull(catalogue);
        ArgumentNullException.ThrowIfNull(connections);
        ArgumentNullException.ThrowIfNull(windowState);
        ArgumentNullException.ThrowIfNull(notifier);
        ArgumentNullException.ThrowIfNull(logger);

        Picker = picker;
        _connections = connections;
        _windowState = windowState;
        _notifier = notifier;
        _logger = logger;

        LoadProjects(catalogue);
    }

    /// <summary>The connections from <c>QuickStat.config.xml</c>, sorted by name.</summary>
    /// <remarks>
    /// <b>Nothing is preselected</b> (<c>05-ui-spec.md</c> §B.1, <c>07-ui-contracts.md</c> §2):
    /// picking an item is what triggers the connection. See <see cref="LastDatabase"/> for why the
    /// remembered database does not change that.
    /// </remarks>
    public ObservableCollection<QuickStatConnection> Projects { get; } = [];

    /// <summary>The embedded picker.</summary>
    public PopulationPickerViewModel Picker { get; }

    /// <summary>
    /// The name of the connection used last time, or <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Decision (g) of <c>Docs/Port/07-ui-contracts.md</c> §5, taken here: the last database is
    /// remembered but neither preselected nor reconnected.</b> Step 3.1 ruled out auto-connecting -
    /// it would reach the database before the user asked for anything - and left preselection open.
    /// </para>
    /// <para>
    /// Preselecting is rejected as well, for a reason that only shows up in WPF: setting
    /// <c>SelectedItem</c> without connecting leaves the combo naming a database the application is
    /// not connected to, and choosing that same entry from the drop-down raises no selection change,
    /// so the user cannot connect to it at all without picking a different entry first. That is
    /// worse than the Delphi, which simply opens with nothing chosen (<c>MainQuickStat.pas:399</c>,
    /// and §B.1's "No item is preselected in <c>cbProject</c>").
    /// </para>
    /// <para>
    /// The value is still written on every successful connect, because persisting it is step 3.1's
    /// decision (f) and this keeps the key populated. Reversal cost: two lines in the constructor -
    /// match <see cref="LastDatabase"/> against <see cref="Projects"/> and assign the backing field
    /// of <see cref="SelectedProject"/> - plus a way to re-raise the connect for an unchanged
    /// selection.
    /// </para>
    /// </remarks>
    public string? LastDatabase => _windowState.GetLastDatabase();

    /// <summary>
    /// Disconnects, connects to <paramref name="connection"/>, and loads the captions.
    /// </summary>
    /// <param name="connection">The chosen entry, or <see langword="null"/> to disconnect.</param>
    /// <param name="cancellationToken">Cancels the connect.</param>
    /// <returns>A task that completes when the session is established, or has failed.</returns>
    /// <remarks>
    /// <para>
    /// Delphi <c>SelectConnection</c>. Everything but the failure handling is inside
    /// <see cref="IConnectionCoordinator.ConnectAsync"/>; what stays here is what the Delphi form
    /// itself does - read the combo box, and let a failure reach the user. <c>SelectConnection</c>
    /// has a <c>try..finally</c> and no <c>except</c>, so a failed connect surfaced through Delphi's
    /// default exception dialog.
    /// </para>
    /// <para>
    /// The <see langword="null"/> branch reproduces <c>if ItemIndex = -1 then fConnection := nil</c>
    /// (<c>:505-506</c>), which still disconnects first. It is unreachable from the drop-down, which
    /// offers no empty entry.
    /// </para>
    /// </remarks>
    [RelayCommand]
    private async Task ConnectAsync(QuickStatConnection? connection, CancellationToken cancellationToken)
    {
        try
        {
            if (connection is null)
            {
                await _connections.DisconnectAsync(cancellationToken).ConfigureAwait(true);

                return;
            }

            _ = await _connections.ConnectAsync(connection, cancellationToken).ConfigureAwait(true);

            // Addition, decision (f) of 07-ui-contracts.md §5. Written only on success: remembering
            // a database that could not be reached would be worse than remembering nothing.
            _windowState.SetLastDatabase(connection.Name);
        }
        catch (OperationCanceledException)
        {
            // The coordinator has already put the status line back to idle.
        }
        catch (Exception exception)
        {
            // IConnectionCoordinator has turned the status line red and logged the detail; it
            // documents that whether to raise a dialog as well is the caller's decision, and this
            // caller knows the user asked for it by name.
            _logger.LogError(exception, "Could not select the project '{Project}'.", connection?.Name);

            await _notifier.ErrorAsync(exception.Message).ConfigureAwait(true);
        }
    }

    private void LoadProjects(IConnectionCatalog catalogue)
    {
        string path = catalogue.DefaultConfigFilePath;

        IReadOnlyList<QuickStatConnection> connections;

        try
        {
            connections = catalogue.Load(path);
        }
        catch (QuickStatConfigurationException exception)
        {
            // The Delphi shows ERR_CONFIG_FILE_MISSING through the log's dialog threshold and carries
            // on with an empty list (MainQuickStat.pas:392-398); Docs/Port/06-contracts.md keeps the
            // log and drops the dialog. Throwing here would instead take the whole shell down before
            // the window appears, because this runs while the container builds MainViewModel.
            _logger.LogError(exception, "Could not read the connection catalogue at {Path}.", path);

            return;
        }

        foreach (QuickStatConnection connection in connections.OrderBy(entry => entry.Name, ProjectOrder))
        {
            Projects.Add(connection);
        }

        if (Projects.Count == 0)
        {
            _logger.LogWarning(
                "No connections were found in {Path}; the project list is empty and nothing can be selected.",
                path);
        }
    }

    partial void OnSelectedProjectChanged(QuickStatConnection? value)
    {
        // "SelectedProject's setter awaits IConnectionCoordinator.ConnectAsync and does nothing
        // else" (07-ui-contracts.md §2). The command is what makes the await observable to a test
        // and keeps the property setter synchronous, which is what WPF binding requires.
        ConnectCommand.Execute(value);
    }
}
