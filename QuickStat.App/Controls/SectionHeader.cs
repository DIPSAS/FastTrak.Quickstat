using System.Windows.Controls;

namespace QuickStat.Controls;

/// <summary>
/// The teal section-header bar: a heading string on the left and an optional right-aligned control.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: the six <c>panHdr*</c> panels, each a <c>TPanel</c> with <c>Color =
/// clGradientInactiveCaption</c> that <c>TArenaColors.StyleHeaderPanel</c> repaints
/// <c>clMenuItemSelectionFill</c> (<c>#178891</c>) at run time, with a single <c>TLabel</c> inside.
/// Two of them carry a right-aligned check box whose caption sits to the <i>left</i> of the box
/// (<c>Alignment = taLeftJustify</c>): <c>cbWideColumns</c> in <c>panHdrYourDataset</c>, and
/// <c>cbSimpleView</c> in the population frame.
/// </para>
/// <para>
/// A <see cref="HeaderedContentControl"/> rather than a <see cref="System.Windows.Controls.UserControl"/>,
/// so the right-hand slot is ordinary content:
/// <c>&lt;q:SectionHeader Header="Your dataset"&gt;&lt;CheckBox … /&gt;&lt;/q:SectionHeader&gt;</c>.
/// Appearance comes from the implicit style in <c>Theme/QuickStat.Styles.xaml</c>, which is based on
/// the keyed <c>QsSectionHeader</c> style, so there is one template and §F.4's key inventory still
/// holds. There is deliberately no <c>DefaultStyleKey</c> override and no <c>Themes/Generic.xaml</c>:
/// the implicit style in <c>Application.Resources</c> is what dresses it, and adding a theme
/// dictionary would mean two places to change the bar.
/// </para>
/// <para>
/// Shared by every Phase 3 step; step 3.1 owns the file. Report a missing capability rather than
/// editing it - two wave-2 steps must not disagree about the section header.
/// </para>
/// </remarks>
public class SectionHeader : HeaderedContentControl
{
    /// <summary>Height of the bar, in device-independent units.</summary>
    /// <remarks>
    /// <c>05-ui-spec.md</c> §A.3: the Delphi panel is 23-24 px including its one-pixel bevel; the
    /// port uses 26 with <c>Padding="8,4"</c>.
    /// </remarks>
    public const double BarHeight = 26;
}
