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
    /// Characters of <see cref="DataPoint.Caption"/> a cell shows when no rule says otherwise.
    /// </summary>
    /// <remarks>
    /// Six, from <c>Copy(fCaption, 1, 6)</c> in <c>TDataPoint.CellText</c>. It is a display
    /// truncation only; the exported value is unaffected.
    /// </remarks>
    public const int DefaultCaptionLength = 6;

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

    /// <summary>
    /// Whether <see cref="DataPoint.Caption"/> is shown in preference to
    /// <see cref="FormatValue"/> when both are available.
    /// </summary>
    /// <remarks>
    /// False for every value-only override - BMI, pulse quality and the two version variables all
    /// ignore the caption outright. True only for the drug rule, whose <c>CellText</c> tests the
    /// caption first and falls back to <c>Ja</c>/<c>Nei</c>
    /// (<c>EPR.QA.DataPoint.Pharmacology.pas:62-70</c>). With no rule at all the caption also wins,
    /// which is the base class's behaviour.
    /// </remarks>
    public bool CaptionTakesPrecedence { get; init; }

    /// <summary>
    /// Characters of the caption to show. Defaults to <see cref="DefaultCaptionLength"/>.
    /// </summary>
    /// <remarks>The drug rule uses eight rather than six.</remarks>
    public int CaptionLength { get; init; } = DefaultCaptionLength;

    /// <summary>
    /// Whether the rule's text is a label, so the cell is drawn left-aligned even though the
    /// datapoint carries no caption of its own.
    /// </summary>
    /// <remarks>
    /// This models a side effect rather than a declaration. <c>TPulseQualityDatapoint.CellText</c>
    /// ends with <c>Caption := Result</c> (<c>EPR.QA.DataPoint.HeartFailure.pas:61</c>), and the
    /// grid evaluates <c>AlignLeft</c> immediately afterwards, so the cell is left-aligned from the
    /// very first paint. Reproducing the assignment itself would be worse than modelling it: on the
    /// pinned library tip the CSV writer reads <see cref="DataPoint.Caption"/> too, so a painted
    /// cell would export differently from an unpainted one.
    /// </remarks>
    public bool SetsCaptionFromText { get; init; }
}
