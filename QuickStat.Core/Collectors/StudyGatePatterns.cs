namespace QuickStat.Collectors;

/// <summary>
/// The five regular expressions that decide which collectors a study gets, transcribed verbatim.
/// </summary>
/// <remarks>
/// <para>
/// These live in the contract rather than inside the registry because they are the single easiest
/// thing in the port to get wrong. Two of them are near-identical literals differing by one
/// alternative, and the most recent functional change to the Delphi application - commit
/// <c>5502b72</c>, "#739506: Ta med GBD-utvalet i Korttid i QuickStat" - consists of adding
/// <c>KORTTID</c> to <em>both</em> (PORT-PLAN.md §10.4). Losing one of the two silently halves what
/// a KORTTID study can collect, and nothing fails.
/// </para>
/// <para>
/// Semantics that must be preserved exactly:
/// </para>
/// <list type="bullet">
///   <item><description>
///     Unanchored substring matches, not equality. A study named <c>MYGBDTEST</c> matches
///     <see cref="Gbd"/>.
///   </description></item>
///   <item><description>
///     Case-<b>sensitive</b>, except <see cref="Dogfood"/>. <c>korttid</c> matches nothing;
///     <c>dogfood</c> matches.
///   </description></item>
///   <item><description>
///     Evaluated independently, so the gates compose.
///   </description></item>
/// </list>
/// <para>
/// The acceptance target is 124 registered collectors for a <c>KORTTID</c> study out of 131 distinct
/// names, counted against the canonical application (PORT-PLAN.md §10.3-§10.4). The 120/126 figures
/// in <c>Docs/Port/03-collectors.md</c> §D.2 describe this repository's reduced copy.
/// </para>
/// </remarks>
public static class StudyGatePatterns
{
    /// <summary>Gate <c>G</c>: the GBD set, plus the diagnosis and drug families.</summary>
    public const string Gbd = "GBD|LANGTID|KORTTID";

    /// <summary>Gate <c>N</c>: the NDV / diabetes set.</summary>
    public const string Ndv = "NDV|ENDO|LANGTID|GBD|KORTTID";

    /// <summary>Gate <c>W</c>.</summary>
    public const string Gwas = "GWAS";

    /// <summary>Gate <c>R</c>.</summary>
    public const string Roas = "ROAS";

    /// <summary>Gate <c>D</c>. Matched case-insensitively; the other four are not.</summary>
    public const string Dogfood = "DOGFOOD";
}
