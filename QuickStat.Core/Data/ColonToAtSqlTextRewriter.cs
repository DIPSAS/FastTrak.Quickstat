using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text;

namespace QuickStat.Data;

/// <summary>
/// The hand-written scanner that turns <c>:Name</c> into <c>@Name</c>.
/// </summary>
/// <remarks>
/// <para>
/// PORT-PLAN.md R2. Population SQL is stored <em>in the database</em>, not in this repository: it is
/// arbitrary T-SQL written by users over many years, and the rewriter sees all of it. A regular
/// expression over the raw text would corrupt any statement containing a colon in one of the five
/// places a colon legally occurs without being a placeholder, so this is a character scanner that
/// skips, in full:
/// </para>
/// <list type="bullet">
///   <item><description><c>'single-quoted literals'</c>, with the <c>''</c> escape - <c>'23:59'</c>.</description></item>
///   <item><description><c>[bracketed identifiers]</c>, with the <c>]]</c> escape.</description></item>
///   <item><description><c>"quoted identifiers"</c>, with the <c>""</c> escape.</description></item>
///   <item><description><c>--</c> line comments, to the next carriage return or line feed.</description></item>
///   <item><description><c>/* */</c> block comments, <em>nested</em>, which T-SQL allows.</description></item>
///   <item><description>the <c>::</c> scope-resolution operator - <c>::fn_helpcollations()</c>.</description></item>
/// </list>
/// <para>
/// A placeholder is a <c>:</c> that is not the first character of a <c>::</c> pair, followed
/// immediately by <c>[A-Za-z_][A-Za-z0-9_]*</c>. Everything else is copied through byte for byte.
/// </para>
/// <para>
/// <strong>Deliberate divergence from Delphi.</strong> <c>TParams.ParseSQL</c> knows about quoted
/// literals and <c>::</c> but not about comments or bracketed identifiers, so it would have
/// discovered a placeholder inside <c>-- :Foo</c> or inside <c>[a:b]</c> and bound a value to it.
/// Skipping those is the behaviour PORT-PLAN.md R2 asks for; it is strictly more correct, and a
/// statement that relied on the old behaviour could never have executed in the first place.
/// </para>
/// <para>
/// When a name repeats under a different casing the rewritten text uses the casing of the
/// <em>first</em> occurrence, so the emitted statement is internally consistent even against a
/// case-sensitive server collation.
/// </para>
/// </remarks>
public sealed class ColonToAtSqlTextRewriter : ISqlTextRewriter
{
    /// <summary>
    /// Statements held in the rewrite cache before it is dropped wholesale.
    /// </summary>
    /// <remarks>
    /// Collector SQL is re-issued once per batch, so caching matters; the population catalogue is
    /// bounded and small. A plain cap with a full clear is enough - there is no eviction policy
    /// worth the complexity here.
    /// </remarks>
    public const int CacheCapacity = 512;

    private readonly ConcurrentDictionary<string, RewrittenSql> _cache = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public RewrittenSql Rewrite(string commandText)
    {
        ArgumentNullException.ThrowIfNull(commandText);

        if (_cache.TryGetValue(commandText, out RewrittenSql cached))
        {
            return cached;
        }

        RewrittenSql rewritten = Scan(commandText);

        if (_cache.Count >= CacheCapacity)
        {
            _cache.Clear();
        }

        _cache[commandText] = rewritten;
        return rewritten;
    }

    /// <summary>The scanner itself, with no caching, so tests can drive it directly.</summary>
    /// <param name="sql">Statement in <c>:Name</c> form.</param>
    /// <returns>The rewritten statement and its placeholders.</returns>
    internal static RewrittenSql Scan(string sql)
    {
        int length = sql.Length;
        int index = 0;
        int copiedUpTo = 0;
        StringBuilder? builder = null;
        List<string> names = [];
        Dictionary<string, string> canonical = new(StringComparer.OrdinalIgnoreCase);
        bool repeated = false;

        while (index < length)
        {
            char current = sql[index];

            switch (current)
            {
                case '\'':
                case '"':
                    index = SkipDelimited(sql, index, current);
                    continue;

                case '[':
                    index = SkipDelimited(sql, index, ']');
                    continue;

                case '-' when index + 1 < length && sql[index + 1] == '-':
                    index = SkipLineComment(sql, index);
                    continue;

                case '/' when index + 1 < length && sql[index + 1] == '*':
                    index = SkipBlockComment(sql, index);
                    continue;

                case ':' when index + 1 < length && sql[index + 1] == ':':
                    // Scope resolution, e.g. '::fn_helpcollations()'. Consuming both colons also
                    // guarantees the second one can never start a placeholder.
                    index += 2;
                    continue;

                case ':' when index + 1 < length && IsNameStart(sql[index + 1]):
                    int nameStart = index + 1;
                    int nameEnd = nameStart;

                    while (nameEnd < length && IsNamePart(sql[nameEnd]))
                    {
                        nameEnd++;
                    }

                    string name = sql[nameStart..nameEnd];

                    if (canonical.TryGetValue(name, out string? firstSeen))
                    {
                        repeated = true;
                        name = firstSeen;
                    }
                    else
                    {
                        canonical.Add(name, name);
                        names.Add(name);
                    }

                    builder ??= new StringBuilder(length + 8);
                    builder.Append(sql, copiedUpTo, index - copiedUpTo);
                    builder.Append('@').Append(name);
                    copiedUpTo = nameEnd;
                    index = nameEnd;
                    continue;

                default:
                    index++;
                    continue;
            }
        }

        // Wrapped, not handed over: the result is cached and shared, so a caller that cast the list
        // back to List<string> and mutated it would corrupt every later rewrite of the statement.
        ReadOnlyCollection<string> frozen = names.AsReadOnly();

        if (builder is null)
        {
            return new RewrittenSql(sql, frozen, repeated);
        }

        builder.Append(sql, copiedUpTo, length - copiedUpTo);
        return new RewrittenSql(builder.ToString(), frozen, repeated);
    }

    private static bool IsNameStart(char c) => char.IsAsciiLetter(c) || c == '_';

    private static bool IsNamePart(char c) => char.IsAsciiLetterOrDigit(c) || c == '_';

    /// <summary>
    /// Consumes a run delimited by <paramref name="closing"/>, honouring the doubled-delimiter
    /// escape that T-SQL uses for all three forms (<c>''</c>, <c>""</c>, <c>]]</c>).
    /// </summary>
    /// <param name="sql">The statement.</param>
    /// <param name="start">Index of the opening delimiter.</param>
    /// <param name="closing">The closing delimiter.</param>
    /// <returns>The index just past the closing delimiter, or the end of the statement.</returns>
    private static int SkipDelimited(string sql, int start, char closing)
    {
        int length = sql.Length;
        int index = start + 1;

        while (index < length)
        {
            if (sql[index] == closing)
            {
                if (index + 1 < length && sql[index + 1] == closing)
                {
                    index += 2;
                    continue;
                }

                return index + 1;
            }

            index++;
        }

        // Unterminated. Consuming the remainder is the safe choice: the statement will not execute
        // anyway, and stopping here cannot invent a placeholder out of quoted text.
        return length;
    }

    private static int SkipLineComment(string sql, int start)
    {
        int length = sql.Length;
        int index = start + 2;

        while (index < length && sql[index] != '\n' && sql[index] != '\r')
        {
            index++;
        }

        return index;
    }

    private static int SkipBlockComment(string sql, int start)
    {
        int length = sql.Length;
        int index = start;
        int depth = 0;

        while (index < length)
        {
            if (index + 1 < length && sql[index] == '/' && sql[index + 1] == '*')
            {
                depth++;
                index += 2;
                continue;
            }

            if (index + 1 < length && sql[index] == '*' && sql[index + 1] == '/')
            {
                depth--;
                index += 2;

                if (depth == 0)
                {
                    return index;
                }

                continue;
            }

            index++;
        }

        return length;
    }
}
