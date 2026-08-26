namespace QuickStat.Domain.Packages;

/// <summary>
/// Reads and writes <c>Report.QuickStat.DataElements</c>, the persisted list of collector names.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: the <c>TStringList</c> configured in <c>TPackagedSelection.AfterConstruction</c>
/// (<c>QuickStat.Selection.pas:71-76</c>) - <c>Delimiter = ';'</c>, <c>StrictDelimiter = true</c>,
/// <c>Sorted = true</c>, <c>Duplicates = dupIgnore</c>. Both directions of the round trip go through
/// that list, so what is stored is always sorted and de-duplicated, and the order carries no
/// meaning: collection order comes from the registry.
/// </para>
/// <para>
/// Sorting is case-insensitive because Delphi's sorted <c>TStringList</c> compares with
/// <c>AnsiCompareText</c>. Ordinal-ignore-case is used rather than a Norwegian culture comparison:
/// collector names are ASCII identifiers such as <c>QS_DRUG_ANTIBIOTIC_RESISTANCE</c>, on which the
/// two agree, and an ordinal comparison cannot change under a locale change.
/// </para>
/// </remarks>
internal static class CollectorNameList
{
    /// <summary>Splits the stored value into collector names.</summary>
    /// <param name="delimited">The <c>DataElements</c> column, which may be null or empty.</param>
    /// <returns>Sorted, de-duplicated names. Empty when there are none.</returns>
    /// <remarks>
    /// Empty entries are dropped. Delphi kept them - <c>"A;;B"</c> produced a blank name that sorted
    /// first and then failed to match any collector, logging <c>MSG_UNKNOWN_COLLECTOR</c>
    /// (<c>MainQuickStat.pas:803</c>). Dropping them removes a warning that never told anyone
    /// anything.
    /// </remarks>
    public static IReadOnlyList<string> Parse(string? delimited)
    {
        if (string.IsNullOrEmpty(delimited))
        {
            return [];
        }

        return Normalise(delimited.Split(PackagedSelection.CollectorNameSeparator));
    }

    /// <summary>Renders collector names for storage.</summary>
    /// <param name="names">The names to store, in any order.</param>
    /// <returns>The sorted, de-duplicated, semicolon-separated value.</returns>
    /// <remarks>
    /// Delphi's <c>DelimitedText</c> getter would quote an entry containing a semicolon or a double
    /// quote. No collector name does, and none may - <c>CollectorNames</c> is a persistence format
    /// that saved specifications in production databases already depend on - so no quoting is
    /// emitted.
    /// </remarks>
    public static string Format(IEnumerable<string>? names)
    {
        if (names is null)
        {
            return "";
        }

        return string.Join(PackagedSelection.CollectorNameSeparator, Normalise(names));
    }

    private static List<string> Normalise(IEnumerable<string> names)
    {
        SortedSet<string> sorted = new(StringComparer.OrdinalIgnoreCase);
        foreach (string name in names)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                sorted.Add(name);
            }
        }

        return [.. sorted];
    }
}
