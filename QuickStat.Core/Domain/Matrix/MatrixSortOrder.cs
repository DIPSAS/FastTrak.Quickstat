namespace QuickStat.Domain.Matrix;

/// <summary>Row order of the result matrix.</summary>
/// <remarks>
/// Delphi: <c>TPersonGridSortOrder</c> (<c>EPR.QA.Matrix.pas:28</c>). The library defaults to
/// <see cref="ReverseName"/> but QuickStat overrides it to <see cref="PersonId"/> on every load
/// (<c>MainQuickStat.pas:563-566</c>), so in practice the grid is always ascending by person id and
/// any <c>ORDER BY</c> inside the population procedure is discarded.
/// </remarks>
public enum MatrixSortOrder
{
    /// <summary>
    /// Ascending <c>PersonId</c>. What QuickStat always uses.
    /// </summary>
    /// <remarks>
    /// The Delphi comparer subtracts one id from the other, which overflows for ids far apart
    /// (<c>EPR.QA.Matrix.Row.pas:239-242</c>). Compare, do not subtract.
    /// </remarks>
    PersonId = 0,

    /// <summary>
    /// Ordinal, case-sensitive comparison of <c>"Last, First"</c>. Unreachable from the QuickStat
    /// UI.
    /// </summary>
    ReverseName = 1,
}
