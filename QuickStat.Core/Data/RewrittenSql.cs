namespace QuickStat.Data;

/// <summary>The result of <see cref="ISqlTextRewriter.Rewrite"/>.</summary>
/// <param name="CommandText">The statement with every <c>:Name</c> placeholder replaced by <c>@Name</c>.</param>
/// <param name="ParameterNames">
/// Distinct placeholder names, without the marker, in order of first appearance - the order the
/// Delphi bound <c>array of Variant</c> values in.
/// </param>
/// <param name="HasRepeatedPlaceholder">
/// Whether any placeholder occurs more than once. Positional binding is rejected in that case,
/// because "n-th value goes to the n-th placeholder" is ambiguous once a name repeats.
/// </param>
public readonly record struct RewrittenSql(
    string CommandText,
    IReadOnlyList<string> ParameterNames,
    bool HasRepeatedPlaceholder);
