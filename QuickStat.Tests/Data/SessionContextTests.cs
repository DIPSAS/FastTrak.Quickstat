using QuickStat.Data;
using Xunit;

namespace QuickStat.Tests.Data;

/// <summary>
/// The explicit replacement for the Delphi's RTTI placeholder resolution
/// (<c>Emetra.Classes.Business.pas:79-84</c> over the published properties of
/// <c>TCRFSimpleContext</c>, <c>CRF.Context.Facade.pas:97-104</c>).
/// </summary>
public class SessionContextTests
{
    private static SessionContext Session() => new()
    {
        StudyName = "NDV",
        StudyId = 7,
        SessionId = 99,
        User = new StudyUser { UserId = 42, UserName = "jdoe", CenterId = 5 },
        Database = new DatabaseInfo { DbVersion = 18300 },
        ServerName = "SQL01",
        DatabaseName = "EFT00028",
    };

    [Fact]
    public void ResolvesTheSixNamesTheDelphiExposed()
    {
        SessionContext session = Session();

        Assert.True(session.TryGetParameterValue("StudyId", out object? studyId));
        Assert.Equal(7, studyId);

        Assert.True(session.TryGetParameterValue("StudyName", out object? studyName));
        Assert.Equal("NDV", studyName);

        Assert.True(session.TryGetParameterValue("UserId", out object? userId));
        Assert.Equal(42, userId);

        Assert.True(session.TryGetParameterValue("SessId", out object? sessId));
        Assert.Equal(99, sessId);

        Assert.True(session.TryGetParameterValue("CenterId", out object? centerId));
        Assert.Equal(5, centerId);

        // Always zero: QuickStat never selects a patient, so TActiveCase never has a case. It still
        // has to resolve, because a population may reference it.
        Assert.True(session.TryGetParameterValue("CaseId", out object? caseId));
        Assert.Equal(0, caseId);
    }

    [Theory]
    [InlineData("studyid")]
    [InlineData("STUDYID")]
    [InlineData("StUdYiD")]
    public void MatchesCaseInsensitively(string name)
    {
        Assert.True(Session().TryGetParameterValue(name, out object? value));
        Assert.Equal(7, value);
    }

    [Theory]
    [InlineData("StartDate")]
    [InlineData("StopDate")]
    public void DoesNotResolveThePeriodPair(string name)
    {
        // The only pair that asks the user anything; it comes from the period prompt (step 2.3),
        // not from session state.
        Assert.False(Session().TryGetParameterValue(name, out object? value));
        Assert.Null(value);
    }

    [Theory]
    [InlineData("ProfId")]
    [InlineData("GroupId")]
    [InlineData("Whatever")]
    [InlineData("")]
    public void DoesNotResolveAnythingElse(string name)
    {
        // Adding a published property to TCRFSimpleContext silently added a resolvable placeholder,
        // and renaming one silently broke every population that used it. The vocabulary is now a
        // closed list.
        Assert.False(Session().TryGetParameterValue(name, out object? value));
        Assert.Null(value);
    }

    [Fact]
    public void RejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => Session().TryGetParameterValue(null!, out _));

    [Fact]
    public void CarriesTheVersionThresholdsAsConstants()
    {
        Assert.Equal(510, DatabaseInfo.MinimumDbVersion);
        Assert.Equal(18200, DatabaseInfo.PopulationsWithVersionDbVersion);
    }
}
