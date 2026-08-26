using System.Security.Cryptography;
using System.Text;

namespace QuickStat.Domain.Populations;

/// <summary>
/// Turns a population's SQL into a short, stable settings key, so the last period used for that
/// population can actually be remembered.
/// </summary>
/// <remarks>
/// <para>
/// PORT-PLAN.md §7.2, "Period settings key is the entire SQL text, with arguments swapped - never
/// round-trips". <c>TPeriodDictionary.TryGetPeriod</c> (<c>EPR.PeriodDictionary.pas:65-66, 75-76</c>)
/// passed the whole <c>EXEC … :StartDate, :StopDate</c> statement where the settings API expected a
/// key, so the answer was written under a multi-line key containing <c>=</c> and could never be read
/// back. Every prompt therefore opened on the defaults, yesterday and today.
/// </para>
/// <para>
/// The fix keeps the semantics - "remember the period per population" - and replaces the key with a
/// hash of the same text. Hashing the statement rather than the <c>ProcId</c> is deliberate: editing
/// a population's SQL server-side changes what the period means, and a changed key is the correct
/// answer to that.
/// </para>
/// </remarks>
public static class PeriodSettingsKey
{
    /// <summary>Settings section the period pair belongs in.</summary>
    /// <remarks>
    /// The Delphi had section and key the wrong way round, so its section was the literal string
    /// <c>PeriodStart</c>. Nothing readable was ever written under it, so there is no stored state to
    /// stay compatible with.
    /// </remarks>
    public const string SettingsSection = "Period";

    /// <summary>Appended to the key from <see cref="For"/> to store the start of the period.</summary>
    public const string StartKeySuffix = ".Start";

    /// <summary>Appended to the key from <see cref="For"/> to store the exclusive end of the period.</summary>
    public const string StopKeySuffix = ".Stop";

    /// <summary>Number of hash bytes kept. 16 bytes is 32 hex characters - short enough for an INI key.</summary>
    private const int KeyBytes = 16;

    /// <summary>Derives the settings key for one population's statement.</summary>
    /// <param name="sqlText">The population's <c>SqlText</c>, hashed exactly as stored.</param>
    /// <returns>A lower-case hexadecimal key, stable across runs and machines.</returns>
    public static string For(string sqlText)
    {
        ArgumentNullException.ThrowIfNull(sqlText);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(sqlText));
        return Convert.ToHexStringLower(hash.AsSpan(0, KeyBytes));
    }
}
