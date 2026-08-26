namespace QuickStat.Data;

/// <summary>One scalar argument, already matched to a placeholder.</summary>
/// <param name="Name">Placeholder name without the marker.</param>
/// <param name="Value">The value; <see langword="null"/> means <c>NULL</c>.</param>
internal readonly record struct BoundParameter(string Name, object? Value);

/// <summary>
/// A <see cref="SqlRequest"/> after rewriting and binding: everything the session needs and nothing
/// it has to work out for itself.
/// </summary>
/// <remarks>
/// The split exists so that binding - the part with the interesting rules and the interesting
/// failure modes - is a pure function that can be tested exhaustively without a server
/// (PORT-PLAN.md §9 R9).
/// </remarks>
internal sealed record BoundSqlCommand
{
    /// <summary>The statement with <c>@Name</c> placeholders.</summary>
    public required string CommandText { get; init; }

    /// <summary>Scalar arguments, in placeholder order.</summary>
    public required IReadOnlyList<BoundParameter> Parameters { get; init; }

    /// <summary>Table-valued arguments.</summary>
    public required IReadOnlyList<SqlTableParameter> TableParameters { get; init; }

    /// <summary>Resolved timeout: the request's own, or the configured default.</summary>
    public required TimeSpan CommandTimeout { get; init; }

    /// <summary>Log label, or <see langword="null"/>.</summary>
    public string? Label { get; init; }
}
