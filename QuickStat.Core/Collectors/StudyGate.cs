namespace QuickStat.Collectors;

/// <summary>
/// Which study families a collector is registered for.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: five independent <c>if TRegEx.IsMatch( fStudyId.StudyName, … )</c> blocks in
/// <c>TQuickStatCollectors.AddCollectorsHardCoded</c> (<c>QuickStat.Collectors.pas:417-490</c>).
/// They are independent <c>if</c>s and not <c>else if</c>s, so a study named <c>GBD_NDV_GWAS</c>
/// gets all three sets - hence <see cref="FlagsAttribute"/>.
/// </para>
/// <para>
/// The patterns themselves are in <see cref="StudyGatePatterns"/>.
/// </para>
/// </remarks>
[Flags]
public enum StudyGate
{
    /// <summary>Registered for every study. The default, and the value used for "ungated".</summary>
    Always = 0,

    /// <summary>
    /// <see cref="StudyGatePatterns.Gbd"/>. Also pulls in the diagnosis and drug families, which
    /// the Delphi registers from inside this block only.
    /// </summary>
    Gbd = 1 << 0,

    /// <summary><see cref="StudyGatePatterns.Ndv"/> - the diabetes set.</summary>
    Ndv = 1 << 1,

    /// <summary><see cref="StudyGatePatterns.Gwas"/>.</summary>
    Gwas = 1 << 2,

    /// <summary><see cref="StudyGatePatterns.Roas"/>.</summary>
    Roas = 1 << 3,

    /// <summary><see cref="StudyGatePatterns.Dogfood"/> - the only case-insensitive gate.</summary>
    Dogfood = 1 << 4,
}
