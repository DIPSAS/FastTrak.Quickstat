using System.ComponentModel;
using QuickStat.Domain.Matrix;
using QuickStat.Domain.Populations;

namespace QuickStat.Services;

/// <summary>The one implementation of <see cref="IShellWorkspace"/>.</summary>
/// <remarks>
/// Holds no data of its own beyond the population, the checked names and one flag: everything else
/// is read straight off <see cref="PersonMatrix"/>, so the workspace and the matrix cannot drift
/// apart. That is why the explicit <see cref="NotifyDataChanged"/> exists - the matrix has no change
/// notification to subscribe to.
/// </remarks>
public sealed class ShellWorkspace : IShellWorkspace
{
    private readonly PersonMatrix _matrix;
    private Population? _population;
    private IReadOnlyList<string> _checkedCollectorNames = [];
    private bool _exportTimestamps;

    /// <summary>Creates the workspace over the application's single matrix.</summary>
    /// <param name="matrix">The one <see cref="PersonMatrix"/>, from the container.</param>
    /// <exception cref="ArgumentNullException"><paramref name="matrix"/> is <see langword="null"/>.</exception>
    public ShellWorkspace(PersonMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        _matrix = matrix;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public event EventHandler? PopulationChanged;

    /// <inheritdoc />
    public event EventHandler? DataChanged;

    /// <inheritdoc />
    public event EventHandler? CollectionsTabRequested;

    /// <inheritdoc />
    public PersonMatrix Matrix => _matrix;

    /// <inheritdoc />
    public Population? Population => _population;

    /// <inheritdoc />
    public bool HasPopulation => _population is not null && _matrix.Rows.Count > 0;

    /// <inheritdoc />
    public bool HasData => _matrix.HasData;

    /// <inheritdoc />
    public int RowCount => _matrix.Rows.Count;

    /// <inheritdoc />
    public int ColumnCount => _matrix.Columns.Count;

    /// <inheritdoc />
    public IReadOnlyList<string> CheckedCollectorNames => _checkedCollectorNames;

    /// <inheritdoc />
    public bool ExportTimestamps
    {
        get => _exportTimestamps;

        set
        {
            if (_exportTimestamps == value)
            {
                return;
            }

            _exportTimestamps = value;

            Raise(nameof(ExportTimestamps));
        }
    }

    /// <inheritdoc />
    public void SetPopulation(Population? population)
    {
        _population = population;

        Raise(nameof(Population));
        Raise(nameof(HasPopulation));
        Raise(nameof(RowCount));
        Raise(nameof(ColumnCount));
        Raise(nameof(HasData));

        PopulationChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void SetCheckedCollectorNames(IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);

        _checkedCollectorNames = [.. names];

        Raise(nameof(CheckedCollectorNames));
    }

    /// <inheritdoc />
    public void NotifyDataChanged()
    {
        Raise(nameof(HasData));
        Raise(nameof(RowCount));
        Raise(nameof(ColumnCount));

        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void RequestCollectionsTab() => CollectionsTabRequested?.Invoke(this, EventArgs.Empty);

    private void Raise(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
