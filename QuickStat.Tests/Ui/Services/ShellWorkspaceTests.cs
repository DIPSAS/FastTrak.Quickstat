using QuickStat.Collectors;
using QuickStat.Domain.DataPoints;
using QuickStat.Domain.Matrix;
using QuickStat.Domain.Patients;
using QuickStat.Domain.Populations;
using QuickStat.Services;
using Xunit;

namespace QuickStat.Tests.Ui.Services;

/// <summary>The cross-tab state, <c>05-ui-spec.md</c> §H.2's "State placement rules".</summary>
public class ShellWorkspaceTests
{
    internal static PersonMatrix NewMatrix() => new(new DataPointFactory(), new CaptionDictionary());

    internal static Population NewPopulation(int procId = 1, string title = "Aktive pasienter") => new()
    {
        ProcId = procId,
        Title = title,
        QueryText = "EXEC dbo.GetCaseList :StudyId",
    };

    internal static Patient NewPatient(int personId) => new()
    {
        PersonId = personId,
        LastName = "Hansen",
        FirstName = "Ola",
        DateOfBirth = new DateTime(1955, 4, 2, 0, 0, 0, DateTimeKind.Unspecified),
        GenderId = 1,
        Sex = Sex.Male,
    };

    internal static void AddColumn(PersonMatrix matrix, string varName, int personId, double value)
    {
        VariableNameSet names = matrix.CreateVariableNameSet();

        names.Add(varName);
        matrix.AddColumns(names);

        matrix.Add(varName, new CollectorResultRow
        {
            PersonId = personId,
            VarName = varName,
            Value = value,
            Timestamp = new DateTime(2019, 8, 14, 0, 0, 0, DateTimeKind.Unspecified),
            RowId = 1,
        });
    }

    [Fact]
    public void StartsWithNoPopulationAndNoData()
    {
        ShellWorkspace workspace = new(NewMatrix());

        Assert.Null(workspace.Population);
        Assert.False(workspace.HasPopulation);
        Assert.False(workspace.HasData);
        Assert.Empty(workspace.CheckedCollectorNames);
        Assert.False(workspace.ExportTimestamps);
    }

    [Fact]
    public void APopulationWithNoPatientsDoesNotCount()
    {
        // The Delphi condition is DataRows > 0, not "a population was selected": an empty cohort
        // leaves the Collections tab hidden.
        PersonMatrix matrix = NewMatrix();
        ShellWorkspace workspace = new(matrix);

        matrix.PreparePopulation([]);
        workspace.SetPopulation(NewPopulation());

        Assert.NotNull(workspace.Population);
        Assert.False(workspace.HasPopulation);
    }

    [Fact]
    public void APopulationWithPatientsCounts()
    {
        PersonMatrix matrix = NewMatrix();
        ShellWorkspace workspace = new(matrix);

        matrix.PreparePopulation([NewPatient(8), NewPatient(13)]);
        workspace.SetPopulation(NewPopulation());

        Assert.True(workspace.HasPopulation);
        Assert.Equal(2, workspace.RowCount);
    }

    [Fact]
    public void SetPopulationRaisesTheEventAndTheFlags()
    {
        PersonMatrix matrix = NewMatrix();
        ShellWorkspace workspace = new(matrix);
        List<string?> properties = [];
        int events = 0;

        workspace.PropertyChanged += (_, e) => properties.Add(e.PropertyName);
        workspace.PopulationChanged += (_, _) => events++;

        matrix.PreparePopulation([NewPatient(1)]);
        workspace.SetPopulation(NewPopulation());

        Assert.Equal(1, events);
        Assert.Contains(nameof(IShellWorkspace.Population), properties);
        Assert.Contains(nameof(IShellWorkspace.HasPopulation), properties);
        Assert.Contains(nameof(IShellWorkspace.RowCount), properties);
    }

    [Fact]
    public void HasDataFollowsTheColumnsAndNotTheRows()
    {
        // PersonMatrix.HasData counts columns: a cohort with no collected variables is "no data".
        PersonMatrix matrix = NewMatrix();
        ShellWorkspace workspace = new(matrix);

        matrix.PreparePopulation([NewPatient(8)]);
        workspace.SetPopulation(NewPopulation());

        Assert.False(workspace.HasData);

        AddColumn(matrix, "AGE", 8, 64);
        workspace.NotifyDataChanged();

        Assert.True(workspace.HasData);
        Assert.Equal(1, workspace.ColumnCount);
    }

    [Fact]
    public void NotifyDataChangedRaisesItsEvent()
    {
        ShellWorkspace workspace = new(NewMatrix());
        int events = 0;

        workspace.DataChanged += (_, _) => events++;

        workspace.NotifyDataChanged();

        Assert.Equal(1, events);
    }

    [Fact]
    public void CheckedCollectorNamesAreSnapshotted()
    {
        // A projection of step 3.3's list, not an alias into it: mutating the source afterwards must
        // not silently change what the Dataset tab sees.
        ShellWorkspace workspace = new(NewMatrix());
        List<string> source = ["QS_AGE", "QS_SEX"];

        workspace.SetCheckedCollectorNames(source);

        source.Add("QS_YOB");

        Assert.Equal(["QS_AGE", "QS_SEX"], workspace.CheckedCollectorNames);
    }

    [Fact]
    public void ExportTimestampsRaisesOnlyOnChange()
    {
        ShellWorkspace workspace = new(NewMatrix());
        int raised = 0;

        workspace.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IShellWorkspace.ExportTimestamps))
            {
                raised++;
            }
        };

        workspace.ExportTimestamps = true;
        workspace.ExportTimestamps = true;

        Assert.Equal(1, raised);
    }

    [Fact]
    public void RequestCollectionsTabIsSeparateFromSettingThePopulation()
    {
        // AfterPopulationSelect switches tabs; the package replay calls LoadPopulationIntoGrid
        // directly and leaves the user on the Packages tab.  Folding the two together would move
        // focus during a replay.
        PersonMatrix matrix = NewMatrix();
        ShellWorkspace workspace = new(matrix);
        int requests = 0;

        workspace.CollectionsTabRequested += (_, _) => requests++;

        matrix.PreparePopulation([NewPatient(1)]);
        workspace.SetPopulation(NewPopulation());

        Assert.Equal(0, requests);

        workspace.RequestCollectionsTab();

        Assert.Equal(1, requests);
    }

    [Fact]
    public void ClearingThePopulationClearsTheFlag()
    {
        PersonMatrix matrix = NewMatrix();
        ShellWorkspace workspace = new(matrix);

        matrix.PreparePopulation([NewPatient(1)]);
        workspace.SetPopulation(NewPopulation());
        Assert.True(workspace.HasPopulation);

        workspace.SetPopulation(null);

        Assert.False(workspace.HasPopulation);
    }
}
