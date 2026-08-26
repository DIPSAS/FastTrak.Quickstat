using System.Security.Cryptography;
using System.Text;

namespace QuickStat.Configuration.Settings;

/// <summary>
/// Turns arbitrary text into a short, stable section or key name.
/// </summary>
/// <remarks>
/// <para>
/// The Delphi stored a population's remembered period under the <em>entire SQL text</em>, with the
/// parameter values already substituted in
/// (<c>EPR.PeriodDictionary.pas:65-66</c>, <c>:75-76</c>). Two things went wrong at once, and either
/// alone was fatal:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///     the section and key arguments were swapped, so the section was the literal string
///     <c>PeriodStart</c> and the key was the query;
///     </description>
///   </item>
///   <item>
///     <description>
///     the key changed every time the arguments changed, so even a key that <em>could</em> be
///     written would never be found again.
///     </description>
///   </item>
/// </list>
/// <para>
/// Step 2.3 owns the fix - hash the SQL for the key (PORT-PLAN.md §7.2,
/// <c>Docs/Port/01-data-access.md</c> §3.4) - and this is the affordance it hashes with.
/// <c>IniSettingsStore</c> would in fact survive the raw SQL, because it escapes <c>=</c>, brackets
/// and newlines in names; a hashed key is still the right answer, because it is short, stable, and
/// carries no fragment of a query into a file on disk.
/// </para>
/// <para>
/// Whether to normalise before hashing - trim, collapse whitespace, upper-case - is the caller's
/// decision, not this class's, because it determines which two queries count as the same query.
/// </para>
/// </remarks>
public static class SettingsKey
{
    /// <summary>
    /// The number of hex characters <see cref="Hash"/> returns.
    /// </summary>
    /// <remarks>
    /// Sixteen hex characters is 64 bits, matching <c>Docs/Port/01-data-access.md</c> §3.4. At
    /// the scale involved - a few hundred saved populations per user, ever - a collision is not a
    /// realistic concern, and the consequence of one would be two populations sharing a remembered
    /// period.
    /// </remarks>
    public const int HashLength = 16;

    /// <summary>
    /// Hashes text into a short lower-case hex token.
    /// </summary>
    /// <param name="text">Text to hash. May be any length and contain anything.</param>
    /// <returns><see cref="HashLength"/> lower-case hex characters.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// SHA-256 over the UTF-8 bytes, truncated. This is a naming device, not a security control;
    /// it is used because it is stable across processes and machines, which <c>string.GetHashCode</c>
    /// deliberately is not.
    /// </remarks>
    public static string Hash(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];

        SHA256.HashData(Encoding.UTF8.GetBytes(text), digest);

        return Convert.ToHexStringLower(digest)[..HashLength];
    }

    /// <summary>
    /// Builds a readable, collision-resistant name from a prefix and arbitrary text.
    /// </summary>
    /// <param name="prefix">A human-readable prefix, for example <c>Period</c>.</param>
    /// <param name="text">The text that identifies the entry, for example a population's SQL.</param>
    /// <returns><c>&lt;prefix&gt;:&lt;hash&gt;</c>.</returns>
    /// <exception cref="ArgumentException"><paramref name="prefix"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public static string ForText(string prefix, string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(prefix);

        return string.Concat(prefix, ":", Hash(text));
    }
}
