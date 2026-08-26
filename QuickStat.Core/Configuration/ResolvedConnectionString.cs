namespace QuickStat.Configuration;

/// <summary>
/// The output of <see cref="IConnectionStringTranslator"/>: an ADO.NET connection string paired
/// with a rendering that is safe to write to the log.
/// </summary>
/// <remarks>
/// The pairing exists so that no caller ever has to decide whether a given string is safe to log.
/// The Delphi logged the expanded OLE DB string wholesale, which leaks <c>Password=</c> for any
/// site using a SQL login (<c>Emetra.Database.Simple.pas:390</c>).
/// </remarks>
public sealed record ResolvedConnectionString
{
    /// <summary>The catalogue entry this was produced from.</summary>
    public required QuickStatConnection Source { get; init; }

    /// <summary>
    /// The connection string to open. May contain credentials; never log this.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// <see cref="Value"/> with <c>Password</c> replaced. Safe for logs and error messages.
    /// </summary>
    public required string Redacted { get; init; }

    /// <summary>
    /// The <c>.UDL</c> file that was expanded, or <see langword="null"/> when the source string
    /// carried no <c>FILE NAME=</c> key.
    /// </summary>
    /// <remarks>
    /// Recorded because path resolution changes in the port - exe directory first, working
    /// directory as a fallback - and support needs to know which file actually answered
    /// (<c>Docs/Port/01-data-access.md</c> §3.2).
    /// </remarks>
    public string? UdlPath { get; init; }
}
