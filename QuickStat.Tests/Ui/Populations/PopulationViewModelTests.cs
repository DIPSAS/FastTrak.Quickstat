using QuickStat.Domain.Populations;
using QuickStat.ViewModels;
using Xunit;

namespace QuickStat.Tests.Ui.Populations;

/// <summary>One row of the population list. <c>05-ui-spec.md</c> §B.1.1.</summary>
public class PopulationViewModelTests
{
    [Fact]
    public void ItProjectsTheCatalogueRowWithoutCopyingIt()
    {
        Population population = PopulationTestDoubles.NewPopulation(
            257,
            "HbA1c > 53 (7%)",
            group: "Type 1 u/pumpe",
            helpText: "Pasientar med dårleg regulert diabetes.");

        PopulationViewModel row = new(population);

        Assert.Same(population, row.Population);
        Assert.Equal(257, row.ProcId);
        Assert.Equal("HbA1c > 53 (7%)", row.Title);
        Assert.Equal("Type 1 u/pumpe", row.Group);
        Assert.Equal("Pasientar med dårleg regulert diabetes.", row.HelpText);
    }

    [Fact]
    public void TheSearchTextComesFromCoreAndIsNotRebuilt()
    {
        // CRF.Population.pas:94 - V + #9 + DN + #9 + Description + #9 + OT. Field order and the tab
        // separator are observable: typing a number filters ProcId by substring.
        Population population = PopulationTestDoubles.NewPopulation(26, "Tittel", group: "Gruppe", helpText: "Hjelp");

        PopulationViewModel row = new(population);

        Assert.Equal(population.SearchText, row.SearchText);
        Assert.Equal("26\tTittel\tHjelp\tGruppe", row.SearchText);
    }

    [Fact]
    public void ARowStartsCollapsedAndRaisesChangeNotification()
    {
        PopulationViewModel row = new(PopulationTestDoubles.NewPopulation(1, "Ein"));
        List<string?> changed = [];

        row.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        Assert.False(row.IsExpanded);

        row.IsExpanded = true;

        Assert.Equal([nameof(PopulationViewModel.IsExpanded)], changed);
    }

    [Fact]
    public void ANullPopulationIsRejected() =>
        Assert.Throws<ArgumentNullException>(() => new PopulationViewModel(null!));
}
