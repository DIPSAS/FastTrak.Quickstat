using System.Globalization;
using System.Runtime.CompilerServices;
using Xunit;

namespace QuickStat.Tests;

/// <summary>
/// Runs the whole suite under a chosen culture, so that "every test must be culture-independent" is
/// something anyone can check in one command instead of taking on trust.
/// </summary>
/// <remarks>
/// <para>
/// <c>dotnet test QuickStat.slnx -e QUICKSTAT_TEST_CULTURE=nb-NO</c>. With the variable unset —
/// which is every ordinary run, and CI — this does nothing at all and the machine's own culture
/// applies.
/// </para>
/// <para>
/// The rule it exists to enforce: this machine is <c>nn-NO</c>, so a display-format or comparison
/// assertion written without an explicit culture passes here and fails elsewhere. <b>The pair worth
/// sweeping is <c>nb-NO</c> and <c>en-US</c></b> — Bokmål because it is what the application
/// actually runs under in the field and it is <em>not</em> this machine's culture, and English
/// because build agents default to it.
/// </para>
/// <para>
/// <b>Deliberately not swept: <c>tr-TR</c>, <c>ar-SA</c>, <c>th-TH</c>.</b> They were swept during
/// Phase 3 and each cost a full extra run of the suite for scenarios this application never meets:
/// QuickStat ships to Norwegian hospitals. They did earn their keep once — <c>ar-SA</c>'s
/// non-Gregorian calendar caught <see cref="QuickStat.Data.SqlParameterFactory"/> throwing out of
/// its own error message, and <c>th-TH</c>'s collation caught an assertion using a linguistic
/// comparison where it meant a byte scan — but both fixes are permanent and correct on any locale,
/// so the sweeps now guard only against regressions that could not reach a user. Removed on
/// 2026-08-27 at the product owner's direction.
/// </para>
/// <para>
/// <b>Two</b> individual tests still pin behaviour <em>under</em> <c>tr-TR</c>, and that is a
/// different thing: there Turkish is a probe that makes "folds with the current culture" observable
/// at all, because nb-NO and en-US fold identically and cannot distinguish it from invariant. Both
/// were negative-controlled by switching the two folds to the invariant overloads — they are the
/// only tests in the suite that fail when that happens. See PORT-PLAN.md §8.8 (i).
/// </para>
/// </remarks>
internal static class CultureSweep
{
    /// <summary>The culture the run was asked for, or <see langword="null"/> for an ordinary run.</summary>
    internal static string? Requested { get; private set; }

    [ModuleInitializer]
    internal static void Apply()
    {
        Requested = Environment.GetEnvironmentVariable("QUICKSTAT_TEST_CULTURE");

        if (string.IsNullOrEmpty(Requested))
        {
            return;
        }

        CultureInfo culture = CultureInfo.GetCultureInfo(Requested);

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}

/// <summary>Proves the sweep is doing something, so a green run cannot be a false negative.</summary>
public class CultureSweepTests
{
    [Fact]
    public void TheRequestedCultureActuallyTook()
    {
        // Without this a sweep that silently failed to change anything would report identical green
        // runs and read as proof. That is not hypothetical - it happened on the first attempt, where
        // the variable was passed to a harness that no longer existed.
        if (string.IsNullOrEmpty(CultureSweep.Requested))
        {
            return;
        }

        Assert.Equal(CultureSweep.Requested, CultureInfo.CurrentCulture.Name);
    }
}
