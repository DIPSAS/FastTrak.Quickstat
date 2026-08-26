using System.IO;
using System.Text;
using QuickStat.Configuration;
using Xunit;

namespace QuickStat.Tests.Configuration;

/// <summary>
/// <see cref="UdlReader"/> against the real <c>FastTrak.UDL</c> and against the shapes the Delphi
/// used to swallow.
/// </summary>
public class UdlReaderTests
{
    private const string RealInitString =
        "Provider=SQLOLEDB.1;Integrated Security=SSPI;Persist Security Info=False;" +
        "Initial Catalog=EFT00028_BEHOVPOL_PRODSETTING;Data Source=localhost";

    [Fact]
    public void ReadsTheShippedUtf16LittleEndianDataLinkFile()
    {
        // The file in the repository root is UTF-16 LE with a byte-order mark and CRLF line endings,
        // which is what the Windows "Data Link Properties" dialog writes.
        string initString = new UdlReader().ReadInitString(RepositoryFiles.UdlFile);

        Assert.Equal(RealInitString, initString);
    }

    [Fact]
    public void TheShippedFileReallyIsUtf16LittleEndianWithAByteOrderMark()
    {
        // Guards the test above: if someone re-saves the file as UTF-8 it would still pass, and the
        // decoding path this step exists to get right would no longer be covered.
        byte[] head = new byte[4];

        using (FileStream stream = File.OpenRead(RepositoryFiles.UdlFile))
        {
            Assert.Equal(4, stream.Read(head, 0, 4));
        }

        Assert.Equal(0xFF, head[0]);
        Assert.Equal(0xFE, head[1]);
        Assert.Equal((byte)'[', head[2]);
        Assert.Equal(0x00, head[3]);
    }

    [Theory]
    [InlineData("utf-16le-bom")]
    [InlineData("utf-16be-bom")]
    [InlineData("utf-8-bom")]
    [InlineData("utf-8-no-bom")]
    public void ReadsEveryEncodingASiteMightHaveSaved(string variant)
    {
        Encoding encoding = variant switch
        {
            "utf-16le-bom" => new UnicodeEncoding(bigEndian: false, byteOrderMark: true),
            "utf-16be-bom" => new UnicodeEncoding(bigEndian: true, byteOrderMark: true),
            "utf-8-bom" => new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            _ => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };

        using TemporaryDirectory directory = new();
        string path = directory.WriteUdl("variant.UDL", RealInitString, encoding);

        Assert.Equal(RealInitString, new UdlReader().ReadInitString(path));
    }

    [Fact]
    public void ThrowsWhenTheFileIsMissing()
    {
        using TemporaryDirectory directory = new();
        string path = Path.Combine(directory.Path, "absent.UDL");

        QuickStatConfigurationException exception =
            Assert.Throws<QuickStatConfigurationException>(() => new UdlReader().ReadInitString(path));

        Assert.Equal(path, exception.FilePath);
    }

    [Fact]
    public void ThrowsWhenTheFileHasFewerThanThreeLines()
    {
        // Delphi: LoadFromUdl did nothing at all here, leaving the connection string as
        // "FILE NAME=..." - which ADO also accepted, so the misconfiguration surfaced much later as
        // an unrelated login failure (Emetra.Database.ConnectionString.pas:193-194).
        using TemporaryDirectory directory = new();
        string path = directory.WriteLines("short.UDL", "[oledb]", "; only two lines");

        QuickStatConfigurationException exception =
            Assert.Throws<QuickStatConfigurationException>(() => new UdlReader().ReadInitString(path));

        Assert.Equal(path, exception.FilePath);
        Assert.Contains("2 line(s)", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowsWhenTheInitialisationStringLineIsBlank()
    {
        using TemporaryDirectory directory = new();
        string path = directory.WriteLines("blank.UDL", "[oledb]", "; comment", "   ");

        QuickStatConfigurationException exception =
            Assert.Throws<QuickStatConfigurationException>(() => new UdlReader().ReadInitString(path));

        Assert.Equal(path, exception.FilePath);
    }

    [Fact]
    public void ReadsLineThreeAndIgnoresAnythingAfterIt()
    {
        using TemporaryDirectory directory = new();
        string path = directory.WriteLines(
            "extra.UDL",
            "[oledb]",
            "; Everything after this line is an OLE DB initstring",
            "Data Source=srv;Initial Catalog=db",
            "Data Source=wrong");

        Assert.Equal("Data Source=srv;Initial Catalog=db", new UdlReader().ReadInitString(path));
    }
}
