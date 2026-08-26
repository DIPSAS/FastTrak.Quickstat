using QuickStat.Domain.Matrix;
using Xunit;

namespace QuickStat.Tests.Domain.Matrix;

/// <summary>
/// The collection that decides column order, which is observable in every exported file.
/// </summary>
public class VariableNameSetTests
{
    [Fact]
    public void ColumnOrderDefaultsToFirstSeen()
    {
        // FirstSeen is value zero, so an un-initialised field, a default struct member and an
        // omitted constructor argument all mean insertion order.  Alphabetical would silently
        // reorder the columns of every export a user has ever produced.
        Assert.Equal(ColumnOrder.FirstSeen, default(ColumnOrder));
        Assert.Equal(0, (int)ColumnOrder.FirstSeen);
        Assert.Equal(ColumnOrder.FirstSeen, new VariableNameSet().Order);
    }

    [Fact]
    public void MembersComeBackInInsertionOrder()
    {
        // For form data insertion order is on-form item order, because the query carries
        // ORDER BY mfi.OrderNumber.
        VariableNameSet names = new();

        Assert.True(names.Add("WEIGHT"));
        Assert.True(names.Add("HEIGHT"));
        Assert.True(names.Add("BMI"));
        Assert.True(names.Add("AGE"));

        Assert.Equal(["WEIGHT", "HEIGHT", "BMI", "AGE"], names);
        Assert.Equal("WEIGHT", names[0]);
        Assert.Equal("AGE", names[3]);
    }

    [Fact]
    public void AlphabeticalIsAvailableAsAPolicyFlip()
    {
        VariableNameSet names = new(ColumnOrder.Alphabetical);

        names.Add("WEIGHT");
        names.Add("HEIGHT");
        names.Add("BMI");
        names.Add("AGE");

        Assert.Equal(["AGE", "BMI", "HEIGHT", "WEIGHT"], names);
        Assert.Equal(ColumnOrder.Alphabetical, names.Order);
    }

    [Fact]
    public void AlphabeticalIsOrdinalNotCultureAware()
    {
        VariableNameSet names = new(ColumnOrder.Alphabetical);

        names.Add("b");
        names.Add("A");
        names.Add("a");
        names.Add("B");

        // Ordinal puts every uppercase letter before every lowercase one; a culture-aware sort
        // would interleave them.
        Assert.Equal(["A", "B", "a", "b"], names);
    }

    [Fact]
    public void DuplicatesAreIgnoredAndDoNotDisturbTheOrder()
    {
        VariableNameSet names = new();

        Assert.True(names.Add("WEIGHT"));
        Assert.True(names.Add("HEIGHT"));
        Assert.False(names.Add("WEIGHT"));
        Assert.True(names.Add("BMI"));
        Assert.False(names.Add("HEIGHT"));

        Assert.Equal(3, names.Count);
        Assert.Equal(["WEIGHT", "HEIGHT", "BMI"], names);
    }

    [Fact]
    public void DeDuplicationIsOrdinal()
    {
        VariableNameSet names = new();

        Assert.True(names.Add("DB_VERSION"));
        Assert.True(names.Add("DbVersion"));

        // The row dictionary that actually stores the data is case-sensitive, so this must be too.
        Assert.Equal(2, names.Count);
        Assert.True(names.Contains("DB_VERSION"));
        Assert.True(names.Contains("DbVersion"));
        Assert.False(names.Contains("dbversion"));
    }

    [Fact]
    public void ClearEmptiesTheSetSoAReRunStartsFresh()
    {
        // Docs/Port/04-matrix-export.md R-4: the Delphi never cleared FVarList, so re-running a
        // collector against a different population kept variables discovered in an earlier run and
        // produced columns that were empty for everyone.
        VariableNameSet names = new();

        names.Add("STALE_A");
        names.Add("STALE_B");

        names.Clear();

        Assert.Empty(names);
        Assert.False(names.Contains("STALE_A"));

        names.Add("FRESH");

        Assert.Equal(["FRESH"], names);
    }

    [Fact]
    public void ClearAlsoResetsTheAlphabeticalView()
    {
        VariableNameSet names = new(ColumnOrder.Alphabetical);

        names.Add("B");
        names.Clear();
        names.Add("C");
        names.Add("A");

        Assert.Equal(["A", "C"], names);
    }

    [Fact]
    public void TheTwoViewsHoldTheSameMembers()
    {
        VariableNameSet insertion = new();
        VariableNameSet alphabetical = new(ColumnOrder.Alphabetical);

        foreach (string name in new[] { "C", "A", "B" })
        {
            insertion.Add(name);
            alphabetical.Add(name);
        }

        Assert.Equal(insertion.Count, alphabetical.Count);
        Assert.Equal(insertion.OrderBy(name => name, StringComparer.Ordinal), alphabetical);
    }

    [Fact]
    public void AddRejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => new VariableNameSet().Add(null!));
}
