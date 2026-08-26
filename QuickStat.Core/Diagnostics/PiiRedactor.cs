using System.Text;

namespace QuickStat.Diagnostics;

/// <summary>
/// Removes personal identifiers from text that is about to be persisted, and prepares the same text
/// for display to a human who is allowed to see it.
/// </summary>
/// <remarks>
/// <para>
/// QuickStat is a patient-data application, so anything it writes to disk - a log line, a settings
/// file - can accumulate identifiers that nobody intended to store. Redaction is therefore applied
/// by default at every write path (<see cref="UserNotifier"/> before it logs,
/// <c>IniSettingsStore</c> before it stores), not offered as something a caller opts into.
/// </para>
/// <para>
/// <strong>Two mechanisms, deliberately.</strong>
/// </para>
/// <list type="number">
///   <item>
///     <description>
///     <c>{{ ... }}</c> handlebars, ported from <c>Emetra.Logging.Utilities.pas:23-37</c>. A call
///     site wraps text it knows to be personal - a patient name, an address - and the convention is
///     "show it to the user, redact it from the file". This is the only way a <em>name</em> can be
///     detected, because a name is not distinguishable from any other word.
///     </description>
///   </item>
///   <item>
///     <description>
///     Norwegian national identity numbers, detected structurally. Unlike a name, a
///     fødselsnummer has a shape that can be recognised without being told, so it is caught
///     even when a call site forgot the handlebars. See <see cref="IsNationalIdentityNumber"/>.
///     </description>
///   </item>
/// </list>
/// <para>
/// <strong>What is deliberately <em>not</em> redacted, and why.</strong>
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///     <em>Person ids (PID).</em> They are surrogate database keys, not identifiers, and
///     <c>PersonIdOnly</c> is treated as a de-identified export mode by the application itself
///     (<c>Docs/Port/04-matrix-export.md</c> §4). The grid's first column is the PID; redacting
///     it would redact the product.
///     </description>
///   </item>
///   <item>
///     <description>
///     <em>Dates.</em> A date of birth is not distinguishable from a period bound, a timestamp or a
///     window-geometry value. Redacting dates would destroy the settings file and every useful log
///     line while catching nothing an attacker could not already infer.
///     </description>
///   </item>
///   <item>
///     <description>
///     <em>The Windows user name.</em> That is the operator, not the patient, and the log file is
///     named after them by design (<c>Docs/Port/01-data-access.md</c> §7.5). Redacting it here
///     while writing it into the file name would be theatre.
///     </description>
///   </item>
/// </list>
/// <para>
/// The residual gap is a patient name that reaches a write path without handlebars. That cannot be
/// closed by pattern matching; it is closed by the call-site convention above.
/// </para>
/// </remarks>
public static class PiiRedactor
{
    /// <summary>
    /// What redacted content is replaced with. Delphi: <c>TXT_REPLACEMENT</c>
    /// (<c>Emetra.Logging.Utilities.pas:10</c>).
    /// </summary>
    public const string Replacement = "(Anonymisert)";

    /// <summary>The number of digits in a Norwegian national identity number.</summary>
    private const int NationalIdDigits = 11;

    /// <summary>Check-digit weights for the first control digit, over digits 1-9.</summary>
    private static ReadOnlySpan<int> FirstCheckWeights => [3, 7, 6, 1, 8, 9, 4, 5, 2];

    /// <summary>Check-digit weights for the second control digit, over digits 1-10.</summary>
    private static ReadOnlySpan<int> SecondCheckWeights => [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];

    /// <summary>
    /// Removes personal identifiers, leaving the rest of the text - including whitespace and line
    /// breaks - exactly as it was.
    /// </summary>
    /// <param name="text">Text to redact. <see langword="null"/> yields an empty string.</param>
    /// <returns>The text with every handlebar span and every national identity number replaced.</returns>
    /// <remarks>
    /// This is the primitive the settings store uses, because a stored value must survive intact:
    /// collapsing whitespace the way <see cref="ForLog"/> does would corrupt multi-line values.
    /// </remarks>
    public static string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return RedactNationalIds(RedactHandlebars(text));
    }

    /// <summary>
    /// Redacts, then folds the result onto a single line.
    /// </summary>
    /// <param name="text">Text to prepare.</param>
    /// <returns>Redacted text with every run of whitespace collapsed to one space.</returns>
    /// <remarks>
    /// Delphi: <c>AnonymizeLogMessage</c> (<c>Emetra.Logging.Utilities.pas:23-27</c>), which did the
    /// same two steps - replace, then <c>\s+</c> to a single space - and was applied to every line
    /// written to the log file. Collapsing whitespace also replaces the separate <c>StripNewlines</c>
    /// pass (<c>Emetra.Logging.PlainText.pas:384-388</c>): one log entry is one line.
    /// </remarks>
    public static string ForLog(string? text) => CollapseWhitespace(Redact(text));

    /// <summary>
    /// Prepares text for a dialog: handlebars removed but their content kept, and the literal
    /// two-character sequence <c>\n</c> expanded to a line break.
    /// </summary>
    /// <param name="text">Text to prepare.</param>
    /// <returns>Text ready to put in front of a user.</returns>
    /// <remarks>
    /// <para>
    /// Delphi: <c>PrepareForDialog</c> (<c>Emetra.Logging.Utilities.pas:34-37</c>). The message
    /// constants really do embed a literal backslash-n - see <c>CONFIRM_DELETE_PACKAGE</c> at
    /// <c>MainQuickStat.pas:226</c> - so without this the user is shown the escape sequence.
    /// </para>
    /// <para>
    /// This deliberately does <em>not</em> redact national identity numbers. The whole point of the
    /// <c>{{ }}</c> convention is that the clinician in front of the screen may see the identifier;
    /// it is the file on disk that may not.
    /// </para>
    /// </remarks>
    public static string ForDisplay(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return RemoveHandlebars(text).Replace("\\n", "\n", StringComparison.Ordinal);
    }

    /// <summary>
    /// Whether <see cref="Redact"/> would change this text.
    /// </summary>
    /// <param name="text">Text to inspect.</param>
    /// <returns><see langword="true"/> when the text carries a handlebar span or an identity number.</returns>
    public static bool ContainsPersonalIdentifier(string? text)
        => !string.IsNullOrEmpty(text) && !string.Equals(Redact(text), text, StringComparison.Ordinal);

    /// <summary>
    /// Whether a span of characters is a Norwegian national identity number.
    /// </summary>
    /// <param name="candidate">Exactly eleven digits, without separators.</param>
    /// <returns><see langword="true"/> when the digits form a valid identity number.</returns>
    /// <remarks>
    /// <para>
    /// Accepts the whole family in use in Norwegian health care:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>fødselsnummer - day 01-31, month 01-12;</description></item>
    ///   <item><description>D-nummer - day plus 40, so 41-71;</description></item>
    ///   <item><description>H-nummer (hjelpenummer) - month plus 40, so 41-52;</description></item>
    ///   <item><description>synthetic test numbers - month plus 80, so 81-92;</description></item>
    ///   <item><description>FH-nummer - leading digit 8 or 9 and no date part at all.</description></item>
    /// </list>
    /// <para>
    /// Both MOD-11 control digits are verified. That matters: without them roughly one in five
    /// arbitrary eleven-digit runs would match on shape alone, and the store would start redacting
    /// numbers that are not identifiers. With them, a false positive needs a number that is
    /// eleven digits long, date-shaped <em>and</em> checksum-correct - which is, in practice, an
    /// identity number.
    /// </para>
    /// </remarks>
    public static bool IsNationalIdentityNumber(ReadOnlySpan<char> candidate)
    {
        if (candidate.Length != NationalIdDigits)
        {
            return false;
        }

        Span<int> digits = stackalloc int[NationalIdDigits];

        for (int i = 0; i < NationalIdDigits; i++)
        {
            if (!char.IsAsciiDigit(candidate[i]))
            {
                return false;
            }

            digits[i] = candidate[i] - '0';
        }

        if (!HasPlausibleDatePart(digits))
        {
            return false;
        }

        return CheckDigit(digits, FirstCheckWeights) == digits[9]
            && CheckDigit(digits, SecondCheckWeights) == digits[10];
    }

    private static bool HasPlausibleDatePart(ReadOnlySpan<int> digits)
    {
        // FH-numbers are allocated from the 8xx/9xx range and carry no date at all.
        if (digits[0] is 8 or 9)
        {
            return true;
        }

        int day = (digits[0] * 10) + digits[1];
        int month = (digits[2] * 10) + digits[3];

        bool dayIsPlausible = day is (>= 1 and <= 31) or (>= 41 and <= 71);
        bool monthIsPlausible = month is (>= 1 and <= 12) or (>= 41 and <= 52) or (>= 81 and <= 92);

        return dayIsPlausible && monthIsPlausible;
    }

    private static int CheckDigit(ReadOnlySpan<int> digits, ReadOnlySpan<int> weights)
    {
        int sum = 0;

        for (int i = 0; i < weights.Length; i++)
        {
            sum += digits[i] * weights[i];
        }

        int remainder = sum % 11;

        // 11 means "check digit zero"; 10 is not expressible and marks the number invalid, which is
        // why -1 rather than 10 is returned - no digit can ever equal it.
        return remainder switch
        {
            0 => 0,
            1 => -1,
            _ => 11 - remainder,
        };
    }

    /// <summary>
    /// Replaces every <c>{{ ... }}</c> span, including an unterminated one.
    /// </summary>
    /// <remarks>
    /// The Delphi used the single greedy regex <c>{{(.*)}}</c>, which swallowed everything between
    /// the first <c>{{</c> and the last <c>}}</c> and matched nothing at all when the closing
    /// braces were missing. This scans span by span, so two markers on one line stay two markers,
    /// and an unterminated <c>{{</c> redacts to the end of the text rather than leaking.
    /// </remarks>
    private static string RedactHandlebars(string text)
    {
        int open = text.IndexOf("{{", StringComparison.Ordinal);

        if (open < 0)
        {
            return text;
        }

        StringBuilder builder = new(text.Length);
        int position = 0;

        while (open >= 0)
        {
            builder.Append(text, position, open - position);
            builder.Append(Replacement);

            int close = text.IndexOf("}}", open + 2, StringComparison.Ordinal);

            if (close < 0)
            {
                // Someone started marking personal data and the marker was truncated. Redacting to
                // the end is the only safe reading.
                return builder.ToString();
            }

            position = close + 2;
            open = text.IndexOf("{{", position, StringComparison.Ordinal);
        }

        builder.Append(text, position, text.Length - position);

        return builder.ToString();
    }

    /// <summary>Strips the braces and keeps the content. Delphi: <c>RemoveHandlebars</c>.</summary>
    private static string RemoveHandlebars(string text)
    {
        int open = text.IndexOf("{{", StringComparison.Ordinal);

        if (open < 0)
        {
            return text;
        }

        StringBuilder builder = new(text.Length);
        int position = 0;

        while (open >= 0)
        {
            builder.Append(text, position, open - position);

            int close = text.IndexOf("}}", open + 2, StringComparison.Ordinal);

            if (close < 0)
            {
                builder.Append(text, open + 2, text.Length - open - 2);

                return builder.ToString();
            }

            builder.Append(text, open + 2, close - open - 2);
            position = close + 2;
            open = text.IndexOf("{{", position, StringComparison.Ordinal);
        }

        builder.Append(text, position, text.Length - position);

        return builder.ToString();
    }

    /// <summary>
    /// Finds and replaces national identity numbers, tolerating the <c>DDMMYY-NNNCC</c> and
    /// <c>DDMMYY NNNCC</c> renderings.
    /// </summary>
    private static string RedactNationalIds(string text)
    {
        StringBuilder? builder = null;
        int position = 0;
        int index = 0;

        while (index < text.Length)
        {
            if (!char.IsAsciiDigit(text[index]) || (index > 0 && char.IsAsciiDigit(text[index - 1])))
            {
                index++;

                continue;
            }

            int end = MatchNationalId(text, index);

            if (end < 0)
            {
                // Skip the whole digit run: no suffix of it can start an identity number either,
                // because a match must begin at a non-digit boundary.
                while (index < text.Length && char.IsAsciiDigit(text[index]))
                {
                    index++;
                }

                continue;
            }

            builder ??= new StringBuilder(text.Length);
            builder.Append(text, position, index - position);
            builder.Append(Replacement);
            position = end;
            index = end;
        }

        if (builder is null)
        {
            return text;
        }

        builder.Append(text, position, text.Length - position);

        return builder.ToString();
    }

    /// <summary>
    /// Tries to read an identity number starting at <paramref name="start"/>.
    /// </summary>
    /// <returns>The index just past the match, or -1.</returns>
    private static int MatchNationalId(string text, int start)
    {
        Span<char> digits = stackalloc char[NationalIdDigits];
        int collected = 0;
        int index = start;
        bool separatorUsed = false;

        while (index < text.Length && collected < NationalIdDigits)
        {
            char current = text[index];

            if (char.IsAsciiDigit(current))
            {
                digits[collected++] = current;
                index++;

                continue;
            }

            // One separator, and only where the printed form puts it: between the date part and the
            // personal number.
            if (!separatorUsed && collected == 6 && current is '-' or ' ')
            {
                separatorUsed = true;
                index++;

                continue;
            }

            break;
        }

        if (collected != NationalIdDigits)
        {
            return -1;
        }

        // A twelfth digit means this was never an identity number.
        if (index < text.Length && char.IsAsciiDigit(text[index]))
        {
            return -1;
        }

        return IsNationalIdentityNumber(digits) ? index : -1;
    }

    /// <summary>Delphi: the <c>\s+</c> pass of <c>AnonymizeLogMessage</c>.</summary>
    private static string CollapseWhitespace(string text)
    {
        if (text.Length == 0)
        {
            return text;
        }

        StringBuilder builder = new(text.Length);
        bool inWhitespace = false;

        foreach (char current in text)
        {
            if (char.IsWhiteSpace(current))
            {
                inWhitespace = true;

                continue;
            }

            if (inWhitespace)
            {
                // Every run becomes exactly one space, leading and trailing runs included - that is
                // what the Delphi regex did, and the self-test in Emetra.Logging.Utilities.pas:41
                // pins it.
                builder.Append(' ');
            }

            inWhitespace = false;
            builder.Append(current);
        }

        if (inWhitespace)
        {
            builder.Append(' ');
        }

        return builder.ToString();
    }
}
