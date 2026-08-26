using System.Collections.ObjectModel;

namespace QuickStat.Domain.Populations;

/// <summary>The outcome of <see cref="IQueryParameterResolver.ResolveAsync"/>.</summary>
/// <remarks>
/// The Delphi returned a bare <c>boolean</c>, so the caller could not tell a deliberate cancel from
/// a broken population, and both ended up as silence. The distinction drives the UI: a cancel is
/// not an error and must not raise anything, whereas an unresolvable placeholder is a defect in the
/// stored population and must name the placeholder.
/// </remarks>
public sealed record ParameterResolution
{
    /// <summary>Whether every placeholder was resolved.</summary>
    public required bool Succeeded { get; init; }

    /// <summary>Placeholder name to value, keyed case-insensitively. Empty when <see cref="Succeeded"/> is false.</summary>
    public IReadOnlyDictionary<string, object?> Values { get; init; } = ReadOnlyDictionary<string, object?>.Empty;

    /// <summary>The user cancelled the period dialog. Not an error; show nothing.</summary>
    public bool CancelledByUser { get; init; }

    /// <summary>
    /// Why resolution failed, naming the offending placeholder. <see langword="null"/> on success
    /// and on cancellation.
    /// </summary>
    public string? FailureReason { get; init; }
}
