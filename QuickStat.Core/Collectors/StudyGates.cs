using System.Text.RegularExpressions;

namespace QuickStat.Collectors;

/// <summary>
/// Evaluates the five study-name gates against a study name.
/// </summary>
/// <remarks>
/// <para>
/// The patterns are <see cref="StudyGatePatterns"/> and are used from there rather than retyped.
/// Everything else about the evaluation is a semantic that has to be preserved exactly, so it is
/// written down once here:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>Unanchored substring</b> matching, not equality. A study named <c>MYGBDTEST</c> opens
///     <see cref="StudyGate.Gbd"/>.
///   </description></item>
///   <item><description>
///     <b>Case-sensitive</b>, except <see cref="StudyGate.Dogfood"/>. The Delphi calls
///     <c>TRegEx.IsMatch</c> with no options for the first four and with <c>[roIgnoreCase]</c> for
///     the fifth, so <c>korttid</c> opens nothing and <c>dogfood</c> opens the dogfood gate.
///   </description></item>
///   <item><description>
///     <b>Independent</b>, not <c>else if</c>: the gates compose, and a study named
///     <c>GBD_NDV_GWAS</c> opens three of them.
///   </description></item>
/// </list>
/// <para>
/// The regular expressions are compiled once into static fields. They are the hot path of
/// <see cref="ICollectorRegistry.BuildAsync"/>, which runs on every project switch.
/// </para>
/// </remarks>
public static partial class StudyGates
{
    private static readonly (StudyGate Gate, Regex Pattern)[] Patterns =
    [
        (StudyGate.Gbd, GbdPattern()),
        (StudyGate.Ndv, NdvPattern()),
        (StudyGate.Gwas, GwasPattern()),
        (StudyGate.Roas, RoasPattern()),
        (StudyGate.Dogfood, DogfoodPattern()),
    ];

    /// <summary>Which gates a study name opens.</summary>
    /// <param name="studyName">
    /// <c>dbo.Study.StudName</c>, the short name. <see langword="null"/> or empty opens nothing.
    /// </param>
    /// <returns>
    /// The open gates combined, or <see cref="StudyGate.Always"/> when none matches. Note that
    /// <see cref="StudyGate.Always"/> is zero, so the return value is "the gated families this
    /// study additionally gets", not "everything it gets".
    /// </returns>
    public static StudyGate For(string? studyName)
    {
        if (string.IsNullOrEmpty(studyName))
        {
            return StudyGate.Always;
        }

        StudyGate open = StudyGate.Always;

        foreach ((StudyGate gate, Regex pattern) in Patterns)
        {
            if (pattern.IsMatch(studyName))
            {
                open |= gate;
            }
        }

        return open;
    }

    /// <summary>Whether a collector is registered for a study whose open gates are given.</summary>
    /// <param name="descriptor">The collector's descriptor.</param>
    /// <param name="openGates">The result of <see cref="For"/>.</param>
    /// <returns><see langword="true"/> when the collector should be registered.</returns>
    /// <remarks>
    /// <see cref="StudyGate.Always"/> is zero, so <c>Gate == Always</c> means "ungated" and the
    /// test is a plain flag intersection for everything else.
    /// </remarks>
    public static bool IsOpenFor(CollectorDescriptor descriptor, StudyGate openGates)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor.Gate == StudyGate.Always || (openGates & descriptor.Gate) != StudyGate.Always;
    }

    [GeneratedRegex(StudyGatePatterns.Gbd, RegexOptions.None)]
    private static partial Regex GbdPattern();

    [GeneratedRegex(StudyGatePatterns.Ndv, RegexOptions.None)]
    private static partial Regex NdvPattern();

    [GeneratedRegex(StudyGatePatterns.Gwas, RegexOptions.None)]
    private static partial Regex GwasPattern();

    [GeneratedRegex(StudyGatePatterns.Roas, RegexOptions.None)]
    private static partial Regex RoasPattern();

    /// <remarks>The only gate that ignores case, matching the Delphi's <c>[roIgnoreCase]</c>.</remarks>
    [GeneratedRegex(StudyGatePatterns.Dogfood, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DogfoodPattern();
}
