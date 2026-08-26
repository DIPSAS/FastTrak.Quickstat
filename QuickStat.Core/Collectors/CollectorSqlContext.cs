namespace QuickStat.Collectors;

/// <summary>Everything <see cref="ICollector.BuildSql"/> needs that varies per run.</summary>
/// <param name="StudyId">
/// Current study. Needed by the study-scoped collectors, which the Delphi implemented by overriding
/// <c>function SQL</c> because <c>fStudyId</c> is only known at <c>RunBatch</c> time.
/// </param>
/// <param name="IdListFragment">
/// What <c>{IdList}</c> expands to for this batch: <c>(SELECT PersonId FROM @pids)</c> for the
/// table-valued strategy, <c>(1,2,3)</c> for the literal fallback, and a fixed token such as
/// <c>(/*PIDS*/)</c> in golden-file tests, which is what makes the generated SQL deterministic.
/// </param>
public readonly record struct CollectorSqlContext(int StudyId, string IdListFragment);
