namespace QuickStat.Domain.Packages;

/// <summary>Reads, writes and deletes packaged selections in <c>Report.QuickStat</c>.</summary>
public interface IPackageRepository
{
    /// <summary>Loads every package saved for one study.</summary>
    /// <param name="studyId">Current study.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The packages in server order.</returns>
    /// <remarks>
    /// <c>SELECT r.* FROM Report.QuickStat r JOIN dbo.Study s ON s.StudyId=r.StudyId
    /// WHERE r.StudyId=:StudyId</c> (<c>EPR.QA.SQL.pas:44</c>).
    /// </remarks>
    Task<IReadOnlyList<PackagedSelection>> GetPackagesAsync(int studyId, CancellationToken cancellationToken = default);

    /// <summary>Saves a package and returns it with its server-assigned row id.</summary>
    /// <param name="package">The package to store.</param>
    /// <param name="cancellationToken">Cancels the command.</param>
    /// <returns>The package with <see cref="PackagedSelection.RowId"/> filled in.</returns>
    /// <remarks>
    /// <c>EXEC Report.AddQuickStat :StudyId,:ProcId,:Title,:DataElements,:Comment</c>
    /// (<c>EPR.QA.SQL.pas:45</c>). Not idempotent - it must never be retried automatically.
    /// </remarks>
    Task<PackagedSelection> SaveAsync(PackagedSelection package, CancellationToken cancellationToken = default);

    /// <summary>Deletes a package.</summary>
    /// <param name="rowId"><see cref="PackagedSelection.RowId"/> of the package.</param>
    /// <param name="cancellationToken">Cancels the command.</param>
    /// <returns>A task that completes when the row is gone.</returns>
    /// <remarks>
    /// <c>EXEC QuickStat.DeletePackage :RowId</c> (<c>QuickStat.Selection.pas:129</c>). The caller
    /// must confirm first through <see cref="QuickStat.Diagnostics.IUserNotifier.ConfirmAsync"/>;
    /// the Delphi's confirmation could fail open and answer yes on the user's behalf.
    /// </remarks>
    Task DeleteAsync(int rowId, CancellationToken cancellationToken = default);
}
