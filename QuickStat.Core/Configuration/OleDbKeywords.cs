using System.Text;

namespace QuickStat.Configuration;

/// <summary>
/// The OLE DB keyword tables used by <see cref="OleDbConnectionStringTranslator"/>: how a keyword is
/// normalised for lookup, which keywords are dropped, and which are renamed on the way to ADO.NET.
/// </summary>
/// <remarks>
/// <para>
/// Source: <c>Docs/Port/01-data-access.md</c> §3.3-§3.4, itself derived from
/// <c>Emetra.Database.ConnectionString.pas:73-88</c> and from what
/// <c>Microsoft.Data.SqlClient</c> accepts.
/// </para>
/// <para>
/// Parsing matches the legacy exactly: the Delphi held the connection string in a
/// <c>TStringList</c> with <c>Delimiter := ';'</c> and <c>StrictDelimiter := true</c>
/// (<c>:147-148</c>), which disables quote handling entirely. So a value cannot contain a semicolon,
/// then or now, and keys with embedded spaces - <c>Initial Catalog</c>, <c>Data Source</c> - survive
/// intact.
/// </para>
/// </remarks>
internal static class OleDbKeywords
{
    /// <summary>
    /// Keywords that must not reach <c>SqlConnectionStringBuilder</c>, in normalised form.
    /// </summary>
    /// <remarks>
    /// <c>Provider</c> is the important one: the builder throws <c>ArgumentException</c> for it, so a
    /// literal translation of any legacy string fails immediately. The rest are OLE DB session
    /// properties with no TDS meaning. Note that <c>Extended Properties</c> has to be dropped
    /// explicitly, because <c>SqlConnectionStringBuilder</c> happens to treat that spelling as a
    /// synonym for <c>AttachDBFilename</c>, which is not what OLE DB meant by it.
    /// </remarks>
    private static readonly HashSet<string> DroppedKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Provider",
        "AutoTranslate",
        "Tagwithcolumncollationwhenpossible",
        "UseProcedureforPrepare",
        "OLEDBServices",
        "GeneralTimeout",
        "Prompt",
        "WindowHandle",
        "Mode",
        "AsynchronousProcessing",
        "ExtendedProperties",
        "LocaleIdentifier",
        "Replication",
        "DataProvider",
    };

    /// <summary>
    /// OLE DB keywords whose ADO.NET equivalent has a different name, in normalised form.
    /// </summary>
    /// <remarks>
    /// Everything not listed here is passed through under its original spelling, because
    /// <c>SqlConnectionStringBuilder</c> already understands the OLE DB spelling as a synonym:
    /// <c>Data Source</c>, <c>Initial Catalog</c>, <c>Integrated Security</c> (including the literal
    /// value <c>SSPI</c>), <c>Trusted_Connection</c>, <c>Persist Security Info</c>, <c>User ID</c>,
    /// <c>Password</c>, <c>Application Name</c>, <c>Workstation ID</c>, <c>Connect Timeout</c>,
    /// <c>Current Language</c>, <c>Packet Size</c> and <c>Failover Partner</c>.
    /// </remarks>
    private static readonly Dictionary<string, string> RenamedKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["UseEncryptionforData"] = "Encrypt",
        ["InitialFileName"] = "AttachDBFilename",
        ["MARSConnection"] = "MultipleActiveResultSets",
    };

    /// <summary>
    /// Spellings of the OLE DB network-library keyword, in normalised form. Note that
    /// <c>Network Address</c> is <em>not</em> one of them - that is a server address and
    /// <c>SqlConnectionStringBuilder</c> treats it as a <c>Data Source</c> synonym.
    /// </summary>
    private static readonly HashSet<string> NetworkLibraryKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "NetworkLibrary",
        "Net",
        "Network",
    };

    /// <summary>Spellings of the data link reference keyword, in normalised form.</summary>
    private static readonly HashSet<string> FileNameKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "FileName",
    };

    /// <summary>Value of the network-library keyword that selects TCP/IP sockets.</summary>
    internal const string TcpNetworkLibrary = "DBMSSOCN";

    /// <summary>Prefix that selects TCP/IP in an ADO.NET <c>Data Source</c>.</summary>
    internal const string TcpDataSourcePrefix = "tcp:";

    /// <summary>
    /// Splits a connection string into key/value pairs, preserving order and duplicates.
    /// </summary>
    /// <param name="text">A semicolon-separated keyword list, possibly <see langword="null"/>.</param>
    /// <returns>The pairs, in the order they appear.</returns>
    internal static List<KeyValuePair<string, string>> Parse(string? text) => Parse(text, out _);

    /// <summary>
    /// Splits a connection string into key/value pairs and reports how many tokens were unusable.
    /// </summary>
    /// <param name="text">A semicolon-separated keyword list, possibly <see langword="null"/>.</param>
    /// <param name="ignoredTokens">Number of non-empty tokens skipped for having no usable key.</param>
    /// <returns>The pairs, in the order they appear.</returns>
    /// <remarks>
    /// <para>
    /// Tokens without an <c>=</c>, and tokens whose key is empty, are skipped - the Delphi's
    /// <c>Values[]</c> lookup ignored them in the same way. Keys and values are trimmed, which the
    /// Delphi did not do; it only ever helps, because an untrimmed key never matched anything.
    /// </para>
    /// <para>
    /// <strong>A value cannot contain a semicolon.</strong> No quoting is recognised, matching
    /// <c>StrictDelimiter := true</c> in the Delphi (<c>:147-148</c>, and note that
    /// <c>LoadFromUdl</c> set <c>QuoteChar</c> to an apostrophe, which the double quotes OLE DB
    /// actually emits would not match anyway). A password containing a semicolon is therefore
    /// truncated - as it was before the port - and the leftover fragment lands in
    /// <paramref name="ignoredTokens"/>, which is what the caller warns about. Nothing logs the
    /// fragment itself; it may be half a password.
    /// </para>
    /// </remarks>
    internal static List<KeyValuePair<string, string>> Parse(string? text, out int ignoredTokens)
    {
        List<KeyValuePair<string, string>> pairs = [];
        ignoredTokens = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return pairs;
        }

        foreach (string token in text.Split(';'))
        {
            string trimmed = token.Trim();

            if (trimmed.Length == 0)
            {
                continue;
            }

            int separator = trimmed.IndexOf('=');
            string key = separator <= 0 ? string.Empty : trimmed[..separator].TrimEnd();

            if (key.Length == 0)
            {
                ignoredTokens++;

                continue;
            }

            pairs.Add(new KeyValuePair<string, string>(key, trimmed[(separator + 1)..].TrimStart()));
        }

        return pairs;
    }

    /// <summary>
    /// Reduces a keyword to its lookup form by removing spaces, underscores and hyphens. Case is
    /// handled by the comparers, not here.
    /// </summary>
    /// <param name="keyword">The keyword as it appeared in the connection string.</param>
    /// <returns>The lookup form.</returns>
    internal static string Normalise(string keyword)
    {
        StringBuilder builder = new(keyword.Length);

        foreach (char character in keyword)
        {
            if (character is ' ' or '_' or '-')
            {
                continue;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    /// <summary>Is this the <c>FILE NAME</c> data link reference?</summary>
    /// <param name="normalisedKeyword">A keyword already passed through <see cref="Normalise"/>.</param>
    /// <returns><see langword="true"/> when the keyword names a data link file.</returns>
    internal static bool IsFileName(string normalisedKeyword) => FileNameKeywords.Contains(normalisedKeyword);

    /// <summary>Is this the OLE DB network-library keyword?</summary>
    /// <param name="normalisedKeyword">A keyword already passed through <see cref="Normalise"/>.</param>
    /// <returns><see langword="true"/> when the keyword selects a network library.</returns>
    internal static bool IsNetworkLibrary(string normalisedKeyword) =>
        NetworkLibraryKeywords.Contains(normalisedKeyword);

    /// <summary>Is this keyword dropped on the way to ADO.NET?</summary>
    /// <param name="normalisedKeyword">A keyword already passed through <see cref="Normalise"/>.</param>
    /// <returns><see langword="true"/> when the keyword has no ADO.NET equivalent.</returns>
    internal static bool IsDropped(string normalisedKeyword) => DroppedKeywords.Contains(normalisedKeyword);

    /// <summary>The ADO.NET spelling of an OLE DB keyword, when it differs.</summary>
    /// <param name="normalisedKeyword">A keyword already passed through <see cref="Normalise"/>.</param>
    /// <returns>The ADO.NET keyword, or <see langword="null"/> when the original spelling is kept.</returns>
    internal static string? Rename(string normalisedKeyword) =>
        RenamedKeywords.TryGetValue(normalisedKeyword, out string? renamed) ? renamed : null;
}
