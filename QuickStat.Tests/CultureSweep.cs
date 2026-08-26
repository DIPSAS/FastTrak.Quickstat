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
/// <c>dotnet test QuickStat.slnx -e QUICKSTAT_TEST_CULTURE=tr-TR</c>. With the variable unset —
/// which is every ordinary run, and CI — this does nothing at all and the machine's own culture
/// applies.
/// </para>
/// <para>
/// The rule it exists to enforce: this machine is <c>nn-NO</c>, so a display-format or comparison
/// assertion written without an explicit culture passes here and fails on an English build agent.
/// <c>en-US</c> and <c>tr-TR</c> are the required pair — Turkish because its dotless <c>ı</c> makes
/// <c>ToUpper</c>/<c>ToLower</c> behave differently from every other Latin locale, which is exactly
/// what the two list filters and the collector sort depend on.
/// </para>
/// <para>
/// It is a permanent file rather than a throwaway because three separate Phase 3 agents each built
/// one, swept with it, and deleted it again. Two real defects were found this way and neither was a
/// formatting nicety: <c>SqlParameterFactory</c> threw
/// <see cref="ArgumentOutOfRangeException"/> out of its own error message under a non-Gregorian
/// calendar, and a collector-SQL assertion was using a collation where it meant a byte scan.
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
        // Without this a sweep that silently failed to change anything would report four identical
        // green runs and read as proof. That is not hypothetical - it happened on the first attempt,
        // where the variable was passed to a harness that no longer existed.
        if (string.IsNullOrEmpty(CultureSweep.Requested))
        {
            return;
        }

        Assert.Equal(CultureSweep.Requested, CultureInfo.CurrentCulture.Name);
    }
}
