using System.Globalization;
using QuickStat.Collectors;
using QuickStat.Domain.DataPoints;
using QuickStat.Domain.Matrix;
using QuickStat.Domain.Patients;
using Xunit;

namespace QuickStat.Tests.Domain.Matrix;

/// <summary>
/// The dataset itself. The structural point of the port is that this owns its rows and columns
/// instead of pushing cell objects into a grid control
/// (<c>Docs/Port/04-matrix-export.md</c> R-11), so all of it is testable with no UI at all.
/// </summary>
public class PersonMatrixTests
{
    private static readonly DateTime Timestamp = new(2019, 8, 14, 0, 0, 0, DateTimeKind.Unspecified);

    private static PersonMatrix NewMatrix(ITitleDictionary? titles = null) =>
        new(new DataPointFactory(), titles ?? new CaptionDictionary());

    private static Patient NewPatient(int personId, string last = "Hansen", string first = "Ola") =>
        new()
        {
            PersonId = personId,
            LastName = last,
            FirstName = first,
            DateOfBirth = new DateTime(1922, 3, 12, 0, 0, 0, DateTimeKind.Unspecified),
            NationalId = "12032212345",
            GenderId = 1,
            Sex = Sex.Male,
        };

    private static VariableNameSet NamesOf(params string[] names)
    {
        VariableNameSet set = new();

        foreach (string name in names)
        {
            set.Add(name);
        }

        return set;
    }

    private static CollectorResultRow Row(int personId, string varName, double value, int rowId = 1) =>
        new()
        {
            PersonId = personId,
            VarName = varName,
            Value = value,
            Timestamp = Timestamp,
            RowId = rowId,
        };

    [Fact]
    public void PreparePopulationSortsByPersonIdAndDropsDuplicates()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(30), NewPatient(10), NewPatient(20), NewPatient(10, "Duplicate")]);

        Assert.Equal([10, 20, 30], matrix.Rows.Select(row => row.PersonId));
        Assert.Equal("Hansen, Ola", matrix.Rows[0].FullName);
    }

    [Fact]
    public void ReverseNameSortsOrdinallyOnLastCommaFirst()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.SortBy = MatrixSortOrder.ReverseName;
        matrix.PreparePopulation([NewPatient(1, "Ås"), NewPatient(2, "Berg"), NewPatient(3, "Aas")]);

        // CompareStr is ordinal, so "Ås" sorts after "Berg" - a culture-aware sort would not.
        Assert.Equal(["Aas, Ola", "Berg, Ola", "Ås, Ola"], matrix.Rows.Select(row => row.FullName));
    }

    [Fact]
    public void PersonIdSortingDoesNotOverflow()
    {
        // The Delphi comparer returns Left.PersonId - Right.PersonId, which wraps for ids far
        // enough apart (EPR.QA.Matrix.Row.pas:239-242).
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(int.MaxValue), NewPatient(int.MinValue + 1), NewPatient(0)]);

        Assert.Equal([int.MinValue + 1, 0, int.MaxValue], matrix.Rows.Select(row => row.PersonId));
    }

    [Fact]
    public void ColumnsFollowTheOrderTheVariablesArrivedIn()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(1)]);
        matrix.AddColumns(NamesOf("PATIENT.AGE", "PATIENT.YOB", "PATIENT.SEX"));
        matrix.AddColumns(NamesOf("WEIGHT", "HEIGHT"));

        Assert.Equal(
            ["PATIENT.AGE", "PATIENT.YOB", "PATIENT.SEX", "WEIGHT", "HEIGHT"],
            matrix.Columns.Select(column => column.VarName));
    }

    [Fact]
    public void ColumnsAreDeDuplicatedAcrossCollectors()
    {
        // The Delphi produced two identical columns here: TPersonGridColumnList.ContainsVariable
        // existed and was never called (EPR.QA.Matrix.Column.pas:83).
        PersonMatrix matrix = NewMatrix();

        matrix.AddColumns(NamesOf("AGE", "YOB"));
        matrix.AddColumns(NamesOf("YOB", "SEX"));

        Assert.Equal(["AGE", "YOB", "SEX"], matrix.Columns.Select(column => column.VarName));
    }

    [Fact]
    public void ATitleFallsBackToTheVariableNameButTheCsvHeaderIsAlwaysTheName()
    {
        CaptionDictionary captions = new();

        captions.AddCaption(new CaptionRecord { VarName = "NPU01566", Title = "P-Kolesterol", Description = "Total" });

        PersonMatrix matrix = NewMatrix(captions);

        matrix.AddColumns(NamesOf("NPU01566", "NDV_INSULIN"));

        Assert.Equal("P-Kolesterol", matrix.Columns[0].Title);
        Assert.Equal("Total", matrix.Columns[0].Description);
        Assert.Equal("NPU01566", matrix.Columns[0].VarName);

        Assert.Equal("NDV_INSULIN", matrix.Columns[1].Title);
        Assert.Equal("", matrix.Columns[1].Description);
    }

    [Fact]
    public void HasDataCountsColumnsNotRows()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(1), NewPatient(2)]);

        Assert.False(matrix.HasData);

        matrix.AddColumns(NamesOf("AGE"));

        Assert.True(matrix.HasData);
    }

    [Fact]
    public void ClearVariablesDropsTheColumnsAndEveryDatapointButKeepsTheRows()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(1)]);
        matrix.AddColumns(NamesOf("AGE"));

        Assert.True(matrix.Add("AGE", Row(1, "AGE", 97)));
        Assert.Single(matrix.Rows[0].DataPoints);

        matrix.ClearVariables();

        Assert.Empty(matrix.Columns);
        Assert.Single(matrix.Rows);
        Assert.Empty(matrix.Rows[0].DataPoints);
    }

    [Fact]
    public void ClearVariablesUnlocksSoTheSameCohortCanBeCollectedAgain()
    {
        // The second click of Collect data.  AddColumns and Add both refuse a locked matrix and Lock
        // is the last thing a run does, so without this the only way back was ClearPopulation - which
        // throws the cohort away and would have meant reloading the population to change which data
        // elements are ticked.  The Delphi has no such restriction: fLocked appears three times in
        // EPR.QA.Matrix.pas - cleared at :214, set at :332, read at :236 for the "(not ready)"
        // placeholder - and never gates adding data.
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(1)]);
        matrix.AddColumns(NamesOf("AGE"));
        Assert.True(matrix.Add("AGE", Row(1, "AGE", 97)));
        matrix.Lock();

        matrix.ClearVariables();

        Assert.False(matrix.IsLocked);

        // And the run that follows really can proceed - both guarded operations, not just the flag.
        matrix.AddColumns(NamesOf("HEIGHT"));
        Assert.True(matrix.Add("HEIGHT", Row(1, "HEIGHT", 182)));

        matrix.Lock();

        Assert.True(matrix.IsLocked);
        Assert.Single(matrix.Rows);
        Assert.Equal(["HEIGHT"], matrix.Columns.Select(column => column.VarName));
    }

    [Fact]
    public void ClearPopulationDropsTheRowsAndUnlocks()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(1)]);
        matrix.AddColumns(NamesOf("AGE"));
        matrix.Lock();

        matrix.ClearPopulation();

        Assert.Empty(matrix.Rows);
        Assert.False(matrix.IsLocked);
        Assert.Single(matrix.Columns);
    }

    [Fact]
    public void PreparePopulationClearsTheColumnsToo()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.AddColumns(NamesOf("STALE"));
        matrix.PreparePopulation([NewPatient(1)]);

        Assert.Empty(matrix.Columns);
    }

    [Fact]
    public void SortOrderCannotChangeAfterLocking()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(1)]);
        matrix.Lock();

        Assert.Throws<InvalidOperationException>(() => matrix.SortBy = MatrixSortOrder.ReverseName);
    }

    [Fact]
    public void NeitherColumnsNorRowsCanBeAddedAfterLocking()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(1)]);
        matrix.AddColumns(NamesOf("AGE"));
        matrix.Lock();

        Assert.Throws<InvalidOperationException>(() => matrix.AddColumns(NamesOf("YOB")));
        Assert.Throws<InvalidOperationException>(() => matrix.Add("AGE", Row(1, "AGE", 97)));
    }

    [Fact]
    public void CreateVariableNameSetCarriesTheMatrixPolicy()
    {
        PersonMatrix matrix = NewMatrix();

        Assert.Equal(ColumnOrder.FirstSeen, matrix.ColumnOrder);
        Assert.Equal(ColumnOrder.FirstSeen, matrix.CreateVariableNameSet().Order);

        matrix.ColumnOrder = ColumnOrder.Alphabetical;

        Assert.Equal(ColumnOrder.Alphabetical, matrix.CreateVariableNameSet().Order);
    }

    [Fact]
    public void RowsForPeopleOutsideTheCohortAreRejected()
    {
        // Expected in bulk: the PidBinding.None collectors scan the whole database.
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(1)]);
        matrix.AddColumns(NamesOf("AGE"));

        Assert.True(matrix.Add("AGE", Row(1, "AGE", 97)));
        Assert.False(matrix.Add("AGE", Row(999, "AGE", 55)));
        Assert.Single(matrix.Rows[0].DataPoints);
    }

    [Fact]
    public void ASecondRowForTheSameCellUpdatesTheFirstAndReportsNotStored()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(1)]);
        matrix.AddColumns(NamesOf("AGE"));

        Assert.True(matrix.Add("AGE", Row(1, "AGE", 97, rowId: 10)));
        Assert.False(matrix.Add("AGE", Row(1, "AGE", 98, rowId: 11)));

        Assert.True(matrix.TryGetDataPoint(0, 0, out DataPoint? dataPoint));
        Assert.Equal(98, dataPoint.Value);
        Assert.Equal(11, dataPoint.RowId);
        Assert.Equal(2, dataPoint.UpdateCount);
    }

    [Fact]
    public void TheSinkCarriesItemIdAndCaptionAcross()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(1)]);
        matrix.AddColumns(NamesOf("FORM.Q1"));

        matrix.Add("FORM.Q1", new CollectorResultRow
        {
            PersonId = 1,
            VarName = "Q1",
            Value = 1,
            Timestamp = Timestamp,
            RowId = 7,
            ItemId = 5917,
            Caption = "Metformin",
        });

        Assert.True(matrix.TryGetDataPoint(0, 0, out DataPoint? dataPoint));
        Assert.Equal("FORM.Q1", dataPoint.VarName);
        Assert.Equal(5917, dataPoint.ItemId);
        Assert.Equal("Metformin", dataPoint.Caption);
    }

    [Fact]
    public void TryGetDataPointReturnsFalseRatherThanThrowingOutsideTheMatrix()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(1)]);
        matrix.AddColumns(NamesOf("AGE"));

        Assert.False(matrix.TryGetDataPoint(-1, 0, out _));
        Assert.False(matrix.TryGetDataPoint(0, -1, out _));
        Assert.False(matrix.TryGetDataPoint(5, 0, out _));
        Assert.False(matrix.TryGetDataPoint(0, 5, out _));
        Assert.False(matrix.TryGetDataPoint(0, 0, out _));
    }

    [Fact]
    public void AnEmptyCellIsGreyAndReportsThatItHasNoValue()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(1)]);
        matrix.AddColumns(NamesOf("AGE"));

        MatrixCell cell = matrix.GetCell(0, 0);

        Assert.False(cell.HasValue);
        Assert.Equal("", cell.Text);
        Assert.Equal("#F5F5F5", cell.Background!.Value.ToHex());
        Assert.Null(cell.Foreground);
        Assert.False(cell.AlignLeft);
    }

    [Fact]
    public void AValueWithNoRuleIsAWhiteRightAlignedNumber()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(1)]);
        matrix.AddColumns(NamesOf("PATIENT.AGE"));
        matrix.Add("PATIENT.AGE", Row(1, "AGE", 97));

        MatrixCell cell = matrix.GetCell(0, 0);

        Assert.True(cell.HasValue);
        Assert.Equal("97", cell.Text);
        Assert.Equal("#FFFFFF", cell.Background!.Value.ToHex());
        Assert.False(cell.AlignLeft);
    }

    [Fact]
    public void ARegisteredLadderColoursTheCell()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(1)]);
        matrix.AddColumns(NamesOf("NPU01566"));
        matrix.Add("NPU01566", Row(1, "NPU01566", 8.5));

        MatrixCell cell = matrix.GetCell(0, 0);

        Assert.Equal("#FF8080", cell.Background!.Value.ToHex());
    }

    [Fact]
    public void ARuleWithAFormatterOverridesTheNumber()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(1)]);
        matrix.AddColumns(NamesOf("BMI"));
        matrix.Add("BMI", Row(1, "BMI", 31));

        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            MatrixCell cell = matrix.GetCell(0, 0);

            Assert.Equal("31.0", cell.Text);
            Assert.Equal("#FFEDBF", cell.Background!.Value.ToHex());
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void ACaptionIsShownTruncatedToSixCharactersAndAlignsLeft()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(1)]);
        matrix.AddColumns(NamesOf("FORM.Q1"));

        matrix.Add("FORM.Q1", new CollectorResultRow
        {
            PersonId = 1,
            VarName = "Q1",
            Value = 3,
            Timestamp = Timestamp,
            RowId = 1,
            Caption = "Betydelig bedre",
        });

        MatrixCell cell = matrix.GetCell(0, 0);

        Assert.Equal("Betyde", cell.Text);
        Assert.True(cell.AlignLeft);
        Assert.True(cell.HasValue);
    }

    [Fact]
    public void PulseQualityIsLeftAlignedEvenWithoutACaptionOfItsOwn()
    {
        // TPulseQualityDatapoint.CellText assigns Caption := Result, and the grid reads AlignLeft
        // immediately afterwards, so the cell is left-aligned from the first paint.
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(1)]);
        matrix.AddColumns(NamesOf("PULSE_QUALITY"));
        matrix.Add("PULSE_QUALITY", Row(1, "PULSE_QUALITY", 2));

        MatrixCell cell = matrix.GetCell(0, 0);

        Assert.Equal("AF", cell.Text);
        Assert.True(cell.AlignLeft);
        Assert.Equal("#FFFFBF", cell.Background!.Value.ToHex());

        // ...and rendering must not have mutated the datapoint, or the export would change.
        Assert.True(matrix.TryGetDataPoint(0, 0, out DataPoint? dataPoint));
        Assert.Null(dataPoint.Caption);
    }

    [Fact]
    public void AFormatterWinsOverACaptionUnlessTheRuleSaysOtherwise()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(1)]);
        matrix.AddColumns(NamesOf("DbVersion"));

        matrix.Add("DbVersion", new CollectorResultRow
        {
            PersonId = 1,
            VarName = "DbVersion",
            Value = 7,
            Timestamp = Timestamp,
            RowId = 1,
            Caption = "ignored",
        });

        Assert.Equal("2016", matrix.GetCell(0, 0).Text);
    }

    [Theory]
    [InlineData(0, "8")]
    [InlineData(1, "12.03.1922")]
    [InlineData(2, "12032212345")]
    [InlineData(3, "Hansen, Ola")]
    public void TheFixedCellsRenderTheIdentityFields(int ordinal, string expected)
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(8)]);

        Assert.Equal(expected, matrix.GetFixedCell(0, ordinal, CultureInfo.GetCultureInfo("nb-NO")).Text);
    }

    [Fact]
    public void OnlyThePersonIdCellIsRightAligned()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(8)]);

        Assert.False(matrix.GetFixedCell(0, FixedColumns.PersonId).AlignLeft);
        Assert.True(matrix.GetFixedCell(0, FixedColumns.DateOfBirth).AlignLeft);
        Assert.True(matrix.GetFixedCell(0, FixedColumns.NationalId).AlignLeft);
        Assert.True(matrix.GetFixedCell(0, FixedColumns.Name).AlignLeft);
    }

    [Fact]
    public void AMissingDateOfBirthOrNationalIdRendersEmptyRatherThanThrowing()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([new Patient { PersonId = 8, LastName = "Hansen", FirstName = "Ola" }]);

        Assert.Equal("", matrix.GetFixedCell(0, FixedColumns.DateOfBirth).Text);
        Assert.Equal("", matrix.GetFixedCell(0, FixedColumns.NationalId).Text);
        Assert.Equal("Hansen, Ola", matrix.GetFixedCell(0, FixedColumns.Name).Text);
    }

    [Fact]
    public void GetCellRejectsAnIndexOutsideTheMatrix()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(1)]);
        matrix.AddColumns(NamesOf("AGE"));

        Assert.Throws<ArgumentOutOfRangeException>(() => matrix.GetCell(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => matrix.GetCell(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => matrix.GetFixedCell(0, FixedColumns.Count));
    }

    [Fact]
    public void ColumnsAndRowsCanBeFoundByKey()
    {
        PersonMatrix matrix = NewMatrix();

        matrix.PreparePopulation([NewPatient(10), NewPatient(20)]);
        matrix.AddColumns(NamesOf("AGE", "YOB"));

        Assert.True(matrix.TryGetColumnIndex("YOB", out int columnIndex));
        Assert.Equal(1, columnIndex);
        Assert.False(matrix.TryGetColumnIndex("yob", out _));

        Assert.True(matrix.TryGetRow(20, out MatrixRow? row));
        Assert.Equal(20, row.PersonId);
        Assert.False(matrix.TryGetRow(30, out _));
    }
}
