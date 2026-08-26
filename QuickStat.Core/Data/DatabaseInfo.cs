namespace QuickStat.Data;

/// <summary>
/// Server and schema facts read once at login from <c>SERVERPROPERTY(…)</c> and
/// <c>EXEC dbo.GetDatabaseInfo</c>.
/// </summary>
/// <remarks>
/// Delphi: <c>TDatabaseInfo.Refresh</c> (<c>Emetra.Database.Info.pas:99-124</c>) plus a second,
/// redundant <c>EXEC dbo.GetDatabaseInfo</c> from the event mapper
/// (<c>CRF.Input.EventMap.pas:47</c>) that only wanted <see cref="EventScale"/> from the same row.
/// Merged into one call.
/// </remarks>
public sealed record DatabaseInfo
{
    /// <summary><c>SERVERPROPERTY('ProductVersion')</c>, e.g. <c>15.0.4123.1</c>.</summary>
    public string ProductVersion { get; init; } = "";

    /// <summary>Leading component of <see cref="ProductVersion"/>.</summary>
    public int ProductMajorVersion { get; init; }

    /// <summary>
    /// Marketing year derived from <see cref="ProductMajorVersion"/> (8 to 2000, 16 to 2022, and so
    /// on), or 9999 when unrecognised.
    /// </summary>
    /// <remarks>Delphi: <c>Emetra.Database.Info.pas:203-219</c>, extended for major version 17.</remarks>
    public int ProductYear { get; init; }

    /// <summary><c>SERVERPROPERTY('Collation')</c>.</summary>
    public string Collation { get; init; } = "";

    /// <summary><c>SERVERPROPERTY('ServerName')</c>.</summary>
    public string ServerName { get; init; } = "";

    /// <summary><c>HOST_NAME()</c> as the server saw it.</summary>
    public string WorkstationName { get; init; } = "";

    /// <summary><c>DB_NAME()</c>.</summary>
    public string DbName { get; init; } = "";

    /// <summary>
    /// FastTrak schema version from <c>dbo.GetDatabaseInfo</c>, column <c>DatabaseVersion</c>.
    /// </summary>
    /// <remarks>
    /// <c>-1</c> when the whole info query failed; the Delphi swallowed that failure deliberately
    /// (<c>Emetra.Database.Info.pas:154-159</c>). Two behaviours key off this value, so a swallowed
    /// failure silently changes them: the population catalogue picks its stored procedure on
    /// <c>&gt;= 18200</c> (<c>EPR.Population.List.pas:104-107</c>), and anything below 510 but above
    /// zero is rejected outright.
    /// </remarks>
    public int DbVersion { get; init; }

    /// <summary>Server product string from <c>dbo.GetDatabaseInfo</c>.</summary>
    public string ServerVersion { get; init; } = "";

    /// <summary>Event-numbering scale used by the clinical event tables.</summary>
    public int EventScale { get; init; }

    /// <summary>Server role reported by <c>dbo.GetDatabaseInfo</c> (production, test, and so on).</summary>
    public string ServerType { get; init; } = "";

    /// <summary>Lowest <see cref="DbVersion"/> the application accepts.</summary>
    /// <remarks>Delphi: <c>VerifyDbVersion</c> (<c>Emetra.Database.Info.pas:131-135</c>).</remarks>
    public const int MinimumDbVersion = 510;

    /// <summary><see cref="DbVersion"/> at or above which the population catalogue takes a version argument.</summary>
    /// <remarks>Delphi: <c>EPR.Population.List.pas:104</c>.</remarks>
    public const int PopulationsWithVersionDbVersion = 18200;
}
