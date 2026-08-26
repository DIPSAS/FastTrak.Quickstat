using System.IO;
using System.Text;

namespace QuickStat.Tests.Configuration;

/// <summary>
/// A scratch directory that deletes itself, plus the two file writers the configuration tests need.
/// </summary>
internal sealed class TemporaryDirectory : IDisposable
{
    private static readonly Encoding Utf16LittleEndianWithBom = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);

    internal TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "QuickStat.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(Path);
    }

    /// <summary>The full path of the directory.</summary>
    internal string Path { get; }

    /// <summary>Writes a file and returns its full path.</summary>
    /// <param name="fileName">Name of the file inside this directory.</param>
    /// <param name="content">Exact content to write.</param>
    /// <param name="encoding">Encoding, including any byte-order mark it emits.</param>
    /// <returns>The full path written.</returns>
    internal string Write(string fileName, string content, Encoding? encoding = null)
    {
        string path = System.IO.Path.Combine(Path, fileName);

        File.WriteAllText(path, content, encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        return path;
    }

    /// <summary>Writes CRLF-separated lines with a trailing terminator, as the real files have.</summary>
    /// <param name="fileName">Name of the file inside this directory.</param>
    /// <param name="lines">The lines.</param>
    /// <returns>The full path written.</returns>
    internal string WriteLines(string fileName, params string[] lines) =>
        Write(fileName, string.Concat(string.Join("\r\n", lines), "\r\n"), Utf16LittleEndianWithBom);

    /// <summary>
    /// Writes a three-line data link file whose third line is <paramref name="initString"/>.
    /// </summary>
    /// <param name="fileName">Name of the file inside this directory.</param>
    /// <param name="initString">The OLE DB initialisation string.</param>
    /// <param name="encoding">Encoding, defaulting to UTF-16 LE with a byte-order mark.</param>
    /// <returns>The full path written.</returns>
    internal string WriteUdl(string fileName, string initString, Encoding? encoding = null) =>
        Write(
            fileName,
            string.Concat(
                "[oledb]\r\n; Everything after this line is an OLE DB initstring\r\n",
                initString,
                "\r\n"),
            encoding ?? Utf16LittleEndianWithBom);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
            // A scratch directory that outlives the test run is noise, not a failure.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
