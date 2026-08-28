using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using QuickStat.Tests.Configuration;
using QuickStat.ViewModels;
using Xunit;

namespace QuickStat.Tests.Ui.Populations;

/// <summary>
/// The two views, read as markup. <c>05-ui-spec.md</c> §B.1 and §B.1.1.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why the source rather than the control.</b> A compiled WPF <c>Page</c> resolves its
/// <c>StaticResource</c> references while <c>InitializeComponent</c> runs, walking the element's own
/// resources and then <see cref="System.Windows.Application"/>. When these were written there was no
/// <see cref="System.Windows.Application"/> under test at all, so instantiating either view threw
/// before a single assertion could run and the markup could only be pinned structurally - the same
/// technique <c>Configuration/Settings/SettingsPathTests</c> uses to prohibit
/// <c>Assembly.Location</c>.
/// </para>
/// <para>
/// <b>That gap is closed elsewhere now, and these cases are still not redundant.</b>
/// <c>Ui/WpfApplicationFixture.cs</c> supplies the assembly's one <see cref="Application"/> and
/// <c>Ui/ViewInstantiationTests.cs</c> constructs both views through it, which is what proves they
/// load and that their bindings resolve - neither of which reading XML can do. What reading XML can
/// still do, and what a constructed control cannot, is assert the <em>absence</em> of an attribute:
/// <c>SelectedIndex</c> not being set on the project combo, <c>FlowDirection</c> and
/// <c>VerticalAlignment</c> not being set locally on the <c>Simplified</c> box. A control reports
/// the effective value and cannot tell a style setter from a local one, so those three cases would
/// pass against exactly the markup they exist to forbid.
/// </para>
/// </remarks>
public class PopulationViewMarkupTests
{
    private static readonly XNamespace Wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    /// <summary>The <c>x:</c> namespace, which is where <c>x:Name</c> and <c>x:Key</c> live.</summary>
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>The <c>input:</c> namespace, which is where the attached double-click lives.</summary>
    private static readonly XNamespace Input = "clr-namespace:QuickStat.Input";

    private static XDocument View(string fileName) =>
        XDocument.Load(Path.Combine(RepositoryFiles.Root, "QuickStat.App", "Views", fileName));

    private static XDocument Picker() => View("PopulationPickerView.xaml");

    private static XDocument Tab() => View("PopulationTabView.xaml");

    private static IEnumerable<XElement> Named(XDocument view, string localName) =>
        view.Descendants(Wpf + localName);

    private static XElement One(XDocument view, string localName, XName attribute, string value) =>
        Assert.Single(Named(view, localName), element => (string?)element.Attribute(attribute) == value);

    private static string? Attribute(XElement element, XName name) => (string?)element.Attribute(name);

    // ---------------------------------------------------------------------------------------
    // The tab
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void TheTabCarriesTheThreeStaticStrings()
    {
        XDocument tab = Tab();

        List<string?> headers = [.. tab.Descendants()
            .Where(element => element.Name.LocalName == "SectionHeader")
            .Select(element => (string?)element.Attribute("Header"))];

        // Against the view-model's constants, not against a second copy of the same literals: a
        // caption that exists twice is a caption that can differ in two places.
        Assert.Equal(
            [PopulationTabViewModel.DatabaseHeader, PopulationTabViewModel.PopulationHeader],
            headers);

        Assert.Contains(
            Named(tab, "TextBlock"),
            element => Attribute(element, "Text") == PopulationTabViewModel.TipText);
    }

    [Fact]
    public void TheProjectComboIsADropDownListBoundToProjects()
    {
        XElement combo = Assert.Single(Named(Tab(), "ComboBox"));

        Assert.Equal("{Binding Projects}", Attribute(combo, "ItemsSource"));
        Assert.Equal("{Binding SelectedProject, Mode=TwoWay}", Attribute(combo, "SelectedItem"));
        Assert.Equal("Name", Attribute(combo, "DisplayMemberPath"));

        // csDropDownList: the user picks, never types.
        Assert.Equal("False", Attribute(combo, "IsEditable"));

        // §B.1: no item is preselected. A SelectedIndex here would connect on start-up.
        Assert.Null(Attribute(combo, "SelectedIndex"));
    }

    [Fact]
    public void TheTabHostsThePickerOnItsOwnViewModel()
    {
        XElement picker = Assert.Single(
            Tab().Descendants(),
            element => element.Name.LocalName == "PopulationPickerView");

        Assert.Equal("{Binding Picker}", Attribute(picker, "DataContext"));
    }

    // ---------------------------------------------------------------------------------------
    // The picker's chrome - the four captions FormCreate overwrites in English
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void TheFourCaptionsAreTheEnglishOnes()
    {
        XDocument picker = Picker();

        // MainQuickStat.pas:289-292 overwrites the frame's Norwegian .dfm captions with these four.
        Assert.Equal("Filter / search text", PopulationPickerViewModel.FilterHeader);
        Assert.Equal("Type filter text here", PopulationPickerViewModel.FilterPlaceholder);
        Assert.Equal("Frequently used only", PopulationPickerViewModel.FrequentlyUsedCaption);
        Assert.Equal("Simplified", PopulationPickerViewModel.SimplifiedCaption);

        Assert.Contains(
            Named(picker, "TextBlock"),
            element => Attribute(element, "Text") == PopulationPickerViewModel.FilterHeader);
        Assert.Contains(
            Named(picker, "TextBlock"),
            element => Attribute(element, "Text") == PopulationPickerViewModel.FilterPlaceholder);
        Assert.Contains(
            Named(picker, "CheckBox"),
            element => Attribute(element, "Content") == PopulationPickerViewModel.FrequentlyUsedCaption);
        Assert.Contains(
            Named(picker, "TextBlock"),
            element => Attribute(element, "Text") == PopulationPickerViewModel.SimplifiedCaption);
    }

    [Fact]
    public void TheFilterBoxUpdatesOnEveryKeystroke()
    {
        XElement box = One(Picker(), "TextBox", Xaml + "Name", "FilterBox");

        // "live filter on every keystroke" (§B.1.1 item 2). The WPF default is LostFocus.
        Assert.Equal("{Binding FilterText, UpdateSourceTrigger=PropertyChanged}", Attribute(box, "Text"));
    }

    [Fact]
    public void FrequentlyUsedOnlyIsDisabledUntilAStudyIsConnected()
    {
        XElement box = One(Picker(), "CheckBox", "Content", PopulationPickerViewModel.FrequentlyUsedCaption);

        Assert.Equal("{Binding CanFilterFrequentlyUsed}", Attribute(box, "IsEnabled"));
        Assert.Equal("{Binding FrequentlyUsedOnly, Mode=TwoWay}", Attribute(box, "IsChecked"));
    }

    [Fact]
    public void SimplifiedPutsItsCaptionToTheLeftOfTheBox()
    {
        // Alignment = taLeftJustify, through the theme's QsCaptionLeftCheckBox rather than a
        // hand-rolled FlowDirection: RightToLeft mirrors the whole visual subtree, including the
        // tick, which the default template draws as a Path.
        XElement box = Assert.Single(
            Named(Picker(), "CheckBox"),
            element => Attribute(element, "Style") == "{StaticResource QsCaptionLeftCheckBox}");

        Assert.Equal("{Binding Simplified, Mode=TwoWay}", Attribute(box, "IsChecked"));

        // A local value beats a style setter, so setting either of these here would reinstate the
        // backwards checkmark the style exists to fix.
        Assert.Null(Attribute(box, "FlowDirection"));
        Assert.Null(Attribute(box, "VerticalAlignment"));

        XElement caption = Assert.Single(box.Descendants(Wpf + "TextBlock"));

        Assert.Equal("LeftToRight", Attribute(caption, "FlowDirection"));
        Assert.Equal(PopulationPickerViewModel.SimplifiedCaption, Attribute(caption, "Text"));
    }

    // ---------------------------------------------------------------------------------------
    // The list
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void TheListAlternatesAndUsesTheSharedRowChrome()
    {
        XElement list = Assert.Single(Named(Picker(), "ListBox"));

        Assert.Equal("2", Attribute(list, "AlternationCount"));
        Assert.Equal("{StaticResource QsPopulationItem}", Attribute(list, "ItemContainerStyle"));
        Assert.Equal("{Binding Populations}", Attribute(list, "ItemsSource"));
        Assert.Equal("{Binding SelectedPopulation, Mode=TwoWay}", Attribute(list, "SelectedItem"));
        Assert.Equal("Stretch", Attribute(list, "HorizontalContentAlignment"));
    }

    [Fact]
    public void EnterAndDoubleClickBothPreparePopulation()
    {
        XDocument picker = Picker();

        XElement key = Assert.Single(Named(picker, "KeyBinding"));

        Assert.Equal("Enter", Attribute(key, "Key"));
        Assert.Equal("{Binding PreparePopulationCommand}", Attribute(key, "Command"));

        // The double click is an attached behaviour and NOT a second InputBinding, which is the
        // whole of Phase 5's fifth defect: this case used to assert a MouseBinding with the right
        // gesture and the right command, and passed for as long as double-clicking a population did
        // nothing whatsoever. ListBoxItem marks the selecting mouse-down handled, so the event never
        // reaches the ListBox whose collection holds the binding. Ui/Input/DoubleClickTests.cs
        // raises the real events; this case only forbids the spelling coming back.
        XElement list = Assert.Single(Named(picker, "ListBox"));

        Assert.Equal("{Binding PreparePopulationCommand}", Attribute(list, Input + "DoubleClick.Command"));
        Assert.Empty(Named(picker, "MouseBinding"));
    }

    [Fact]
    public void TheRowHasTheThreeRunsAndTheWrappedHelpText()
    {
        XElement template = Assert.Single(Picker().Descendants(Wpf + "DataTemplate"));

        List<XElement> runs = [.. template.Descendants(Wpf + "TextBlock")];

        Assert.Equal(4, runs.Count);

        XElement code = runs[0];
        XElement title = runs[1];
        XElement group = runs[2];
        XElement help = runs[3];

        Assert.Equal("{Binding ProcId}", Attribute(code, "Text"));
        Assert.Equal("{StaticResource PopulationCodeRun}", Attribute(code, "Style"));

        Assert.Equal("{Binding Title}", Attribute(title, "Text"));
        Assert.Equal("Bold", Attribute(title, "FontWeight"));
        Assert.Equal("CharacterEllipsis", Attribute(title, "TextTrimming"));

        Assert.Equal("{Binding Group}", Attribute(group, "Text"));
        Assert.Equal("{StaticResource PopulationGroupRun}", Attribute(group, "Style"));

        Assert.Equal("{Binding HelpText}", Attribute(help, "Text"));
        Assert.Equal("{StaticResource PopulationHelpRun}", Attribute(help, "Style"));
        Assert.Equal("Wrap", Attribute(help, "TextWrapping"));
    }

    [Fact]
    public void TheGroupIsDrawnInsideTheTitleColumn()
    {
        // §B.1.1: "drawn INSIDE column 1, right-aligned" - PaintContents shortens the title's
        // rectangle by the group's width rather than giving the group a column of its own.
        XElement template = Assert.Single(Picker().Descendants(Wpf + "DataTemplate"));

        XElement outer = Assert.Single(template.Elements(Wpf + "Grid"));

        List<XElement> outerColumns =
            [.. outer.Elements(Wpf + "Grid.ColumnDefinitions").Single().Elements(Wpf + "ColumnDefinition")];

        Assert.Equal(2, outerColumns.Count);
        Assert.Equal("32", Attribute(outerColumns[0], "MinWidth"));

        // One width for the whole list, as the VCL grid's real column 0 has, or the titles stop
        // lining up as soon as two ids differ in length.
        Assert.Equal("PopulationId", Attribute(outerColumns[0], "SharedSizeGroup"));
        Assert.Equal(
            "True",
            Attribute(Assert.Single(Named(Picker(), "ListBox")), "Grid.IsSharedSizeScope"));

        XElement inner = Assert.Single(outer.Elements(Wpf + "Grid"));

        Assert.Equal("1", Attribute(inner, "Grid.Column"));
        Assert.Contains(inner.Descendants(Wpf + "TextBlock"), run => Attribute(run, "Text") == "{Binding Group}");
    }

    [Fact]
    public void TheCodeAndGroupRunsTurnWhiteWithTheSelection()
    {
        // §B.1.1: selected, all three runs turn #FFFFFF. Title and help text inherit it from
        // QsPopulationItem; these two carry an explicit colour and have to be told.
        XDocument picker = Picker();

        foreach ((string key, string restingBrush) in new[]
        {
            ("PopulationCodeRun", "QsCodeBrush"),
            ("PopulationGroupRun", "QsCategoryBrush"),
        })
        {
            XElement style = One(picker, "Style", Xaml + "Key", key);

            Assert.Contains(
                style.Elements(Wpf + "Setter"),
                setter => Attribute(setter, "Value") == $"{{StaticResource {restingBrush}}}");

            XElement trigger = Assert.Single(style.Descendants(Wpf + "DataTrigger"));

            Assert.Equal("True", Attribute(trigger, "Value"));
            Assert.Contains("IsSelected", Attribute(trigger, "Binding") ?? "", StringComparison.Ordinal);
            Assert.Contains(
                trigger.Elements(Wpf + "Setter"),
                setter => Attribute(setter, "Value") == "{StaticResource QsOnAccentBrush}");
        }
    }

    [Fact]
    public void TheGroupRunIsOnePointSmaller()
    {
        XElement style = One(
            Picker(),
            "Style",
            Xaml + "Key",
            "PopulationGroupRun");

        Assert.Contains(
            style.Elements(Wpf + "Setter"),
            setter => Attribute(setter, "Property") == "FontSize"
                && Attribute(setter, "Value") == "{StaticResource QsSmallFontSize}");
    }

    [Fact]
    public void HelpTextIsHiddenWhenTheRowIsCollapsedOrHasNothingToSay()
    {
        XElement style = One(
            Picker(),
            "Style",
            Xaml + "Key",
            "PopulationHelpRun");

        Assert.Equal("Collapsed", Attribute(Assert.Single(style.Elements(Wpf + "Setter")), "Value"));

        List<XElement> triggers = [.. style.Descendants(Wpf + "DataTrigger")];

        Assert.Equal(2, triggers.Count);
        Assert.Equal("{Binding IsExpanded}", Attribute(triggers[0], "Binding"));
        Assert.Equal("Visible", Attribute(Assert.Single(triggers[0].Elements(Wpf + "Setter")), "Value"));

        // Second, so it wins: a row with no HelpText adds no height in the VCL either.
        Assert.Equal("{Binding HelpText}", Attribute(triggers[1], "Binding"));
        Assert.Equal("", Attribute(triggers[1], "Value"));
        Assert.Equal("Collapsed", Attribute(Assert.Single(triggers[1].Elements(Wpf + "Setter")), "Value"));
    }

    [Fact]
    public void TheEmptyStateReplacesTheVclsVanishingGrid()
    {
        XElement message = Assert.Single(
            Named(Picker(), "TextBlock"),
            element => Attribute(element, "Text") == "{Binding EmptyStateText}");

        Assert.Equal(
            "{Binding IsEmpty, Converter={conv:BoolToVisibilityConverter}}",
            Attribute(message, "Visibility"));

        // It sits over the list, so a stray click cannot land on it instead of a row.
        Assert.Equal("False", Attribute(message, "IsHitTestVisible"));
    }

    // ---------------------------------------------------------------------------------------
    // The SQL preview
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void ThePreviewIsAReadOnlyUnwrappedCodePane()
    {
        XElement preview = Assert.Single(
            Named(Picker(), "TextBox"),
            element => Attribute(element, Xaml + "Name") != "FilterBox");

        Assert.Equal("{Binding SqlPreview, Mode=OneWay}", Attribute(preview, "Text"));
        Assert.Equal("True", Attribute(preview, "IsReadOnly"));
        Assert.Equal("NoWrap", Attribute(preview, "TextWrapping"));
        Assert.Equal("{StaticResource QsCodeFontFamily}", Attribute(preview, "FontFamily"));
        Assert.Equal("{StaticResource QsSmallFontSize}", Attribute(preview, "FontSize"));
    }

    [Fact]
    public void ThePreviewAndItsSplitterAreGatedTogether()
    {
        XDocument picker = Picker();

        const string Gate = "{Binding IsSqlPreviewVisible, Converter={conv:BoolToVisibilityConverter}}";

        XElement splitter = Assert.Single(Named(picker, "GridSplitter"));

        Assert.Equal("9", Attribute(splitter, "Height"));
        Assert.Equal(Gate, Attribute(splitter, "Visibility"));

        XElement preview = Assert.Single(
            Named(picker, "TextBox"),
            element => Attribute(element, Xaml + "Name") != "FilterBox");

        Assert.Equal(Gate, Attribute(preview, "Visibility"));
    }

    [Fact]
    public void TheSplitterAndPreviewRowsAreAutoSoTheyVanishTogether()
    {
        // The gate is denied by default (§I.9), so those two rows must leave no hole behind. Auto
        // rows do that; a fixed height would reserve 113 px of nothing for every user.
        XElement grid = Assert.Single(Picker().Root!.Elements(Wpf + "Grid"));

        List<XElement> rows =
            [.. grid.Elements(Wpf + "Grid.RowDefinitions").Single().Elements(Wpf + "RowDefinition")];

        Assert.Equal(6, rows.Count);
        Assert.Equal("Auto", Attribute(rows[4], "Height"));
        Assert.Equal("Auto", Attribute(rows[5], "Height"));
    }

    [Fact]
    public void AnAutoRowWithACollapsedChildTakesNoHeightAtAll()
    {
        // The WPF invariant the two rows above rely on, checked rather than assumed.
        (double hidden, double shown) = StaTestRunner.Run(() =>
        {
            Grid grid = new();

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBox preview = new() { MinHeight = 104, Visibility = Visibility.Collapsed };

            Grid.SetRow(preview, 0);
            grid.Children.Add(preview);

            grid.Measure(new Size(300, double.PositiveInfinity));
            grid.Arrange(new Rect(new Point(0, 0), grid.DesiredSize));

            double collapsedHeight = grid.RowDefinitions[0].ActualHeight;

            preview.Visibility = Visibility.Visible;

            grid.Measure(new Size(300, double.PositiveInfinity));
            grid.Arrange(new Rect(new Point(0, 0), grid.DesiredSize));

            return (collapsedHeight, grid.RowDefinitions[0].ActualHeight);
        });

        Assert.Equal(0, hidden);
        Assert.True(shown >= 104, $"An open preview should be at least 104 units tall, but it was {shown}.");
    }
}
