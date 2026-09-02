using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace QuickStat.Configuration;

/// <summary>
/// The default <see cref="IConnectionStringTranslator"/>: expands <c>FILE NAME=</c>, maps the OLE DB
/// keywords onto <c>Microsoft.Data.SqlClient</c>, applies the overrides and then the defaults, and
/// refuses to hand back a string that cannot authenticate.
/// </summary>
/// <remarks>
/// <para>
/// The pipeline, in order - the order is the contract, because each stage can overwrite the
/// previous one:
/// </para>
/// <list type="number">
/// <item><description>
/// Parse <see cref="QuickStatConnection.ConnectionString"/>. If it carries <c>FILE NAME=</c>, resolve
/// the path, read the data link file and <em>replace the whole key set</em> with its initialisation
/// string. That replacement is legacy behaviour (<c>Emetra.Database.ConnectionString.pas:184-198</c>,
/// <c>:261-268</c>): any keyword written next to <c>FILE NAME=</c> is silently discarded.
/// </description></item>
/// <item><description>
/// Map each OLE DB keyword; see <see cref="OleDbKeywords"/>. Keywords whose value is empty once
/// unquoted are dropped rather than set, because the data link dialog writes every property it knows
/// about and spells the unset ones <c>""</c> (<see cref="OleDbKeywords.Unquote"/>).
/// </description></item>
/// <item><description>
/// Apply <see cref="QuickStatConnection.SqlOptions"/>, then the
/// <see cref="OptionsEnvironmentVariable"/> environment variable. Both overwrite. This is the stage
/// that survives the data link file replacing everything, which is exactly why the override lives in
/// the XML and not in the connection string.
/// </description></item>
/// <item><description>
/// Inject defaults for keywords still unset: the encryption pair, <c>Application Name</c>,
/// <c>Connect Timeout</c> and <c>Command Timeout</c>, all from <see cref="SqlOptions"/>.
/// </description></item>
/// <item><description>Validate server, database and credentials.</description></item>
/// </list>
/// <para>
/// <strong>Encryption (PORT-PLAN.md §8.2, R1).</strong> The legacy OLE DB strings carry no
/// encryption keyword at all and the providers they named did not encrypt by default;
/// <c>Microsoft.Data.SqlClient</c> defaults to <c>Encrypt=Mandatory</c>. A literal translation
/// therefore turns TLS on and, against an on-premise server presenting a self-signed certificate,
/// fails at login. The injected default is <c>Encrypt=True;TrustServerCertificate=True</c>. Be clear
/// about what that buys: the transport is encrypted, but the server's certificate is
/// <em>not verified</em>, so this is not proof against an attacker who can intercept the connection.
/// It preserves the connectivity the site has today and is not a security improvement. A site that
/// wants either verified TLS or the literal legacy behaviour overrides it per connection.
/// </para>
/// </remarks>
public sealed class OleDbConnectionStringTranslator : IConnectionStringTranslator
{
    /// <summary>
    /// Process-wide escape hatch, applied after <see cref="QuickStatConnection.SqlOptions"/> and
    /// therefore winning over it.
    /// </summary>
    /// <remarks>
    /// Exists for support: a site whose servers are old enough that they cannot negotiate TLS 1.2
    /// needs <c>Encrypt=False</c>, and asking someone to hand-edit an XML file over the telephone is
    /// worse than asking them to set one environment variable.
    /// </remarks>
    public const string OptionsEnvironmentVariable = "QUICKSTAT_SQL_OPTIONS";

    /// <summary>What a password is replaced by in <see cref="ResolvedConnectionString.Redacted"/>.</summary>
    public const string RedactedPasswordPlaceholder = "***";

    private const string ApplicationNameKeyword = "Application Name";
    private const string ConnectTimeoutKeyword = "Connect Timeout";
    private const string CommandTimeoutKeyword = "Command Timeout";

    private readonly SqlOptions _options;
    private readonly IUdlReader _udlReader;
    private readonly ILogger<OleDbConnectionStringTranslator> _logger;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="options">The process-wide knobs supplying the injected defaults.</param>
    /// <param name="udlReader">Reads the data link file a <c>FILE NAME=</c> keyword points at.</param>
    /// <param name="logger">Optional logger; defaults to a no-op logger.</param>
    public OleDbConnectionStringTranslator(
        SqlOptions options,
        IUdlReader udlReader,
        ILogger<OleDbConnectionStringTranslator>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(udlReader);

        _options = options;
        _udlReader = udlReader;
        _logger = logger ?? NullLogger<OleDbConnectionStringTranslator>.Instance;
    }

    /// <inheritdoc />
    public ResolvedConnectionString Translate(QuickStatConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        List<KeyValuePair<string, string>> keywords =
            Parse(connection.ConnectionString, "the <ConnectionString> element", connection);

        string? udlPath = null;
        string? dataLinkReference = FindDataLinkReference(keywords);

        if (dataLinkReference is not null)
        {
            udlPath = ResolveUdlPath(dataLinkReference, connection);
            keywords = Parse(_udlReader.ReadInitString(udlPath), udlPath, connection);
        }

        SqlConnectionStringBuilder builder = new();
        bool forceTcp = UsesTcpNetworkLibrary(keywords);

        foreach (KeyValuePair<string, string> keyword in keywords)
        {
            MapKeyword(builder, keyword.Key, keyword.Value, connection);
        }

        if (forceTcp)
        {
            ApplyTcpPrefix(builder, connection);
        }

        ApplyOverrides(builder, connection.SqlOptions, "the <SqlOptions> element", connection);
        ApplyOverrides(
            builder,
            Environment.GetEnvironmentVariable(OptionsEnvironmentVariable),
            OptionsEnvironmentVariable,
            connection);

        ApplyDefaults(builder, connection);
        Validate(builder, connection, udlPath);

        ResolvedConnectionString resolved = new()
        {
            Source = connection,
            Value = builder.ConnectionString,
            Redacted = Redact(builder),
            UdlPath = udlPath,
        };

        _logger.LogInformation(
            "Connection {ConnectionName} translated to {ConnectionString}.",
            connection.Name,
            resolved.Redacted);

        return resolved;
    }

    /// <summary>
    /// Executable directory first, working directory second - the pure half of the resolution, so it
    /// is testable without changing the process working directory.
    /// </summary>
    /// <param name="value">The value of the <c>FILE NAME</c> keyword, absolute or relative.</param>
    /// <param name="baseDirectory">Directory holding the executable.</param>
    /// <param name="workingDirectory">Process working directory.</param>
    /// <param name="path">The resolved path, or the executable-relative candidate when nothing exists.</param>
    /// <param name="usedWorkingDirectory">Whether the working-directory candidate answered.</param>
    /// <returns><see langword="true"/> when a file was found.</returns>
    /// <remarks>
    /// The Delphi resolved relative data link paths against the working directory only
    /// (<c>Emetra.Database.ConnectionString.pas:192</c> - <c>TStringList.LoadFromFile</c> does no
    /// executable-relative resolution), which breaks whenever the shortcut's "Start in" folder does
    /// not happen to match. PORT-PLAN.md §4.1 reverses the priority; the working directory stays as a
    /// fallback so a site depending on the old behaviour still starts.
    /// </remarks>
    internal static bool TryResolveUdlPath(
        string value,
        string baseDirectory,
        string workingDirectory,
        out string path,
        out bool usedWorkingDirectory)
    {
        usedWorkingDirectory = false;
        path = Path.GetFullPath(value, baseDirectory);

        if (File.Exists(path))
        {
            return true;
        }

        string fallback = Path.GetFullPath(value, workingDirectory);

        if (!string.Equals(path, fallback, StringComparison.OrdinalIgnoreCase) && File.Exists(fallback))
        {
            path = fallback;
            usedWorkingDirectory = true;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Produces the log-safe rendering: the same connection string with the password masked.
    /// </summary>
    /// <param name="builder">The finished builder.</param>
    /// <returns>A string that cannot contain the password.</returns>
    /// <remarks>
    /// Two layers on purpose. The builder clone masks the <c>Password</c> keyword properly, including
    /// its <c>PWD</c> spelling and any quoting the value needed. The literal replacement afterwards
    /// then guarantees the property this method exists for - the password does not appear in the
    /// output - even if the same text also ended up in some other keyword's value. It can mangle an
    /// unrelated keyword whose value happens to contain the password as a substring; that is the
    /// right way round to be wrong.
    /// <para>The user name is not masked: it is not a secret and support needs it.</para>
    /// </remarks>
    internal static string Redact(SqlConnectionStringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        string connectionString = builder.ConnectionString;
        string password = builder.Password;

        if (string.IsNullOrEmpty(password))
        {
            return connectionString;
        }

        SqlConnectionStringBuilder masked = new(connectionString)
        {
            Password = RedactedPasswordPlaceholder,
        };

        return masked.ConnectionString.Replace(password, RedactedPasswordPlaceholder, StringComparison.Ordinal);
    }

    private static string? FindDataLinkReference(List<KeyValuePair<string, string>> keywords)
    {
        // Values[] in Delphi returns the first match, so a repeated key is not an override.
        foreach (KeyValuePair<string, string> keyword in keywords)
        {
            if (OleDbKeywords.IsFileName(OleDbKeywords.Normalise(keyword.Key)) && keyword.Value.Length > 0)
            {
                return keyword.Value;
            }
        }

        return null;
    }

    private static bool UsesTcpNetworkLibrary(List<KeyValuePair<string, string>> keywords)
    {
        foreach (KeyValuePair<string, string> keyword in keywords)
        {
            if (OleDbKeywords.IsNetworkLibrary(OleDbKeywords.Normalise(keyword.Key))
                && string.Equals(keyword.Value, OleDbKeywords.TcpNetworkLibrary, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string Seconds(TimeSpan value)
    {
        double seconds = Math.Max(0, Math.Round(value.TotalSeconds));

        return ((int)Math.Min(seconds, int.MaxValue)).ToString(CultureInfo.InvariantCulture);
    }

    private static void Validate(SqlConnectionStringBuilder builder, QuickStatConnection connection, string? udlPath)
    {
        string origin = udlPath is null
            ? $"the <ConnectionString> element of connection '{connection.Name}'"
            : $"the data link file '{udlPath}'";

        if (string.IsNullOrWhiteSpace(builder.DataSource))
        {
            throw new QuickStatConfigurationException(
                $"Connection '{connection.Name}' has no server: {origin} sets no Data Source.")
            {
                FilePath = udlPath,
            };
        }

        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            throw new QuickStatConfigurationException(
                $"Connection '{connection.Name}' has no database: {origin} sets no Initial Catalog.")
            {
                FilePath = udlPath,
            };
        }

        // Delphi: TSimpleDatabase.Connect accepted integrated security or an embedded SQL login and
        // otherwise raised EDatabaseCredentialsMissing, 'Påloggingsinformasjon mangler!'
        // (Emetra.Database.Simple.pas:364-379, :122). Its login-dialog branch is dead code -
        // TSimpleDatabase.LoginDialog is never assigned anywhere in QuickStat - so those two are the
        // only ways to authenticate, then and now. Checked here rather than at connect time so the
        // message can name the file that is wrong.
        if (!builder.IntegratedSecurity
            && (string.IsNullOrEmpty(builder.UserID) || string.IsNullOrEmpty(builder.Password)))
        {
            throw new QuickStatConfigurationException(
                $"Connection '{connection.Name}' has no credentials: {origin} sets neither Integrated Security nor a User ID and Password.")
            {
                FilePath = udlPath,
            };
        }
    }

    private string ResolveUdlPath(string value, QuickStatConnection connection)
    {
        string path;
        bool usedWorkingDirectory;
        bool found;

        try
        {
            found = TryResolveUdlPath(
                value,
                AppContext.BaseDirectory,
                Environment.CurrentDirectory,
                out path,
                out usedWorkingDirectory);
        }
        catch (ArgumentException ex)
        {
            throw new QuickStatConfigurationException(
                $"Connection '{connection.Name}' points at the data link file '{value}', which is not a usable path.",
                ex)
            {
                FilePath = value,
            };
        }

        if (!found)
        {
            throw new QuickStatConfigurationException(
                $"Connection '{connection.Name}' points at the data link file '{value}', which was found neither at '{path}' nor in the working directory '{Environment.CurrentDirectory}'.")
            {
                FilePath = path,
            };
        }

        if (usedWorkingDirectory)
        {
            _logger.LogWarning(
                "Connection {ConnectionName} resolved its data link file from the working directory ({UdlPath}) because nothing answered beside the executable in {BaseDirectory}. Move the file next to the executable: the working directory depends on how the application was launched.",
                connection.Name,
                path,
                AppContext.BaseDirectory);
        }

        return path;
    }

    private void MapKeyword(SqlConnectionStringBuilder builder, string key, string value, QuickStatConnection connection)
    {
        string normalised = OleDbKeywords.Normalise(key);

        if (value.Length == 0)
        {
            // An unset property, not a property set to nothing. The data link dialog writes every
            // keyword it knows and spells the empty ones "" - which OleDbKeywords.Unquote has just
            // turned into this. Setting them would be actively harmful: an empty AttachDBFilename is
            // still an AttachDBFilename, and SqlClient attaches a file for it.
            _logger.LogDebug(
                "Connection {ConnectionName}: dropped the empty keyword {Keyword}.",
                connection.Name,
                key);

            return;
        }

        if (OleDbKeywords.IsFileName(normalised))
        {
            // Already expanded, and the expansion replaced everything else anyway.
            return;
        }

        if (OleDbKeywords.IsNetworkLibrary(normalised))
        {
            _logger.LogDebug(
                "Connection {ConnectionName}: dropped the OLE DB keyword {Keyword}; ADO.NET selects the protocol through the Data Source prefix.",
                connection.Name,
                key);

            return;
        }

        if (OleDbKeywords.IsDropped(normalised))
        {
            _logger.LogDebug(
                "Connection {ConnectionName}: dropped the OLE DB-only keyword {Keyword}.",
                connection.Name,
                key);

            return;
        }

        TrySet(builder, OleDbKeywords.Rename(normalised) ?? key, value, connection, "the connection string");
    }

    private void ApplyTcpPrefix(SqlConnectionStringBuilder builder, QuickStatConnection connection)
    {
        string dataSource = builder.DataSource;

        // A colon already present means an explicit protocol prefix, or an IPv6 literal. Leave both.
        if (dataSource.Length == 0 || dataSource.Contains(':'))
        {
            return;
        }

        builder.DataSource = OleDbKeywords.TcpDataSourcePrefix + dataSource;

        _logger.LogDebug(
            "Connection {ConnectionName}: the OLE DB network library was TCP/IP, so the Data Source became {DataSource}.",
            connection.Name,
            builder.DataSource);
    }

    private void ApplyOverrides(
        SqlConnectionStringBuilder builder,
        string? overrides,
        string origin,
        QuickStatConnection connection)
    {
        if (string.IsNullOrWhiteSpace(overrides))
        {
            return;
        }

        foreach (KeyValuePair<string, string> keyword in Parse(overrides, origin, connection))
        {
            TrySet(builder, keyword.Key, keyword.Value, connection, origin);
        }
    }

    /// <summary>
    /// Parses a keyword list and warns when a token had no keyword at all - the symptom of a value
    /// containing a semicolon, which neither this build nor the Delphi one can represent. The token
    /// itself is deliberately not logged: it may be half a password.
    /// </summary>
    private List<KeyValuePair<string, string>> Parse(string? text, string origin, QuickStatConnection connection)
    {
        List<KeyValuePair<string, string>> keywords = OleDbKeywords.Parse(text, out int ignoredTokens);

        if (ignoredTokens > 0)
        {
            _logger.LogWarning(
                "Connection {ConnectionName}: ignored {IgnoredTokenCount} entries of {Origin} that carried no keyword. A value containing a semicolon cannot be represented - quoting is not supported, and was not supported by the Delphi build either.",
                connection.Name,
                ignoredTokens,
                origin);
        }

        return keywords;
    }

    private void ApplyDefaults(SqlConnectionStringBuilder builder, QuickStatConnection connection)
    {
        // Per keyword, not all-or-nothing: a string that already asks for encryption but says nothing
        // about certificate validation still needs TrustServerCertificate, or it hits exactly the
        // failure this default exists to prevent.
        foreach (KeyValuePair<string, string> keyword in
                 Parse(_options.DefaultEncryptionOptions, "the injected defaults", connection))
        {
            TrySetDefault(builder, keyword.Key, keyword.Value, connection);
        }

        TrySetDefault(builder, ApplicationNameKeyword, _options.ApplicationName, connection);
        TrySetDefault(builder, ConnectTimeoutKeyword, Seconds(_options.ConnectTimeout), connection);

        // The per-command timeout in SqlOptions.DefaultCommandTimeout remains authoritative for the
        // execution layer; this only moves the same number into the connection string so that a
        // command created without one inherits it, and so the value shows up in the redacted string a
        // support engineer is looking at.
        TrySetDefault(builder, CommandTimeoutKeyword, Seconds(_options.DefaultCommandTimeout), connection);
    }

    private void TrySetDefault(
        SqlConnectionStringBuilder builder,
        string keyword,
        string value,
        QuickStatConnection connection)
    {
        if (string.IsNullOrEmpty(value) || builder.ShouldSerialize(keyword))
        {
            return;
        }

        TrySet(builder, keyword, value, connection, "the injected defaults");
    }

    private void TrySet(
        SqlConnectionStringBuilder builder,
        string keyword,
        string value,
        QuickStatConnection connection,
        string origin)
    {
        try
        {
            builder[keyword] = value;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                ex,
                "Connection {ConnectionName}: dropped {Keyword} from {Origin}; Microsoft.Data.SqlClient does not accept it.",
                connection.Name,
                keyword,
                origin);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(
                ex,
                "Connection {ConnectionName}: dropped {Keyword} from {Origin}; its value could not be converted.",
                connection.Name,
                keyword,
                origin);
        }
    }
}
