using CommunityToolkit.Mvvm.ComponentModel;
using QuickStat.Domain.Packages;

namespace QuickStat.ViewModels;

/// <summary>The <c>Save specification</c> modal: a unique name and a comment.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.6. This is a compiling stub.</b>
/// </para>
/// <para>
/// <b>What is left to do</b> (<c>05-ui-spec.md</c> §E): the window - 388 x 288, not resizable,
/// centred on the owner, a 41 px white banner with the header label, then <c>Unique name</c> over a
/// single-line box and <c>Comments</c> over a multi-line one, and a 48 px button bar with
/// <c>OK</c> (<c>IsDefault</c>) and <c>Cancel</c> (<c>IsCancel</c>).
/// </para>
/// <para>
/// Three details worth not rediscovering:
/// </para>
/// <list type="bullet">
///   <item><description>
///     The Delphi creates the form <b>once</b> and reuses it, so the fields keep their contents
///     between invocations unless <c>Clear</c> is called. A per-invocation view-model removes the
///     whole question; if you keep one instance, call <see cref="Clear"/> before every show.
///   </description></item>
///   <item><description>
///     <see cref="CanSave"/> is an <b>improvement</b>, flagged in §E: the Delphi has no validation
///     at all and accepts an empty title. Disabling <c>OK</c> while the title is blank is low risk,
///     but it is a change.
///   </description></item>
///   <item><description>
///     §I.3 asks whether the dialog should be cleared for <c>Save selection</c>, which the Delphi
///     does not do. <b>It does not arise.</b> That path is <c>actSavePatientSelection</c>, and
///     PORT-PLAN.md §7.1 removes it as unreachable - it is bound to no menu item, button or toolbar
///     in <c>MainQuickStat.dfm</c>. Step 3.1 confirmed the port has no caller: nothing references
///     <c>Save selection</c>, and the only header this dialog is ever given is
///     <c>Save specification</c>. Do not add the second one back.
///   </description></item>
/// </list>
/// </remarks>
public sealed partial class SaveSpecViewModel : ObservableObject
{
    /// <summary>The only header this dialog is given. Delphi <c>TXT_SAVE_SPEC</c>.</summary>
    public const string SaveSpecificationHeader = "Save specification";

    /// <summary>
    /// Cap on the name box, forwarded from <see cref="PackagedSelection.MaxTitleLength"/> so the
    /// markup can reach it with <c>x:Static</c> and there is still only one 80 in the solution.
    /// </summary>
    public const int MaxTitleLength = PackagedSelection.MaxTitleLength;

    [ObservableProperty]
    private string _header = SaveSpecificationHeader;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private string _title = "";

    [ObservableProperty]
    private string _comment = "";

    /// <summary>Whether <c>OK</c> is enabled. <b>An improvement over the Delphi</b>; see the remarks.</summary>
    public bool CanSave => !string.IsNullOrWhiteSpace(Title);

    /// <summary>Empties both fields. Delphi <c>TfrmSaveSpec.Clear</c>.</summary>
    public void Clear()
    {
        Title = "";
        Comment = "";
    }
}
