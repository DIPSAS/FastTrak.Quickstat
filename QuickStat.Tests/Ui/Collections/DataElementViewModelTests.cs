using System.ComponentModel;
using QuickStat.ViewModels;
using Xunit;

namespace QuickStat.Tests.Ui.Collections;

/// <summary>One row of the check list.</summary>
public class DataElementViewModelTests
{
    [Fact]
    public void ANameAndATitleAreBothRequired()
    {
        Assert.Throws<ArgumentNullException>(() => new DataElementViewModel(null!, "Alfa"));
        Assert.Throws<ArgumentNullException>(() => new DataElementViewModel("A", null!));
    }

    [Fact]
    public void TheTitleKeepsItsSortPrefix()
    {
        // §B.2: the leading "^ " is a sort hack, and the titles are parity that must not drift
        // (PORT-PLAN.md §6), so nothing trims it for display.
        DataElementViewModel element = new("DEMO.AGE", "^ Alder");

        Assert.Equal("DEMO.AGE", element.Name);
        Assert.Equal("^ Alder", element.Title);
    }

    [Fact]
    public void TickingTellsTheOwnerAndUntickingTellsItAgain()
    {
        // The Delphi recomputes on every OnClickCheck, in both directions.
        List<bool> notifications = [];

        DataElementViewModel element = new("A", "Alfa", e => notifications.Add(e.IsChecked));

        element.IsChecked = true;
        element.IsChecked = true;
        element.IsChecked = false;

        Assert.Equal([true, false], notifications);
    }

    [Fact]
    public void AnElementWithNoOwnerStillTicks()
    {
        DataElementViewModel element = new("A", "Alfa");

        element.IsChecked = true;

        Assert.True(element.IsChecked);
    }

    [Fact]
    public void TheCollectingMarkIsObservable()
    {
        // The check list's DataTemplate has a DataTrigger on it, so it has to raise.
        DataElementViewModel element = new("A", "Alfa");
        List<string?> changed = [];

        ((INotifyPropertyChanged)element).PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        element.IsCollecting = true;

        Assert.Equal([nameof(DataElementViewModel.IsCollecting)], changed);
    }
}
