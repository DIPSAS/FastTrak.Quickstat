namespace QuickStat.Configuration;

/// <summary>
/// Reads the deployed <c>QuickStat.config.xml</c> and yields the projects offered in the picker.
/// </summary>
/// <remarks>
/// Delphi: <c>TConnectionList.Load</c> (<c>QuickStat.Connections.pas:56-76</c>), which collects
/// every node named <c>Connection</c> <em>anywhere</em> in the document rather than at a fixed
/// path. Reproduce that: real deployments nest the element differently.
/// </remarks>
public interface IConnectionCatalog
{
    /// <summary>
    /// <c>ChangeFileExt(ParamStr(0), '.config.xml')</c> resolved against
    /// <see cref="AppContext.BaseDirectory"/>.
    /// </summary>
    /// <remarks>
    /// Not the working directory: launching from a shortcut sets an arbitrary CWD, and the Delphi
    /// build's CWD-relative resolution is a latent deployment bug (PORT-PLAN.md §4.1).
    /// </remarks>
    string DefaultConfigFilePath { get; }

    /// <summary>Loads the catalogue, keeping the first entry for each duplicated name.</summary>
    /// <param name="configFilePath">Path to the XML file.</param>
    /// <returns>
    /// The connections in document order. An <em>empty</em> list when the file is missing - the
    /// Delphi logs and carries on with an empty project list rather than aborting startup
    /// (<c>MainQuickStat.pas:392-398</c>), and that behaviour is preserved minus the modal dialog.
    /// </returns>
    /// <exception cref="QuickStatConfigurationException">The file exists but cannot be parsed.</exception>
    IReadOnlyList<QuickStatConnection> Load(string configFilePath);
}
