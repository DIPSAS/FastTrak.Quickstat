namespace QuickStat.Configuration;

/// <summary>
/// Reads a Microsoft Data Link file (<c>.UDL</c>) and returns the OLE DB initialisation string it
/// carries.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>TMSSQLConnString.LoadFromUdl</c>
/// (<c>Emetra.Database.ConnectionString.pas:184-198</c>). A data link file written by the Windows
/// "Data Link Properties" dialog is UTF-16 LE with a byte-order mark and has exactly three lines:
/// </para>
/// <code>
/// [oledb]
/// ; Everything after this line is an OLE DB initstring
/// Provider=SQLOLEDB.1;Integrated Security=SSPI;...
/// </code>
/// <para>
/// Only the third line matters, and it replaces the <em>entire</em> key set of the connection string
/// that referenced it - any keyword sitting next to <c>FILE NAME=</c> is discarded, exactly as the
/// Delphi did.
/// </para>
/// </remarks>
public interface IUdlReader
{
    /// <summary>
    /// Zero-based index of the line carrying the initialisation string. The Delphi read
    /// <c>udlFile[2]</c> and silently did nothing when the file was shorter
    /// (<c>Emetra.Database.ConnectionString.pas:193-194</c>).
    /// </summary>
    const int InitStringLineIndex = 2;

    /// <summary>Reads the OLE DB initialisation string.</summary>
    /// <param name="path">Absolute path of the data link file.</param>
    /// <returns>The third line of the file, trimmed.</returns>
    /// <exception cref="QuickStatConfigurationException">
    /// The file is missing, unreadable, shorter than three lines, or its third line is blank. The
    /// Delphi failed invisibly in the last two cases: the connection string stayed
    /// <c>FILE NAME=...</c>, which ADO also accepted, so the misconfiguration surfaced as an
    /// unrelated login error.
    /// </exception>
    string ReadInitString(string path);
}
