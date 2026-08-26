using System.Windows.Controls;

namespace QuickStat.Views;

/// <summary>The embedded population picker.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.2.</b> Delphi <c>TfrmPopulations</c>
/// (<c>EPR.VclFrame.Populations.pas</c> / <c>.dfm</c>), <c>05-ui-spec.md</c> §B.1.1.
/// </para>
/// <para>
/// No code-behind beyond the constructor, on purpose. The two gestures the frame has - double click
/// and <c>Enter</c>, which <c>TObjectListView.DoKeyPress</c> turns into a double click
/// (<c>Emetra.VclComp.ListView.pas:762-766</c>) - are <c>InputBindings</c> on the list, and the
/// expansion rule lives on <see cref="QuickStat.ViewModels.PopulationPickerViewModel"/> where it can
/// be tested without a window.
/// </para>
/// </remarks>
public partial class PopulationPickerView : UserControl
{
    /// <summary>Initialises the picker.</summary>
    public PopulationPickerView() => InitializeComponent();
}
