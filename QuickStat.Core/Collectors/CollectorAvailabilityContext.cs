namespace QuickStat.Collectors;

/// <summary>
/// What a <see cref="CollectorAvailability"/> predicate gets to look at when the registry is built.
/// </summary>
/// <remarks>
/// Everything here is known before any collector runs, which is the point: availability is decided
/// once, at registration, so an unavailable collector never appears in the list and can never be
/// ticked.
/// </remarks>
public readonly record struct CollectorAvailabilityContext
{
    /// <summary>The study short name the gates were evaluated against.</summary>
    public required string StudyName { get; init; }

    /// <summary>The resolved study id.</summary>
    public required int StudyId { get; init; }

    /// <summary>
    /// Of the objects named in <see cref="CollectorAvailability.RequiredDatabaseObjects"/> across
    /// the whole registry, those for which <c>OBJECT_ID(name) IS NOT NULL</c>.
    /// </summary>
    /// <remarks>
    /// Probed in a single round trip for the whole registry, not once per collector. Compared
    /// case-insensitively, because object names are.
    /// </remarks>
    public required IReadOnlySet<string> ResolvedDatabaseObjects { get; init; }
}
