using System.Globalization;

namespace QuickStat.Domain.DataPoints;

/// <summary>A 24-bit colour, with no dependency on any UI framework.</summary>
/// <param name="R">Red channel.</param>
/// <param name="G">Green channel.</param>
/// <param name="B">Blue channel.</param>
/// <remarks>
/// <para>
/// PORT-PLAN.md §5 Phase 1: <c>QuickStat.Core</c> must never reference <c>PresentationCore</c> or
/// <c>WindowsBase</c>. Cell colouring is domain logic - fourteen analyte classes carry hardcoded
/// threshold ladders that decide what a clinician sees - so the colour has to be expressible here.
/// The App layer converts to <c>System.Windows.Media.Color</c>.
/// </para>
/// <para>
/// Watch the byte order when transcribing. Delphi <c>TColor</c> literals are <c>$00BBGGRR</c>,
/// reversed relative to HTML: <c>clGraveRisk = $008080FF</c> is <c>#FF8080</c>, not <c>#8080FF</c>.
/// Half the palette is symmetric and half is not, so a transcription error shows up on some
/// swatches and not others. Use <see cref="FromDelphi"/> rather than converting by eye.
/// </para>
/// </remarks>
public readonly record struct Rgb(byte R, byte G, byte B)
{
    /// <summary>Converts a Delphi <c>TColor</c> literal.</summary>
    /// <param name="bbggrr">The <c>$00BBGGRR</c> value, e.g. <c>0x008080FF</c>.</param>
    /// <returns>The colour.</returns>
    /// <remarks>
    /// The low byte is red and the high byte of the low three is blue, which is the reverse of every
    /// HTML literal. The top byte carries the VCL's system-colour flag and is discarded: no colour
    /// reaching this method is a system colour, because the palette constants are all plain
    /// <c>$00…</c> literals.
    /// </remarks>
    public static Rgb FromDelphi(int bbggrr) => new(
        (byte)(bbggrr & 0xFF),
        (byte)((bbggrr >> 8) & 0xFF),
        (byte)((bbggrr >> 16) & 0xFF));

    /// <summary>Renders as <c>#RRGGBB</c>, culture-invariantly.</summary>
    /// <returns>Seven characters, uppercase hex.</returns>
    public string ToHex() => string.Create(CultureInfo.InvariantCulture, $"#{R:X2}{G:X2}{B:X2}");
}
