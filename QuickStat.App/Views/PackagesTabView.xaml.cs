using System.Windows;
using System.Windows.Controls;
using QuickStat.ViewModels;
using QuickStat.Views.Dialogs;

namespace QuickStat.Views;

/// <summary>The <c>Packages</c> tab.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.4.</b> <c>05-ui-spec.md</c> §B.3, §D.3. The layout, the filter, the toolbar and
/// the context menu are all declarative; the only thing the code-behind does is put the
/// <c>Save specification</c> modal on screen, because a <see cref="Window"/> is the one part of the
/// save path a view-model must not touch.
/// </para>
/// <para>
/// Why here rather than behind a service: <c>Views/Dialogs/SaveSpecDialog.xaml.cs</c> already
/// specifies the contract - <em>the caller sets the <see cref="FrameworkElement.DataContext"/> to a
/// <see cref="SaveSpecViewModel"/> and reads it back after <see cref="Window.ShowDialog"/> returns
/// true</em> - and doing it in the view needs no container registration, so nothing has to be wired
/// into the composition root when the four wave-2 branches are merged. The view-model half stays
/// fully testable: <see cref="PackagesTabViewModel.SaveSpecRequested"/> carries a mutable
/// <see cref="SaveSpecRequest"/>, and with no subscriber at all it reads as a cancel.
/// </para>
/// </remarks>
public partial class PackagesTabView : UserControl
{
    private PackagesTabViewModel? _subscription;

    /// <summary>Initialises the tab.</summary>
    public PackagesTabView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// Follows the <see cref="FrameworkElement.DataContext"/> rather than
    /// <see cref="FrameworkElement.Loaded"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="TabItem"/> that is not selected has its content taken out of the visual tree, so
    /// <see cref="FrameworkElement.Unloaded"/> fires every time the user looks at another tab - and
    /// <c>Package dataset specification for reuse</c> is raised from the dataset grid's context menu,
    /// which is exactly when this tab is likely to be the one <em>not</em> showing. The control
    /// object itself lives as long as the window, so the subscription follows the data context and
    /// nothing else.
    /// </remarks>
    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_subscription is not null)
        {
            _subscription.SaveSpecRequested -= OnSaveSpecRequested;
            _subscription = null;
        }

        if (e.NewValue is PackagesTabViewModel viewModel)
        {
            _subscription = viewModel;
            _subscription.SaveSpecRequested += OnSaveSpecRequested;
        }
    }

    /// <summary>Shows <c>TfrmSaveSpec</c> and copies the answer back into the request.</summary>
    /// <remarks>
    /// Delphi <c>actSaveDataPackageExecute</c>: <c>frmSaveSpec.Clear</c>, then
    /// <c>SetHeader('Save specification')</c>, then <c>ShowModal</c>. The <c>Clear</c> matters there
    /// because the form is created once and reused (§E); here the view-model is transient, so the
    /// call only states the intent.
    /// </remarks>
    private void OnSaveSpecRequested(object? sender, SaveSpecRequest request)
    {
        SaveSpecViewModel dialogViewModel = new();

        dialogViewModel.Clear();
        dialogViewModel.Header = request.Header;

        // Null when the tab is not in the visual tree; ShowDialog copes, it just cannot centre on an
        // owner it does not have.  Application.Current is deliberately not consulted: it may be null
        // under test and this method must stay callable from a headless host.
        SaveSpecDialog dialog = new()
        {
            DataContext = dialogViewModel,
            Owner = Window.GetWindow(this),
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        request.Accepted = true;
        request.Title = dialogViewModel.Title;
        request.Comment = dialogViewModel.Comment;
    }
}
