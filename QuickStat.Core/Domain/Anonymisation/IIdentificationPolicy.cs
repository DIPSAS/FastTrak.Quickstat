namespace QuickStat.Domain.Anonymisation;

/// <summary>
/// The single, shared answer to "how identified is this dataset?".
/// </summary>
/// <remarks>
/// <para>
/// Registered as a singleton and injected into both the grid view-model and whatever builds
/// <see cref="QuickStat.Export.DatasetExportOptions"/>, so there is exactly one value and the two
/// cannot disagree. This is the structural half of the fix; <see cref="IdentificationColumns"/> is
/// the derivational half.
/// </para>
/// <para>
/// The Delphi's third state - "no radio button checked" - raised
/// <c>EAbort('Unhandled identification strategy.')</c> at export time
/// (<c>MainQuickStat.pas:914</c>). An enum cannot be in that state.
/// </para>
/// </remarks>
public interface IIdentificationPolicy
{
    /// <summary>The current mode. Setting it raises <see cref="ModeChanged"/>.</summary>
    PersonIdentification Mode { get; set; }

    /// <summary>The column set implied by <see cref="Mode"/>.</summary>
    IdentificationColumns Columns { get; }

    /// <summary>
    /// Raised after <see cref="Mode"/> changes, so the grid can re-render and any cached export
    /// options can be discarded.
    /// </summary>
    event EventHandler<PersonIdentification>? ModeChanged;
}
