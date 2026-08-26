using QuickStat.Configuration.Settings;
using Xunit;

namespace QuickStat.Tests.Configuration.Settings;

/// <summary>
/// The affordance step 2.3 uses to key a remembered period on a population's SQL (PORT-PLAN.md
/// §7.2).
/// </summary>
public class SettingsKeyTests
{
    private const string Sql = """
        SELECT p.PersonId, p.FullName
        FROM   dbo.Person p
        WHERE  p.Included >= :PeriodStart
        """;

    [Fact]
    public void AHashIsSixteenLowerCaseHexCharacters()
    {
        string hash = SettingsKey.Hash(Sql);

        Assert.Equal(SettingsKey.HashLength, hash.Length);
        Assert.All(hash, character => Assert.True(
            char.IsAsciiDigit(character) || character is >= 'a' and <= 'f',
            $"'{character}' is not lower-case hex."));
    }

    [Fact]
    public void TheSameTextAlwaysHashesTheSameWay()
    {
        // The Delphi's period key changed whenever the substituted arguments changed, so it never
        // found what it had written. A hash of the unsubstituted text is stable across runs and
        // across machines - which string.GetHashCode deliberately is not.
        Assert.Equal(SettingsKey.Hash(Sql), SettingsKey.Hash(Sql));
        Assert.Equal("e3b0c44298fc1c14", SettingsKey.Hash(string.Empty));
    }

    [Fact]
    public void DifferentTextHashesDifferently()
    {
        Assert.NotEqual(SettingsKey.Hash(Sql), SettingsKey.Hash(Sql + " "));
    }

    [Fact]
    public void HashingIsUnaffectedByLength()
    {
        Assert.Equal(SettingsKey.HashLength, SettingsKey.Hash(new string('x', 1_000_000)).Length);
    }

    [Fact]
    public void NorwegianCharactersHashAsUtf8()
    {
        // Not as UTF-16 and not as the current code page: the same text must produce the same key on
        // any machine.
        Assert.Equal("81933364db16cccd", SettingsKey.Hash("Fødselsnummer"));
    }

    [Fact]
    public void ForTextPutsTheHashBehindAReadablePrefix()
    {
        Assert.Equal($"Period:{SettingsKey.Hash(Sql)}", SettingsKey.ForText("Period", Sql));
    }

    [Fact]
    public void AGeneratedNameSurvivesTheStoreUnchanged()
    {
        string section = SettingsKey.ForText("Period", Sql);

        using TemporarySettingsFile file = new();

        using (IniSettingsStore writing = file.Open())
        {
            writing.SetDateTime(section, "Start", DateTime.UnixEpoch);
            writing.SetDateTime(section, "End", DateTime.UnixEpoch.AddDays(1));
            writing.Flush();
        }

        using IniSettingsStore reading = file.Open();

        Assert.Equal(DateTime.UnixEpoch, reading.GetDateTime(section, "Start", DateTime.MaxValue));
        Assert.Equal(DateTime.UnixEpoch.AddDays(1), reading.GetDateTime(section, "End", DateTime.MaxValue));
    }

    [Fact]
    public void ArgumentsAreValidated()
    {
        Assert.Throws<ArgumentNullException>(() => { _ = SettingsKey.Hash(null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = SettingsKey.ForText("Period", null!); });
        Assert.Throws<ArgumentException>(() => { _ = SettingsKey.ForText(string.Empty, Sql); });
        Assert.Throws<ArgumentNullException>(() => { _ = SettingsKey.ForText(null!, Sql); });
    }
}
