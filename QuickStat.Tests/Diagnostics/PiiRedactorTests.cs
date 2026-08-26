using QuickStat.Diagnostics;
using Xunit;

namespace QuickStat.Tests.Diagnostics;

/// <summary>
/// Tests for <see cref="PiiRedactor"/>: what it removes, what it deliberately leaves alone, and
/// parity with the two Delphi functions it replaces.
/// </summary>
public class PiiRedactorTests
{
    // Checksum-valid identity numbers, generated mechanically and cross-checked against the
    // alternative MOD-11 formulation (weights including the control digits themselves). None
    // belongs to a person.
    public const string ValidFodselsnummer = "01019000083";
    public const string AnotherFodselsnummer = "31128500181";
    public const string ThirdFodselsnummer = "15067200036";
    public const string ValidDNumber = "41019000077";
    public const string ValidHNumber = "01419000066";
    public const string ValidSyntheticNumber = "01819000049";
    public const string ValidFhNumber = "80000000098";

    // Eleven digits, MOD-11 correct, but day 12 / month 34 - not a date, so not an identifier.
    private const string ChecksumValidButNotDateShaped = "12345600181";

    // Eleven digits, date-shaped, but the first control digit is wrong.
    private const string DateShapedButWrongChecksum = "01019000003";

    [Fact]
    public void RedactReplacesAHandlebarSpan()
    {
        Assert.Equal($"Pasient {PiiRedactor.Replacement} er registrert.", PiiRedactor.Redact("Pasient {{Ola Nordmann}} er registrert."));
    }

    [Fact]
    public void RedactReplacesEachHandlebarSpanSeparately()
    {
        // The Delphi's single greedy regex {{(.*)}} collapsed everything between the first {{ and
        // the last }} into one replacement, eating the text between the two markers.
        Assert.Equal(
            $"a{PiiRedactor.Replacement}b{PiiRedactor.Replacement}c",
            PiiRedactor.Redact("a{{one}}b{{two}}c"));
    }

    [Fact]
    public void RedactTreatsAnUnterminatedHandlebarAsRunningToTheEnd()
    {
        // The Delphi regex simply did not match, so an unterminated marker leaked its content.
        Assert.Equal($"Pasient {PiiRedactor.Replacement}", PiiRedactor.Redact("Pasient {{Ola Nordmann"));
    }

    [Fact]
    public void RedactPreservesWhitespaceAndLineBreaks()
    {
        // Unlike ForLog. A settings value must survive intact.
        Assert.Equal("line one\r\n  line two\t", PiiRedactor.Redact("line one\r\n  line two\t"));
    }

    [Theory]
    [InlineData(ValidFodselsnummer)]
    [InlineData(AnotherFodselsnummer)]
    [InlineData(ThirdFodselsnummer)]
    [InlineData(ValidDNumber)]
    [InlineData(ValidHNumber)]
    [InlineData(ValidSyntheticNumber)]
    [InlineData(ValidFhNumber)]
    [InlineData("90012300068")]
    public void IsNationalIdentityNumberAcceptsTheWholeFamily(string identifier)
    {
        Assert.True(PiiRedactor.IsNationalIdentityNumber(identifier));
    }

    [Theory]
    [InlineData(ChecksumValidButNotDateShaped)]
    [InlineData(DateShapedButWrongChecksum)]
    [InlineData("00129000082")]      // day 00
    [InlineData("72129000067")]      // day 72, past the D-number range
    [InlineData("12345678901")]      // the obvious placeholder; fails MOD-11
    [InlineData("0101900008")]       // ten digits
    [InlineData("010190000834")]     // twelve digits
    [InlineData("0101900008a")]      // not all digits
    [InlineData("")]
    public void IsNationalIdentityNumberRejectsEverythingElse(string candidate)
    {
        Assert.False(PiiRedactor.IsNationalIdentityNumber(candidate));
    }

    [Fact]
    public void RedactRemovesAnIdentityNumberNobodyMarked()
    {
        // The point of detecting these structurally: a call site that forgot the handlebars still
        // does not leak.
        Assert.Equal(
            $"Fant pasient {PiiRedactor.Replacement} i utvalget.",
            PiiRedactor.Redact($"Fant pasient {ValidFodselsnummer} i utvalget."));
    }

    [Theory]
    [InlineData("01019000083")]
    [InlineData("010190-00083")]
    [InlineData("010190 00083")]
    public void RedactRecognisesThePrintedSeparators(string rendering)
    {
        Assert.Equal(PiiRedactor.Replacement, PiiRedactor.Redact(rendering));
    }

    [Fact]
    public void RedactRemovesEveryIdentityNumberOnTheLine()
    {
        Assert.Equal(
            $"{PiiRedactor.Replacement} og {PiiRedactor.Replacement}",
            PiiRedactor.Redact($"{ValidFodselsnummer} og {AnotherFodselsnummer}"));
    }

    [Fact]
    public void RedactLeavesALongerDigitRunAlone()
    {
        // An identity number embedded in a longer number is not an identity number, and truncating
        // the run would corrupt the value while redacting nothing.
        string longer = ValidFodselsnummer + "77";

        Assert.Equal(longer, PiiRedactor.Redact(longer));
    }

    [Theory]
    [InlineData("Width=1920")]
    [InlineData("Height=1080")]
    [InlineData("2026-08-26T10:42:33.1234567+02:00")]
    [InlineData("PID 4711 lastet")]
    [InlineData("2147483647")]
    [InlineData("1.7976931348623157E+308")]
    [InlineData("frmQuickStat.1920x1080")]
    public void RedactLeavesOrdinarySettingsValuesAlone(string value)
    {
        // Over-redaction would be its own defect: an int32 is at most ten digits, a round-trip date
        // has separators, and a double's mantissa is longer than eleven digits - none of them can
        // trip the rule, and this pins that.
        Assert.Equal(value, PiiRedactor.Redact(value));
    }

    [Fact]
    public void ContainsPersonalIdentifierAgreesWithRedact()
    {
        Assert.True(PiiRedactor.ContainsPersonalIdentifier($"x {ValidFodselsnummer}"));
        Assert.True(PiiRedactor.ContainsPersonalIdentifier("x {{navn}}"));
        Assert.False(PiiRedactor.ContainsPersonalIdentifier("Width=1920"));
        Assert.False(PiiRedactor.ContainsPersonalIdentifier(null));
        Assert.False(PiiRedactor.ContainsPersonalIdentifier(string.Empty));
    }

    [Fact]
    public void ForLogReproducesTheDelphiSelfTest()
    {
        // Emetra.Logging.Utilities.pas:19-20, :41 - the unit asserts this exact transformation at
        // start-up, so it is the closest thing to a specification the original has.
        const string DelphiTestMessage = "Hello  {{Napoleon Æ. Bonaparte}}\n\r  test.";

        Assert.Equal($"Hello {PiiRedactor.Replacement} test.", PiiRedactor.ForLog(DelphiTestMessage));
    }

    [Fact]
    public void ForLogCollapsesLeadingAndTrailingWhitespaceToOneSpace()
    {
        Assert.Equal(" a b ", PiiRedactor.ForLog("  a\t\r\nb  "));
    }

    [Fact]
    public void ForLogFoldsAMultiLineMessageOntoOneLine()
    {
        Assert.Equal("first second third", PiiRedactor.ForLog("first\nsecond\nthird"));
    }

    [Fact]
    public void ForLogRedactsAsWellAsFolds()
    {
        Assert.Equal($"a {PiiRedactor.Replacement} b", PiiRedactor.ForLog($"a\n{ValidFodselsnummer}\nb"));
    }

    [Theory]
    [InlineData("a{{b}}c", "abc")]
    [InlineData("{{b}}c", "bc")]
    [InlineData("a{{b}}", "ab")]
    public void ForDisplayReproducesTheDelphiRemoveHandlebarsAssertions(string input, string expected)
    {
        // Emetra.Logging.Utilities.pas:43-45.
        Assert.Equal(expected, PiiRedactor.ForDisplay(input));
    }

    [Fact]
    public void ForDisplayHandlesTwoMarkersWhereTheDelphiDidNot()
    {
        // The greedy regex turned this into "ab}}c{{de". Nothing in QuickStat depended on that.
        Assert.Equal("abcde", PiiRedactor.ForDisplay("a{{b}}c{{d}}e"));
    }

    [Fact]
    public void ForDisplayExpandsTheLiteralNewlineEscape()
    {
        // MainQuickStat.pas:226 - CONFIRM_DELETE_PACKAGE really does embed a backslash-n.
        Assert.Equal("Do you really want to delete this package:\n\"X\"?", PiiRedactor.ForDisplay("Do you really want to delete this package:\\n\"X\"?"));
    }

    [Fact]
    public void ForDisplayKeepsTheIdentifierTheUserIsAllowedToSee()
    {
        // The whole point of the {{ }} convention: show it on screen, keep it out of the file.
        Assert.Equal(
            $"Slett {ValidFodselsnummer}?",
            PiiRedactor.ForDisplay($"Slett {{{{{ValidFodselsnummer}}}}}?"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EveryEntryPointTreatsNullAndEmptyAsEmpty(string? text)
    {
        Assert.Equal(string.Empty, PiiRedactor.Redact(text));
        Assert.Equal(string.Empty, PiiRedactor.ForLog(text));
        Assert.Equal(string.Empty, PiiRedactor.ForDisplay(text));
    }

    [Fact]
    public void RedactHandlesNorwegianCharacters()
    {
        const string Text = "Fødselsdato og bostedsadresse i Ålesund, æøåÆØÅ.";

        Assert.Equal(Text, PiiRedactor.Redact(Text));
    }
}
