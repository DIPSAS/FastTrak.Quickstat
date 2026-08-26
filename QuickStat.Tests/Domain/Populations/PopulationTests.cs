using QuickStat.Domain.Populations;
using Xunit;

namespace QuickStat.Tests.Domain.Populations;

/// <summary>
/// <see cref="Population.SearchText"/> decides what the population filter matches, so its field
/// order and separator are observable.
/// </summary>
public class PopulationTests
{
    private static Population Sample() => new()
    {
        ProcId = 261,
        Title = "HbA1c > 53 (7%)",
        QueryText = "EXEC dbo.GetCaseListHbA1c9Plus :StudyId",
        Group = "Type 2",
        HelpText = "Pasientar med dårleg regulert diabetes",
        InfoCaption = "ignored",
        SourceCode = "CREATE PROCEDURE dbo.GetCaseListHbA1c9Plus",
    };

    [Fact]
    public void SearchTextIsProcIdTitleHelpTextAndGroupSeparatedByTabs()
    {
        // CRF.Population.pas:94 - fListBoxText := V + #9 + DN + #9 + Description + #9 + OT.
        Assert.Equal(
            "261\tHbA1c > 53 (7%)\tPasientar med dårleg regulert diabetes\tType 2",
            Sample().SearchText);
    }

    [Fact]
    public void SearchTextMakesTheProcIdMatchAsASubstring()
    {
        // Typing "26" finds 26, 126, 261 and 260 alike. Users rely on it.
        Assert.Contains("26", Sample().SearchText, StringComparison.Ordinal);
    }

    [Fact]
    public void SearchTextIncludesTheGroupAndTheHelpText()
    {
        string text = Sample().SearchText;

        Assert.Contains("Type 2", text, StringComparison.Ordinal);
        Assert.Contains("dårleg", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SearchTextExcludesTheStatementAndTheSourceCode()
    {
        // The filter must not match on SQL: a user typing "GetCaseList" is not searching for that.
        string text = Sample().SearchText;

        Assert.DoesNotContain("EXEC", text, StringComparison.Ordinal);
        Assert.DoesNotContain("CREATE PROCEDURE", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ignored", text, StringComparison.Ordinal);
    }

    [Fact]
    public void OptionalFieldsDefaultToEmptyRatherThanNull()
    {
        Population minimal = new() { ProcId = 1, Title = "T", QueryText = "EXEC x" };

        Assert.Equal("1\tT\t\t", minimal.SearchText);
    }
}
