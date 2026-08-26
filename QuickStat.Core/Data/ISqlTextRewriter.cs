namespace QuickStat.Data;

/// <summary>
/// Translates the Delphi/ADO <c>:Name</c> placeholder syntax into <c>@Name</c> and reports the
/// placeholders it found.
/// </summary>
/// <remarks>
/// <para>
/// This cannot be a regular expression over the raw text. Population SQL is arbitrary
/// server-authored T-SQL (PORT-PLAN.md R2) that may contain a colon inside a string literal
/// (<c>'23:59'</c>), inside a bracketed or quoted identifier, inside a comment, or as the
/// old-style <c>::fn_</c> prefix. A scanner that skips <c>'…'</c> with the <c>''</c> escape,
/// <c>[…]</c> with the <c>]]</c> escape, <c>"…"</c>, <c>--</c> line comments and nested
/// <c>/* */</c> blocks is the only safe implementation.
/// </para>
/// <para>
/// Step 2.3 also uses this to answer "does this population need a period?" - the period dialog is
/// shown only when <em>both</em> <c>StartDate</c> and <c>StopDate</c> appear
/// (<c>Emetra.Database.ParameterDictionary.pas:96-98</c>) - which is why the rewriter is a contract
/// and not an implementation detail of the executor.
/// </para>
/// </remarks>
public interface ISqlTextRewriter
{
    /// <summary>Rewrites a statement and lists its placeholders.</summary>
    /// <param name="commandText">Statement in <c>:Name</c> form.</param>
    /// <returns>The rewritten text and the distinct placeholder names in first-appearance order.</returns>
    RewrittenSql Rewrite(string commandText);
}
