using System.Globalization;

namespace QuickStat.Collectors.Sql;

/// <summary>
/// The three Delphi helpers every collector SQL string is built from.
/// </summary>
/// <remarks>
/// All three are pure functions of their arguments, which is what lets
/// <see cref="ICollector.BuildSql"/> stay deterministic and golden-file-testable
/// (PORT-PLAN.md R3).
/// </remarks>
public static class SqlLiteral
{
    /// <summary>Delphi <c>QuotedStr</c>: wraps in apostrophes and doubles embedded ones.</summary>
    /// <param name="value">Raw text.</param>
    /// <returns>A SQL string literal.</returns>
    /// <remarks>
    /// Every current call site passes a compile-time constant or a form name that came from
    /// <c>Report.GetFormClasses</c>, but the form name now round-trips through a UI-visible string,
    /// so the escaping is centralised rather than assumed away
    /// (<c>Docs/Port/03-collectors.md</c> §C.3).
    /// </remarks>
    public static string Quote(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    /// <summary>Delphi <c>ConvertArrayToList</c>: <c>", "</c>-separated decimal integers.</summary>
    /// <param name="identifiers">Item ids or lab-class ids, in registration order.</param>
    /// <returns>For example <c>3224, 3225, 3310</c>.</returns>
    /// <remarks>
    /// The Delphi builds <c>', ' + IntToStr(id)</c> per element and then strips the leading two
    /// characters (<c>EPR.QA.SQL.pas:134-142</c>), so the separator is a comma <em>and a space</em>.
    /// Order is preserved: it is observable in the generated <c>IN ( … )</c> list and therefore in
    /// the golden files.
    /// </remarks>
    public static string List(IReadOnlyList<int> identifiers)
    {
        ArgumentNullException.ThrowIfNull(identifiers);

        return string.Join(", ", identifiers.Select(id => id.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Delphi <c>ConvertAtcPatternToVariableName</c>: <c>'['</c> becomes <c>'x'</c>, <c>'%'</c> and
    /// <c>']'</c> disappear.
    /// </summary>
    /// <param name="matchPattern">An ATC or ICD-10 <c>LIKE</c> pattern.</param>
    /// <returns>A name-safe rendering, e.g. <c>C0[23789]%</c> becomes <c>C0x23789</c>.</returns>
    /// <remarks>
    /// Must stay byte-identical: its output becomes part of a collector <b>name</b>, and names are
    /// persisted in saved packages
    /// (<see cref="QuickStat.Domain.Packages.PackagedSelection.CollectorNames"/>). The Delphi uses
    /// two <c>TRegEx.Replace</c> calls, <c>'\['</c> then <c>'[%\]]'</c>; plain replacements are
    /// equivalent for these character classes and avoid a regular expression in a hot construction
    /// path.
    /// </remarks>
    public static string AtcPatternToVariableName(string matchPattern) =>
        matchPattern
            .Replace("[", "x", StringComparison.Ordinal)
            .Replace("%", string.Empty, StringComparison.Ordinal)
            .Replace("]", string.Empty, StringComparison.Ordinal);

    /// <summary>Renders an integer the way Delphi <c>Format</c>'s <c>%d</c> does.</summary>
    /// <param name="value">The value.</param>
    /// <returns>Invariant decimal text.</returns>
    internal static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Renders a double the way Delphi <c>Format</c>'s <c>%g</c> does under <c>en-US</c>.</summary>
    /// <param name="value">The value.</param>
    /// <returns>Invariant general-format text, e.g. <c>120</c> for <c>120.0</c>.</returns>
    /// <remarks>
    /// <c>SpSnapshotQuantityIfBelowThreshold</c> passes an explicit
    /// <c>TFormatSettings.Create('en-US')</c> so that the decimal separator is a full stop even on a
    /// Norwegian machine (<c>EPR.QA.SQL.pas:610</c>). Delphi's <c>%g</c> defaults to 15 significant
    /// digits, which is what <c>G15</c> reproduces.
    /// </remarks>
    internal static string General(double value) => value.ToString("G15", CultureInfo.InvariantCulture);
}
