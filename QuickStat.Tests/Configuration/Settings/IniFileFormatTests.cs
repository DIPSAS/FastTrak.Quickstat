using QuickStat.Configuration.Settings;
using Xunit;

namespace QuickStat.Tests.Configuration.Settings;

/// <summary>
/// Unit tests for the on-disk format, below the store: escaping, unescaping and line classification.
/// </summary>
public class IniFileFormatTests
{
    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("", "")]
    [InlineData("a=b", "a=b")]
    [InlineData("a\\b", @"a\\b")]
    [InlineData("a\rb", @"a\rb")]
    [InlineData("a\nb", @"a\nb")]
    [InlineData("a\tb", @"a\tb")]
    [InlineData("a\0b", @"a\0b")]
    [InlineData(" a", @"\sa")]
    [InlineData("a ", @"a\s")]
    [InlineData(" ", @"\s")]
    [InlineData("  ", @"\s\s")]
    [InlineData("   ", @"\s \s")]
    [InlineData("a b", "a b")]
    public void EscapeValueProducesTheDocumentedForm(string raw, string escaped)
    {
        Assert.Equal(escaped, IniFileFormat.EscapeValue(raw));
        Assert.Equal(raw, IniFileFormat.Unescape(escaped));
    }

    [Theory]
    [InlineData("a=b", @"a\=b")]
    [InlineData("=", @"\=")]
    [InlineData("[key", @"\[key")]
    [InlineData("key[", "key[")]
    [InlineData(";key", @"\;key")]
    [InlineData("#key", @"\#key")]
    [InlineData("key;", "key;")]
    public void EscapeKeyProtectsTheCharactersThatWouldChangeTheLineType(string raw, string escaped)
    {
        Assert.Equal(escaped, IniFileFormat.EscapeKey(raw));
        Assert.Equal(raw, IniFileFormat.Unescape(escaped));
    }

    [Theory]
    [InlineData("[bracket]", @"\[bracket\]")]
    [InlineData("]", @"\]")]
    [InlineData("a]b[c", @"a\]b\[c")]
    public void EscapeSectionProtectsBothBrackets(string raw, string escaped)
    {
        Assert.Equal(escaped, IniFileFormat.EscapeSection(raw));
        Assert.Equal(raw, IniFileFormat.Unescape(escaped));
    }

    [Theory]
    [InlineData(@"\q", @"\q")]
    [InlineData(@"\", @"\")]
    [InlineData(@"a\", @"a\")]
    [InlineData(@"C:\Users\ola", @"C:\Users\ola")]
    [InlineData(@"C:\Program Files\DIPS", @"C:\Program Files\DIPS")]
    public void AnEscapeTheWriterNeverProducesIsKeptVerbatim(string escaped, string raw)
    {
        // A hand-edited Windows path must survive being read. Dropping the backslash would turn
        // C:\Users\ola into C:Usersola, which is worse than not helping at all.
        Assert.Equal(raw, IniFileFormat.Unescape(escaped));
    }

    [Theory]
    [InlineData(@"C:\temp", "C:\temp")]
    [InlineData(@"\\server\share", "\\server hare")]
    [InlineData(@"C:\new", "C:\new")]
    public void AKnownEscapeStillMeansWhatItSaysInAHandWrittenValue(string escaped, string raw)
    {
        // The residual hazard, pinned rather than hidden: a path segment that starts with t, n, r,
        // s or 0 is still read as the escape. There is no format that both escapes line breaks and
        // reads raw Windows paths, which is why the writer escapes and the file header says so.
        Assert.Equal(raw, IniFileFormat.Unescape(escaped));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void BlankLinesAreBlank(string line)
    {
        Assert.Equal(IniLineKind.Blank, IniFileFormat.ParseLine(line, out _, out _, out _));
    }

    [Theory]
    [InlineData("; comment")]
    [InlineData("# comment")]
    [InlineData("   ; indented comment")]
    public void CommentsAreComments(string line)
    {
        Assert.Equal(IniLineKind.Comment, IniFileFormat.ParseLine(line, out _, out _, out _));
    }

    [Theory]
    [InlineData("[Section]", "Section")]
    [InlineData("  [Section]  ", "Section")]
    [InlineData(@"[a\]b]", "a]b")]
    [InlineData(@"[ends with backslash\\]", @"ends with backslash\")]
    [InlineData("[]", "")]
    public void SectionHeadersAreRecognised(string line, string expected)
    {
        Assert.Equal(IniLineKind.Section, IniFileFormat.ParseLine(line, out string section, out _, out _));
        Assert.Equal(expected, section);
    }

    [Theory]
    [InlineData("Key=Value", "Key", "Value")]
    [InlineData("Key=", "Key", "")]
    [InlineData("Key = Value", "Key", "Value")]
    [InlineData("Key=a=b", "Key", "a=b")]
    [InlineData(@"a\=b=Value", "a=b", "Value")]
    [InlineData(@"Key=\sValue\s", "Key", " Value ")]
    public void EntriesSplitOnTheFirstUnescapedEquals(string line, string key, string value)
    {
        Assert.Equal(IniLineKind.Entry, IniFileFormat.ParseLine(line, out _, out string parsedKey, out string parsedValue));
        Assert.Equal(key, parsedKey);
        Assert.Equal(value, parsedValue);
    }

    [Theory]
    [InlineData("no equals sign here")]
    [InlineData("[unterminated section")]
    [InlineData("=no key")]
    [InlineData("[")]
    public void AnythingElseIsUnparsable(string line)
    {
        Assert.Equal(IniLineKind.Unparsable, IniFileFormat.ParseLine(line, out _, out _, out _));
    }

    [Fact]
    public void ASectionNameEndingInAnEscapedBracketIsNotMistakenForATerminator()
    {
        // "[a\]" - the closing bracket is escaped, so the line has no real terminator.
        Assert.Equal(IniLineKind.Unparsable, IniFileFormat.ParseLine(@"[a\]", out _, out _, out _));
    }
}
