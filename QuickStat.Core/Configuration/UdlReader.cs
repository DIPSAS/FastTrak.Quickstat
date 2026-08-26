using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace QuickStat.Configuration;

/// <summary>
/// The default <see cref="IUdlReader"/>: byte-order-mark driven decoding, then line index
/// <see cref="IUdlReader.InitStringLineIndex"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Encoding.</strong> The shipped <c>FastTrak.UDL</c> is UTF-16 LE with a byte-order mark,
/// which is what the Windows data link dialog writes, and the mark is what selects the decoder here.
/// Delphi's <c>TStringList.LoadFromFile</c> behaved the same way - it inspects the mark first and
/// only falls back to the system ANSI code page - so a hand-written, mark-less file used to be
/// decoded as ANSI. This implementation falls back to UTF-8 instead, which decodes the ASCII that
/// any realistic connection string consists of identically while also handling a mark-less UTF-8
/// file that ANSI would mangle.
/// </para>
/// <para>
/// <strong>Failure.</strong> Every problem raises <see cref="QuickStatConfigurationException"/>
/// naming the file. This is the deliberate change described on
/// <see cref="IUdlReader.ReadInitString"/>: the Delphi's silent no-op turned a broken data link file
/// into a confusing login failure much later.
/// </para>
/// </remarks>
public sealed class UdlReader : IUdlReader
{
    /// <summary>
    /// Decoder used when the file carries no byte-order mark. UTF-8 without a mark of its own, so it
    /// never re-emits one.
    /// </summary>
    private static readonly Encoding FallbackEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly ILogger<UdlReader> _logger;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="logger">Optional logger; defaults to a no-op logger.</param>
    public UdlReader(ILogger<UdlReader>? logger = null) => _logger = logger ?? NullLogger<UdlReader>.Instance;

    /// <inheritdoc />
    public string ReadInitString(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new QuickStatConfigurationException($"The data link file '{path}' does not exist.")
            {
                FilePath = path,
            };
        }

        IReadOnlyList<string> lines;

        try
        {
            lines = ReadLines(path);
        }
        catch (IOException ex)
        {
            throw new QuickStatConfigurationException($"The data link file '{path}' could not be read.", ex)
            {
                FilePath = path,
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new QuickStatConfigurationException($"The data link file '{path}' could not be read.", ex)
            {
                FilePath = path,
            };
        }

        if (lines.Count <= IUdlReader.InitStringLineIndex)
        {
            string found = lines.Count.ToString(CultureInfo.InvariantCulture);

            throw new QuickStatConfigurationException(
                $"The data link file '{path}' has {found} line(s); the OLE DB initialisation string is expected on line 3.")
            {
                FilePath = path,
            };
        }

        string initString = lines[IUdlReader.InitStringLineIndex].Trim();

        if (initString.Length == 0)
        {
            throw new QuickStatConfigurationException(
                $"Line 3 of the data link file '{path}' is blank; it should hold the OLE DB initialisation string.")
            {
                FilePath = path,
            };
        }

        _logger.LogDebug("Read the OLE DB initialisation string from the data link file {UdlPath}.", path);

        return initString;
    }

    private static List<string> ReadLines(string path)
    {
        using StreamReader reader = new(path, FallbackEncoding, detectEncodingFromByteOrderMarks: true);

        List<string> lines = new(4);

        while (reader.ReadLine() is { } line)
        {
            lines.Add(line);
        }

        return lines;
    }
}
