using QuickStat.Diagnostics;

namespace QuickStat.Data;

/// <summary>
/// Step 0: put the session options in force and record who answered.
/// </summary>
/// <remarks>
/// <para>
/// PORT-PLAN.md §7.2, the fixed ordering bug. In the Delphi, <c>SET XACT_ABORT ON</c> and
/// <c>SET DATEFORMAT ymd</c> lived in login observer #2 (<c>Emetra.Database.Info.pas:146-147</c>),
/// which ran after <c>TSimpleDatabase.Connect</c> had already issued
/// <c>SELECT @@SERVERNAME, DB_NAME()</c> and after observer #1 had run
/// <c>EXEC dbo.GetStudyAndUser</c>. So the first user query of every session parsed dates under
/// whatever the server default was.
/// </para>
/// <para>
/// Here the options come first twice over. <see cref="SqlClientSession"/> issues them as part of
/// every physical open, so they also survive a reconnect during retry - which the Delphi's retry
/// path silently dropped (<c>Emetra.Database.Simple.pas:662</c>) - and this step re-states them in
/// the same round trip as the identity query, so the guarantee is visible in the pipeline rather
/// than buried in the connection.
/// </para>
/// </remarks>
internal sealed class SessionOptionsStep : ILoginStep
{
    /// <inheritdoc />
    public string Name => "Session options";

    /// <inheritdoc />
    public int Order => LoginStepOrder.SessionOptions;

    /// <inheritdoc />
    public async Task ExecuteAsync(LoginContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Progress?.Report(new OperationProgress("Connecting", "Setting session options ...", null));

        SqlResultSet result = await context.Sql.QueryAsync(
            new SqlRequest
            {
                CommandText = SqlClientSession.SessionOptionsBatch + "\r\n" + DataSql.ServerAndDatabase,
                IsIdempotent = true,
                Label = "Session options",
            },
            cancellationToken).ConfigureAwait(false);

        if (result.Count > 0)
        {
            SqlRow row = result[0];

            // Read by ordinal, as the Delphi did (Emetra.Database.Simple.pas:385-386).
            context.ServerName = row.GetString(0);
            context.DatabaseName = row.GetString(1);
        }
    }
}
