using System.Globalization;
using System.Text;

namespace QuickStat.Export;

/// <summary>
/// Writes the <c>.mapping.txt</c> re-identification key. Writing it at all is opt-in.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>TMatrixAnonymizer.SaveToFile</c> (<c>EPR.QA.Matrix.Anoymizer.pas:64-82</c>), which ran
/// unconditionally for every <c>pgiRandomPersonId</c> export - including the temporary CSV behind
/// <c>Open this dataset in Excel</c>. That CSV was tracked for deletion; its <c>.mapping.txt</c>
/// sibling never was, so plaintext keys mapping pseudonyms back to real person ids accumulated in
/// the user's <c>%TEMP%</c> indefinitely (PORT-PLAN.md §7.2). A key file sitting beside an
/// anonymised export defeats the anonymisation, so in the port it is written only when
/// <see cref="DatasetExportOptions.WriteKeyFile"/> is set, the caller is warned, and the file is
/// tracked for deletion through <see cref="ITempFileTracker"/>.
/// </para>
/// <para>
/// The format itself is preserved: <c>&lt;pseudonym&gt;=&lt;PersonId&gt;</c> lines, sorted as text,
/// CRLF-terminated including the final line, ANSI/Windows-1252, no byte-order mark. Every pseudonym
/// in one export has the same digit count - they all lie in <c>[scale, 10 * scale - 1]</c> - so the
/// textual sort is also numeric.
/// </para>
/// </remarks>
public static class PseudonymKeyWriter
{
    /// <summary>Separator between pseudonym and person id, from <c>TStringList.Values[]</c>.</summary>
    public const char NameValueSeparator = '=';

    /// <summary>The path a key file takes beside an export.</summary>
    /// <param name="exportPath">The export's own path.</param>
    /// <returns>
    /// The same path with its extension replaced by
    /// <see cref="DatasetExportOptions.KeyFileExtension"/>, matching Delphi's
    /// <c>ChangeFileExt(fFileName, '.mapping.txt')</c>.
    /// </returns>
    public static string KeyFilePathFor(string exportPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exportPath);

        string directory = Path.GetDirectoryName(exportPath) ?? string.Empty;
        string stem = Path.GetFileNameWithoutExtension(exportPath) + DatasetExportOptions.KeyFileExtension;

        return directory.Length == 0 ? stem : Path.Combine(directory, stem);
    }

    /// <summary>Renders the key file's text.</summary>
    /// <param name="pseudonymToPersonId">The map, from <c>IAnonymiser.PseudonymToPersonId</c>.</param>
    /// <returns>Sorted <c>pseudonym=personId</c> lines, each terminated with CRLF.</returns>
    public static string Render(IReadOnlyDictionary<int, int> pseudonymToPersonId)
    {
        ArgumentNullException.ThrowIfNull(pseudonymToPersonId);

        var lines = new List<string>(pseudonymToPersonId.Count);

        foreach (KeyValuePair<int, int> entry in pseudonymToPersonId)
        {
            lines.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"{entry.Key}{NameValueSeparator}{entry.Value}"));
        }

        // TStringList.Sort, i.e. lexicographic on the whole "name=value" line.
        lines.Sort(StringComparer.Ordinal);

        var text = new StringBuilder();

        foreach (string line in lines)
        {
            // TStrings.Text terminates every line, the last one included.
            text.Append(line).Append(CsvMatrixWriter.LineTerminator);
        }

        return text.ToString();
    }

    /// <summary>Writes the key file.</summary>
    /// <param name="pseudonymToPersonId">The map.</param>
    /// <param name="stream">Destination. Left open; the caller owns it.</param>
    /// <param name="encoding">
    /// Text encoding, or <see langword="null"/> for Windows-1252 without a byte-order mark, which is
    /// what <c>TStringList.SaveToFile</c> produced.
    /// </param>
    public static void Write(
        IReadOnlyDictionary<int, int> pseudonymToPersonId,
        Stream stream,
        Encoding? encoding = null)
    {
        ArgumentNullException.ThrowIfNull(pseudonymToPersonId);
        ArgumentNullException.ThrowIfNull(stream);

        using var writer = new StreamWriter(
            stream,
            encoding ?? CsvMatrixWriter.LegacyEncoding,
            bufferSize: -1,
            leaveOpen: true);

        writer.Write(Render(pseudonymToPersonId));
        writer.Flush();
    }
}
