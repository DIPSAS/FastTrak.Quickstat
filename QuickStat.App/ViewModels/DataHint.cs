using System.Windows;

namespace QuickStat.ViewModels;

/// <summary>The floating panel that appears below a clicked grid cell.</summary>
/// <param name="Line1">
/// <c>PersonId = &lt;n&gt;</c> when the grid is anonymous, otherwise the patient's full name.
/// </param>
/// <param name="Line2">
/// <see cref="QuickStat.Domain.DataPoints.DataPoint.Describe()"/> - several lines, separated by bare
/// <c>LF</c> as the Delphi builds them with <c>#10</c>.
/// </param>
/// <param name="Anchor">
/// Where the panel's top-left corner goes, in the grid's own coordinate space.
/// </param>
/// <remarks>
/// <para>
/// <c>05-ui-spec.md</c> §G.2. <b>Not a tooltip.</b> It is a panel that moves on <em>click</em> and on
/// nothing else - not hover, not keyboard navigation - and it is hidden whenever the clicked cell
/// has no datapoint or <c>Show data hint</c> is off. Keeping it click-driven is why
/// <see cref="QuickStat.Controls.Dataset.MatrixGrid"/> raises a separate <c>CellActivated</c> event
/// instead of letting the tab watch the current-cell properties.
/// </para>
/// <para>
/// A record with no <c>IsOpen</c> flag: absence is <see langword="null"/>. §H.2 sketches
/// <c>DataHint? Hint { …; bool IsOpen }</c>, which would allow a hint that exists and is closed and
/// a hint that is null - two ways to say the same thing, and therefore two ways to disagree.
/// </para>
/// </remarks>
public sealed record DataHint(string Line1, string Line2, Point Anchor);
