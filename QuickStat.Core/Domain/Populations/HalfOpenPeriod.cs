namespace QuickStat.Domain.Populations;

/// <summary>
/// A date range that <b>includes <paramref name="Start"/> and excludes <paramref name="Stop"/></b>.
/// </summary>
/// <param name="Start">First instant in the period, inclusive.</param>
/// <param name="Stop">First instant <em>after</em> the period. Not part of it.</param>
/// <remarks>
/// <para>
/// The name says "half-open" because the exclusive end is the single easiest thing to get wrong
/// here, and getting it wrong shifts every cohort by a day without any error (PORT-PLAN.md R8).
/// The Delphi period dialog states the same rule to the user in as many words: <i>"Angis som fra og
/// med første dato (til venstre), og til men ikke inkludert siste dato (til høyre)."</i>
/// (<c>Emetra.VclForm.Period.pas:36-42</c>).
/// </para>
/// <para>
/// Validation is strictly <c>Start &lt; Stop</c> - equal dates are rejected, so an empty period
/// cannot be expressed (<c>Emetra.VclForm.Period.pas:52</c>).
/// </para>
/// </remarks>
public readonly record struct HalfOpenPeriod(DateTime Start, DateTime Stop)
{
    /// <summary>Whether this is a period the period dialog would accept.</summary>
    public bool IsValid => Start < Stop;

    /// <summary>Whether an instant falls inside the period.</summary>
    /// <param name="value">Instant to test.</param>
    /// <returns><see langword="true"/> when <c>Start &lt;= value &lt; Stop</c>.</returns>
    public bool Contains(DateTime value) => value >= Start && value < Stop;
}
