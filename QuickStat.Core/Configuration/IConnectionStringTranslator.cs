namespace QuickStat.Configuration;

/// <summary>
/// Turns a legacy OLE DB / UDL connection string into one <c>Microsoft.Data.SqlClient</c> accepts.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>TMSSQLConnString</c> (<c>Emetra.Database.ConnectionString.pas</c>), whose
/// <c>Set_Value</c> expands <c>FILE NAME=</c> by loading the third line of the UDL and
/// <em>replacing the entire key set</em> (<c>:184-198</c>, <c>:261-268</c>).
/// </para>
/// <para>
/// The result is deliberately a <see cref="ResolvedConnectionString"/> and not a
/// <c>SqlConnectionStringBuilder</c>: contracts must not expose <c>Microsoft.Data.SqlClient</c>
/// types, which are step 2.2's implementation detail (PORT-PLAN.md §5 Phase 1).
/// </para>
/// </remarks>
public interface IConnectionStringTranslator
{
    /// <summary>Resolves the UDL, maps the OLE DB keywords and applies the defaults.</summary>
    /// <param name="connection">The catalogue entry to translate.</param>
    /// <returns>An ADO.NET connection string plus a log-safe rendering of it.</returns>
    /// <exception cref="QuickStatConfigurationException">
    /// The UDL is missing or malformed, or the result carries neither integrated security nor a
    /// user name and password - the equivalent of the Delphi's
    /// <c>EDatabaseCredentialsMissing</c> (<c>Emetra.Database.Simple.pas:379</c>), raised at
    /// translation time rather than at connect time so the failure names the configuration.
    /// </exception>
    ResolvedConnectionString Translate(QuickStatConnection connection);
}
