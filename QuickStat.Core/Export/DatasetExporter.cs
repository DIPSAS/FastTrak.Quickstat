using Microsoft.Extensions.Logging;
using QuickStat.Diagnostics;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.Matrix;

namespace QuickStat.Export;

/// <summary>Writes a locked matrix to a file, in whichever format the options ask for.</summary>
/// <remarks>
/// <para>
/// Delphi: <c>TPersonGridData.SaveToFile</c> (<c>EPR.QA.Matrix.pas:445-497</c>), reached from both
/// <c>Open this dataset in Excel</c> and <c>Save this dataset to CSV file</c>. Two differences are
/// deliberate and are the privacy half of PORT-PLAN.md §7.2:
/// </para>
/// <list type="number">
///   <item><description>
///     The anonymiser is <b>shared and long-lived</b>, not constructed per call. The Delphi built a
///     fresh <c>TMatrixAnonymizer</c> for every export and drew from an unseeded global RNG, so the
///     same patient changed pseudonym between two exports of one loaded dataset while two different
///     cohorts of equal size received identical pseudonym lists. Here, two exports of one loaded
///     dataset agree and two datasets are unlinkable.
///   </description></item>
///   <item><description>
///     The <c>.mapping.txt</c> re-identification key is written <b>only</b> when
///     <see cref="DatasetExportOptions.WriteKeyFile"/> says so. When it is written the event is
///     logged as a warning, the user is told, and the path comes back in
///     <see cref="DatasetExportResult.KeyFilePath"/> so the caller can track it for deletion.
///   </description></item>
/// </list>
/// </remarks>
public sealed class DatasetExporter : IDatasetExporter
{
    /// <summary>
    /// What the user is told when a key file is written. Norwegian would be wrong here: the
    /// application chrome is English (PORT-PLAN.md §8.6).
    /// </summary>
    public const string KeyFileWarning =
        "A re-identification key was written next to this export. It maps every pseudonym back to a " +
        "real PersonId, so the export is only anonymous while the two are kept apart. Delete the key " +
        "file as soon as it has served its purpose, and never send it with the data.";

    private readonly IAnonymiser _anonymiser;
    private readonly ILogger<DatasetExporter> _logger;
    private readonly IUserNotifier? _notifier;

    /// <summary>Creates an exporter.</summary>
    /// <param name="anonymiser">
    /// The shared anonymiser. Must be the same instance for the lifetime of a loaded dataset.
    /// </param>
    /// <param name="logger">Where the key-file warning goes.</param>
    /// <param name="notifier">
    /// Optional user-facing notifier, used only to warn about a key file.
    /// </param>
    public DatasetExporter(
        IAnonymiser anonymiser,
        ILogger<DatasetExporter> logger,
        IUserNotifier? notifier = null)
    {
        ArgumentNullException.ThrowIfNull(anonymiser);
        ArgumentNullException.ThrowIfNull(logger);

        _anonymiser = anonymiser;
        _logger = logger;
        _notifier = notifier;
    }

    /// <inheritdoc />
    public async Task<DatasetExportResult> ExportAsync(
        PersonMatrix matrix,
        string filePath,
        DatasetExportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(options);

        return await ExportAsync(ExportDataset.FromMatrix(matrix), filePath, options, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Exports an already-flattened dataset.</summary>
    /// <param name="dataset">The dataset.</param>
    /// <param name="filePath">Destination. Overwritten if it exists.</param>
    /// <param name="options">Identification, timestamps, format, dialect.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The paths written and the dimensions.</returns>
    /// <remarks>
    /// The overload the unit tests use, and the one to call when the dataset was not produced by a
    /// <see cref="PersonMatrix"/>.
    /// </remarks>
    public async Task<DatasetExportResult> ExportAsync(
        ExportDataset dataset,
        string filePath,
        DatasetExportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(options);

        IdentificationColumns columns = options.Columns;

        if (columns.UsesPseudonyms && _anonymiser.EnsureSpaceFor(dataset.Rows.Count))
        {
            // Whoever loaded the population should have called Reset. Recovering silently would hide
            // the case where a stale map from a previous cohort is still in place.
            _logger.LogInformation(
                "No pseudonym space existed for {RowCount} people, so one was created at export time.",
                dataset.Rows.Count);
        }

        string directory = Path.GetDirectoryName(Path.GetFullPath(filePath))!;
        Directory.CreateDirectory(directory);

        await WritePayloadAsync(dataset, filePath, options, cancellationToken).ConfigureAwait(false);

        string? keyFilePath = await WriteKeyFileAsync(filePath, options, cancellationToken)
            .ConfigureAwait(false);

        return new DatasetExportResult
        {
            FilePath = filePath,
            KeyFilePath = keyFilePath,
            RowCount = dataset.Rows.Count,
            ColumnCount = CsvMatrixWriter.CountColumns(dataset, options),
        };
    }

    private async Task WritePayloadAsync(
        ExportDataset dataset,
        string filePath,
        DatasetExportOptions options,
        CancellationToken cancellationToken)
    {
        var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);

        await using (stream.ConfigureAwait(false))
        {
            switch (options.Format)
            {
                case ExportFormat.Csv:
                    CsvMatrixWriter.Write(dataset, stream, options, _anonymiser, cancellationToken);
                    break;

                case ExportFormat.Xlsx:
                    XlsxMatrixWriter.Write(dataset, stream, options, _anonymiser, cancellationToken);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(options),
                        options.Format,
                        "Unhandled export format.");
            }
        }
    }

    private async Task<string?> WriteKeyFileAsync(
        string filePath,
        DatasetExportOptions options,
        CancellationToken cancellationToken)
    {
        if (!options.WriteKeyFile)
        {
            return null;
        }

        if (!options.Columns.UsesPseudonyms)
        {
            // There is nothing to undo: the file would either be empty or, worse, imply that the
            // export was pseudonymised when it carries real person ids.
            _logger.LogInformation(
                "No re-identification key was written: {Identification} does not pseudonymise.",
                options.Identification);

            return null;
        }

        string keyFilePath = PseudonymKeyWriter.KeyFilePathFor(filePath);
        var stream = new FileStream(keyFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);

        await using (stream.ConfigureAwait(false))
        {
            PseudonymKeyWriter.Write(_anonymiser.PseudonymToPersonId, stream);
        }

        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogWarning(
            "Wrote a plaintext re-identification key to {KeyFilePath}. It maps every pseudonym back " +
            "to a real PersonId, so it must be tracked for deletion and never shipped with the export.",
            keyFilePath);

        if (_notifier is not null)
        {
            await _notifier.WarnAsync(KeyFileWarning).ConfigureAwait(false);
        }

        return keyFilePath;
    }
}
