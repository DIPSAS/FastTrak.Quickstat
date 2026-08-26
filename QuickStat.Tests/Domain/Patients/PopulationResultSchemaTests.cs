using QuickStat.Domain.Patients;
using QuickStat.Domain.Populations;
using Xunit;

namespace QuickStat.Tests.Domain.Patients;

/// <summary>
/// PORT-PLAN.md §7.2: a population that omits <c>FullName</c> must fail loudly instead of returning
/// zero patients.
/// </summary>
public class PopulationResultSchemaTests
{
    [Fact]
    public void ACompleteResultSetPasses()
    {
        PopulationResultSchema.Validate(["PersonId", "FullName", "DOB"], 261, "HbA1c > 53");
    }

    [Fact]
    public void ColumnNamesAreMatchedCaseInsensitively()
    {
        PopulationResultSchema.Validate(["personid", "FULLNAME"], 261, "HbA1c > 53");
    }

    [Fact]
    public void AMissingFullNameThrows()
    {
        // The Delphi read it with FieldByName inside a try whose handler silently freed the row, so
        // the whole cohort came back empty with no message at all.
        PopulationSchemaException error = Assert.Throws<PopulationSchemaException>(
            () => PopulationResultSchema.Validate(["PersonId", "DOB"], 261, "HbA1c > 53"));

        Assert.Equal("FullName", error.MissingColumn);
        Assert.Equal(261, error.ProcId);
        Assert.Equal("HbA1c > 53", error.PopulationTitle);
    }

    [Fact]
    public void TheMessageNamesThePopulationAndTheColumn()
    {
        PopulationSchemaException error = Assert.Throws<PopulationSchemaException>(
            () => PopulationResultSchema.Validate(["PersonId"], 261, "HbA1c > 53"));

        Assert.Contains("261", error.Message, StringComparison.Ordinal);
        Assert.Contains("HbA1c > 53", error.Message, StringComparison.Ordinal);
        Assert.Contains("FullName", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingPersonIdThrows()
    {
        // Without it every patient reads as -1 and the de-duplication collapses the cohort to one row.
        PopulationSchemaException error = Assert.Throws<PopulationSchemaException>(
            () => PopulationResultSchema.Validate(["FullName", "DOB"], 7, "Something"));

        Assert.Equal("PersonId", error.MissingColumn);
    }

    [Fact]
    public void AResultSetWithNoColumnsAtAllThrows()
    {
        // A population that returns nothing is the very case that used to look like an empty cohort.
        Assert.Throws<PopulationSchemaException>(
            () => PopulationResultSchema.Validate([], 0, null));
    }

    [Fact]
    public void FullNameIsReportedFirstWhenBothAreMissing()
    {
        PopulationSchemaException error = Assert.Throws<PopulationSchemaException>(
            () => PopulationResultSchema.Validate(["DOB"], 1, "T"));

        Assert.Equal("FullName", error.MissingColumn);
    }

    [Fact]
    public void AnUntitledStatementStillProducesAUsableMessage()
    {
        PopulationSchemaException error = Assert.Throws<PopulationSchemaException>(
            () => PopulationResultSchema.Validate(["PersonId"], 0, null));

        Assert.Contains("Population 0", error.Message, StringComparison.Ordinal);
        Assert.Null(error.PopulationTitle);
    }
}
