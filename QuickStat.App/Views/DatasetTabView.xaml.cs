using System.Windows;
using System.Windows.Controls;
using QuickStat.Controls.Dataset;
using QuickStat.ViewModels;

namespace QuickStat.Views;

/// <summary>The right-hand pane: caption bar, the dataset grid, the floating hint.</summary>
/// <remarks>
/// <para>
/// <c>05-ui-spec.md</c> §C.1. Step 3.1 owns this; the <see cref="MatrixGrid"/> inside it is step
/// 3.5's control.
/// </para>
/// <para>
/// The code-behind does exactly one thing, and it is the thing that cannot be done in XAML: turn a
/// click on the grid into a positioned hint. <see cref="MatrixGrid.TryGetCellBounds"/> is a control
/// API and the view-model must not call it, so the view asks and passes the answer on. Everything
/// else - whether the hint may appear, what it says, and where relative to the cell - is
/// <see cref="DatasetViewModel"/>'s, and is unit-tested there.
/// </para>
/// </remarks>
public partial class DatasetTabView : UserControl
{
    private DatasetViewModel? _viewModel;

    /// <summary>Initialises the tab.</summary>
    public DatasetTabView()
    {
        InitializeComponent();

        Grid.CellActivated += OnCellActivated;
        DataContextChanged += OnDataContextChanged;

        Unloaded += (_, _) =>
        {
            Grid.CellActivated -= OnCellActivated;
            DataContextChanged -= OnDataContextChanged;

            Detach();
        };
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        Detach();

        _viewModel = e.NewValue as DatasetViewModel;

        if (_viewModel is not null)
        {
            _viewModel.GridRefreshRequested += OnGridRefreshRequested;
        }
    }

    private void Detach()
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.GridRefreshRequested -= OnGridRefreshRequested;
        _viewModel = null;
    }

    /// <summary>
    /// Repaints the grid after the matrix was mutated in place.
    /// </summary>
    /// <remarks>
    /// <see cref="Domain.Matrix.PersonMatrix"/> raises no change notification, so a collect run that
    /// adds columns to the bound instance moves no dependency property and WPF has nothing to react
    /// to. <see cref="MatrixGrid.Refresh"/> exists for exactly this.
    /// </remarks>
    private void OnGridRefreshRequested(object? sender, EventArgs e) => Grid.Refresh();

    private void OnCellActivated(object? sender, MatrixGridCellEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        // A cell that is scrolled out of view returns false, and the view-model hides the hint
        // rather than parking it outside the window.
        Rect? bounds = Grid.TryGetCellBounds(e.RowIndex, e.ColumnIndex, out Rect cell) ? cell : null;

        _viewModel.UpdateHint(e.RowIndex, e.ColumnIndex, bounds);
    }
}
