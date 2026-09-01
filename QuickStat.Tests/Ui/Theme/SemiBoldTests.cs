using System.IO;
using System.Windows;
using System.Xml.Linq;
using QuickStat.Controls.Dataset;
using QuickStat.Tests.Configuration;
using Xunit;

namespace QuickStat.Tests.Ui.Theme;

/// <summary>
/// Nothing in this application may ask for <c>SemiBold</c>, because nothing in this application
/// draws it.
/// </summary>
/// <remarks>
/// <para>
/// PORT-PLAN.md §8.11 (14). Measured off the running window at 100 % scale, a run declared
/// <c>SemiBold</c> came back <em>pixel for pixel</em> identical to the same run with no weight at
/// all — ink mass 52 570 and drawn width 92 px for both, against 80 839 / 93 px for <c>Bold</c>.
/// The same machine renders all three weights distinctly <em>outside</em> the application and the
/// family carries a real SemiBold face, so the mechanism is not pinned; the outcome is, three ways.
/// </para>
/// <para>
/// That makes every <c>SemiBold</c> declaration a silent no-op, and four of them were parity losses:
/// §F.2 says the selected tab caption, the wordmark, the <c>Progress</c> header and the grid's
/// header and current rows are <b>bold</b>, and all of them were drawing plain. They now say
/// <c>Bold</c>. Two others went the other way and were removed rather than promoted — the data
/// hint's first line and the error colour on <c>lblInfo</c> — because the Delphi draws those plain
/// and promoting them would have invented an emphasis rather than restored one.
/// </para>
/// <para>
/// The sweep reads the markup as XML, so a comment may still discuss the weight; only a declaration
/// fails. That is deliberate: the reasoning has to survive next to the code it explains.
/// </para>
/// </remarks>
public class SemiBoldTests
{
    private const string SemiBold = "SemiBold";

    private static readonly XNamespace Wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    private static string AppDirectory => Path.Combine(RepositoryFiles.Root, "QuickStat.App");

    private static IEnumerable<string> AppSources(string pattern) =>
        Directory.EnumerateFiles(AppDirectory, pattern, SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

    [Fact]
    public void NoMarkupDeclaresIt()
    {
        List<string> offenders = [];

        foreach (string file in AppSources("*.xaml"))
        {
            foreach (XElement element in XDocument.Load(file).Descendants())
            {
                foreach (XAttribute attribute in element.Attributes())
                {
                    if (attribute.Value == SemiBold)
                    {
                        offenders.Add($"{Path.GetFileName(file)}: <{element.Name.LocalName} {attribute.Name}=\"{SemiBold}\">");
                    }
                }
            }
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void NoCodeDeclaresIt()
    {
        List<string> offenders = [.. AppSources("*.cs")
            .Where(file => File.ReadAllText(file).Contains($"FontWeights.{SemiBold}", StringComparison.Ordinal))
            .Select(file => Path.GetFileName(file) ?? file)];

        Assert.Empty(offenders);
    }

    /// <summary>The dataset grid's header row and current row. §F.2, Delphi <c>[fsBold]</c>.</summary>
    [Fact]
    public void TheGridEmphasisesInBold() => Assert.Equal(
        FontWeights.Bold,
        (FontWeight)MatrixGrid.EmphasisFontWeightProperty.DefaultMetadata.DefaultValue!);

    /// <summary>The selected tab's caption. §F.2 again, and checklist 8.1.</summary>
    [Fact]
    public void TheSelectedTabCaptionIsBold()
    {
        XDocument theme = XDocument.Load(
            Path.Combine(AppDirectory, "Theme", "QuickStat.Styles.xaml"));

        List<XElement> weights = [.. theme.Descendants(Wpf + "Setter")
            .Where(setter => (string?)setter.Attribute("Property") == "FontWeight")];

        // Every declared weight in the theme, not merely the tab's: there are two, and both are the
        // grid header's emphasis under different names.
        Assert.NotEmpty(weights);
        Assert.All(weights, setter => Assert.Equal("Bold", (string?)setter.Attribute("Value")));
    }
}
