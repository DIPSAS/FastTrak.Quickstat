using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using QuickStat.Controls;
using QuickStat.Tests.Ui.Dialogs;
using QuickStat.Tests.Ui.Shell;
using QuickStat.ViewModels;
using QuickStat.Views;
using Xunit;

namespace QuickStat.Tests.Ui.Collections;

/// <summary>
/// Every word the Collections tab puts on screen, and which radio starts checked.
/// </summary>
/// <remarks>
/// <para>
/// <b>Checklist items 3.1 and 3.2</b> (<c>Docs/Port/08-parity-checklist.md</c>), which were manual
/// only because nobody had written the assertion. They are literal strings in a realised view, so a
/// person reading them off the screen and a test reading them off the visual tree are doing the same
/// work - and the test does it on every build.
/// </para>
/// <para>
/// <b>The expected strings are transcribed here, not read from the view-model's constants.</b>
/// <see cref="CollectionsTabViewModel.InfoParagraph"/> and its siblings are one copy of the wording;
/// quoting them would assert that a field equals itself and would go on passing after the wording
/// changed. These came from <c>05-ui-spec.md</c> §B.2 and the <c>.dfm</c> captions behind it, which
/// is the same source a person walking the checklist would compare against.
/// </para>
/// <para>
/// What this deliberately does not cover: the wording being <em>right</em>. A test can hold a string
/// still; only the specification says what it should be. If §B.2 is ever corrected, this fails and
/// that is the intended signal.
/// </para>
/// </remarks>
[Collection(WpfApplicationCollection.Name)]
public class CollectionsTabCaptionTests
{
    private const string ElementsHeader = "Select data elements";
    private const string ExportOptionsHeader = "Export options";

    /// <summary>The wrapped paragraph of §B.2, <b>with two spaces after <c>process.</c></b></summary>
    private const string InfoParagraph =
        "Select data elements from the list below, and click \"Collect data\" at the bottom to start "
        + "the process.  Depending on what you select, this will take some time!";

    private const string FullyIdentified = "Fully identified patients";
    private const string PidOnly = "Identified with PID only";
    private const string RandomPids = "Generate new random PIDs";
    private const string TimestampBox = "Export timestamp for every data element";

    private readonly WpfApplicationFixture _wpf;

    /// <summary>Takes the assembly's one application; the view names theme keys.</summary>
    /// <param name="wpf">Injected by xUnit from <see cref="WpfApplicationCollection"/>.</param>
    public CollectionsTabCaptionTests(WpfApplicationFixture wpf)
    {
        ArgumentNullException.ThrowIfNull(wpf);

        _wpf = wpf;
    }

    [Fact]
    public void TheTwoTealHeadersReadWhatTheSpecificationSays()
    {
        // 3.1 and the first half of 3.2.  Read off the bar's own TextBlock rather than off
        // SectionHeader.Header, so a template that stopped rendering the heading would fail here.
        string[] headings = Realise(view =>
            VisualTree.Descendants<SectionHeader>(view)
                .Select(HeadingOf)
                .ToArray());

        Assert.Equal([ElementsHeader, ExportOptionsHeader], headings);
    }

    [Fact]
    public void TheParagraphIsVerbatimIncludingTheTwoSpaces()
    {
        // 3.1.  Exactly one element in the whole tab carries it, so this is also a check that the
        // paragraph has not been duplicated into a tooltip or a second panel.
        string paragraph = Realise(view =>
            Assert.Single(
                VisualTree.Descendants<TextBlock>(view).Select(block => block.Text),
                text => text.StartsWith("Select data elements from", StringComparison.Ordinal)));

        Assert.Equal(InfoParagraph, paragraph);

        // The double space is what the checklist item singles out and it is invisible in the literal
        // above, so it is stated once more in a form a reader can count.
        Assert.Contains("the process.  Depending on what", paragraph, StringComparison.Ordinal);
    }

    [Fact]
    public void TheThreeRadiosReadTheirCaptionsInOrder()
    {
        // 3.2.  Order matters: §B.2 lists them most-identifying first, which is the opposite of the
        // safe-by-default reading, and it is what the Delphi shows.
        string[] captions = Realise(view => Radios(view).Select(radio => (string)radio.Content).ToArray());

        Assert.Equal([FullyIdentified, PidOnly, RandomPids], captions);
    }

    [Fact]
    public void IdentifiedWithPidOnlyIsTheOneCheckedOnStartUp()
    {
        // 3.2, and the one assertion here that is not a string: the middle radio starts checked
        // because IIdentificationPolicy starts at PersonIdOnly and the converter binding shows it.
        // Which radio the middle one *is* comes from the order case above; this is the state.
        // IdentificationRadioTests proves the reverse direction - pressing one moves the policy.
        bool?[] states = Realise(view => Radios(view).Select(radio => radio.IsChecked).ToArray());

        Assert.Equal([false, true, false], states);
    }

    [Fact]
    public void TheTimestampBoxReadsItsCaptionAndStartsClear()
    {
        // 3.2.  With no data elements loaded the list is empty, so the only CheckBox in the tab is
        // this one - which is itself worth asserting, because a stray box would be a second way to
        // change an export.
        (string Caption, bool? State) box = Realise(view =>
        {
            CheckBox only = Assert.Single(VisualTree.Descendants<CheckBox>(view));

            return ((string)only.Content, only.IsChecked);
        });

        Assert.Equal(TimestampBox, box.Caption);
        Assert.False(box.State);
    }

    /// <summary>The three identification radios, in document order.</summary>
    /// <param name="view">A realised tab.</param>
    /// <returns>The radios.</returns>
    private static RadioButton[] Radios(CollectionsTabView view) =>
        VisualTree.Descendants<RadioButton>(view).ToArray();

    /// <summary>Realises the real tab against the container's own view-model and reads something off it.</summary>
    /// <typeparam name="T">What the body returns.</typeparam>
    /// <param name="body">Runs against the live tree.</param>
    /// <returns>Whatever <paramref name="body"/> returned.</returns>
    /// <remarks>
    /// The view-model comes from the shell's container rather than from <c>new</c>, so the radio
    /// group is bound to the same <c>IIdentificationPolicy</c> singleton the exporter reads. A
    /// hand-built view-model would answer the caption questions correctly and the default-checked
    /// question by accident.
    /// </remarks>
    private T Realise<T>(Func<CollectionsTabView, T> body)
    {
        using ServiceProvider provider = ShellCompositionTests.Build();

        CollectionsTabViewModel collections = provider.GetRequiredService<CollectionsTabViewModel>();

        return _wpf.Run(() =>
        {
            CollectionsTabView view = new() { DataContext = collections };
            T result = default!;

            RealisedWindow.RunControl(view, realised =>
            {
                realised.UpdateLayout();

                result = body(realised);
            });

            return result;
        });
    }

    /// <summary>The text the teal bar actually paints, out of the control template.</summary>
    /// <param name="header">A realised section header.</param>
    /// <returns>The heading string.</returns>
    private static string HeadingOf(SectionHeader header) =>
        VisualTree.Descendants<TextBlock>(header).First().Text;
}
