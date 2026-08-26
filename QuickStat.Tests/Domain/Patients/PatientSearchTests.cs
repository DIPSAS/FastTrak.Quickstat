using QuickStat.Data;
using QuickStat.Domain.Patients;
using Xunit;

namespace QuickStat.Tests.Domain.Patients;

/// <summary>
/// The free-text search dispatch table of <c>TPatientList.TryFindPeople</c>
/// (<c>CRF.Patient.List.pas:350-386</c>). The order of the tests is the contract.
/// </summary>
public class PatientSearchTests
{
    private static readonly DateTime Now = new(2026, 8, 26, 0, 0, 0, DateTimeKind.Unspecified);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void NothingToSearchForMeansNoStatement(string? text)
    {
        Assert.Null(PatientSearch.Build(1, text, Now));
    }

    [Theory]
    [InlineData("01019012345")]
    [InlineData("010190 12345")]
    public void ElevenDigitsAreANationalIdentityNumber(string text)
    {
        SqlRequest request = Require(PatientSearch.Build(1, text, Now));

        Assert.Equal(PatientSql.PersonByNationalId, request.CommandText);
        Assert.Equal("01019012345", request.NamedValues!["NationalId"]);
    }

    [Fact]
    public void ADateFollowedByANameSearchesOnBoth()
    {
        SqlRequest request = Require(PatientSearch.Build(9, "01.01.1990 Nordmann", Now));

        Assert.Equal(PatientSql.StudyPersonByDobAndName, request.CommandText);
        Assert.Equal(9, request.NamedValues!["StudyId"]);
        Assert.Equal(new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), request.NamedValues["DOB"]);
        Assert.Equal("Nordmann%", request.NamedValues["PartialLastName"]);
    }

    [Fact]
    public void ABareDateSearchesOnDateOfBirthWithinTheStudy()
    {
        SqlRequest request = Require(PatientSearch.Build(9, "01011990", Now));

        Assert.Equal(PatientSql.StudyPersonByDob, request.CommandText);
        Assert.Equal(new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), request.NamedValues!["DOB"]);
        Assert.Equal(2, request.NamedValues.Count);
    }

    [Fact]
    public void APositiveIntegerIsAPersonId()
    {
        SqlRequest request = Require(PatientSearch.Build(9, "2076", Now));

        Assert.Equal(PatientSql.PersonById, request.CommandText);
        Assert.Equal(2076, request.NamedValues!["PersonId"]);
    }

    [Fact]
    public void APersonIdSearchIgnoresTheStudy()
    {
        // QRY_PERSON_BY_ID has no JOIN_STUDY, unlike the fuzzy searches.
        SqlRequest request = Require(PatientSearch.Build(9, "2076", Now));

        Assert.DoesNotContain("StudCase", request.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public void AnythingElseWithLettersIsALastNamePrefix()
    {
        SqlRequest request = Require(PatientSearch.Build(9, "Bjørnstad", Now));

        Assert.Equal(PatientSql.StudyPersonByLastName, request.CommandText);
        Assert.Equal("Bjørnstad%", request.NamedValues!["SearchFor"]);
        Assert.Equal(9, request.NamedValues["StudyId"]);
    }

    [Fact]
    public void ANameSearchIsLimitedToPatientsEnrolledInTheStudy()
    {
        // #498565, the reason JOIN_STUDY exists.
        SqlRequest request = Require(PatientSearch.Build(9, "Bjørnstad", Now));

        Assert.Contains("JOIN dbo.StudCase sc ON sc.StudyId=:StudyId", request.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSearchTextIsTrimmedBeforeDispatch()
    {
        SqlRequest request = Require(PatientSearch.Build(9, "  2076  ", Now));

        Assert.Equal(PatientSql.PersonById, request.CommandText);
    }

    [Fact]
    public void ZeroAndNegativeIntegersAreNotPersonIds()
    {
        Assert.Null(PatientSearch.Build(9, "0", Now));
        Assert.Null(PatientSearch.Build(9, "-4", Now));
    }

    [Fact]
    public void EverySearchStatementOrdersByLastThenFirstName()
    {
        foreach (string sql in new[]
        {
            PatientSql.PersonById,
            PatientSql.PersonByNationalId,
            PatientSql.StudyPersonByDob,
            PatientSql.StudyPersonByDobAndName,
            PatientSql.StudyPersonByLastName,
        })
        {
            Assert.EndsWith(" ORDER BY p.LstName, p.FstName", sql, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("01011990", 1990, 1, 1)]
    [InlineData("01.01.1990", 1990, 1, 1)]
    [InlineData("31121899", 1899, 12, 31)]
    public void FourDigitYearsAreTakenLiterally(string text, int year, int month, int day)
    {
        Assert.True(PatientSearch.TryParseDateOfBirth(text, out DateTime value, Now));
        Assert.Equal(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified), value);
    }

    [Fact]
    public void ATwoDigitYearThatWouldBeInTheFutureGoesBackACentury()
    {
        // The last line of Emetra.Dates.Utils.GetDate.
        Assert.True(PatientSearch.TryParseDateOfBirth("010190", out DateTime value, Now));
        Assert.Equal(new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), value);
    }

    [Fact]
    public void ATwoDigitYearInsideTheWindowStaysInThisCentury()
    {
        Assert.True(PatientSearch.TryParseDateOfBirth("010120", out DateTime value, Now));
        Assert.Equal(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), value);
    }

    [Theory]
    [InlineData("32011990")]
    [InlineData("01131990")]
    [InlineData("29021990")]
    [InlineData("12345")]
    [InlineData("")]
    public void ImpossibleDatesAreRejected(string text)
    {
        Assert.False(PatientSearch.TryParseDateOfBirth(text, out _, Now));
    }

    private static SqlRequest Require(SqlRequest? request)
    {
        Assert.NotNull(request);
        Assert.NotNull(request!.NamedValues);
        return request;
    }
}
