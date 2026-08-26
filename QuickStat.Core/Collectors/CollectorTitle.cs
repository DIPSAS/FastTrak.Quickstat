using System.Globalization;

namespace QuickStat.Collectors;

/// <summary>
/// The title-suffix rules the Delphi collector <em>classes</em> applied in their constructors.
/// </summary>
/// <remarks>
/// <para>
/// These are rules, not string literals, and they get one home so they cannot diverge. In the
/// Delphi they live in four constructors across two units
/// (<c>EPR.QA.Collector.VarSet.pas:83,90,98,106</c> and
/// <c>EPR.QA.Collector.Labdata.pas:48-53</c>), which is why the registry sometimes double-applies
/// them: commit <c>8a9954c13</c> registered the literal <c>'Autommunitet (siste)'</c> and the
/// follow-up <c>08e35bd8d</c> had to strip it back to <c>'Autommunitet'</c> because
/// <c>TVarSetCollector</c> appends the suffix itself.
/// </para>
/// <para>
/// The <c>Autommunitet</c> misspelling is preserved on purpose: matching production output beats
/// fixing a typo (PORT-PLAN.md §8.3).
/// </para>
/// </remarks>
public static class CollectorTitle
{
    /// <summary>
    /// Appended by <c>TVarSetCollector</c>, <c>TVarSetAgeCollector</c> and
    /// <c>TFormAgeCollector</c>. Note the leading space.
    /// </summary>
    /// <remarks>Delphi: <c>TXT_LAST</c> (<c>EPR.QA.Collector.VarSet.pas:52</c>).</remarks>
    public const string LastSuffix = " (siste)";

    /// <summary>Appended by <c>TVarSetMaxCollector</c>. Note the leading space.</summary>
    /// <remarks>Delphi: <c>TXT_MAX</c> (<c>EPR.QA.Collector.VarSet.pas:53</c>).</remarks>
    public const string MaxSuffix = " (høyeste)";

    /// <summary>Wrapper applied by <c>TLabSetCollector</c> - but only conditionally.</summary>
    /// <remarks>Delphi: <c>StrTitleLabsetTemplate</c> (<c>EPR.QA.Collector.Names.pas:126</c>).</remarks>
    public const string LabSetTemplate = "Labdata: {0} (siste)";

    /// <summary>Applies <see cref="LastSuffix"/>.</summary>
    /// <param name="title">Registered title, without the suffix.</param>
    /// <returns>The displayed title.</returns>
    public static string WithLastSuffix(string title) => title + LastSuffix;

    /// <summary>Applies <see cref="MaxSuffix"/>.</summary>
    /// <param name="title">Registered title, without the suffix.</param>
    /// <returns>The displayed title.</returns>
    public static string WithMaxSuffix(string title) => title + MaxSuffix;

    /// <summary>Applies the lab-set wrapper, conditionally.</summary>
    /// <param name="groupName">The group name passed at registration.</param>
    /// <returns>
    /// <see cref="LabSetTemplate"/> filled with <paramref name="groupName"/> when the group name
    /// contains no colon; otherwise <paramref name="groupName"/> unchanged.
    /// </returns>
    /// <remarks>
    /// The colon test is what lets <c>QST_LAB_GERIATRIC</c> display as
    /// <c>GBD: Sentrale labdata (siste)</c> instead of being wrapped into
    /// <c>Labdata: GBD: … (siste)</c> (<c>EPR.QA.Collector.Labdata.pas:48-53</c>). Any group name
    /// that already carries a prefix opts itself out of the wrapper by having a colon in it.
    /// </remarks>
    public static string ForLabSet(string groupName) =>
        groupName.Contains(':', StringComparison.Ordinal)
            ? groupName
            : string.Format(CultureInfo.InvariantCulture, LabSetTemplate, groupName);
}
