using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using QuickStat.Domain.Anonymisation;
using QuickStat.Tests.Ui.Dialogs;
using QuickStat.Tests.Ui.Shell;
using QuickStat.ViewModels;
using QuickStat.Views;
using Xunit;

namespace QuickStat.Tests.Ui.Collections;

/// <summary>
/// The three <c>Export options</c> radio buttons, driven as radio buttons rather than as a
/// view-model property.
/// </summary>
/// <remarks>
/// <para>
/// PORT-PLAN.md acceptance criterion 5 names one span as untested: <em>radio →
/// <see cref="IIdentificationPolicy"/> → grid columns → export options → writer</em>. Every link had
/// unit tests; the <b>first</b> link had none that went through the real control, because
/// <c>CollectionsTabViewModelTests</c> assigns
/// <see cref="CollectionsTabViewModel.Identification"/> directly and the markup tests compare the
/// binding's text against itself. This class closes that link, and it needs no database - the other
/// half of the criterion, a real recovered national id reaching a real file, is
/// <c>Live/FullyIdentifiedExportTests</c>.
/// </para>
/// <para>
/// <b>Realising the view is not optional.</b> A binding compiled into BAML is unattached until the
/// element is in a presentation source, so an unrealised control evaluates nothing and every
/// assertion here would pass against a view whose bindings had been deleted. <c>RealisedWindow</c>
/// records that experiment; this class reuses it.
/// </para>
/// <para>
/// What makes this worth a test rather than a reading of the XAML: <c>IsChecked</c> is a
/// <em>two-way</em> binding on a group of three, so checking one un-checks the other two and each of
/// those raises <c>ConvertBack</c> with <see langword="false"/>. If
/// <c>EnumToBooleanConverter.ConvertBack</c> returned anything but <c>Binding.DoNothing</c> there,
/// the mode would be written twice and the second write would lose - and the visible symptom would
/// be an export that quietly disagrees with the radio the user pressed. That is precisely the
/// display-versus-export divergence §7.2 exists to remove.
/// </para>
/// </remarks>
[Collection(WpfApplicationCollection.Name)]
public class IdentificationRadioTests
{
    private const string FullyIdentified = "Fully identified patients";
    private const string PidOnly = "Identified with PID only";
    private const string RandomPids = "Generate new random PIDs";

    private readonly WpfApplicationFixture _wpf;

    public IdentificationRadioTests(WpfApplicationFixture wpf) => _wpf = wpf;

    [Fact]
    public void TheFullyIdentifiedRadioSetsTheSharedPolicy()
    {
        using ServiceProvider provider = ShellCompositionTests.Build();

        CollectionsTabViewModel collections = provider.GetRequiredService<CollectionsTabViewModel>();
        IIdentificationPolicy policy = provider.GetRequiredService<IIdentificationPolicy>();

        // The default, so the assertion below is a change and not a coincidence.
        Assert.Equal(PersonIdentification.PersonIdOnly, policy.Mode);

        _wpf.Run(() => Check(collections, FullyIdentified));

        Assert.Equal(PersonIdentification.Full, policy.Mode);
        Assert.Equal(PersonIdentification.Full, collections.Identification);

        // The derived half: what the exporter will be told to write. Asserted here rather than
        // trusted, because this is the one place where a radio press and a file's columns meet.
        Assert.True(policy.Columns.IncludesNationalId);
    }

    [Fact]
    public void EachRadioSetsItsOwnModeAndTheGroupNeverWritesTwice()
    {
        using ServiceProvider provider = ShellCompositionTests.Build();

        CollectionsTabViewModel collections = provider.GetRequiredService<CollectionsTabViewModel>();
        IIdentificationPolicy policy = provider.GetRequiredService<IIdentificationPolicy>();

        List<PersonIdentification> raised = [];

        policy.ModeChanged += (_, mode) => raised.Add(mode);

        // Full last, so the sequence ends on the mode the criterion cares about and every transition
        // has had to un-check a sibling on the way.
        _wpf.Run(() =>
        {
            Check(collections, RandomPids);
            Check(collections, PidOnly);
            Check(collections, FullyIdentified);
        });

        // Three presses, three events. A fourth would mean a sibling's ConvertBack had written the
        // mode back on being un-checked.
        Assert.Equal(
            [PersonIdentification.RandomPersonId, PersonIdentification.PersonIdOnly, PersonIdentification.Full],
            raised);
    }

    /// <summary>Presses one radio in a realised <c>CollectionsTabView</c> and lets the binding run.</summary>
    /// <param name="collections">The view-model the shell would give the view.</param>
    /// <param name="content">The radio's caption, which is how the view names it - none is x:Named.</param>
    private static void Check(CollectionsTabViewModel collections, string content)
    {
        CollectionsTabView view = new() { DataContext = collections };

        RealisedWindow.RunControl(view, realised =>
        {
            realised.UpdateLayout();

            RadioButton radio = FindRadio(realised, content);

            // The user's gesture, as far as the binding is concerned: ToggleButton raises Checked and
            // the two-way binding calls ConvertBack. Clicking would additionally test WPF's input
            // routing, which is not this port's code.
            radio.IsChecked = true;
        });
    }

    private static RadioButton FindRadio(DependencyObject root, string content)
    {
        RadioButton? found = Find(root);

        Assert.True(found is not null, $"No RadioButton captioned \"{content}\" in CollectionsTabView.");

        return found;

        RadioButton? Find(DependencyObject node)
        {
            if (node is RadioButton candidate && Equals(candidate.Content, content))
            {
                return candidate;
            }

            int children = VisualTreeHelper.GetChildrenCount(node);

            for (int index = 0; index < children; index++)
            {
                if (Find(VisualTreeHelper.GetChild(node, index)) is { } match)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
