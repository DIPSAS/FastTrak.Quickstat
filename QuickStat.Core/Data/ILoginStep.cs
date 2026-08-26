namespace QuickStat.Data;

/// <summary>
/// One stage of the connect-and-log-in sequence.
/// </summary>
/// <remarks>
/// <para>
/// Replaces the Delphi's five <c>ILoginObserver</c> registrations
/// (<c>CRF.Context.Facade.pas:169-172</c>, <c>MainQuickStat.pas:271</c>), which fired in
/// registration order with no way to inspect, reorder or test the sequence. Making the order an
/// explicit number fixes a real defect: <c>SET DATEFORMAT ymd</c> ran in observer #2, <em>after</em>
/// observer #1 had already issued a user query (<c>Emetra.Database.Info.pas:147</c>).
/// </para>
/// <para>
/// Steps are resolved from the container as <c>IEnumerable&lt;ILoginStep&gt;</c> and run in
/// ascending <see cref="Order"/>, so a later phase adds a stage with one registration line.
/// </para>
/// </remarks>
public interface ILoginStep
{
    /// <summary>Stable name for logs and progress reporting.</summary>
    string Name { get; }

    /// <summary>Ascending sort key. Leave gaps so a step can be inserted without renumbering.</summary>
    int Order { get; }

    /// <summary>Runs the step, reading and writing <paramref name="context"/>.</summary>
    /// <param name="context">Accumulating session state.</param>
    /// <param name="cancellationToken">Cancels the login.</param>
    /// <returns>A task that completes when the step is done.</returns>
    Task ExecuteAsync(LoginContext context, CancellationToken cancellationToken = default);
}
