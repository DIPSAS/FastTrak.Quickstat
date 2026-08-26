namespace QuickStat.Collectors;

/// <summary>One row of <c>EXEC Report.GetFormClasses :StudyId</c>.</summary>
/// <param name="FormName">
/// <c>FormName</c> - the stable code, e.g. <c>BARTHEL</c>. Becomes part of two collector names and
/// of every column those collectors produce.
/// </param>
/// <param name="FormTitle">
/// <c>FormTitle</c> - the human-readable name shown inside the parentheses of the two titles.
/// </param>
public readonly record struct FormClass(string FormName, string FormTitle);
