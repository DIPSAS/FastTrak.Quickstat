using System.Text;

namespace QuickStat.Configuration.Settings;

/// <summary>
/// Reading and writing the on-disk form of the settings file.
/// </summary>
/// <remarks>
/// <para>
/// The Delphi went through <c>WritePrivateProfileString</c>, which imposes the Win32 INI
/// limitations: a key cannot contain <c>=</c>, nothing can contain a line break, and leading or
/// trailing spaces are silently lost. That is not a detail - it is the mechanism by which the
/// remembered period never round-tripped, because the key was a multi-line SQL query
/// (<c>Docs/Port/01-data-access.md</c> §6.3).
/// </para>
/// <para>
/// This reader and writer are the only users of the file, so the format can be INI-shaped without
/// being Win32 INI. Escaping is backslash-based:
/// </para>
/// <list type="table">
///   <listheader><term>Escape</term><description>Meaning</description></listheader>
///   <item><term><c>\\</c></term><description>a backslash</description></item>
///   <item><term><c>\r</c></term><description>carriage return</description></item>
///   <item><term><c>\n</c></term><description>line feed</description></item>
///   <item><term><c>\t</c></term><description>tab</description></item>
///   <item><term><c>\s</c></term><description>a space, used only where one would otherwise be trimmed</description></item>
///   <item><term><c>\0</c></term><description>NUL</description></item>
///   <item>
///     <term><c>\=</c> <c>\[</c> <c>\]</c> <c>\;</c> <c>\#</c></term>
///     <description>the character itself, where it would otherwise change what the line means</description>
///   </item>
/// </list>
/// <para>
/// Any other backslash is <strong>kept as written</strong>, backslash included. That is the
/// difference between mangling and merely not helping: a hand-edited
/// <c>LastExportFolder=C:\Users\ola</c> survives, where dropping unknown backslashes would silently
/// turn it into <c>C:Usersola</c>. It is not a complete rescue - <c>\t</c>, <c>\n</c>, <c>\r</c>,
/// <c>\s</c> and <c>\0</c> still mean what they say, so <c>C:\temp</c> hand-written is still a
/// tab - which is why the writer escapes and the file says so at the top.
/// </para>
/// </remarks>
internal static class IniFileFormat
{
    /// <summary>The comment block written at the top of every settings file.</summary>
    internal static readonly string[] HeaderLines =
    [
        "; QuickStat settings. Written by the application; safe to delete, safe to read.",
        ";",
        "; Escapes inside a section name, a key or a value:",
        ";   \\\\ backslash   \\r return   \\n line feed   \\t tab   \\s space   \\0 nul",
        ";   \\= \\[ \\] \\; \\# mean the character itself. Any other backslash is kept as written.",
        ";",
        "; Personal identifiers are removed before anything is written here.",
    ];

    /// <summary>Escapes text so it can be a value on the right of an <c>=</c>.</summary>
    /// <param name="text">Raw text.</param>
    /// <returns>Escaped text.</returns>
    internal static string EscapeValue(string text) => Escape(text, EscapeContext.Value);

    /// <summary>Escapes text so it can be a key on the left of an <c>=</c>.</summary>
    /// <param name="text">Raw text.</param>
    /// <returns>Escaped text.</returns>
    internal static string EscapeKey(string text) => Escape(text, EscapeContext.Key);

    /// <summary>Escapes text so it can sit between the square brackets of a section header.</summary>
    /// <param name="text">Raw text.</param>
    /// <returns>Escaped text.</returns>
    internal static string EscapeSection(string text) => Escape(text, EscapeContext.Section);

    /// <summary>Reverses any of the three escape functions.</summary>
    /// <param name="text">Escaped text.</param>
    /// <returns>The original text.</returns>
    internal static string Unescape(ReadOnlySpan<char> text)
    {
        if (text.IndexOf('\\') < 0)
        {
            return text.ToString();
        }

        StringBuilder builder = new(text.Length);

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '\\')
            {
                builder.Append(text[i]);

                continue;
            }

            if (i == text.Length - 1)
            {
                // A trailing lone backslash is not something this writer emits. Keep it rather than
                // lose it.
                builder.Append('\\');

                break;
            }

            i++;

            switch (text[i])
            {
                case 'r':
                    builder.Append('\r');

                    break;

                case 'n':
                    builder.Append('\n');

                    break;

                case 't':
                    builder.Append('\t');

                    break;

                case 's':
                    builder.Append(' ');

                    break;

                case '0':
                    builder.Append('\0');

                    break;

                case '\\' or '=' or '[' or ']' or ';' or '#':
                    builder.Append(text[i]);

                    break;

                default:
                    // Not an escape this writer produces. Keep it verbatim rather than eat the
                    // backslash out of a hand-written path.
                    builder.Append('\\').Append(text[i]);

                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Classifies and decodes one line of a settings file.
    /// </summary>
    /// <param name="line">The raw line, without its terminator.</param>
    /// <param name="section">The decoded section name when the result is <see cref="IniLineKind.Section"/>.</param>
    /// <param name="key">The decoded key when the result is <see cref="IniLineKind.Entry"/>.</param>
    /// <param name="value">The decoded value when the result is <see cref="IniLineKind.Entry"/>.</param>
    /// <returns>What kind of line it was.</returns>
    internal static IniLineKind ParseLine(
        string line,
        out string section,
        out string key,
        out string value)
    {
        section = string.Empty;
        key = string.Empty;
        value = string.Empty;

        ReadOnlySpan<char> trimmed = line.AsSpan().Trim();

        if (trimmed.IsEmpty)
        {
            return IniLineKind.Blank;
        }

        if (trimmed[0] is ';' or '#')
        {
            return IniLineKind.Comment;
        }

        if (trimmed[0] == '[')
        {
            if (trimmed.Length < 2 || trimmed[^1] != ']' || IsEscaped(trimmed, trimmed.Length - 1))
            {
                return IniLineKind.Unparsable;
            }

            section = Unescape(trimmed[1..^1]);

            return IniLineKind.Section;
        }

        int separator = IndexOfUnescaped(trimmed, '=');

        if (separator < 0)
        {
            return IniLineKind.Unparsable;
        }

        ReadOnlySpan<char> rawKey = trimmed[..separator].TrimEnd();

        if (rawKey.IsEmpty)
        {
            return IniLineKind.Unparsable;
        }

        key = Unescape(rawKey);
        value = Unescape(trimmed[(separator + 1)..].TrimStart());

        return IniLineKind.Entry;
    }

    private static string Escape(string text, EscapeContext context)
    {
        if (text.Length == 0)
        {
            return text;
        }

        StringBuilder builder = new(text.Length + 8);

        for (int i = 0; i < text.Length; i++)
        {
            char current = text[i];
            bool atEdge = i == 0 || i == text.Length - 1;

            switch (current)
            {
                case '\\':
                    builder.Append(@"\\");

                    break;

                case '\r':
                    builder.Append(@"\r");

                    break;

                case '\n':
                    builder.Append(@"\n");

                    break;

                case '\t':
                    builder.Append(@"\t");

                    break;

                case '\0':
                    builder.Append(@"\0");

                    break;

                // Only the first and last space need escaping; the reader trims the line, not the
                // interior.
                case ' ' when atEdge:
                    builder.Append(@"\s");

                    break;

                case '=' when context == EscapeContext.Key:
                    builder.Append(@"\=");

                    break;

                case '[' when context == EscapeContext.Section:
                    builder.Append(@"\[");

                    break;

                case ']' when context == EscapeContext.Section:
                    builder.Append(@"\]");

                    break;

                // A key that starts with one of these would be read back as a comment or a section
                // header. Interior occurrences are harmless.
                case '[' or ';' or '#' when context == EscapeContext.Key && i == 0:
                    builder.Append('\\');
                    builder.Append(current);

                    break;

                default:
                    builder.Append(current);

                    break;
            }
        }

        return builder.ToString();
    }

    private static int IndexOfUnescaped(ReadOnlySpan<char> text, char target)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == target && !IsEscaped(text, i))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Whether the character at <paramref name="index"/> is preceded by an odd number of
    /// backslashes, and is therefore escaped rather than significant.
    /// </summary>
    private static bool IsEscaped(ReadOnlySpan<char> text, int index)
    {
        int backslashes = 0;

        for (int i = index - 1; i >= 0 && text[i] == '\\'; i--)
        {
            backslashes++;
        }

        return (backslashes % 2) == 1;
    }

    private enum EscapeContext
    {
        Value,
        Key,
        Section,
    }
}

/// <summary>What one line of a settings file turned out to be.</summary>
internal enum IniLineKind
{
    /// <summary>Whitespace only.</summary>
    Blank,

    /// <summary>A <c>;</c> or <c>#</c> comment.</summary>
    Comment,

    /// <summary>A <c>[section]</c> header.</summary>
    Section,

    /// <summary>A <c>key=value</c> entry.</summary>
    Entry,

    /// <summary>None of the above. Skipped, counted and logged; never fatal.</summary>
    Unparsable,
}
