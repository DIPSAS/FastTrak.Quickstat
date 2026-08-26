using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Data;
using QuickStat.Diagnostics;
using QuickStat.Tests.Data.Fakes;
using Xunit;

namespace QuickStat.Tests.Data;

/// <summary>
/// The ordered login pipeline that replaces the five unordered <c>ILoginObserver</c>s.
/// </summary>
/// <remarks>
/// The Delphi's sequence was whatever registration order happened to be
/// (<c>CRF.Context.Facade.pas:169-172</c> plus <c>MainQuickStat.pas:271</c>), it could not be
/// inspected, and one of its consequences was a real defect: <c>SET DATEFORMAT ymd</c> ran after the
/// first user query. Making the order a number makes it assertable, which is what these tests do.
/// </remarks>
public class LoginPipelineTests
{
    private static LoginContext Context(ISqlExecutor sql, IProgress<OperationProgress>? progress = null) => new()
    {
        StudyName = "NDV",
        Sql = sql,
        Progress = progress,
    };

    // ---------------------------------------------------------------- ordering

    [Fact]
    public void TheStepsRunInTheDocumentedOrder()
    {
        List<ILoginStep> unordered =
        [
            new StudySessionStep(NullLogger<StudySessionStep>.Instance),
            new ActiveUserStep(NullLogger<ActiveUserStep>.Instance),
            new SessionOptionsStep(),
            new DatabaseInfoStep(NullLogger<DatabaseInfoStep>.Instance),
        ];

        Assert.Equal<string>(
            ["Session options", "Database information", "Active user", "Study session"],
            [.. unordered.OrderBy(step => step.Order).Select(step => step.Name)]);
    }

    [Fact]
    public void TheOrdersLeaveRoomForInsertion()
    {
        int[] orders =
        [
            LoginStepOrder.SessionOptions,
            LoginStepOrder.DatabaseInfo,
            LoginStepOrder.ActiveUser,
            LoginStepOrder.StudySession,
        ];

        Assert.Equal<int>([0, 100, 200, 300], orders);
        Assert.Equal(orders.Length, orders.Distinct().Count());
    }

    // ---------------------------------------------------------------- step 0

    [Fact]
    public async Task SessionOptionsStepPutsTheSetStatementsAheadOfTheIdentityQuery()
    {
        RecordingSqlExecutor sql = new();
        _ = sql.Returns(SqlResultSet.Create(["ServerName", "DatabaseName"], ["SQL01", "EFT00028"]));

        LoginContext context = Context(sql);

        await new SessionOptionsStep().ExecuteAsync(context);

        string statement = Assert.Single(sql.Statements);

        Assert.StartsWith("SET XACT_ABORT ON;", statement, StringComparison.Ordinal);
        Assert.Contains("SET DATEFORMAT ymd;", statement, StringComparison.Ordinal);
        Assert.EndsWith(DataSql.ServerAndDatabase, statement, StringComparison.Ordinal);

        Assert.Equal("SQL01", context.ServerName);
        Assert.Equal("EFT00028", context.DatabaseName);
    }

    [Fact]
    public async Task SessionOptionsStepToleratesAnEmptyAnswer()
    {
        RecordingSqlExecutor sql = new();

        await new SessionOptionsStep().ExecuteAsync(Context(sql));

        Assert.Single(sql.Requests);
    }

    // ---------------------------------------------------------------- step 100

    [Fact]
    public async Task DatabaseInfoStepReadsPropertiesAndTheFastTrakVersionInOnePass()
    {
        RecordingSqlExecutor sql = new();

        _ = sql
            .Returns(SqlResultSet.Create(
                ["ProductVersion", "Collation", "ServerName", "WorkstationName", "DatabaseName"],
                ["15.0.4123.1", "Danish_Norwegian_CI_AS", "SQL01", "PC42", "EFT00028"]))
            .Returns(SqlResultSet.Create(
                ["ServerType", "DatabaseName", "DatabaseVersion", "ServerVersion", "EventScale"],
                ["1", "EFT00028", 18300, "16.0", 1000]));

        LoginContext context = Context(sql);

        await new DatabaseInfoStep(NullLogger<DatabaseInfoStep>.Instance).ExecuteAsync(context);

        DatabaseInfo info = Assert.IsType<DatabaseInfo>(context.Database);

        Assert.Equal("15.0.4123.1", info.ProductVersion);
        Assert.Equal(15, info.ProductMajorVersion);
        Assert.Equal(2019, info.ProductYear);
        Assert.Equal("Danish_Norwegian_CI_AS", info.Collation);
        Assert.Equal("PC42", info.WorkstationName);
        Assert.Equal("EFT00028", info.DbName);
        Assert.Equal(18300, info.DbVersion);
        Assert.Equal(1000, info.EventScale);

        // dbo.GetDatabaseInfo exactly once: the Delphi asked twice, the second time only for
        // EventScale, out of the very same row (CRF.Input.EventMap.pas:47).
        Assert.Equal(1, sql.Statements.Count(s => s == DataSql.DatabaseInfo));
    }

    [Theory]
    [InlineData(6, 1996)]
    [InlineData(8, 2000)]
    [InlineData(9, 2005)]
    [InlineData(10, 2008)]
    [InlineData(11, 2012)]
    [InlineData(12, 2014)]
    [InlineData(13, 2016)]
    [InlineData(14, 2017)]
    [InlineData(15, 2019)]
    [InlineData(16, 2022)]
    [InlineData(17, 2025)]
    [InlineData(99, 9999)]
    public void ProductYearMatchesTheDelphiTable(int major, int expected) =>
        Assert.Equal(expected, DatabaseInfoStep.ProductYearFor(major));

    [Theory]
    [InlineData("15.0.4123.1", 15)]
    [InlineData("8", 8)]
    [InlineData("", 0)]
    [InlineData("not a version", 0)]
    public void MajorVersionIsTheLeadingComponent(string productVersion, int expected) =>
        Assert.Equal(expected, DatabaseInfoStep.MajorVersionOf(productVersion));

    [Fact]
    public async Task DatabaseInfoStepSwallowsAFailureAndReportsMinusOne()
    {
        // Emetra.Database.Info.pas:154-159. The value is load-bearing: the population catalogue
        // falls back to its no-version query on it (EPR.Population.List.pas:104-109).
        RecordingSqlExecutor sql = new();
        _ = sql.Throws(new SqlCommandFailedException("dbo.GetDatabaseInfo does not exist"));

        LoginContext context = Context(sql);

        await new DatabaseInfoStep(NullLogger<DatabaseInfoStep>.Instance).ExecuteAsync(context);

        Assert.Equal(-1, context.Database?.DbVersion);
    }

    [Fact]
    public async Task DatabaseInfoStepRejectsATooOldSchema()
    {
        // Emetra.Database.Info.pas:131-135. In the Delphi this raise happened inside the same
        // try..except that swallows everything, so it could never reach the user; here it does.
        RecordingSqlExecutor sql = new();

        _ = sql
            .Returns(SqlResultSet.Create(
                ["ProductVersion", "Collation", "ServerName", "WorkstationName", "DatabaseName"],
                ["15.0.4123.1", "x", "SQL01", "PC42", "EFT"]))
            .Returns(SqlResultSet.Create(
                ["ServerType", "DatabaseName", "DatabaseVersion", "ServerVersion", "EventScale"],
                ["1", "EFT", 509, "16.0", 1000]));

        DatabaseVersionTooOldException exception = await Assert.ThrowsAsync<DatabaseVersionTooOldException>(
            () => new DatabaseInfoStep(NullLogger<DatabaseInfoStep>.Instance).ExecuteAsync(Context(sql)));

        Assert.Equal(509, exception.DbVersion);
        Assert.Equal(510, exception.MinimumDbVersion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(510)]
    [InlineData(18200)]
    public async Task DatabaseInfoStepAcceptsZeroAndAnythingAtOrAboveTheMinimum(int dbVersion)
    {
        // Zero means 'not a FastTrak database' and was never rejected (fDbVersion > 0 in the guard).
        RecordingSqlExecutor sql = new();

        _ = sql
            .Returns(SqlResultSet.Create(
                ["ProductVersion", "Collation", "ServerName", "WorkstationName", "DatabaseName"],
                ["15.0", "x", "SQL01", "PC42", "EFT"]))
            .Returns(SqlResultSet.Create(
                ["ServerType", "DatabaseName", "DatabaseVersion", "ServerVersion", "EventScale"],
                ["1", "EFT", dbVersion, "16.0", 0]));

        LoginContext context = Context(sql);

        await new DatabaseInfoStep(NullLogger<DatabaseInfoStep>.Instance).ExecuteAsync(context);

        Assert.Equal(dbVersion, context.Database?.DbVersion);
    }

    // ---------------------------------------------------------------- step 200

    private static SqlResultSet StudyUserRow(
        string professionName = "Lege",
        string centerName = "Med. avd.",
        int studyId = 7) => SqlResultSet.Create(
        [
            "UserId", "UserName", "PersonId", "FstName", "MidName", "LstName", "Signature", "HPRNo",
            "ProfId", "ProfName", "ProfType", "CenterId", "CenterName", "GroupId", "GroupName",
            "IsSuperuser", "IsDbOwner", "IsSingleGroupUser", "ShowMyGroup", "BlockRules",
            "RelationCount", "CaseList", "StudyId",
        ],
        [
            42, "jdoe", 1234, "Jane", "Q", "Doe", "JQD", 987654,
            3, professionName, "LEGE", 5, centerName, 9, "Sengepost 3",
            1, 0, 0, true, 4,
            17, 2, studyId,
        ]);

    [Fact]
    public async Task ActiveUserStepReadsTheProcedureOnce()
    {
        RecordingSqlExecutor sql = new();
        _ = sql.Returns(StudyUserRow());

        LoginContext context = Context(sql);

        await new ActiveUserStep(NullLogger<ActiveUserStep>.Instance).ExecuteAsync(context);

        // The Delphi ran dbo.GetStudyAndUser three times per project switch.
        Assert.Equal(DataSql.StudyAndUser, Assert.Single(sql.Statements));
        Assert.Equal<object?>(["NDV"], [.. sql.Requests[0].Values]);

        StudyUser user = Assert.IsType<StudyUser>(context.User);

        Assert.Equal(42, user.UserId);
        Assert.Equal("jdoe", user.UserName);
        Assert.Equal(1234, user.PersonId);
        Assert.Equal("Jane Q. Doe", user.FullName);
        Assert.Equal("JQD", user.Signature);
        Assert.Equal("987654", user.HprNumber);
        Assert.Equal(3, user.ProfessionId);
        Assert.Equal("Lege", user.ProfessionName);
        Assert.Equal("LEGE", user.ProfessionType);
        Assert.Equal(5, user.CenterId);
        Assert.Equal(9, user.GroupId);
        Assert.Equal("Sengepost 3", user.GroupName);
        Assert.True(user.IsSuperuser);
        Assert.False(user.IsDatabaseOwner);
        Assert.False(user.IsSingleGroupUser);
        Assert.True(user.ShowMyGroup);
        Assert.Equal(4, user.BlockRules);
        Assert.Equal(17, user.RelationCount);
        Assert.Equal(2, user.CaseList);

        // The study id resolves here, once (CRF.Context.ActiveUser.pas:236).
        Assert.Equal(7, context.StudyId);
        Assert.False(context.HasIncompleteUserProfile);
    }

    [Fact]
    public async Task ActiveUserStepReadsTheRolesAsIntegersComparedAgainstOne()
    {
        // CRF.Context.ActiveUser.pas:231-233 - 'ReadInteger(...) = 1', not 'not zero'.
        RecordingSqlExecutor sql = new();

        _ = sql.Returns(SqlResultSet.Create(
            ["UserId", "UserName", "IsSuperuser", "IsDbOwner", "IsSingleGroupUser", "ProfName", "CenterName"],
            [1, "u", 2, 1, 0, "Lege", "Avd"]));

        LoginContext context = Context(sql);

        await new ActiveUserStep(NullLogger<ActiveUserStep>.Instance).ExecuteAsync(context);

        Assert.False(context.User?.IsSuperuser);
        Assert.True(context.User?.IsDatabaseOwner);
    }

    [Fact]
    public async Task ActiveUserStepDefaultsRelationCountToMinusOne()
    {
        // CRF.Context.ActiveUser.pas:235 passes -1 as the default, unlike every other field.
        RecordingSqlExecutor sql = new();
        _ = sql.Returns(SqlResultSet.Create(["UserId", "UserName", "ProfName", "CenterName"], [1, "u", "Lege", "Avd"]));

        LoginContext context = Context(sql);

        await new ActiveUserStep(NullLogger<ActiveUserStep>.Instance).ExecuteAsync(context);

        Assert.Equal(-1, context.User?.RelationCount);
    }

    [Fact]
    public async Task ActiveUserStepPrefersAProjectedFullName()
    {
        RecordingSqlExecutor sql = new();

        _ = sql.Returns(SqlResultSet.Create(
            ["UserId", "UserName", "FullName", "FstName", "LstName", "ProfName", "CenterName"],
            [1, "u", "Doe, Jane", "Jane", "Doe", "Lege", "Avd"]));

        LoginContext context = Context(sql);

        await new ActiveUserStep(NullLogger<ActiveUserStep>.Instance).ExecuteAsync(context);

        Assert.Equal("Doe, Jane", context.User?.FullName);
    }

    [Fact]
    public async Task ActiveUserStepComposesAFullNameWithoutAMiddleName()
    {
        RecordingSqlExecutor sql = new();

        _ = sql.Returns(SqlResultSet.Create(
            ["UserId", "UserName", "FstName", "MidName", "LstName", "ProfName", "CenterName"],
            [1, "u", "Jane", "", "Doe", "Lege", "Avd"]));

        LoginContext context = Context(sql);

        await new ActiveUserStep(NullLogger<ActiveUserStep>.Instance).ExecuteAsync(context);

        Assert.Equal("Jane Doe", context.User?.FullName);
    }

    [Theory]
    [InlineData("", "Med. avd.")]
    [InlineData("Lege", "")]
    [InlineData("", "")]
    public async Task ActiveUserStepReportsAnIncompleteProfileInsteadOfCrashing(string profession, string centre)
    {
        // In the Delphi this state raised a modal dialog and then dereferenced the never-assigned
        // GlobalPickList, i.e. an access violation in Release
        // (CRF.Context.ActiveUser.pas:209-218, Emetra.Database.Dialog.Interfaces.pas:60).
        RecordingSqlExecutor sql = new();
        _ = sql.Returns(StudyUserRow(profession, centre));

        LoginContext context = Context(sql);

        await new ActiveUserStep(NullLogger<ActiveUserStep>.Instance).ExecuteAsync(context);

        Assert.True(context.HasIncompleteUserProfile);
        Assert.Equal(42, context.User?.UserId);
    }

    [Fact]
    public async Task ActiveUserStepToleratesAnEmptyResultSet()
    {
        RecordingSqlExecutor sql = new();
        _ = sql.Returns(SqlResultSet.Empty);

        LoginContext context = Context(sql);

        await new ActiveUserStep(NullLogger<ActiveUserStep>.Instance).ExecuteAsync(context);

        Assert.Equal(0, context.User?.UserId);
        Assert.True(context.HasIncompleteUserProfile);
    }

    [Fact]
    public async Task ActiveUserStepToleratesMissingColumns()
    {
        // dbo.GetStudyAndUser projects different columns on different schema versions, and the
        // Delphi's ReadInteger/ReadString helpers used FindField, not FieldByName.
        RecordingSqlExecutor sql = new();
        _ = sql.Returns(SqlResultSet.Create(["UserId", "UserName"], [5, "jdoe"]));

        LoginContext context = Context(sql);

        await new ActiveUserStep(NullLogger<ActiveUserStep>.Instance).ExecuteAsync(context);

        Assert.Equal(5, context.User?.UserId);
        Assert.Equal("", context.User?.GroupName);
        Assert.Equal(0, context.StudyId);
    }

    // ---------------------------------------------------------------- step 300

    [Fact]
    public async Task StudySessionStepSkipsTheStudyLookupWhenTheIdIsAlreadyKnown()
    {
        // The whole point of collapsing three resolutions into one (PORT-PLAN.md §7.3).
        RecordingSqlExecutor sql = new();
        _ = sql.Returns(SqlResultSet.Create(["SessId"], [99]));

        LoginContext context = Context(sql);
        context.StudyId = 7;

        await new StudySessionStep(NullLogger<StudySessionStep>.Instance, "1.2.3").ExecuteAsync(context);

        Assert.Equal(DataSql.AddSession, Assert.Single(sql.Statements));
        Assert.Equal(99, context.SessionId);
    }

    [Fact]
    public async Task StudySessionStepFallsBackToDboStudyWhenTheIdIsUnknown()
    {
        RecordingSqlExecutor sql = new();

        _ = sql
            .Returns(SqlResultSet.Create(["StudyId"], [7]))
            .Returns(SqlResultSet.Create(["SessId"], [99]));

        LoginContext context = Context(sql);

        await new StudySessionStep(NullLogger<StudySessionStep>.Instance, "1.2.3").ExecuteAsync(context);

        Assert.Equal<string>([DataSql.StudyId, DataSql.AddSession], [.. sql.Statements]);
        Assert.Equal(7, context.StudyId);
        Assert.Equal(99, context.SessionId);
    }

    [Fact]
    public async Task StudySessionStepDoesNotOpenASessionForAnUnknownStudy()
    {
        RecordingSqlExecutor sql = new();
        _ = sql.Returns(SqlResultSet.Empty);

        LoginContext context = Context(sql);

        await new StudySessionStep(NullLogger<StudySessionStep>.Instance, "1.2.3").ExecuteAsync(context);

        Assert.Equal(DataSql.StudyId, Assert.Single(sql.Statements));
        Assert.Equal(0, context.SessionId);
    }

    [Fact]
    public async Task StudySessionStepSendsTheRealApplicationVersion()
    {
        // The Delphi always sent '' because nothing ever set Session.AppVersion
        // (CRF.Context.Session.pas:218).
        RecordingSqlExecutor sql = new();
        _ = sql.Returns(SqlResultSet.Create(["SessId"], [1]));

        LoginContext context = Context(sql);
        context.StudyId = 7;

        await new StudySessionStep(NullLogger<StudySessionStep>.Instance, "22.12.21.547").ExecuteAsync(context);

        SqlRequest request = Assert.Single(sql.Requests);

        Assert.Equal(5, request.Values.Count);
        Assert.Equal(7, request.Values[0]);
        Assert.Equal(Environment.MachineName, request.Values[1]);
        Assert.Equal(Environment.UserName, request.Values[2]);
        Assert.IsType<DateTime>(request.Values[3]);
        Assert.Equal("22.12.21.547", request.Values[4]);
    }

    [Fact]
    public async Task StudySessionStepNeverRetriesTheInsert()
    {
        // dbo.AddSession inserts a row; retrying it is the duplication hazard §7.2 removes.
        RecordingSqlExecutor sql = new();
        _ = sql.Returns(SqlResultSet.Create(["SessId"], [1]));

        LoginContext context = Context(sql);
        context.StudyId = 7;

        await new StudySessionStep(NullLogger<StudySessionStep>.Instance, "1.0").ExecuteAsync(context);

        Assert.False(Assert.Single(sql.Requests).IsIdempotent);
    }

    [Fact]
    public void TheApplicationVersionIsTrimmedToFitTheColumn()
    {
        string version = StudySessionStep.CurrentAppVersion();

        Assert.True(version.Length <= StudySessionStep.MaximumAppVersionLength);
        Assert.DoesNotContain('+', version);
    }

    // ---------------------------------------------------------------- progress

    [Fact]
    public async Task EveryStepReportsProgress()
    {
        List<OperationProgress> reports = [];
        RecordingSqlExecutor sql = new();

        // Progress<T> posts to the captured synchronisation context; collect synchronously instead.
        SynchronousProgress collector = new(reports);

        _ = sql
            .Returns(SqlResultSet.Create(["ServerName", "DatabaseName"], ["SQL01", "EFT"]))
            .Returns(SqlResultSet.Empty)
            .Returns(SqlResultSet.Empty)
            .Returns(SqlResultSet.Create(["UserId", "UserName", "ProfName", "CenterName", "StudyId"], [1, "u", "L", "A", 7]))
            .Returns(SqlResultSet.Create(["SessId"], [3]));

        LoginContext context = Context(sql, collector);

        await new SessionOptionsStep().ExecuteAsync(context);
        await new DatabaseInfoStep(NullLogger<DatabaseInfoStep>.Instance).ExecuteAsync(context);
        await new ActiveUserStep(NullLogger<ActiveUserStep>.Instance).ExecuteAsync(context);
        await new StudySessionStep(NullLogger<StudySessionStep>.Instance, "1.0").ExecuteAsync(context);

        Assert.Equal(4, reports.Count);
        Assert.All(reports, report => Assert.Equal("Connecting", report.Header));
        Assert.All(reports, report => Assert.False(string.IsNullOrWhiteSpace(report.Info)));
    }

    private sealed class SynchronousProgress : IProgress<OperationProgress>
    {
        private readonly List<OperationProgress> _reports;

        public SynchronousProgress(List<OperationProgress> reports) => _reports = reports;

        public void Report(OperationProgress value) => _reports.Add(value);
    }
}
