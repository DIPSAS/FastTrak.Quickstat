using System.Globalization;
using System.IO;
using QuickStat.Collectors;
using QuickStat.Tests.Configuration;
using QuickStat.ViewModels;
using Xunit;

namespace QuickStat.Tests.Ui.Collections;

/// <summary>
/// The order of the check list, which is the column order of every exported file.
/// </summary>
/// <remarks>
/// <para>
/// PORT-PLAN.md §6 calls this out as parity that must not drift, and it is the one thing on this tab
/// a customer's scripts depend on: <c>cbDataCollector.Sorted := true</c>
/// (<c>MainQuickStat.pas:400</c>) is set before <c>AfterLogin</c> fills the list, and
/// <c>actCollectDataExecute</c> (<c>:650-671</c>) walks <c>Items</c> from index 0, so the sorted list
/// <em>is</em> the column order.
/// </para>
/// <para>
/// <b>The rule is not <see cref="StringComparer.Ordinal"/>, and these cases exist to keep it from
/// being "corrected" back to one.</b> <c>05-ui-spec.md</c> §G.5, PORT-PLAN.md §6 and
/// <c>07-ui-contracts.md</c> §5 all prescribe an ordinal sort on the grounds that it "keeps the
/// <c>^ </c>-prefixed demographic collectors first". It does the exact opposite - see
/// <see cref="AnOrdinalSortWouldPutTheDemographicElementsLast"/> - because U+005E sits between
/// <c>'Z'</c> and <c>'a'</c> and every other title starts with a capital letter.
/// </para>
/// <para>
/// Ground truth is <c>Docs/Screenshots/QuickStat bilde 2.png</c>, the shipped build's own
/// <c>Collections</c> tab.
/// </para>
/// </remarks>
public class CollectorOrderTests
{
    /// <summary>
    /// Every data-element title legible in <c>Docs/Screenshots/QuickStat bilde 2.png</c>, in the
    /// order the shipped build lists them.
    /// </summary>
    /// <remarks>
    /// The screenshot is of build 19.8.14.477, whose registry is a subset of today's, so this is
    /// compared as a <em>subsequence</em>: the titles it does not show are skipped, the ones it does
    /// must appear in this order. The <c>(siste)</c> suffixes are the ones
    /// <see cref="CollectorTitle"/> appends.
    /// </remarks>
    private static readonly string[] ShippedOrder =
    [
        "^ Alder",
        "^ Dødsår",
        "^ Fødselmåned",
        "^ Fødselsår",
        "^ Gruppe / avdeling nå",
        "^ Gruppe / avdeling ved død",
        "^ Institusjon / sted",
        "^ Institusjon / sted ved død",
        "^ Kjønn",
        "^ Postnummer",
        "^ Statuskode",
        "Antropometri: Høyde og vekt (siste)",
        "Labdata: Alle med høy konfidens",
        "Labdata: Alle med lav konfidens",
        "Labdata: Alle med middels konfidens",
        "Labdata: Anemi (siste)",
        "Labdata: Antall prøver siste 12 mnd",
        "Labdata: Antall prøver siste 24 mnd (2 år)",
        "Labdata: Antall prøver siste 3 mnd",
        "Labdata: Antall prøver siste 6 mnd",
        "Labdata: Antall prøver siste 60 mnd (5 år)",
        "Labdata: CRP (siste)",
        "Labdata: Diabetes (siste)",
        "Labdata: Digitalis (siste)",
        "Labdata: Glukose (siste)",
        "Labdata: Hjertesviktrelaterte labdata (siste)",
        "Labdata: Hyperparatyreoidisme (siste)",
        "Labdata: INR fra labarket (siste)",
        "Labdata: Leverstatus (siste)",
        "Labdata: Lipider (siste)",
        "Labdata: Nyrefunksjon (siste)",
        "Labdata: Tyreoidea (siste)",
        "NDV: Basisdata (siste)",
    ];

    /// <summary>Forces a culture for the duration of a case, and puts it back afterwards.</summary>
    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

        internal CultureScope(string name) => CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);

        public void Dispose() => CultureInfo.CurrentCulture = _previous;
    }

    [Fact]
    public void TheCheckListMatchesTheOrderTheShippedBuildShows()
    {
        // nb-NO because that is what the shipped build runs on: Sorted := true puts LBS_SORT on the
        // Win32 list box, which orders with CompareStringW(LOCALE_USER_DEFAULT, NORM_IGNORECASE).
        using CultureScope culture = new("nb-NO");

        List<string> sorted = SortedTitles("KORTTID");
        List<string> visible = [.. sorted.Where(title => ShippedOrder.Contains(title, StringComparer.Ordinal))];

        Assert.Equal(ShippedOrder, visible);
    }

    [Fact]
    public void BokmaalAndNynorskAgree()
    {
        // Worth pinning because the rule reads the machine's culture and Norwegian installations are
        // both: the development machine is nn-NO and the shipped build's users are on nb-NO. The two
        // share one collation, so the column order of an export does not depend on which.
        List<string> bokmaal;
        List<string> nynorsk;

        using (new CultureScope("nb-NO"))
        {
            bokmaal = SortedTitles("KORTTID");
        }

        using (new CultureScope("nn-NO"))
        {
            nynorsk = SortedTitles("KORTTID");
        }

        Assert.Equal(bokmaal, nynorsk);
    }

    [Theory]
    [InlineData("nb-NO")]
    [InlineData("nn-NO")]
    [InlineData("en-US")]
    public void TheDemographicElementsComeFirstWhateverTheMachinesCultureIs(string cultureName)
    {
        // The whole point of the "^ " prefix (05-ui-spec.md §B.2: "a sort hack").  Punctuation sorts
        // before letters in every linguistic collation, so this holds in all three.
        using CultureScope culture = new(cultureName);

        List<string> sorted = SortedTitles("KORTTID");
        int demographics = sorted.Count(title => title.StartsWith("^ ", StringComparison.Ordinal));

        Assert.Equal(11, demographics);
        Assert.All(sorted.Take(demographics), title => Assert.StartsWith("^ ", title, StringComparison.Ordinal));
    }

    [Fact]
    public void AnOrdinalSortWouldPutTheDemographicElementsLast()
    {
        // Not a test of the port - a test of the rule the specification asks for, so that the reason
        // for diverging from it is in the suite rather than only in a comment.  '^' is U+005E: above
        // 'Z' (U+005A) and below 'a' (U+0061), and every non-demographic title starts with a capital.
        List<string> ordinal = [.. Titles("KORTTID").OrderBy(static title => title, StringComparer.Ordinal)];

        Assert.DoesNotContain("^ ", ordinal[0], StringComparison.Ordinal);
        Assert.StartsWith("^ ", ordinal[^1], StringComparison.Ordinal);
        Assert.Equal(11, ordinal.TakeLast(11).Count(title => title.StartsWith("^ ", StringComparison.Ordinal)));
    }

    [Fact]
    public void TheRuleIsCaseInsensitiveAndPlacesPunctuationFirst()
    {
        using CultureScope culture = new("nb-NO");

        List<string> sorted =
        [
            .. new[] { "beta", "Ålesund", "^ Zulu", "Alfa", "alfa", "Øst", "Æra" }
                .OrderBy(static title => title, DataElementViewModel.TitleOrder),
        ];

        // "^ Zulu" first because of the punctuation; alfa/Alfa adjacent because the fold is
        // case-insensitive; æ ø å last because this is Norwegian collation (§G.5).
        Assert.Equal("^ Zulu", sorted[0]);
        Assert.Equal(["Alfa", "alfa"], sorted.Skip(1).Take(2).Order(StringComparer.Ordinal));
        Assert.Equal("beta", sorted[3]);
        Assert.Equal(["Æra", "Øst", "Ålesund"], sorted.Skip(4));
    }

    [Fact]
    public void TheComparerFollowsTheMachinesCultureRatherThanFreezingOne()
    {
        // StringComparer.CurrentCultureIgnoreCase captures CultureInfo.CurrentCulture when it is
        // read, so TitleOrder has to be a property.  A cached static would freeze whatever culture
        // happened to be current when the type was initialised.
        //
        // These two are real titles, and the only pair in the whole registry whose order depends on
        // the machine: Norwegian sorts "å" after "z", English treats it as a variant of "a".  So the
        // shipped column order is the Norwegian one, exactly as it is in the Delphi - LBS_SORT reads
        // LOCALE_USER_DEFAULT too.
        string[] titles = ["Medisin: Antall på utvalgte ATC-grupper", "Medisin: Antall per behandlingstype"];

        using (new CultureScope("nb-NO"))
        {
            Assert.Equal(
                ["Medisin: Antall per behandlingstype", "Medisin: Antall på utvalgte ATC-grupper"],
                titles.OrderBy(static t => t, DataElementViewModel.TitleOrder));
        }

        using (new CultureScope("en-US"))
        {
            Assert.Equal(
                ["Medisin: Antall på utvalgte ATC-grupper", "Medisin: Antall per behandlingstype"],
                titles.OrderBy(static t => t, DataElementViewModel.TitleOrder));
        }
    }

    [Theory]
    [InlineData("nb-NO")]
    [InlineData("nn-NO")]
    public void TheComparerReproducesTheShippedListBoxOnAWholeCheckList(string cultureName)
    {
        // The strongest evidence available for this rule, and the test that would have caught the
        // defect Phase 5 found. DelphiCheckList.NDV.txt is not derived from the port: it was read out
        // of the running 22.12.21.547 build with LB_GETTEXT, item by item, while it was connected to
        // a real database with the NDV study selected - so it is literally what LBS_SORT produced,
        // 213 elements including the 111 form classes Report.GetFormClasses returned.
        //
        // Sorting a shuffled copy back into that order exercises the comparer on real punctuation
        // collisions ("Skjema:" against "Skjema-alder:" and "Skjema-data:") that the 131-entry static
        // catalog does not contain - which is exactly why no earlier test could see the problem.
        using CultureScope culture = new(cultureName);

        List<string> expected = ShippedCheckList();
        List<string> shuffled = Shuffle(expected);

        Assert.Equal(expected, [.. shuffled.OrderBy(static title => title, DataElementViewModel.TitleOrder)]);
    }

    [Fact]
    public void TheFrameworksOwnComparerWouldMisplaceTheFormCountElements()
    {
        // Not a test of the port - a test of the rule the port deliberately does not use, so the
        // reason for the P/Invoke is in the suite and not only in a comment. Same intent as
        // AnOrdinalSortWouldPutTheDemographicElementsLast above.
        //
        // .NET collates with ICU since .NET 5; the list box collates with NLS. They disagree about
        // "-" against ":", so StringComparer.CurrentCultureIgnoreCase moves the five
        // "Skjema: Antall ..." elements from positions 41-45 to the end of the list - and, because
        // column order is insertion order, five columns to the right-hand edge of every export.
        using CultureScope culture = new("nb-NO");

        List<string> expected = ShippedCheckList();
        List<string> icu = [.. Shuffle(expected).OrderBy(static title => title, StringComparer.CurrentCultureIgnoreCase)];

        Assert.NotEqual(expected, icu);

        // Stated exactly, because the exact shape is the finding: the shipped build puts the five
        // "Skjema: Antall ..." elements together immediately before the first "Skjema-alder:", and
        // ICU puts them at the very end - so everything in between shifts up five places too.
        Assert.All(
            expected.Skip(40).Take(5),
            title => Assert.StartsWith("Skjema: Antall", title, StringComparison.Ordinal));
        Assert.StartsWith("Skjema-alder:", expected[45], StringComparison.Ordinal);

        Assert.All(
            icu.TakeLast(5),
            title => Assert.StartsWith("Skjema: Antall", title, StringComparison.Ordinal));
    }

    /// <summary>
    /// The check list as the shipped build showed it, one title per line, in list-box order.
    /// </summary>
    /// <returns>213 titles.</returns>
    private static List<string> ShippedCheckList() =>
    [
        .. File.ReadAllLines(Path.Combine(
            RepositoryFiles.Root,
            "QuickStat.Tests",
            "Ui",
            "Collections",
            "DelphiCheckList.NDV.txt")),
    ];

    /// <summary>
    /// Reorders deterministically, so the sort has real work to do and the test cannot pass by
    /// accident of the input already being sorted.
    /// </summary>
    /// <param name="titles">The titles.</param>
    /// <returns>The same titles, in a fixed non-sorted order.</returns>
    private static List<string> Shuffle(List<string> titles)
    {
        List<string> shuffled = [.. titles];
        for (int i = 0; i < shuffled.Count; i++)
        {
            int j = (i * 7919) % shuffled.Count;
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return shuffled;
    }

    private static IEnumerable<string> Titles(string studyName) =>
        CollectorRegistryBuilder
            .Build(
                studyName,
                [],
                new CollectorAvailabilityContext
                {
                    StudyName = studyName,
                    StudyId = 42,
                    ResolvedDatabaseObjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                })
            .Select(static collector => collector.Descriptor.Title);

    private static List<string> SortedTitles(string studyName) =>
        [.. Titles(studyName).OrderBy(static title => title, DataElementViewModel.TitleOrder)];
}
