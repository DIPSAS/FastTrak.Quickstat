namespace QuickStat.Data;

/// <summary>
/// One statement plus everything needed to bind and govern it.
/// </summary>
/// <remarks>
/// <para>
/// A record rather than a method-parameter list because the number of knobs only grows, and
/// because a request is worth logging and asserting on as a whole.
/// </para>
/// <para>
/// Placeholders stay in the Delphi's <c>:Name</c> form. That is not nostalgia: population SQL is
/// stored <em>in the database</em> (<c>dbo.DbProcList.SqlText</c>) and executed verbatim
/// (<c>CRF.Patient.List.pas:283</c>), so the client cannot change the syntax. The executor rewrites
/// <c>:Name</c> to <c>@Name</c> through <see cref="ISqlTextRewriter"/> immediately before binding.
/// </para>
/// </remarks>
public sealed record SqlRequest
{
    /// <summary>The statement, with <c>:Name</c> placeholders and <c>{IdList}</c> already expanded.</summary>
    public required string CommandText { get; init; }

    /// <summary>
    /// Values in order of first appearance of the placeholders - the shape of the Delphi's
    /// <c>array of Variant</c> (<c>Emetra.Database.Simple.pas:415-433</c>).
    /// </summary>
    /// <remarks>
    /// Mutually exclusive with <see cref="NamedValues"/>. Unlike the Delphi, which looped to
    /// <c>Parameters.Count</c> and read past the end of the array when too few values were
    /// supplied, a count mismatch is a <see cref="SqlParameterBindingException"/>. A statement that
    /// repeats a placeholder cannot be bound positionally and is rejected for the same reason.
    /// </remarks>
    public IReadOnlyList<object?> Values { get; init; } = [];

    /// <summary>Values by placeholder name; preferred for new code.</summary>
    /// <remarks>Mutually exclusive with <see cref="Values"/>. Lookup is case-insensitive.</remarks>
    public IReadOnlyDictionary<string, object?>? NamedValues { get; init; }

    /// <summary>Table-valued arguments, typically the person-id list.</summary>
    /// <remarks>
    /// The only way to pass more than 2 100 ids in one statement, and the reason
    /// <c>WHERE PersonId IN (4711,88,…)</c> disappears from the port (PORT-PLAN.md §7.3).
    /// </remarks>
    public IReadOnlyList<SqlTableParameter> TableParameters { get; init; } = [];

    /// <summary>
    /// Per-request timeout, or <see langword="null"/> for
    /// <see cref="QuickStat.Configuration.SqlOptions.DefaultCommandTimeout"/>.
    /// </summary>
    public TimeSpan? CommandTimeout { get; init; }

    /// <summary>
    /// Whether re-running this statement after a transient failure is safe.
    /// </summary>
    /// <remarks>
    /// Reads set this; writes must not. The Delphi retried everything up to ten times, so a dropped
    /// connection during <c>Report.AddSelectionMember</c> or <c>dbo.AddSession</c> produced
    /// duplicate rows (<c>Docs/Port/01-data-access.md</c> §5.1).
    /// </remarks>
    public bool IsIdempotent { get; init; }

    /// <summary>Short human-readable label for the log and the busy indicator.</summary>
    public string? Label { get; init; }
}
