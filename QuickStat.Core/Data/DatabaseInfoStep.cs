using System.Globalization;
using Microsoft.Extensions.Logging;
using QuickStat.Diagnostics;

namespace QuickStat.Data;

/// <summary>
/// Step 100: server properties and the FastTrak schema version.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>TDatabaseInfo.Refresh</c> (<c>Emetra.Database.Info.pas:99-124</c>) merged with
/// <c>TCRFEventMapper.AfterLogin</c> (<c>CRF.Input.EventMap.pas:47-49</c>), which issued a second
/// <c>EXEC dbo.GetDatabaseInfo</c> purely to read <c>EventScale</c> out of the same row.
/// </para>
/// <para>
/// The Delphi's deliberate swallow is kept: the whole body was wrapped in <c>try..except</c> and any
/// failure set <c>DbVersion</c> to <c>-1</c> (<c>:154-159</c>). That value is load-bearing - the
/// population catalogue falls back to its no-version query on it
/// (<c>EPR.Population.List.pas:104-109</c>) - so swallowing here is behaviour, not laziness.
/// </para>
/// <para>
/// The version check is <em>not</em> swallowed, which is a deliberate change. In the Delphi,
/// <c>VerifyDbVersion</c> raised inside the same <c>try</c>, so its exception was caught two lines
/// later and turned into <c>DbVersion = -1</c>: the "database too old" check could never reach the
/// user. Here it runs outside the guard and raises
/// <see cref="DatabaseVersionTooOldException"/>.
/// </para>
/// </remarks>
internal sealed class DatabaseInfoStep : ILoginStep
{
    private const string ProductVersionColumn = "ProductVersion";
    private const string CollationColumn = "Collation";
    private const string ServerNameColumn = "ServerName";
    private const string WorkstationNameColumn = "WorkstationName";

    /// <summary>Field names read from <c>dbo.GetDatabaseInfo</c> (<c>Emetra.Database.Info.pas:85-89</c>).</summary>
    private const string DatabaseNameField = "DatabaseName";
    private const string DatabaseVersionField = "DatabaseVersion";
    private const string EventScaleField = "EventScale";
    private const string ServerTypeField = "ServerType";
    private const string ServerVersionField = "ServerVersion";

    private readonly ILogger<DatabaseInfoStep> _logger;

    public DatabaseInfoStep(ILogger<DatabaseInfoStep> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Database information";

    /// <inheritdoc />
    public int Order => LoginStepOrder.DatabaseInfo;

    /// <summary>
    /// Marketing year for a major version, from <c>TDatabaseInfo.Get_ProductYear</c>
    /// (<c>Emetra.Database.Info.pas:203-219</c>), extended for major version 17.
    /// </summary>
    /// <param name="majorVersion">Leading component of <c>SERVERPROPERTY('ProductVersion')</c>.</param>
    /// <returns>The year, or 9999 when unrecognised.</returns>
    internal static int ProductYearFor(int majorVersion) => majorVersion switch
    {
        6 => 1996,
        7 => 1998,
        8 => 2000,
        9 => 2005,
        10 => 2008,
        11 => 2012,
        12 => 2014,
        13 => 2016,
        14 => 2017,
        15 => 2019,
        16 => 2022,
        17 => 2025,
        _ => 9999,
    };

    /// <summary>Leading integer of a dotted product version, or zero.</summary>
    /// <param name="productVersion">For example <c>15.0.4123.1</c>.</param>
    /// <returns>The major version.</returns>
    internal static int MajorVersionOf(string productVersion)
    {
        int dot = productVersion.IndexOf('.', StringComparison.Ordinal);
        string head = dot < 0 ? productVersion : productVersion[..dot];

        return int.TryParse(head, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(LoginContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Progress?.Report(new OperationProgress("Connecting", "Reading database information ...", null));

        DatabaseInfo info;

        try
        {
            info = await ReadAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (QuickStatDataException exception)
        {
            // Delphi Emetra.Database.Info.pas:154-159, verbatim in intent.
            _logger.LogError(exception, "Database information could not be read; setting DbVersion to -1.");
            context.Database = new DatabaseInfo { DbVersion = -1 };
            return;
        }

        context.Database = info;

        // VerifyDbVersion (Emetra.Database.Info.pas:131-135), outside the swallow - see the remarks.
        if (info.DbVersion > 0 && info.DbVersion < DatabaseInfo.MinimumDbVersion)
        {
            throw new DatabaseVersionTooOldException(string.Format(
                CultureInfo.CurrentCulture,
                "Databasen (versjon {0}) må oppgraderes til versjon {1}.",
                info.DbVersion,
                DatabaseInfo.MinimumDbVersion))
            {
                DbVersion = info.DbVersion,
            };
        }
    }

    private static async Task<DatabaseInfo> ReadAsync(LoginContext context, CancellationToken cancellationToken)
    {
        SqlResultSet properties = await context.Sql.QueryAsync(
            new SqlRequest
            {
                CommandText = DataSql.ServerProperties,
                IsIdempotent = true,
                Label = "Server properties",
            },
            cancellationToken).ConfigureAwait(false);

        string productVersion = "";
        string collation = "";
        string serverName = "";
        string workstationName = "";
        string dbName = "";

        if (properties.Count > 0)
        {
            SqlRow row = properties[0];

            // The Delphi read the first four by ordinal and DatabaseName by name
            // (Emetra.Database.Info.pas:103-107).
            productVersion = row.GetString(properties.GetOrdinal(ProductVersionColumn));
            collation = row.GetString(properties.GetOrdinal(CollationColumn));
            serverName = row.GetString(properties.GetOrdinal(ServerNameColumn));
            workstationName = row.GetString(properties.GetOrdinal(WorkstationNameColumn));
            dbName = row.GetString(properties.GetOrdinal(DatabaseNameField));
        }

        SqlResultSet databaseInfo = await context.Sql.QueryAsync(
            new SqlRequest
            {
                CommandText = DataSql.DatabaseInfo,
                IsIdempotent = true,
                Label = "Database information",
            },
            cancellationToken).ConfigureAwait(false);

        string serverType = "";
        int dbVersion = 0;
        string serverVersion = "";
        int eventScale = 0;

        if (databaseInfo.Count > 0)
        {
            SqlRow row = databaseInfo[0];

            serverType = row.GetString(databaseInfo.GetOrdinal(ServerTypeField));
            dbName = row.GetString(databaseInfo.GetOrdinal(DatabaseNameField));
            dbVersion = row.GetInt32(databaseInfo.GetOrdinal(DatabaseVersionField));
            serverVersion = row.GetString(databaseInfo.GetOrdinal(ServerVersionField));
            eventScale = row.GetInt32(databaseInfo.GetOrdinal(EventScaleField));
        }

        int major = MajorVersionOf(productVersion);

        return new DatabaseInfo
        {
            ProductVersion = productVersion,
            ProductMajorVersion = major,
            ProductYear = ProductYearFor(major),
            Collation = collation,
            ServerName = serverName,
            WorkstationName = workstationName,
            DbName = dbName,
            DbVersion = dbVersion,
            ServerVersion = serverVersion,
            EventScale = eventScale,
            ServerType = serverType,
        };
    }
}
