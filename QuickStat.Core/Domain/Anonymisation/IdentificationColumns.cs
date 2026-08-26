namespace QuickStat.Domain.Anonymisation;

/// <summary>
/// Which identity columns a <see cref="PersonIdentification"/> mode allows. Derived in exactly one
/// place.
/// </summary>
/// <remarks>
/// <para>
/// The whole reason this type exists is PORT-PLAN.md §7.2: in the Delphi, display anonymity and
/// export anonymity are <em>independent code paths</em> that merely happen to be driven by the same
/// radio group. Display hides columns by setting their width to <c>-1</c>
/// (<c>EPR.QA.GUI.Grid.pas:321-331</c>) and infers its own state by reading a width back; export
/// re-decides from the radio buttons at save time. Nothing enforces that they agree, and column
/// resizing is enabled, so a user can widen a "hidden" column back into view.
/// </para>
/// <para>
/// Both the grid and the exporter must call <see cref="For"/>. Neither may branch on
/// <see cref="PersonIdentification"/> itself.
/// </para>
/// </remarks>
public readonly record struct IdentificationColumns
{
    /// <summary>Always <see langword="true"/>: some person column is always written.</summary>
    public required bool IncludesPersonId { get; init; }

    /// <summary>Whether the <c>Født</c> column is present at all.</summary>
    public required bool IncludesDateOfBirth { get; init; }

    /// <summary>Whether the <c>Fødselsnummer</c> column is present at all.</summary>
    public required bool IncludesNationalId { get; init; }

    /// <summary>Whether the <c>Navn</c> column is present at all.</summary>
    public required bool IncludesName { get; init; }

    /// <summary>
    /// Whether the person id shown is a pseudonym rather than the real
    /// <c>PersonId</c>.
    /// </summary>
    public required bool UsesPseudonyms { get; init; }

    /// <summary>The one place a mode is turned into a column set.</summary>
    /// <param name="identification">The selected mode.</param>
    /// <returns>Which columns that mode allows.</returns>
    public static IdentificationColumns For(PersonIdentification identification) =>
        throw new NotImplementedException();
}
