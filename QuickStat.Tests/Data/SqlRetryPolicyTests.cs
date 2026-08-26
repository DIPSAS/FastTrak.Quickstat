using QuickStat.Configuration;
using QuickStat.Data;
using Xunit;

namespace QuickStat.Tests.Data;

/// <summary>
/// Retry policy: which failures, how many times, how long between.
/// </summary>
public class SqlRetryPolicyTests
{
    private static SqlRetryPolicy Policy(SqlOptions? options = null) => new(options ?? new SqlOptions());

    [Theory]
    [InlineData(-2)]
    [InlineData(20)]
    [InlineData(64)]
    [InlineData(233)]
    [InlineData(1205)]
    [InlineData(10053)]
    [InlineData(10054)]
    [InlineData(10060)]
    [InlineData(10061)]
    [InlineData(40613)]
    public void RecognisesTransientFailures(int number) => Assert.True(SqlRetryPolicy.IsTransient(number));

    [Theory]
    [InlineData(208)]
    [InlineData(229)]
    [InlineData(50000)]
    [InlineData(0)]
    public void DoesNotRecogniseRealFailuresAsTransient(int number) => Assert.False(SqlRetryPolicy.IsTransient(number));

    [Fact]
    public void AnUnknownErrorNumberIsNotTransient() => Assert.False(SqlRetryPolicy.IsTransient(null));

    [Fact]
    public void RetriesAnIdempotentReadAfterATransientFailure() =>
        Assert.True(Policy().ShouldRetry(Transient(), isIdempotent: true, attempt: 1));

    [Fact]
    public void NeverRetriesANonIdempotentCommand()
    {
        // PORT-PLAN.md §7.2: the Delphi retried everything up to ten times, so a transient failure
        // during Report.AddSelectionMember or dbo.AddSession duplicated rows.
        Assert.False(Policy().ShouldRetry(Transient(), isIdempotent: false, attempt: 1));
    }

    [Fact]
    public void NeverRetriesANonTransientFailure() =>
        Assert.False(Policy().ShouldRetry(
            new SqlCommandFailedException("bad object name") { Number = 208 },
            isIdempotent: true,
            attempt: 1));

    [Fact]
    public void StopsAtTheConfiguredAttemptCount()
    {
        SqlRetryPolicy policy = Policy();

        Assert.Equal(3, policy.MaxAttempts);
        Assert.True(policy.ShouldRetry(Transient(), isIdempotent: true, attempt: 2));
        Assert.False(policy.ShouldRetry(Transient(), isIdempotent: true, attempt: 3));
    }

    [Fact]
    public void HonoursAConfiguredAttemptCount()
    {
        SqlRetryPolicy policy = Policy(new SqlOptions { MaxRetryAttempts = 5 });

        Assert.True(policy.ShouldRetry(Transient(), isIdempotent: true, attempt: 4));
        Assert.False(policy.ShouldRetry(Transient(), isIdempotent: true, attempt: 5));
    }

    [Fact]
    public void TreatsZeroAttemptsAsOne() => Assert.Equal(1, Policy(new SqlOptions { MaxRetryAttempts = 0 }).MaxAttempts);

    [Fact]
    public void BacksOffExponentially()
    {
        SqlRetryPolicy policy = Policy(new SqlOptions { RetryBaseDelay = TimeSpan.FromMilliseconds(100) });

        // base * 2^(attempt-1), plus up to a quarter as jitter.
        Assert.InRange(policy.DelayFor(1).TotalMilliseconds, 100, 125);
        Assert.InRange(policy.DelayFor(2).TotalMilliseconds, 200, 250);
        Assert.InRange(policy.DelayFor(3).TotalMilliseconds, 400, 500);
    }

    [Fact]
    public void CapsTheBackoff()
    {
        SqlRetryPolicy policy = Policy(new SqlOptions { RetryBaseDelay = TimeSpan.FromSeconds(10) });

        Assert.InRange(policy.DelayFor(12).TotalSeconds, 30, 37.5);
    }

    private static SqlCommandFailedException Transient() =>
        new("A transport-level error occurred.") { Number = 10054 };
}
