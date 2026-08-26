using System.Text;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.Matrix;
using Xunit;

namespace QuickStat.Tests.Domain.Matrix;

/// <summary>
/// The four leading identity columns. Their headers are parity that must not drift (PORT-PLAN.md
/// §6) and step 2.6 writes them straight into the CSV header row.
/// </summary>
public class FixedColumnsTests
{
    private static IdentificationColumns Columns(bool dateOfBirth, bool nationalId, bool name, bool pseudonyms = false) =>
        new()
        {
            IncludesPersonId = true,
            IncludesDateOfBirth = dateOfBirth,
            IncludesNationalId = nationalId,
            IncludesName = name,
            UsesPseudonyms = pseudonyms,
        };

    [Fact]
    public void TheHeadersAreByteExact()
    {
        Assert.Equal("PID", FixedColumns.PersonIdHeader);
        Assert.Equal("Født", FixedColumns.DateOfBirthHeader);
        Assert.Equal("Fødselsnummer", FixedColumns.NationalIdHeader);
        Assert.Equal("Navn", FixedColumns.NameHeader);
    }

    [Theory]
    // The two Norwegian headers pinned by code point, so a mangled encoding cannot pass by looking
    // right in a diff.  U+00F8 is LATIN SMALL LETTER O WITH STROKE.
    [InlineData("Født", 4, 1, 'ø')]
    [InlineData("Fødselsnummer", 13, 1, 'ø')]
    public void TheNorwegianHeadersSurviveTheSourceEncoding(string header, int length, int index, char expected)
    {
        Assert.Equal(length, header.Length);
        Assert.Equal(expected, header[index]);

        // Round-trips as two UTF-8 bytes and one UTF-16 code unit; a double-encoded source file
        // would have produced "FÃ¸dt" and failed the length check above.
        Assert.Equal(length + 1, Encoding.UTF8.GetByteCount(header));
    }

    [Fact]
    public void TheOrdinalsMatchTheDelphiConstants()
    {
        Assert.Equal(0, FixedColumns.PersonId);
        Assert.Equal(1, FixedColumns.DateOfBirth);
        Assert.Equal(2, FixedColumns.NationalId);
        Assert.Equal(3, FixedColumns.Name);
        Assert.Equal(4, FixedColumns.Count);
        Assert.Equal(1, FixedColumns.HeaderRowCount);
    }

    [Fact]
    public void HeadersAreListedInOrdinalOrder()
    {
        Assert.Equal(["PID", "Født", "Fødselsnummer", "Navn"], FixedColumns.Headers);

        for (int ordinal = 0; ordinal < FixedColumns.Count; ordinal++)
        {
            Assert.Equal(FixedColumns.Headers[ordinal], FixedColumns.Header(ordinal));
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void AnOrdinalOutsideTheFourIsRejected(int ordinal) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => FixedColumns.Header(ordinal));

    [Fact]
    public void FullIdentificationEmitsAllFourInGridOrder()
    {
        // PORT-PLAN.md §6: PID, then Født / Fødselsnummer / Navn, then the data columns.
        IdentificationColumns full = Columns(dateOfBirth: true, nationalId: true, name: true);

        Assert.Equal([0, 1, 2, 3], FixedColumns.VisibleOrdinals(full));
        Assert.Equal(["PID", "Født", "Fødselsnummer", "Navn"], FixedColumns.HeadersFor(full));
    }

    [Fact]
    public void TheAnonymousModesOmitThreeColumnsEntirelyRatherThanBlankingThem()
    {
        // "Omitted entirely" means no field and no separator, so the header row and the data rows
        // both get shorter and stay aligned.  A blanked column would keep the separator and change
        // the shape of every exported file.
        IdentificationColumns anonymous = Columns(dateOfBirth: false, nationalId: false, name: false);

        Assert.Equal([0], FixedColumns.VisibleOrdinals(anonymous));
        Assert.Equal(["PID"], FixedColumns.HeadersFor(anonymous));

        Assert.DoesNotContain("Født", FixedColumns.HeadersFor(anonymous));
        Assert.DoesNotContain("Fødselsnummer", FixedColumns.HeadersFor(anonymous));
        Assert.DoesNotContain("Navn", FixedColumns.HeadersFor(anonymous));
        Assert.DoesNotContain("", FixedColumns.HeadersFor(anonymous));
    }

    [Fact]
    public void PseudonymisationDoesNotChangeWhichColumnsAppear()
    {
        // Only the value in the PID column changes; the column set is the same as PersonIdOnly.
        IdentificationColumns pseudonymous = Columns(dateOfBirth: false, nationalId: false, name: false, pseudonyms: true);

        Assert.Equal(["PID"], FixedColumns.HeadersFor(pseudonymous));
    }

    [Theory]
    [InlineData(0, false)] // PID is right-aligned like the data columns
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public void OnlyTheThreeIdentityColumnsAreTextColumns(int ordinal, bool expected) =>
        Assert.Equal(expected, FixedColumns.IsTextColumn(ordinal));
}
