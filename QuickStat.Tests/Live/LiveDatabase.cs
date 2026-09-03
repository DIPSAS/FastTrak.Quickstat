using System.Globalization;
using Xunit;

namespace QuickStat.Tests.Live;

/// <summary>
/// How a run opts in to the handful of tests that need a real SQL Server, and which one they get.
/// </summary>
/// <remarks>
/// <para>
/// <b>The suite is hermetic by default and stays that way.</b> Every test in this folder is
/// <em>skipped</em> unless <see cref="ConnectionVariable"/> names a database, so
/// <c>dotnet test QuickStat.slnx</c> on a machine with no server behaves exactly as it did before
/// this folder existed - PORT-PLAN.md §9 R9 is relaxed, not withdrawn. Skipped rather than passed:
/// a test that quietly returns when its precondition is missing is a test that reports success for
/// having done nothing.
/// </para>
/// <para>
/// <b>Why any of this exists.</b> Two of Phase 5's defects were invisible to all 2 622 hermetic
/// tests by construction - <c>PersonIdListTypeName</c> named a table type that has never existed on
/// any server, and the ICU/NLS sort collision only appears once <c>Report.GetFormClasses</c> rows
/// join the data-element list. Both needed a catalogue. Until now the only thing that had one was a
/// scratch console outside the repository, rewritten from memory for each investigation and thrown
/// away afterwards; PORT-PLAN.md §8.11 (3) and §8.14 rest on runs nobody can now reproduce. A
/// gated test is the same capability under review, in version control, re-runnable on demand.
/// </para>
/// <para>
/// <b>These tests may read patient data; they must never emit it.</b> PORT-PLAN.md R6 treats a
/// privacy regression as release-blocking, and a test that prints a national id into a CI log is
/// one. The rules, which every test here follows: assert on <em>counts</em> and on <em>shape</em>,
/// never on a value; put no field content in an assertion message; write any exported file to a
/// caller-owned temporary path and delete it in a <c>finally</c>.
/// </para>
/// </remarks>
internal static class LiveDatabase
{
    /// <summary>The connection string. Setting it is what opts a run in.</summary>
    internal const string ConnectionVariable = "QUICKSTAT_LIVE_CONNECTION";

    /// <summary>The study name the session is gated on. Defaults to <see cref="DefaultStudyName"/>.</summary>
    /// <remarks>
    /// It is not cosmetic: <c>StudyGatePatterns</c> keys the whole 38-collector drug family off
    /// <c>GBD|LANGTID|KORTTID</c> and the NDV family off its own pattern, so the wrong name here is a
    /// shorter data-element list rather than an error.
    /// </remarks>
    internal const string StudyVariable = "QUICKSTAT_LIVE_STUDY";

    /// <summary>The population to load, by <c>ProcId</c>. Defaults to <see cref="DefaultProcId"/>.</summary>
    internal const string PopulationVariable = "QUICKSTAT_LIVE_POPULATION";

    /// <summary>The study the Phase 5 runs used.</summary>
    internal const string DefaultStudyName = "NDV";

    /// <summary>
    /// <c>Alle testpersoner</c> - the population PORT-PLAN.md §8.11 (3) measured, 281 patients of
    /// whom 280 have a national id on file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Chosen for its cohort, not for its plumbing: on <c>EFT00028_TEST_020</c> it is the only
    /// population that returns patients at all, because ProcId 23 emptied study 2 on 2026-09-01.
    /// </para>
    /// <para>
    /// <b>It supplies <c>NationalId</c> itself</b> - <c>dbo.GetCaseListTest</c> projects
    /// <c>PersonId, DOB, FullName, GroupName, GenderId, NationalId, InfoText</c>, checked with
    /// <c>sp_describe_first_result_set</c>. So <c>NationalIdRecovery.IncludesNationalId</c> answers
    /// true and the recovery query never runs for this population. That is why there are two live
    /// tests and not one: the export test proves a real id reaches a real file, and a separate test
    /// exercises the recovery path, which no population on this database can reach - every procedure
    /// that omits the column returns an empty cohort here.
    /// </para>
    /// </remarks>
    internal const int DefaultProcId = 14;

    /// <summary>Whether this run opted in.</summary>
    internal static bool IsConfigured => ConnectionString is not null;

    /// <summary>The connection string, or <see langword="null"/> when the run did not opt in.</summary>
    internal static string? ConnectionString => Read(ConnectionVariable);

    /// <summary>The study name to connect as.</summary>
    internal static string StudyName => Read(StudyVariable) ?? DefaultStudyName;

    /// <summary>The population to load.</summary>
    internal static int ProcId =>
        int.TryParse(Read(PopulationVariable), NumberStyles.Integer, CultureInfo.InvariantCulture, out int procId)
            ? procId
            : DefaultProcId;

    /// <summary>Why a test in this folder was skipped, worded so the reader can act on it.</summary>
    internal static string SkipReason =>
        $"Needs a live database. Set {ConnectionVariable} to a connection string "
        + $"(optionally {StudyVariable}, default {DefaultStudyName}; "
        + $"{PopulationVariable}, default {DefaultProcId}).";

    private static string? Read(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
}

/// <summary>
/// A <see cref="FactAttribute"/> that skips itself when no live database is configured.
/// </summary>
/// <remarks>
/// xUnit v2 has no run-time skip, and this project is pinned to v2 on purpose - the header of
/// <c>QuickStat.Tests.csproj</c> records that v3 makes <c>dotnet test</c> report "Zero tests ran".
/// Setting <see cref="FactAttribute.Skip"/> from the constructor is the v2 idiom: attributes are
/// constructed during discovery, so the environment is read then and the test is reported as
/// skipped rather than passed. It needs no extra package.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class LiveDatabaseFactAttribute : FactAttribute
{
    /// <summary>Creates the attribute, skipping the test unless the environment opts in.</summary>
    public LiveDatabaseFactAttribute()
    {
        if (!LiveDatabase.IsConfigured)
        {
            Skip = LiveDatabase.SkipReason;
        }
    }
}
