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
/// cell and its rectangle into a positioned hint. <see cref="MatrixGrid.TryGetCellBounds"/> is a
/// control API and the view-model must not call it, so the view asks and passes the answer on.
/// Everything else - whether the hint may appear, what it says, and where relative to the cell - is
/// <see cref="DatasetViewModel"/>'s, and is unit-tested there.
/// </para>
/// <para>
/// Two things ask for it, and the second is easy to forget: the grid raising
/// <see cref="MatrixGrid.CellActivated"/> when the caret moves, and the view-model raising
/// <see cref="DatasetViewModel.HintRefreshRequested"/> when <c>Show data hint</c> is toggled while
/// the caret stays put.
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
            _viewModel.HintRefreshRequested += OnHintRefreshRequested;
        }
    }

    private void Detach()
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.GridRefreshRequested -= OnGridRefreshRequested;
        _viewModel.HintRefreshRequested -= OnHintRefreshRequested;
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

    private void OnCellActivated(object? sender, MatrixGridCellEventArgs e) =>
        UpdateHint(e.RowIndex, e.ColumnIndex);

    /// <summary>
    /// Rebuilds the hint for the cell the caret is already on, because <c>Show data hint</c> moved.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The event carries no indices, and it cannot: nothing moved, so no
    /// <see cref="MatrixGrid.CellActivated"/> is coming and the caret is wherever the last click or
    /// key left it. That is the Delphi reading <c>fGrid.Col</c> and <c>fGrid.Row</c> inside
    /// <c>UpdateDataHintPanel</c>, and the same two values are dependency properties here.
    /// </para>
    /// <para>
    /// With nothing ever clicked both are <see cref="MatrixGrid.NoIndex"/> and no hint appears. The
    /// Delphi would have shown one, because a <c>TCustomGrid</c> always has a current cell; this
    /// grid deliberately starts without a caret and grows one on the first click or arrow key. That
    /// difference is the caret's, not the hint's, and it is left alone.
    /// </para>
    /// </remarks>
    private void OnHintRefreshRequested(object? sender, EventArgs e) =>
        UpdateHint(Grid.CurrentRowIndex, Grid.CurrentColumnIndex);

    private void UpdateHint(int rowIndex, int columnIndex)
    {
        if (_viewModel is null)
        {
            return;
        }

        // A cell that is scrolled out of view returns false, and the view-model hides the hint
        // rather than parking it outside the window.
        Rect? bounds = Grid.TryGetCellBounds(rowIndex, columnIndex, out Rect cell) ? cell : null;

        _viewModel.UpdateHint(rowIndex, columnIndex, bounds);
    }
}
