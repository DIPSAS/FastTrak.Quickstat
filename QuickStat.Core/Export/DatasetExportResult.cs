namespace QuickStat.Export;

/// <summary>What an export produced.</summary>
/// <remarks>
/// Returned rather than inferred, mainly so the caller learns about
/// <see cref="KeyFilePath"/> without having to reconstruct it. The Delphi never told anyone that
/// file existed, which is why nothing ever deleted it.
/// </remarks>
public sealed record DatasetExportResult
{
    /// <summary>The file that was written.</summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// The re-identification key file, or <see langword="null"/> when none was written.
    /// </summary>
    /// <remarks>
    /// When this is non-null the caller <b>must</b> track it for deletion alongside the export
    /// itself, and should tell the user it exists.
    /// </remarks>
    public string? KeyFilePath { get; init; }

    /// <summary>Data rows written, excluding the header.</summary>
    public required int RowCount { get; init; }

    /// <summary>Columns written, including the identity columns and any timestamp columns.</summary>
    public required int ColumnCount { get; init; }
}
