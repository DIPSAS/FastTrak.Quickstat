using System.IO;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Configuration;
using QuickStat.Data;
using QuickStat.Diagnostics;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.Matrix;
using QuickStat.Domain.Patients;
using QuickStat.Domain.Populations;
using QuickStat.Export;
using QuickStat.Services;
using QuickStat.Tests.Ui.Services;
using QuickStat.Tests.Ui.Shell;
using QuickStat.ViewModels;
using Xunit;

namespace QuickStat.Tests.Live;

/// <summary>
/// Acceptance criterion 5, end to end: a real cohort, a real recovery query, a real file with
/// national identity numbers in it.
/// </summary>
/// <remarks>
/// <para>
/// <b>What this adds over what already existed.</b> PORT-PLAN.md §8.11 (3) counted 280 of 281
/// national ids on a real cohort but exported only the two PID-only variants; §8.14 exported the
/// fully identified variant and matched the shipped build cell for cell, but on a 31-patient cohort
/// without recording where the ids had come from. The two halves never met. These tests are the
/// join, and they are two rather than one because writing them showed the halves are further apart
/// than the plan said - see <see cref="TheRecoveryQueryFillsInIdsAPopulationDidNotReturn"/>.
/// </para>
/// <para>
/// <b>The provenance correction.</b> §8.11 (3) reads "280 of 281 <em>recovered</em>", which is not
/// what happened: its population is <c>dbo.GetCaseListTest</c>, whose result set already contains
/// <c>NationalId</c>, so <c>NationalIdRecovery.IncludesNationalId</c> answered true and the recovery
/// query never ran. The ids were selected by the population procedure. Found by negative control -
/// suppressing the assignment in <c>EnsureNationalIdsAsync</c> left this test passing, which is the
/// definition of an assertion that proves nothing - and confirmed against the catalogue with
/// <c>sp_describe_first_result_set</c>. What §8.11 (1) really established is that the recovery
/// <em>statement</em> works (342 ids for the first 500 patients); the path through
/// <c>PopulationLoader</c> is what had never run, and now does, in its own test below.
/// </para>
/// <para>
/// <b>Composed as the product composes it</b>, through <c>ShellCompositionTests.Build</c>, with only
/// the three WPF seams replaced - there is no window here, and a save dialog or a message box would
/// hang the run. The view-models are the shipped ones: <see cref="PopulationPickerViewModel"/> loads
/// the cohort, <see cref="CollectionsTabViewModel"/> ticks elements and sets the mode,
/// <see cref="DatasetViewModel"/> writes the file. The radio button that drives that mode in the real
/// window is covered separately and without a database by
/// <c>Ui/Collections/IdentificationRadioTests</c>; the two meet at
/// <see cref="CollectionsTabViewModel.Identification"/>.
/// </para>
/// <para>
/// <b>Privacy.</b> The file this writes contains real national identity numbers. Nothing here prints
/// a field, no assertion message carries one, and the file is deleted in a <c>finally</c> whether the
/// test passed or threw. See <see cref="LiveDatabase"/> for the rules and PORT-PLAN.md R6 for why
/// they are not negotiable.
/// </para>
/// </remarks>
public class FullyIdentifiedExportTests
{
    private const string NationalIdColumn = "Fødselsnummer";
    private const string DateOfBirthColumn = "Født";
    private const string NameColumn = "Navn";

    /// <summary>
    /// The recovery query, on a real cohort, through the real repository - the path
    /// <c>PopulationLoader</c> takes when a population does not select the column.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The cohort is real and the blanking is not.</b> No population on
    /// <c>EFT00028_TEST_020</c> can reach this path: every procedure that omits <c>NationalId</c>
    /// returns an empty cohort here, because ProcId 23 emptied study 2 on 2026-09-01, and the one
    /// population that still has patients selects the column itself. So the test takes that cohort
    /// and clears the ids, which is exactly the state a column-less population would hand
    /// <see cref="PopulationLoader"/>. Everything after that point is production code against a
    /// production catalogue: the real <c>IPatientRepository</c>, the real statement, the real
    /// chunking.
    /// </para>
    /// <para>
    /// This is the assertion §8.11 (1) would have failed. The default
    /// <c>SqlOptions.PersonIdListTypeName</c> named a table type that has never existed, every call
    /// came back <c>Msg 2715</c>, and <see cref="NationalIdRecovery"/> caught it and returned zero -
    /// a blank column, no exception, no failing test anywhere in the suite.
    /// </para>
    /// </remarks>
    [LiveDatabaseFact]
    public async Task TheRecoveryQueryFillsInIdsAPopulationDidNotReturn()
    {
        using ServiceProvider provider = ShellCompositionTests.Build(services =>
        {
            services.AddSingleton<IUiDispatcher, InlineUiDispatcher>();
            services.AddSingleton<IUserNotificationPresenter, HeadlessNotificationPresenter>();
        });

        IConnectionCoordinator coordinator = provider.GetRequiredService<IConnectionCoordinator>();

        SessionContext session = await coordinator.ConnectAsync(new QuickStatConnection
        {
            Name = "live-test",
            StudyName = LiveDatabase.StudyName,
            ConnectionString = LiveDatabase.ConnectionString!,
        });

        try
        {
            IPatientRepository patients = provider.GetRequiredService<IPatientRepository>();
            Population population = await FindPopulationAsync(provider, session);

            IReadOnlyList<Patient> cohort = await patients.LoadPopulationAsync(
                population,
                (await provider.GetRequiredService<IQueryParameterResolver>()
                    .ResolveAsync(population.QueryText)).Values);

            Assert.True(cohort.Count > 0, "The population returned no patients.");

            foreach (Patient patient in cohort)
            {
                patient.NationalId = null;
            }

            Assert.False(
                NationalIdRecovery.IncludesNationalId(cohort),
                "The cohort still looks complete after blanking, so the guard would skip the query.");

            int recovered = await NationalIdRecovery.EnsureNationalIdsAsync(
                patients,
                cohort,
                NullLogger.Instance);

            int carrying = cohort.Count(patient => !string.IsNullOrEmpty(patient.NationalId));

            Assert.True(
                recovered > 0,
                $"The recovery query returned nothing for {cohort.Count} patients. It degrades rather "
                + "than throwing, so this assertion is the only thing standing between a broken "
                + "recovery and a silently blank Fødselsnummer column - PORT-PLAN.md §8.11 (1).");

            // Every id it reports is an id it actually assigned; the count is the contract.
            Assert.Equal(recovered, carrying);
        }
        finally
        {
            await coordinator.DisconnectAsync();
        }
    }

    [LiveDatabaseFact]
    public async Task AFullyIdentifiedExportCarriesTheNationalIds()
    {
        FakeFileDialogService dialog = new();

        using ServiceProvider provider = ShellCompositionTests.Build(services =>
        {
            // The three WPF seams. Last registration wins, so these replace what AddQuickStatShell
            // installed - an inline dispatcher instead of one that needs Application.Current, a save
            // dialog that answers with a path instead of one that shows, and the headless notifier
            // AddQuickStatDiagnostics would have used had the shell not overridden it. A message box
            // in a test run is a hang, not a failure.
            services.AddSingleton<IUiDispatcher, InlineUiDispatcher>();
            services.AddSingleton<IFileDialogService>(dialog);
            services.AddSingleton<IUserNotificationPresenter, HeadlessNotificationPresenter>();
        });

        // Before connecting, as the shell does: both view-models subscribe in their constructors -
        // CollectionsTabViewModel to ICollectorRegistry.Rebuilt, which is what fills the
        // data-element list, and DatasetViewModel to the workspace. Resolved after the login and
        // they would each have missed the one event that matters.
        CollectionsTabViewModel collections = provider.GetRequiredService<CollectionsTabViewModel>();
        DatasetViewModel dataset = provider.GetRequiredService<DatasetViewModel>();

        IConnectionCoordinator coordinator = provider.GetRequiredService<IConnectionCoordinator>();

        SessionContext session = await coordinator.ConnectAsync(new QuickStatConnection
        {
            Name = "live-test",
            StudyName = LiveDatabase.StudyName,
            ConnectionString = LiveDatabase.ConnectionString!,
        });

        try
        {
            await RunAsync(provider, session, collections, dataset, dialog);
        }
        finally
        {
            await coordinator.DisconnectAsync();
        }
    }

    private static async Task RunAsync(
        ServiceProvider provider,
        SessionContext session,
        CollectionsTabViewModel collections,
        DatasetViewModel dataset,
        FakeFileDialogService dialog)
    {
        Population population = await FindPopulationAsync(provider, session);

        // PopulationLoader rather than PopulationPickerViewModel, which is not in the span this test
        // covers and cannot run outside a dispatcher: its constructor takes an ICollectionView over
        // Populations, and a CollectionView refuses a mutation from any thread but the one it was
        // made on. The loader is what the picker calls, and it is where the recovery lives.
        PopulationLoadResult result = await provider
            .GetRequiredService<PopulationLoader>()
            .LoadAsync(population, NullLogger.Instance);

        Assert.True(
            result.Loaded,
            $"Population ProcId {population.ProcId} did not load: a placeholder in its query text "
            + "could not be resolved. PORT-PLAN.md §9 R2a lists the names no session can supply.");

        IShellWorkspace workspace = provider.GetRequiredService<IShellWorkspace>();
        PersonMatrix matrix = workspace.Matrix;

        Assert.True(matrix.Rows.Count > 0, "The population loaded no patients, so nothing below means anything.");

        // The measurement the criterion is actually about, taken before any export exists: the
        // recovery ran during LoadAsync and put ids on the rows. A count, never a value.
        int rowsWithNationalId = matrix.Rows.Count(row => !string.IsNullOrEmpty(row.NationalId));

        Assert.True(
            rowsWithNationalId > 0,
            $"No national id reached the matrix for any of {matrix.Rows.Count} patients. "
            + "Either this population selects the column itself (choose one that does not), or the "
            + "recovery query failed and degraded - PORT-PLAN.md §8.11 (1) is the last time that "
            + "happened silently.");

        await CollectAsync(collections);

        Assert.True(
            matrix.HasData,
            "No collector produced a column, and DatasetViewModel.CanExport requires one, so the "
            + "export below would have been skipped rather than written.");

        SetFullyIdentified(provider, collections);

        string path = Path.Combine(
            Path.GetTempPath(),
            $"quickstat-live-{Guid.NewGuid():N}.csv");

        dialog.Answer = path;

        try
        {
            await dataset.SaveDatasetToCsvCommand.ExecuteAsync(null);

            Assert.True(File.Exists(path), "The export command did not write a file.");

            AssertNationalIdsAreInTheFile(path, rowsWithNationalId, matrix.Rows.Count);
        }
        finally
        {
            // R6: this file holds real national identity numbers. It goes whether or not the
            // assertions above threw.
            File.Delete(path);
        }
    }

    /// <summary>Finds the configured population in this database's catalogue.</summary>
    /// <param name="provider">The composed container.</param>
    /// <param name="session">The session the connection produced.</param>
    /// <returns>The population to load.</returns>
    private static async Task<Population> FindPopulationAsync(ServiceProvider provider, SessionContext session)
    {
        IReadOnlyList<Population> catalogue = await provider
            .GetRequiredService<IPopulationRepository>()
            .GetPopulationsAsync(session.StudyId, session.Database.DbVersion, frequentlyUsedOnly: false);

        Population? found = catalogue.FirstOrDefault(candidate => candidate.ProcId == LiveDatabase.ProcId);

        Assert.True(
            found is not null,
            $"Population ProcId {LiveDatabase.ProcId} is not in this database's catalogue for study "
            + $"{session.StudyId}. Set {LiveDatabase.PopulationVariable} to one that is.");

        return found;
    }

    /// <summary>Ticks the cheapest elements that reliably return columns and runs the collect.</summary>
    /// <remarks>
    /// <c>CanExport</c> is <c>HasData &amp;&amp; IsLocked</c>, and only a collect run sets either, so
    /// something has to be collected before a file can be written. <c>PATIENT.*</c> is chosen because
    /// §8.11 (3) measured it returning rows on this database while every <c>DRUG.*</c> and
    /// <c>LAB.*</c> collector correctly returned none; the identity columns under test are not
    /// collected data and do not depend on which elements these are.
    /// </remarks>
    private static async Task CollectAsync(CollectionsTabViewModel collections)
    {
        Assert.True(
            collections.DataElements.Count > 0,
            "The data-element list is empty, so the registry was never rebuilt for this session.");

        List<DataElementViewModel> patientElements =
        [
            .. collections.DataElements.Where(static element =>
                element.Name.StartsWith("PATIENT.", StringComparison.OrdinalIgnoreCase)),
        ];

        Assert.True(patientElements.Count > 0, "This session's registry has no PATIENT.* collector.");

        foreach (DataElementViewModel element in patientElements)
        {
            element.IsChecked = true;
        }

        await collections.CollectDataCommand.ExecuteAsync(null);
    }

    /// <summary>Sets the mode the way the radio button's binding sets it.</summary>
    /// <remarks>
    /// The property, not the control: driving the real <c>RadioButton</c> needs an STA thread and an
    /// <c>Application</c>, and that link is covered on its own in
    /// <c>Ui/Collections/IdentificationRadioTests</c> where it costs no database. Asserting the
    /// shared policy afterwards is what keeps the two tests joined - if this pass-through ever grew a
    /// backing field, this line would still pass and that assertion would not.
    /// </remarks>
    private static void SetFullyIdentified(ServiceProvider provider, CollectionsTabViewModel collections)
    {
        collections.Identification = PersonIdentification.Full;

        IIdentificationPolicy policy = provider.GetRequiredService<IIdentificationPolicy>();

        Assert.Equal(PersonIdentification.Full, policy.Mode);
        Assert.True(policy.Columns.IncludesNationalId);
    }

    /// <summary>Counts what reached the file, and checks the shape of it without reading a value.</summary>
    /// <param name="path">The exported CSV.</param>
    /// <param name="expected">Rows that carried a national id in the matrix.</param>
    /// <param name="rowCount">Patients in the cohort.</param>
    private static void AssertNationalIdsAreInTheFile(string path, int expected, int rowCount)
    {
        string[] lines = File.ReadAllLines(path, CsvMatrixWriter.LegacyEncoding);

        Assert.True(lines.Length == rowCount + 1, $"Expected {rowCount} rows and a header, got {lines.Length} lines.");

        string[] header = SplitFields(lines[0]);

        // The whole identity block, because PersonIdentification.Full is a set of four columns and a
        // regression that dropped one of the other three would otherwise pass here.
        Assert.Contains(DateOfBirthColumn, header, StringComparer.Ordinal);
        Assert.Contains(NameColumn, header, StringComparer.Ordinal);

        int column = Array.IndexOf(header, NationalIdColumn);

        Assert.True(column >= 0, $"No \"{NationalIdColumn}\" column in the exported header.");

        int populated = 0;
        int malformed = 0;

        foreach (string line in lines.Skip(1))
        {
            string[] fields = SplitFields(line);
            string value = column < fields.Length ? fields[column] : "";

            if (value.Length == 0)
            {
                continue;
            }

            populated++;

            // Shape only. A Norwegian national id is 11 digits; anything else here would mean the
            // column had been filled from the wrong field. The value itself is never captured.
            if (value.Length != 11 || !value.All(char.IsAsciiDigit))
            {
                malformed++;
            }
        }

        Assert.True(
            malformed == 0,
            $"{malformed} of {populated} exported national ids are not 11 digits.");

        // The join this test exists for: every id the recovery put on a row is in the file, and the
        // file invented none. Counts in the message, never a field - a failing assertion is the one
        // place a careless test leaks the data it was checking.
        Assert.True(
            populated == expected,
            $"{populated} national ids in the file against {expected} on the matrix rows, "
            + $"for {rowCount} patients.");
    }

    /// <summary>
    /// Splits one exported line into unquoted fields.
    /// </summary>
    /// <param name="line">A line of the CSV.</param>
    /// <returns>The fields, quotes removed and doubled quotes collapsed.</returns>
    /// <remarks>
    /// The writer quotes every field and separates with <c>;</c>, and every line carries a trailing
    /// separator, so the last element is always empty - see PORT-PLAN.md §8.14. Written out rather
    /// than reusing the export code, because a reader built from the writer would agree with it by
    /// construction.
    /// </remarks>
    private static string[] SplitFields(string line)
    {
        List<string> fields = [];
        StringBuilder field = new();
        bool quoted = false;

        for (int index = 0; index < line.Length; index++)
        {
            char character = line[index];

            if (quoted)
            {
                if (character != '"')
                {
                    field.Append(character);
                }
                else if (index + 1 < line.Length && line[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else
                {
                    quoted = false;
                }
            }
            else if (character == '"')
            {
                quoted = true;
            }
            else if (character == ';')
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(character);
            }
        }

        fields.Add(field.ToString());

        return [.. fields];
    }
}
