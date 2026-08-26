using System.Collections;

namespace QuickStat.Domain.Matrix;

/// <summary>
/// An ordered, de-duplicating set of variable names: the thing that decides column order.
/// </summary>
/// <remarks>
/// <para>
/// Delphi kept two parallel structures for this - <c>FVarList</c>, a sorted
/// duplicate-ignoring <c>TStringList</c> used as the dedupe set, and <c>FVarOrder</c>, appended to
/// only when <c>FVarList</c> actually grew, used as the ordered projection. One type here, with
/// <see cref="Order"/> selecting which view <see cref="this[int]"/> and the enumerator present.
/// </para>
/// <para>
/// Modelling this explicitly, rather than reaching for a <c>List&lt;string&gt;</c> at each call
/// site, is the point: column order is observable in every exported file and a silent reordering
/// would not surface until byte-comparison testing in Phase 5.
/// </para>
/// <para>
/// Owned by step 2.5 and consumed by step 2.4, which accumulates into it while reading a batch.
/// </para>
/// </remarks>
public sealed class VariableNameSet : IReadOnlyList<string>
{
    /// <summary>Creates an empty set.</summary>
    /// <param name="order">
    /// Which order to present. Defaults to <see cref="ColumnOrder.FirstSeen"/>.
    /// </param>
    public VariableNameSet(ColumnOrder order = ColumnOrder.FirstSeen) => throw new NotImplementedException();

    /// <summary>The order this set presents its members in.</summary>
    public ColumnOrder Order => throw new NotImplementedException();

    /// <inheritdoc />
    public int Count => throw new NotImplementedException();

    /// <inheritdoc />
    public string this[int index] => throw new NotImplementedException();

    /// <summary>Adds a name if it is not already present.</summary>
    /// <param name="variableName">Column name, prefix included.</param>
    /// <returns><see langword="true"/> when the name was new.</returns>
    /// <remarks>
    /// Comparison is ordinal. The Delphi is inconsistent here - the row dictionary is
    /// case-sensitive while <c>TPersonGridColumnList.TryGetColumn</c> uses <c>SameText</c> - and
    /// ordinal matches the dictionary that actually stores the data.
    /// </remarks>
    public bool Add(string variableName) => throw new NotImplementedException();

    /// <summary>Whether a name is present.</summary>
    /// <param name="variableName">Column name.</param>
    /// <returns><see langword="true"/> when present.</returns>
    public bool Contains(string variableName) => throw new NotImplementedException();

    /// <summary>Empties the set.</summary>
    /// <remarks>
    /// Must be called before each run. The Delphi never cleared <c>FVarList</c>, so re-running a
    /// collector against a different population kept the variables discovered in an earlier run and
    /// produced columns that were empty for everyone
    /// (<c>Docs/Port/04-matrix-export.md</c> R-4).
    /// </remarks>
    public void Clear() => throw new NotImplementedException();

    /// <inheritdoc />
    public IEnumerator<string> GetEnumerator() => throw new NotImplementedException();

    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
}
