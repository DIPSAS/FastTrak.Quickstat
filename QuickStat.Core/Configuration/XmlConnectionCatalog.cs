using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace QuickStat.Configuration;

/// <summary>
/// The default <see cref="IConnectionCatalog"/>: reads <c>&lt;exe&gt;.config.xml</c> with
/// <see cref="XDocument"/>.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>TConnectionList.Load</c> (<c>QuickStat.Connections.pas:56-76</c>) over
/// <c>TNodeList</c> (<c>Emetra.Xml.NodeList.pas:33-51</c>), which walks the document element's
/// subtree and collects <em>every</em> node named <c>Connection</c> at any depth. That recursion is
/// reproduced with <see cref="XContainer.Descendants(XName)"/> on the root element, so a file that
/// nests the elements differently keeps working.
/// </para>
/// <para>
/// Two other legacy behaviours are reproduced exactly: duplicate <c>&lt;Name&gt;</c> values are
/// dropped, first one wins (<c>QuickStat.Connections.pas:68-69</c>; the Delphi key comparison is
/// ordinal and case-sensitive, which <see cref="StringComparer.Ordinal"/> matches), and a missing
/// configuration file yields an empty list rather than a failure
/// (<c>MainQuickStat.pas:392-398</c> logged and carried on with an empty project picker). The modal
/// error dialog the Delphi showed is not reproduced; a warning goes to the log instead, and the
/// user-facing half of that decision belongs to the shell.
/// </para>
/// <para>
/// The catalogue does not sort. The Delphi stored the entries in a <c>TObjectDictionary</c> - hash
/// order - and then set <c>cbProject.Sorted := true</c> (<c>MainQuickStat.pas:399</c>), so sorting
/// is the picker's job and document order is what this returns.
/// </para>
/// </remarks>
public sealed class XmlConnectionCatalog : IConnectionCatalog
{
    /// <summary>
    /// The extension appended by <c>ChangeFileExt(ParamStr(0), '.config.xml')</c>
    /// (<c>MainQuickStat.pas:391</c>).
    /// </summary>
    public const string ConfigFileExtension = ".config.xml";

    /// <summary>Element that carries one connection, at any depth in the document.</summary>
    public const string ConnectionElementName = "Connection";

    /// <summary>
    /// Base name used when neither the process path nor the entry assembly gives one - the
    /// executable is named <c>QuickStat.exe</c> by hard constraint (PORT-PLAN.md §4.1).
    /// </summary>
    public const string FallbackApplicationName = "QuickStat";

    private const string NameElementName = "Name";
    private const string StudyNameElementName = "StudyName";
    private const string ConnectionStringElementName = "ConnectionString";
    private const string SqlOptionsElementName = "SqlOptions";

    private const string HostExecutableName = "dotnet";

    private readonly ILogger<XmlConnectionCatalog> _logger;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="logger">Optional logger; defaults to a no-op logger.</param>
    public XmlConnectionCatalog(ILogger<XmlConnectionCatalog>? logger = null) =>
        _logger = logger ?? NullLogger<XmlConnectionCatalog>.Instance;

    /// <inheritdoc />
    /// <remarks>
    /// Resolved from <see cref="AppContext.BaseDirectory"/> rather than the working directory
    /// (PORT-PLAN.md §4.1): launching from a shortcut sets an arbitrary working directory, and the
    /// Delphi's <c>ParamStr(0)</c> happened to be immune only because it is an absolute path while
    /// its <em>UDL</em> resolution was not. <c>Assembly.Location</c> is deliberately not used - it is
    /// empty under single-file publish.
    /// <para>
    /// The property touches the file system: when nothing answers beside the executable but a file
    /// of the same name sits in the working directory, that path is returned instead, which keeps a
    /// site that relied on the old behaviour running. When neither exists the executable-relative
    /// path is returned, so an error message names the location the file is supposed to occupy.
    /// </para>
    /// </remarks>
    public string DefaultConfigFilePath => ResolveDefaultConfigFilePath(
        AppContext.BaseDirectory,
        Environment.CurrentDirectory,
        Environment.ProcessPath,
        Assembly.GetEntryAssembly()?.GetName().Name);

    /// <inheritdoc />
    public IReadOnlyList<QuickStatConnection> Load(string configFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configFilePath);

        if (!File.Exists(configFilePath))
        {
            _logger.LogWarning(
                "The configuration file {ConfigFilePath} was not found; the project list will be empty.",
                configFilePath);

            return [];
        }

        XDocument document = LoadDocument(configFilePath);
        XElement? root = document.Root;

        if (root is null)
        {
            _logger.LogWarning("The configuration file {ConfigFilePath} has no root element.", configFilePath);

            return [];
        }

        List<QuickStatConnection> connections = [];
        HashSet<string> seenNames = new(StringComparer.Ordinal);

        foreach (XElement element in root.Descendants(ConnectionElementName))
        {
            QuickStatConnection connection = ParseConnection(element, configFilePath);

            if (!seenNames.Add(connection.Name))
            {
                _logger.LogWarning(
                    "The configuration file {ConfigFilePath} declares more than one connection named {ConnectionName}; only the first is used.",
                    configFilePath,
                    connection.Name);

                continue;
            }

            connections.Add(connection);
        }

        _logger.LogInformation(
            "Loaded {ConnectionCount} connection(s) from {ConfigFilePath}.",
            connections.Count,
            configFilePath);

        return connections;
    }

    /// <summary>
    /// The pure half of <see cref="DefaultConfigFilePath"/>, so the resolution order is testable
    /// without touching the process.
    /// </summary>
    /// <param name="baseDirectory">Directory holding the executable.</param>
    /// <param name="workingDirectory">Process working directory, used only as a fallback.</param>
    /// <param name="processPath">Full path of the running process, or <see langword="null"/>.</param>
    /// <param name="entryAssemblyName">Simple name of the entry assembly, or <see langword="null"/>.</param>
    /// <returns>The path the catalogue should be read from.</returns>
    internal static string ResolveDefaultConfigFilePath(
        string baseDirectory,
        string workingDirectory,
        string? processPath,
        string? entryAssemblyName)
    {
        string fileName = DeriveConfigFileName(processPath, entryAssemblyName);
        string preferred = Path.Combine(baseDirectory, fileName);

        if (File.Exists(preferred))
        {
            return preferred;
        }

        string fallback = Path.Combine(workingDirectory, fileName);

        if (!string.Equals(preferred, fallback, StringComparison.OrdinalIgnoreCase) && File.Exists(fallback))
        {
            return fallback;
        }

        return preferred;
    }

    /// <summary>
    /// <c>ChangeFileExt(ParamStr(0), '.config.xml')</c>, minus the directory part.
    /// </summary>
    /// <param name="processPath">Full path of the running process, or <see langword="null"/>.</param>
    /// <param name="entryAssemblyName">Simple name of the entry assembly, or <see langword="null"/>.</param>
    /// <returns>The bare file name, for example <c>QuickStat.config.xml</c>.</returns>
    internal static string DeriveConfigFileName(string? processPath, string? entryAssemblyName)
    {
        string? stem = string.IsNullOrEmpty(processPath) ? null : Path.GetFileNameWithoutExtension(processPath);

        // "dotnet MyApp.dll" makes the process "dotnet"; the entry assembly is what the user launched.
        if (string.IsNullOrEmpty(stem) || string.Equals(stem, HostExecutableName, StringComparison.OrdinalIgnoreCase))
        {
            stem = entryAssemblyName;
        }

        if (string.IsNullOrEmpty(stem))
        {
            stem = FallbackApplicationName;
        }

        return stem + ConfigFileExtension;
    }

    private static XDocument LoadDocument(string configFilePath)
    {
        try
        {
            return XDocument.Load(configFilePath, LoadOptions.None);
        }
        catch (XmlException ex)
        {
            throw new QuickStatConfigurationException(
                $"The configuration file '{configFilePath}' is not well-formed XML.",
                ex)
            {
                FilePath = configFilePath,
            };
        }
        catch (IOException ex)
        {
            throw new QuickStatConfigurationException($"The configuration file '{configFilePath}' could not be read.", ex)
            {
                FilePath = configFilePath,
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new QuickStatConfigurationException($"The configuration file '{configFilePath}' could not be read.", ex)
            {
                FilePath = configFilePath,
            };
        }
    }

    private static QuickStatConnection ParseConnection(XElement element, string configFilePath)
    {
        // Delphi read these through IXMLNode's default ChildValues property, which raises when the
        // child is absent (QuickStat.Connections.pas:41-43). Raising a typed exception that names the
        // file is the same outcome with a message a support engineer can act on.
        string name = RequiredChildValue(element, NameElementName, configFilePath);
        string studyName = RequiredChildValue(element, StudyNameElementName, configFilePath);
        string connectionString = RequiredChildValue(element, ConnectionStringElementName, configFilePath);

        // The .NET-only extension (PORT-PLAN.md §8.1). Trimmed, because unlike the three legacy
        // elements it is new and nothing depends on its incidental whitespace.
        string? sqlOptions = element.Element(SqlOptionsElementName)?.Value.Trim();

        return new QuickStatConnection
        {
            Name = name,
            StudyName = studyName,
            ConnectionString = connectionString,
            SqlOptions = string.IsNullOrEmpty(sqlOptions) ? null : sqlOptions,
        };
    }

    private static string RequiredChildValue(XElement element, string childName, string configFilePath)
    {
        XElement? child = element.Element(childName);

        if (child is null)
        {
            throw new QuickStatConfigurationException(
                $"A <{ConnectionElementName}> element in '{configFilePath}' has no <{childName}> child element.")
            {
                FilePath = configFilePath,
            };
        }

        // Deliberately not trimmed: the Delphi did not trim either, and Name and StudyName are
        // compared and displayed verbatim - StudyName in particular drives collector gating.
        return child.Value;
    }
}
