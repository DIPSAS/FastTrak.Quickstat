namespace QuickStat.Theme;

/// <summary>
/// The Segoe MDL2 Assets code points that stand in for the Delphi image lists.
/// </summary>
/// <remarks>
/// <para>
/// <c>05-ui-spec.md</c> §I.10 left this open: extract <c>lstActiveImages</c> /
/// <c>lstDisabledImages</c> - two 24 x 24 image lists embedded in <c>MainQuickStat.dfm</c> as binary
/// blobs - or substitute glyphs. <b>Step 3.1 chose glyphs</b>, which is what the spec recommends.
/// They scale with DPI, they take their colour from the theme, they need no build action and no new
/// <c>&lt;Resource&gt;</c> in the <c>.csproj</c>, and two of the eight blobs (indices 5 and 7) are
/// not visible in any screenshot, so half of an extraction would have been guesswork anyway.
/// </para>
/// <para>
/// The code points are the ones §I.10 suggests. They are written as <c>\uXXXX</c> escapes rather
/// than as literal characters: these live in the Unicode private use area, so a literal would render
/// as a box in every editor and diff, and would be silently destroyed by any tool that
/// re-encoded the file.
/// </para>
/// <para>
/// Reversing the decision means replacing these constants and adding the extracted images as
/// resources; nothing else refers to the image list. <see cref="ChevronDown"/> has no image-list
/// original and would stay a glyph either way. Used from XAML as
/// <c>Text="{x:Static theme:SegoeIcons.Package}"</c> together with the <c>QsIconGlyph</c> style,
/// which supplies the typeface.
/// </para>
/// </remarks>
public static class SegoeIcons
{
    /// <summary>Package this dataset for reuse. Delphi image index 1, a tan parcel box.</summary>
    public const string Package = "\uE7B8";

    /// <summary>Open this dataset in Excel. Delphi image index 3, a green Excel "X".</summary>
    public const string Excel = "\uE8A5";

    /// <summary>Collect data. Delphi image index 4, a gold magic wand with sparkles.</summary>
    public const string Collect = "\uE9D9";

    /// <summary>Save dataset to CSV file. Delphi image index 6, a floppy disk.</summary>
    public const string Save = "\uE74E";

    /// <summary>Delete this package. Delphi image index 7, not visible in any screenshot.</summary>
    public const string Delete = "\uE74D";

    /// <summary>A generic document, for a place that needs a glyph and has no Delphi original.</summary>
    public const string Document = "\uE8A5";

    /// <summary>
    /// The "there is a menu under this" chevron on the <c>Export</c> button. No Delphi original: the
    /// button itself is an addition, surfacing <c>mnuGridPopup</c> where it can be found without
    /// guessing that the grid has a right-click menu.
    /// </summary>
    public const string ChevronDown = "\uE70D";
}
