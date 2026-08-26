namespace QuickStat.Collectors.Registry;

/// <summary>One gated block of the Delphi registry: the gate, and the collectors it admits.</summary>
/// <param name="Gate">The gate that must be open. Never <see cref="StudyGate.Always"/>.</param>
/// <param name="Collectors">The collectors, in registration order.</param>
/// <remarks>
/// Modelled as data rather than as five <c>if</c> blocks so that the order of the blocks, which is
/// observable in the check-list, is a list that a test can assert on.
/// </remarks>
public readonly record struct GatedCollectorFamily(StudyGate Gate, IReadOnlyList<ICollector> Collectors);
