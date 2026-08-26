namespace QuickStat.Domain.DataPoints;

/// <summary>
/// How one variable is displayed and coloured: the data that replaces a <c>TDataPoint</c> subclass.
/// </summary>
/// <remarks>
/// <para>
/// Sixteen variables carry a rule today (<c>QuickStat.Collectors.pas:154-176</c>), each a hardcoded
/// threshold ladder such as <c>Value &gt; 8 -&gt; clGraveRisk</c>. This is the colouring users
/// actually see, and it is ported as-is. It is <em>not</em> the percentile machinery, which is dead
/// as shipped: <c>ProvideColor</c> gates on <c>InheritsFrom(TColoredDatapoint)</c> and nothing
/// registered descends from it, so 35 colour registrations and about 40 stored-procedure round
/// trips per login do nothing (PORT-PLAN.md §7.1).
/// </para>
/// <para>
/// Value semantics with injected functions rather than inheritance, so a rule is a table test:
/// value in, colour and text out, no object graph.
/// </para>
/// </remarks>
public sealed record DataPointRule
{
    /// <summary>
    /// Display text override, or <see langword="null"/> for the default <c>%g</c> rendering.
    /// </summary>
    /// <remarks>
    /// Used by <c>BMI</c> (<c>%.1f</c>), <c>PULSE_QUALITY</c> (<c>Rgm</c>/<c>AF</c>/<c>ES</c>) and
    /// the two Dogfood version variables. Screen only - the CSV writes the raw value regardless,
    /// which is correct for analysis but surprises users who compare the two.
    /// </remarks>
    public Func<double, string>? FormatValue { get; init; }

    /// <summary>Cell background, or <see langword="null"/> for the default empty-cell colour.</summary>
    /// <remarks>
    /// Watch the boundaries when transcribing: the ladders mix <c>&gt;</c> and <c>&gt;=</c>, and
    /// three of them - sodium, potassium and haemoglobin - have <b>no</b> no-data branch, so a
    /// value of zero renders as grave risk (<c>Docs/Port/04-matrix-export.md</c> R-9).
    /// </remarks>
    public Func<double, Rgb?>? BrushColor { get; init; }

    /// <summary>Cell foreground, or <see langword="null"/> for the default.</summary>
    public Func<double, Rgb?>? FontColor { get; init; }

    /// <summary>Whether a datapoint carrying a caption is drawn left-aligned.</summary>
    /// <remarks>Delphi: <c>TDataPoint.AlignLeft</c> is simply <c>fCaption &lt;&gt; ''</c>.</remarks>
    public bool AlignLeftWhenCaptioned { get; init; } = true;
}
