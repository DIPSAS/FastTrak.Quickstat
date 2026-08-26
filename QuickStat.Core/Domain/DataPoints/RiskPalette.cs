namespace QuickStat.Domain.DataPoints;

/// <summary>
/// The cell colours the threshold ladders return, transcribed from the Delphi literals.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>EPR.QA.DataPoint.Colors.pas:8-16</c>, plus the three VCL web colours the ladders and
/// the grid reach for directly. Every constant goes through <see cref="Rgb.FromDelphi"/> rather than
/// being written out as <c>#RRGGBB</c>, because the Delphi literal is <c>$00BBGGRR</c> and half the
/// palette is byte-symmetric - a transcription error would show on some swatches and not others.
/// </para>
/// <para>
/// This is the *risk* palette only. Grid chrome (fixed-cell background, selection, the teal Arena
/// theme) belongs to the WPF theme, and the percentile blend palette is not ported at all: its
/// machinery is dead as shipped (PORT-PLAN.md §7.1).
/// </para>
/// </remarks>
public static class RiskPalette
{
    /// <summary>Normal value. Delphi <c>clNoRisk</c> = <c>clWhite</c>, <c>#FFFFFF</c>.</summary>
    public static Rgb NoRisk { get; } = Rgb.FromDelphi(0x00FFFFFF);

    /// <summary>Pale green. Delphi <c>clLowRisk</c>, <c>#D1EFB3</c>.</summary>
    public static Rgb LowRisk { get; } = Rgb.FromDelphi(0x00B3EFD1);

    /// <summary>Pale yellow. Delphi <c>clMildRisk</c>, <c>#FFFFBF</c>.</summary>
    public static Rgb MildRisk { get; } = Rgb.FromDelphi(0x00BFFFFF);

    /// <summary>Pale amber. Delphi <c>clModerateRisk</c>, <c>#FFEDBF</c>.</summary>
    public static Rgb ModerateRisk { get; } = Rgb.FromDelphi(0x00BFEDFF);

    /// <summary>Pale orange. Delphi <c>clHighRisk</c>, <c>#FFDBBF</c>.</summary>
    public static Rgb HighRisk { get; } = Rgb.FromDelphi(0x00BFDBFF);

    /// <summary>Salmon red. Delphi <c>clGraveRisk</c>, <c>#FF8080</c>.</summary>
    public static Rgb GraveRisk { get; } = Rgb.FromDelphi(0x008080FF);

    /// <summary>Grey. Delphi <c>clNoData</c> = <c>clWebGainsboro</c>, <c>#DCDCDC</c>.</summary>
    public static Rgb NoData { get; } = Rgb.FromDelphi(0x00DCDCDC);

    /// <summary>
    /// Pale purple. Delphi <c>clDataPalePurple</c>, <c>#EEB2E7</c>. Used by exactly one ladder -
    /// digitoxin's second band.
    /// </summary>
    public static Rgb DataPalePurple { get; } = Rgb.FromDelphi(0x00E7B2EE);

    /// <summary>
    /// Delphi <c>clWebAliceBlue</c>, <c>#F0F8FF</c>. Used by exactly one ladder - HbA1c in mmol/mol
    /// puts its lowest positive band here instead of <see cref="NoRisk"/>.
    /// </summary>
    public static Rgb AliceBlue { get; } = Rgb.FromDelphi(0x00FFF8F0);

    /// <summary>
    /// A cell the person has no value for. Delphi <c>clWebWhiteSmoke</c>, <c>#F5F5F5</c>.
    /// </summary>
    /// <remarks>
    /// <c>TStudyOverviewGrid.SelectEmptyColor</c> looks the variable up in a per-variable map first,
    /// but <c>AddEmptyColor</c> is never called in QuickStat, so this fallback is the only empty-cell
    /// colour that ever renders.
    /// </remarks>
    public static Rgb EmptyCell { get; } = Rgb.FromDelphi(0x00F5F5F5);

    /// <summary>
    /// A grid position with no object behind it at all. Delphi <c>clWebSnow</c>, <c>#FFFAFA</c>.
    /// </summary>
    /// <remarks>
    /// Unreachable through <see cref="QuickStat.Domain.Matrix.PersonMatrix.GetCell"/>, which only
    /// addresses real rows and columns. Kept because the grid still paints the padding cells the
    /// Delphi's "never zero" dimensions produce.
    /// </remarks>
    public static Rgb MissingObject { get; } = Rgb.FromDelphi(0x00FAFAFF);
}
