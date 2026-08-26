namespace QuickStat.Configuration;

/// <summary>
/// One <c>&lt;Connection&gt;</c> element of the deployed <c>QuickStat.config.xml</c>.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>TQuickStatConnection</c> (<c>QuickStat.Connections.pas:11-21</c>). The three legacy
/// child elements are reproduced verbatim because an existing configuration file must keep working
/// untouched (PORT-PLAN.md §6). The Delphi parser reads only <c>Name</c>, <c>StudyName</c> and
/// <c>ConnectionString</c> and ignores unknown children, which is what makes
/// <see cref="SqlOptions"/> a safe additive extension: a file carrying it still loads in the old
/// build.
/// </para>
/// <para>
/// <see cref="ConnectionString"/> is the <em>raw</em> OLE DB text, typically
/// <c>FILE NAME=.\FastTrak.UDL</c>. It is not usable by ADO.NET until
/// <see cref="IConnectionStringTranslator"/> has resolved the UDL and dropped the OLE DB-only
/// keywords.
/// </para>
/// </remarks>
public sealed record QuickStatConnection
{
    /// <summary>Display name shown in the project picker; also the catalogue key.</summary>
    /// <remarks>
    /// Duplicates are dropped by the catalogue, first entry wins
    /// (<c>QuickStat.Connections.pas:68-69</c>).
    /// </remarks>
    public required string Name { get; init; }

    /// <summary>
    /// <c>dbo.Study.StudName</c> of the protocol this entry opens.
    /// </summary>
    /// <remarks>
    /// Load-bearing beyond the login: the collector registry gates on this string
    /// (PORT-PLAN.md §10.4, <see cref="QuickStat.Collectors.StudyGatePatterns"/>).
    /// </remarks>
    public required string StudyName { get; init; }

    /// <summary>The <c>&lt;ConnectionString&gt;</c> element, verbatim and untranslated.</summary>
    public required string ConnectionString { get; init; }

    /// <summary>
    /// Optional <c>&lt;SqlOptions&gt;</c> element - a semicolon-separated ADO.NET keyword list
    /// applied <em>after</em> the UDL has been expanded, so it survives the UDL replacing the whole
    /// key set.
    /// </summary>
    /// <remarks>
    /// The .NET-only escape hatch for the <c>Encrypt</c> compatibility trap: SqlClient 7 defaults to
    /// <c>Encrypt=Mandatory</c>, which the legacy OLE DB strings never asked for and which fails
    /// against on-premise servers with self-signed certificates (PORT-PLAN.md §8.2,
    /// <c>Docs/Port/01-data-access.md</c> §3.5). <see langword="null"/> when the element is absent.
    /// </remarks>
    public string? SqlOptions { get; init; }
}
