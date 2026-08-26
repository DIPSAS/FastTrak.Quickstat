namespace QuickStat.Export;

/// <summary>
/// Remembers the files an export left in a temporary directory, and deletes them.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>fFilesThatMustBeDeleted</c> (<c>MainQuickStat.pas:156</c>), drained in
/// <c>FormDestroy</c> (<c>:326-337</c>). It tracked the temporary <c>%TEMP%\&lt;guid&gt;.csv</c>
/// written for <c>Open this dataset in Excel</c> - but not the <c>.mapping.txt</c> key file written
/// beside it, which is the leak PORT-PLAN.md §7.2 records. <b>Whatever tracks the export must track
/// its key file</b>, which is why <c>DatasetExportResult.KeyFilePath</c> exists.
/// </para>
/// <para>
/// Deletion is best-effort: a file Excel still holds open cannot be removed, and failing an export
/// over it would be worse than leaving it. Failures are logged, never thrown.
/// </para>
/// </remarks>
public interface ITempFileTracker : IDisposable
{
    /// <summary>Paths currently tracked.</summary>
    IReadOnlyCollection<string> TrackedPaths { get; }

    /// <summary>Remembers a file to delete later.</summary>
    /// <param name="path">Absolute path. Tracking the same path twice is harmless.</param>
    void Track(string path);

    /// <summary>Deletes one tracked file now and forgets it.</summary>
    /// <param name="path">The path.</param>
    /// <returns><see langword="true"/> when the file is gone afterwards.</returns>
    bool Delete(string path);

    /// <summary>Deletes everything tracked so far, best effort.</summary>
    /// <returns>How many files are gone afterwards.</returns>
    int DeleteAll();
}
