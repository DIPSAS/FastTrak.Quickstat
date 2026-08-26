namespace QuickStat.Collectors.Sql;

/// <summary>
/// Which column of <c>dbo.ClinDataPoint</c> a var-set snapshot reads, and how it qualifies a row.
/// </summary>
/// <remarks>
/// Delphi: <c>TCrfVarType</c> (<c>EPR.QA.SQL.pas:6</c>). Enum answers are <em>not</em> a member here:
/// they are read through <see cref="Numeric"/> (via <c>Quantity</c>) by every collector QuickStat
/// registers. The separate <c>SpSnapshotEnum</c> path is reachable only from
/// <c>QS_PUMPE_VARSET</c> and <c>QST_BDR_COMORBID</c>, both of which are among the 39 factory names
/// QuickStat never registers and which PORT-PLAN.md §7.1 drops.
/// </remarks>
public enum CrfVarType
{
    /// <summary>
    /// <c>cdp.Quantity</c>, qualified by <c>ISNULL(cdp.Quantity,-1) &lt;&gt; -1</c>.
    /// </summary>
    /// <remarks>
    /// The qualifier discards NULL <em>and</em> a genuine quantity of exactly <c>-1</c>. That is a
    /// real defect in the shipping query and it is reproduced deliberately
    /// (<c>Docs/Port/03-collectors.md</c> §B.6).
    /// </remarks>
    Numeric = 0,

    /// <summary><c>DATEDIFF(DD,'1899-12-30',cdp.DTVal)</c> - an Excel serial date.</summary>
    Date = 1,

    /// <summary><c>DATALENGTH(cdp.TextVal)</c> - the length of the text, not the text.</summary>
    Text = 2,
}
