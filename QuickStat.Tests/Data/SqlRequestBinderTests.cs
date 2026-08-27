using QuickStat.Configuration;
using QuickStat.Data;
using Xunit;

namespace QuickStat.Tests.Data;

/// <summary>
/// Binding rules. Each one turns a Delphi silent failure into a named exception
/// (<c>Emetra.Database.Simple.pas:415-433</c>).
/// </summary>
public class SqlRequestBinderTests
{
    private static readonly ColonToAtSqlTextRewriter Rewriter = new();
    private static readonly SqlOptions Options = new();

    private static BoundSqlCommand Bind(SqlRequest request) => SqlRequestBinder.Bind(request, Rewriter, Options);

    [Fact]
    public void BindsPositionalValuesInPlaceholderOrder()
    {
        BoundSqlCommand command = Bind(new SqlRequest
        {
            CommandText = "EXEC dbo.AddSession :StudyId,:CompName,:CompUser,:CompTime,:AppVer",
            Values = [7, "PC01", "jdoe", new DateTime(2026, 8, 26, 9, 0, 0, DateTimeKind.Unspecified), "1.0.0"],
        });

        Assert.Equal("EXEC dbo.AddSession @StudyId,@CompName,@CompUser,@CompTime,@AppVer", command.CommandText);

        Assert.Equal<string>(
            ["StudyId", "CompName", "CompUser", "CompTime", "AppVer"],
            [.. command.Parameters.Select(p => p.Name)]);

        Assert.Equal(7, command.Parameters[0].Value);
        Assert.Equal("1.0.0", command.Parameters[4].Value);
    }

    [Fact]
    public void BindsNamedValuesCaseInsensitively()
    {
        BoundSqlCommand command = Bind(new SqlRequest
        {
            CommandText = "SELECT * FROM t WHERE StudyId = :StudyId",
            NamedValues = new Dictionary<string, object?> { ["STUDYID"] = 11 },
        });

        Assert.Equal(11, Assert.Single(command.Parameters).Value);
    }

    [Fact]
    public void IgnoresNamedValuesTheStatementDoesNotUse()
    {
        // The parameter resolver hands over everything the session knows; a population that uses
        // two of the six names must not fail because of the other four.
        BoundSqlCommand command = Bind(new SqlRequest
        {
            CommandText = "SELECT :StudyId",
            NamedValues = new Dictionary<string, object?>
            {
                ["StudyId"] = 1,
                ["UserId"] = 2,
                ["CaseId"] = 0,
            },
        });

        Assert.Single(command.Parameters);
    }

    [Fact]
    public void RejectsTooFewPositionalValues()
    {
        // The Delphi looped to Parameters.Count and read past the end of the open array instead.
        SqlParameterBindingException exception = Assert.Throws<SqlParameterBindingException>(() => Bind(new SqlRequest
        {
            CommandText = "SELECT :A, :B",
            Values = [1],
        }));

        Assert.Contains(":A", exception.Message, StringComparison.Ordinal);
        Assert.Contains(":B", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsTooManyPositionalValues() =>
        Assert.Throws<SqlParameterBindingException>(() => Bind(new SqlRequest
        {
            CommandText = "SELECT :A",
            Values = [1, 2],
        }));

    [Fact]
    public void RejectsMissingValuesEntirely() =>
        Assert.Throws<SqlParameterBindingException>(() => Bind(new SqlRequest
        {
            CommandText = "SELECT :A",
        }));

    [Fact]
    public void RejectsAnUnknownNamedPlaceholder()
    {
        SqlParameterBindingException exception = Assert.Throws<SqlParameterBindingException>(() => Bind(new SqlRequest
        {
            CommandText = "SELECT :Missing",
            NamedValues = new Dictionary<string, object?> { ["Other"] = 1 },
        }));

        Assert.Equal("Missing", exception.ParameterName);
    }

    [Fact]
    public void RejectsBothBindingStylesAtOnce() =>
        Assert.Throws<SqlParameterBindingException>(() => Bind(new SqlRequest
        {
            CommandText = "SELECT :A",
            Values = [1],
            NamedValues = new Dictionary<string, object?> { ["A"] = 1 },
        }));

    [Fact]
    public void RejectsPositionalBindingOfARepeatedPlaceholder()
    {
        SqlParameterBindingException exception = Assert.Throws<SqlParameterBindingException>(() => Bind(new SqlRequest
        {
            CommandText = "SELECT * FROM t WHERE a = :Id OR b = :Id",
            Values = [1],
        }));

        Assert.Contains("NamedValues", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AllowsNamedBindingOfARepeatedPlaceholder()
    {
        BoundSqlCommand command = Bind(new SqlRequest
        {
            CommandText = "SELECT * FROM t WHERE a = :Id OR b = :Id",
            NamedValues = new Dictionary<string, object?> { ["Id"] = 42 },
        });

        // One placeholder, one parameter, referenced twice in the text.
        Assert.Equal(42, Assert.Single(command.Parameters).Value);
        Assert.Equal("SELECT * FROM t WHERE a = @Id OR b = @Id", command.CommandText);
    }

    [Fact]
    public void RejectsAnEmptyStatement() =>
        Assert.Throws<SqlParameterBindingException>(() => Bind(new SqlRequest { CommandText = "   " }));

    [Fact]
    public void CollapsesDbNullOntoNull()
    {
        BoundSqlCommand command = Bind(new SqlRequest
        {
            CommandText = "SELECT :A",
            Values = [DBNull.Value],
        });

        Assert.Null(Assert.Single(command.Parameters).Value);
    }

    // ---------------------------------------------------------------- table-valued parameters

    [Fact]
    public void DoesNotDemandAScalarValueForATableValuedPlaceholder()
    {
        // The TVP placeholder is in the text and is rewritten like any other, but its value comes
        // from TableParameters, so it must not consume a positional value.
        BoundSqlCommand command = Bind(new SqlRequest
        {
            CommandText = "SELECT p.* FROM dbo.Person p JOIN :Ids i ON i.PersonId = p.PersonId WHERE p.StudyId = :StudyId",
            Values = [3],
            TableParameters =
            [
                new SqlTableParameter
                {
                    Name = "Ids",
                    TypeName = "Report.PersonIdList",
                    ColumnName = "PersonId",
                    Values = [1, 2, 3],
                },
            ],
        });

        Assert.Equal("StudyId", Assert.Single(command.Parameters).Name);
        Assert.Equal("Ids", Assert.Single(command.TableParameters).Name);
        Assert.Contains("@Ids", command.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolvesATableValuedPlaceholderAlongsideNamedValues()
    {
        // The exact shape step 2.3 produces: the TVP name is in the statement text like any other
        // placeholder, but its value arrives through TableParameters rather than NamedValues, so
        // demanding a named value for it would reject every national-id and cohort query.
        BoundSqlCommand command = Bind(new SqlRequest
        {
            CommandText =
                "SELECT p.PersonId, p.NationalId FROM dbo.Person p " +
                "JOIN :PersonIds i ON i.PersonId = p.PersonId WHERE p.StudyId = :StudyId",
            NamedValues = new Dictionary<string, object?> { ["StudyId"] = 7 },
            TableParameters =
            [
                new SqlTableParameter
                {
                    Name = "PersonIds",
                    TypeName = "Report.PersonIdList",
                    ColumnName = "PersonId",
                    Values = [11, 22, 33],
                },
            ],
        });

        Assert.Equal("StudyId", Assert.Single(command.Parameters).Name);
        Assert.Equal(7, command.Parameters[0].Value);

        SqlTableParameter table = Assert.Single(command.TableParameters);

        Assert.Equal("PersonIds", table.Name);
        Assert.Equal<int>([11, 22, 33], [.. table.Values]);

        Assert.Contains("JOIN @PersonIds i", command.CommandText, StringComparison.Ordinal);
        Assert.Contains("p.StudyId = @StudyId", command.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public void APlaceholderSatisfiedByNeitherIsTheOnlyBindingError()
    {
        SqlParameterBindingException exception = Assert.Throws<SqlParameterBindingException>(() => Bind(new SqlRequest
        {
            CommandText = "SELECT 1 FROM :Ids WHERE a = :Known AND b = :Forgotten",
            NamedValues = new Dictionary<string, object?> { ["Known"] = 1 },
            TableParameters =
            [
                new SqlTableParameter
                {
                    Name = "Ids",
                    TypeName = "Report.PersonIdList",
                    ColumnName = "PersonId",
                    Values = [1],
                },
            ],
        }));

        Assert.Equal("Forgotten", exception.ParameterName);
    }

    [Fact]
    public void MatchesTableValuedPlaceholderNamesCaseInsensitively()
    {
        BoundSqlCommand command = Bind(new SqlRequest
        {
            CommandText = "SELECT 1 FROM :personids",
            TableParameters =
            [
                new SqlTableParameter
                {
                    Name = "PersonIds",
                    TypeName = "Report.PersonIdList",
                    ColumnName = "PersonId",
                    Values = [1],
                },
            ],
        });

        Assert.Empty(command.Parameters);
    }

    [Fact]
    public void BuildsThePersonIdTableParameterFromSqlOptions()
    {
        // A local configuration rather than the shared Options: the table type is not configured by
        // default, because it exists on no database (SqlOptions.PersonIdListTypeName).
        SqlOptions options = new() { PersonIdListTypeName = "Report.PersonIdList" };

        SqlTableParameter table = SqlTableParameter.ForPersonIds(options, "PersonIds", [1, 2, 3]);

        Assert.Equal("PersonIds", table.Name);
        Assert.Equal(options.PersonIdListTypeName, table.TypeName);
        Assert.Equal(options.PersonIdListColumnName, table.ColumnName);
        Assert.Equal(3, table.Values.Count);
    }

    [Fact]
    public void RefusesToBuildAPersonIdTableParameterWhenTheTypeIsUnavailable()
    {
        // SqlOptions.PersonIdListTypeName is nullable precisely so a customer database without the
        // table type can be detected once and fall back to chunked literals.
        SqlOptions withoutType = new() { PersonIdListTypeName = null };

        _ = Assert.Throws<InvalidOperationException>(
            () => SqlTableParameter.ForPersonIds(withoutType, "PersonIds", [1]));
    }

    [Fact]
    public void RejectsADuplicatedTableValuedParameter()
    {
        SqlTableParameter table = new()
        {
            Name = "Ids",
            TypeName = "Report.PersonIdList",
            ColumnName = "PersonId",
            Values = [1],
        };

        SqlParameterBindingException exception = Assert.Throws<SqlParameterBindingException>(() => Bind(new SqlRequest
        {
            CommandText = "SELECT 1 FROM :Ids",
            TableParameters = [table, table],
        }));

        Assert.Equal("Ids", exception.ParameterName);
    }

    [Fact]
    public void CarriesMoreIdsThanSqlServerAllowsParameters()
    {
        // PORT-PLAN.md §7.3: the whole point of the table-valued parameter is that 2 100 is not a
        // ceiling any more.
        int[] ids = [.. Enumerable.Range(1, 5000)];

        BoundSqlCommand command = Bind(new SqlRequest
        {
            CommandText = "SELECT 1 FROM :Ids",
            TableParameters =
            [
                new SqlTableParameter
                {
                    Name = "Ids",
                    TypeName = "Report.PersonIdList",
                    ColumnName = "PersonId",
                    Values = ids,
                },
            ],
        });

        Assert.Empty(command.Parameters);
        Assert.Equal(5000, Assert.Single(command.TableParameters).Values.Count);
    }

    // ---------------------------------------------------------------- timeout

    [Fact]
    public void UsesTheConfiguredTimeoutByDefault()
    {
        BoundSqlCommand command = Bind(new SqlRequest { CommandText = "SELECT 1" });

        Assert.Equal(Options.DefaultCommandTimeout, command.CommandTimeout);
        Assert.Equal(TimeSpan.FromSeconds(300), command.CommandTimeout);
    }

    [Fact]
    public void LetsARequestOverrideTheTimeout()
    {
        BoundSqlCommand command = Bind(new SqlRequest
        {
            CommandText = "SELECT 1",
            CommandTimeout = TimeSpan.FromSeconds(5),
        });

        Assert.Equal(TimeSpan.FromSeconds(5), command.CommandTimeout);
    }
}
