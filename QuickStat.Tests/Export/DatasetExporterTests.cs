using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Diagnostics;
using QuickStat.Domain.Anonymisation;
using QuickStat.Export;
using Xunit;

namespace QuickStat.Tests.Export;

/// <summary>End-to-end file writing, key-file opt-in, and the DI registration.</summary>
public class DatasetExporterTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "QuickStat.Tests",
        Guid.NewGuid().ToString("N"));

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        GC.SuppressFinalize(this);

        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private string PathFor(string name) => Path.Combine(_directory, name);

    private static DatasetExporter NewExporter(
        out MatrixAnonymiser anonymiser,
        IUserNotifier? notifier = null)
    {
        anonymiser = new MatrixAnonymiser();
        return new DatasetExporter(anonymiser, NullLogger<DatasetExporter>.Instance, notifier);
    }

    [Fact]
    public async Task NoKeyFileIsWrittenUnlessItIsAskedFor()
    {
        DatasetExporter exporter = NewExporter(out MatrixAnonymiser anonymiser);
        anonymiser.Reset(1);

        string path = PathFor("export.csv");

        DatasetExportResult result = await exporter.ExportAsync(
            ExportFixtures.WorkedExample(),
            path,
            new DatasetExportOptions
            {
                Identification = PersonIdentification.RandomPersonId,
                Culture = ExportFixtures.Norwegian,
            });

        Assert.Null(result.KeyFilePath);
        Assert.True(File.Exists(path));
        Assert.False(File.Exists(PseudonymKeyWriter.KeyFilePathFor(path)));
        Assert.Empty(Directory.GetFiles(_directory, "*.mapping.txt"));
    }

    [Fact]
    public async Task TheKeyFileIsWrittenWarnedAboutAndReportedWhenOptedIn()
    {
        var notifier = new RecordingNotifier();
        DatasetExporter exporter = NewExporter(out MatrixAnonymiser anonymiser, notifier);
        anonymiser.Reset(1);

        string path = PathFor("export.csv");

        DatasetExportResult result = await exporter.ExportAsync(
            ExportFixtures.WorkedExample(),
            path,
            new DatasetExportOptions
            {
                Identification = PersonIdentification.RandomPersonId,
                WriteKeyFile = true,
                Culture = ExportFixtures.Norwegian,
            });

        Assert.Equal(PseudonymKeyWriter.KeyFilePathFor(path), result.KeyFilePath);
        Assert.True(File.Exists(result.KeyFilePath));

        string expected = FormattableString.Invariant($"{anonymiser.GetPseudonym(8)}=8\r\n");

        Assert.Equal(expected, File.ReadAllText(result.KeyFilePath!, ExportFixtures.Cp1252));
        Assert.Equal(new[] { DatasetExporter.KeyFileWarning }, notifier.Warnings);
    }

    [Fact]
    public async Task AKeyFileIsPointlessWithoutPseudonymsAndIsNotWritten()
    {
        DatasetExporter exporter = NewExporter(out _);

        string path = PathFor("export.csv");

        DatasetExportResult result = await exporter.ExportAsync(
            ExportFixtures.WorkedExample(),
            path,
            new DatasetExportOptions
            {
                Identification = PersonIdentification.Full,
                WriteKeyFile = true,
                Culture = ExportFixtures.Norwegian,
            });

        Assert.Null(result.KeyFilePath);
        Assert.Empty(Directory.GetFiles(_directory, "*.mapping.txt"));
    }

    [Fact]
    public async Task TheResultReportsTheDimensionsThatWereWritten()
    {
        DatasetExporter exporter = NewExporter(out _);

        DatasetExportResult result = await exporter.ExportAsync(
            ExportFixtures.Cohort(17),
            PathFor("export.csv"),
            new DatasetExportOptions
            {
                Identification = PersonIdentification.Full,
                IncludeTimestamps = true,
                Culture = ExportFixtures.Norwegian,
            });

        Assert.Equal(17, result.RowCount);
        Assert.Equal(6, result.ColumnCount);   // four identity + VAL + VAL.DATE
        Assert.Equal(18, File.ReadAllLines(result.FilePath, ExportFixtures.Cp1252).Length);
    }

    [Fact]
    public async Task TwoExportsOfOneLoadedDatasetAgreeOnEveryPseudonym()
    {
        // The Delphi's central pseudonym defect, asserted at the level a user would notice it.
        DatasetExporter exporter = NewExporter(out MatrixAnonymiser anonymiser);
        anonymiser.Reset(17);

        var options = new DatasetExportOptions
        {
            Identification = PersonIdentification.RandomPersonId,
            Culture = ExportFixtures.Norwegian,
        };

        ExportDataset dataset = ExportFixtures.Cohort(17);

        await exporter.ExportAsync(dataset, PathFor("first.csv"), options);
        await exporter.ExportAsync(dataset, PathFor("second.csv"), options);

        Assert.Equal(
            File.ReadAllBytes(PathFor("first.csv")),
            File.ReadAllBytes(PathFor("second.csv")));
    }

    [Fact]
    public async Task AnExportAfterAResetIsUnlinkableToTheOneBefore()
    {
        DatasetExporter exporter = NewExporter(out MatrixAnonymiser anonymiser);
        anonymiser.Reset(17);

        var options = new DatasetExportOptions
        {
            Identification = PersonIdentification.RandomPersonId,
            Culture = ExportFixtures.Norwegian,
        };

        ExportDataset dataset = ExportFixtures.Cohort(17);

        await exporter.ExportAsync(dataset, PathFor("before.csv"), options);
        anonymiser.Reset(17);
        await exporter.ExportAsync(dataset, PathFor("after.csv"), options);

        Assert.NotEqual(
            File.ReadAllBytes(PathFor("before.csv")),
            File.ReadAllBytes(PathFor("after.csv")));
    }

    [Fact]
    public async Task AnExporterWithNoPseudonymSpaceCreatesOneRatherThanThrowing()
    {
        DatasetExporter exporter = NewExporter(out MatrixAnonymiser anonymiser);

        Assert.False(anonymiser.HasPseudonymSpace);

        await exporter.ExportAsync(
            ExportFixtures.Cohort(17),
            PathFor("export.csv"),
            new DatasetExportOptions
            {
                Identification = PersonIdentification.RandomPersonId,
                Culture = ExportFixtures.Norwegian,
            });

        Assert.True(anonymiser.HasPseudonymSpace);
        Assert.Equal(100, anonymiser.ScaleFactor);
    }

    [Fact]
    public async Task TheXlsxFormatWritesARealWorkbook()
    {
        DatasetExporter exporter = NewExporter(out _);
        string path = PathFor("export.xlsx");

        DatasetExportResult result = await exporter.ExportAsync(
            ExportFixtures.WorkedExample(),
            path,
            new DatasetExportOptions
            {
                Identification = PersonIdentification.Full,
                Format = ExportFormat.Xlsx,
            });

        byte[] bytes = File.ReadAllBytes(result.FilePath);

        // "PK\x03\x04" - an xlsx is a zip package, not a renamed CSV.
        Assert.Equal(new byte[] { 0x50, 0x4B, 0x03, 0x04 }, bytes[..4]);
    }

    [Fact]
    public async Task ADirectoryThatDoesNotExistYetIsCreated()
    {
        DatasetExporter exporter = NewExporter(out _);
        string path = Path.Combine(_directory, "nested", "deeper", "export.csv");

        DatasetExportResult result = await exporter.ExportAsync(
            ExportFixtures.WorkedExample(),
            path,
            new DatasetExportOptions { Identification = PersonIdentification.PersonIdOnly });

        Assert.True(File.Exists(result.FilePath));
    }

    [Fact]
    public async Task ArgumentsAreValidated()
    {
        DatasetExporter exporter = NewExporter(out _);
        var options = new DatasetExportOptions { Identification = PersonIdentification.Full };

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => exporter.ExportAsync((ExportDataset)null!, PathFor("a.csv"), options));

        await Assert.ThrowsAsync<ArgumentException>(
            () => exporter.ExportAsync(ExportFixtures.WorkedExample(), "   ", options));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => exporter.ExportAsync(ExportFixtures.WorkedExample(), PathFor("a.csv"), null!));
    }

    [Fact]
    public void TheContainerResolvesOneSharedPolicyAndOneSharedAnonymiser()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddQuickStatExport()
            .BuildServiceProvider();

        Assert.Same(
            provider.GetRequiredService<IIdentificationPolicy>(),
            provider.GetRequiredService<IIdentificationPolicy>());

        Assert.Same(
            provider.GetRequiredService<IAnonymiser>(),
            provider.GetRequiredService<IAnonymiser>());

        Assert.NotNull(provider.GetRequiredService<IDatasetExporter>());
        Assert.NotNull(provider.GetRequiredService<ITempFileTracker>());

        // A bare container: no logging, no notifier, and it still resolves.
        Assert.IsType<IdentificationPolicy>(provider.GetRequiredService<IIdentificationPolicy>());
        Assert.IsType<MatrixAnonymiser>(provider.GetRequiredService<IAnonymiser>());
    }

    [Fact]
    public async Task TheKeyFileCanBeTrackedForDeletionAlongsideTheExport()
    {
        // Docs/Port/04-matrix-export.md R-2: the Delphi tracked the temporary CSV but never its
        // .mapping.txt sibling, so plaintext keys accumulated in %TEMP% forever.
        DatasetExporter exporter = NewExporter(out MatrixAnonymiser anonymiser);
        anonymiser.Reset(1);

        string path = PathFor("temp.csv");

        DatasetExportResult result = await exporter.ExportAsync(
            ExportFixtures.WorkedExample(),
            path,
            new DatasetExportOptions
            {
                Identification = PersonIdentification.RandomPersonId,
                WriteKeyFile = true,
                Culture = ExportFixtures.Norwegian,
            });

        using (var tracker = new TempFileTracker())
        {
            tracker.Track(result.FilePath);
            tracker.Track(result.KeyFilePath!);

            Assert.Equal(2, tracker.TrackedPaths.Count);
        }

        Assert.False(File.Exists(result.FilePath));
        Assert.False(File.Exists(result.KeyFilePath));
    }

    private sealed class RecordingNotifier : IUserNotifier
    {
        public List<string> Warnings { get; } = [];

        public Task InformAsync(string message, string? title = null) => Task.CompletedTask;

        public Task WarnAsync(string message, string? title = null)
        {
            Warnings.Add(message);
            return Task.CompletedTask;
        }

        public Task ErrorAsync(string message, string? title = null) => Task.CompletedTask;

        public Task<bool> ConfirmAsync(
            string message,
            NotificationSeverity severity = NotificationSeverity.Warning,
            string? title = null) => Task.FromResult(false);
    }
}
