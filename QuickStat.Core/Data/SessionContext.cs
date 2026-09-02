namespace QuickStat.Data;

/// <summary>
/// An immutable snapshot of one successful login. The single source of "which study, which user,
/// which database" for every other step.
/// </summary>
/// <remarks>
/// <para>
/// Replaces <c>TCRFSimpleContext</c> (<c>CRF.Context.Facade.pas</c>), whose state was spread over
/// four mutable sub-objects and exposed to SQL as published properties resolved by RTTI
/// (<c>Emetra.Classes.Business.pas:79-84</c>). Adding a published property there silently added a
/// resolvable <c>:Placeholder</c>; renaming one silently broke every population using it.
/// <see cref="TryGetParameterValue"/> is the explicit, testable replacement.
/// </para>
/// <para>
/// Consumed by step 2.3 (population parameters, <c>DbVersion</c>), step 2.4 (study gating on
/// <see cref="StudyName"/>) and step 2.5 (<see cref="StudyId"/>).
/// </para>
/// </remarks>
public sealed record SessionContext
{
    /// <summary><c>dbo.Study.StudName</c> as configured, before any database round trip.</summary>
    public required string StudyName { get; init; }

    /// <summary><c>dbo.Study.StudyId</c>.</summary>
    public required int StudyId { get; init; }

    /// <summary>Row id returned by <c>dbo.AddSession</c>; closed again by <c>dbo.CloseSession</c>.</summary>
    public required int SessionId { get; init; }

    /// <summary>The logged-in user.</summary>
    public required StudyUser User { get; init; }

    /// <summary>Server and schema facts.</summary>
    public required DatabaseInfo Database { get; init; }

    /// <summary><c>@@SERVERNAME</c>.</summary>
    public required string ServerName { get; init; }

    /// <summary><c>DB_NAME()</c>.</summary>
    public required string DatabaseName { get; init; }

    /// <summary>
    /// The user has no profession or no work site registered. QuickStat still works; the condition
    /// is surfaced through <see cref="QuickStat.Diagnostics.IUserNotifier"/> rather than crashing.
    /// </summary>
    public bool HasIncompleteUserProfile { get; init; }

    /// <summary>
    /// Resolves a population's <c>:Name</c> placeholder from session state.
    /// </summary>
    /// <param name="name">Placeholder name without the marker; matched case-insensitively.</param>
    /// <param name="value">The resolved value.</param>
    /// <returns><see langword="true"/> when the name is one this session can supply.</returns>
    /// <remarks>
    /// <para>
    /// The vocabulary is the six published properties of <c>TCRFSimpleContext</c>
    /// (<c>CRF.Context.Facade.pas:97-104</c>): <c>StudyId</c>, <c>StudyName</c>, <c>UserId</c>,
    /// <c>SessId</c>, <c>CenterId</c>, <c>CaseId</c>. <c>CaseId</c> is always zero in QuickStat -
    /// it never selects a patient - but populations may still reference it, so it must resolve
    /// rather than fail. <c>StartDate</c> and <c>StopDate</c> are <em>not</em> handled here; they
    /// come from the period prompt and are the only pair that asks the user anything.
    /// </para>
    /// <para>
    /// The Delphi resolves a name with <c>IsPublishedProp</c> (<c>TBusiness.TryGetValue</c>,
    /// <c>Emetra.Classes.Business.pas:79-84</c>), and published properties are inherited, so its
    /// vocabulary is strictly those six plus <c>Count</c> from <c>TObjectContainer</c>
    /// (<c>Emetra.ObjectContainer.pas:39-41</c>). <c>Count</c> is the container's child count and
    /// means nothing to a population; it is not reproduced. Nothing in either catalogue swept for
    /// PORT-PLAN.md R2 uses it.
    /// </para>
    /// </remarks>
    public bool TryGetParameterValue(string name, out object? value)
    {
        ArgumentNullException.ThrowIfNull(name);

        switch (name)
        {
            case not null when Matches(name, "StudyId"):
                value = StudyId;
                return true;

            case not null when Matches(name, "StudyName"):
                value = StudyName;
                return true;

            case not null when Matches(name, "UserId"):
                value = User.UserId;
                return true;

            case not null when Matches(name, "SessId"):
                value = SessionId;
                return true;

            case not null when Matches(name, "CenterId"):
                value = User.CenterId;
                return true;

            case not null when Matches(name, "CaseId"):
                // Always zero: QuickStat never calls TCRFSimpleContext.Select(personId), so
                // TActiveCase never has a case. It still has to resolve, because a population may
                // reference it and the Delphi's RTTI lookup would have answered.
                value = 0;
                return true;

            default:
                value = null;
                return false;
        }
    }

    private static bool Matches(string name, string candidate) =>
        string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase);
}
