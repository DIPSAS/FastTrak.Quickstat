using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using QuickStat.Tests.Configuration;
using QuickStat.Tests.Ui.Dialogs;
using QuickStat.Views;
using Xunit;

namespace QuickStat.Tests.Ui;

/// <summary>
/// The banner's 32 x 32 icon, which is <b>not</b> the application icon.
/// </summary>
/// <remarks>
/// <para>
/// <c>MainQuickStat.dfm</c> carries <c>imgAppIcon.Picture.Data</c> as an inline <c>TIcon</c> stream
/// rather than as a file on disk, so there was nothing in the repository to port and the banner was
/// given <c>QuickStat_Icon.ico</c> - the executable's shell icon - instead. The two are different
/// pictures: the shell icon is a line chart on a grid and the banner's is an area chart on a grid,
/// which is close enough to look right and wrong enough to be noticed against a screenshot of the
/// shipped build. <c>05-ui-spec.md</c> §A.1 recorded the wrong file too.
/// </para>
/// <para>
/// The provenance case below is the load-bearing one: it re-extracts the icon from the <c>.dfm</c>
/// every run and compares it byte for byte with <c>QuickStat_Banner_Icon.ico</c>, so the file cannot
/// quietly become "an icon somebody drew" and the Delphi form stays the authority for what the
/// banner shows.
/// </para>
/// </remarks>
[Collection(WpfApplicationCollection.Name)]
public class AppBannerIconTests
{
    /// <summary>The banner's icon, extracted from the Delphi form.</summary>
    private const string BannerIconUri =
        "pack://application:,,,/QuickStat;component/Assets/QuickStat_Banner_Icon.ico";

    /// <summary>The executable's shell icon, on the window and the taskbar.</summary>
    private const string ShellIconUri =
        "pack://application:,,,/QuickStat;component/Assets/QuickStat_Icon.ico";

    private readonly WpfApplicationFixture _wpf;

    /// <summary>Takes the assembly's one application; the view names theme keys.</summary>
    /// <param name="wpf">Injected by xUnit from <see cref="WpfApplicationCollection"/>.</param>
    public AppBannerIconTests(WpfApplicationFixture wpf)
    {
        ArgumentNullException.ThrowIfNull(wpf);

        _wpf = wpf;
    }

    [Fact]
    public void TheBannerIconIsTheOneTheDelphiFormCarries()
    {
        byte[] embedded = IconInsideTheForm("imgAppIcon");
        byte[] onDisk = File.ReadAllBytes(Path.Combine(RepositoryFiles.Root, "QuickStat_Banner_Icon.ico"));

        Assert.Equal(embedded, onDisk);
    }

    [Fact]
    public void TheBannerAndTheWindowDoNotShareOneIcon()
    {
        // The defect in one assertion.  Both files exist, both load, and the banner used to resolve
        // to the same bytes as the title bar - which is not what the shipped build shows.
        (byte[] Banner, byte[] Shell) icons = _wpf.Run(() => (Resource(BannerIconUri), Resource(ShellIconUri)));

        Assert.NotEmpty(icons.Banner);
        Assert.NotEmpty(icons.Shell);
        Assert.NotEqual(icons.Banner, icons.Shell);
    }

    [Fact]
    public void TheBannerImageResolvesToTheBannerIcon()
    {
        // Realised, not read as XML: a BitmapImage written in markup is not decoded until the tree
        // is live, so a URI naming a resource that is not embedded fails here and nowhere earlier.
        (string? Uri, int Width, int Height) icon = _wpf.Run(() =>
        {
            AppBannerView banner = new();
            (string? Uri, int Width, int Height) found = default;

            RealisedWindow.RunControl(banner, _ =>
            {
                BitmapImage source = Assert.IsType<BitmapImage>(TheIcon(banner).Source);

                found = (source.UriSource?.ToString(), source.PixelWidth, source.PixelHeight);
            });

            return found;
        });

        Assert.Equal(BannerIconUri, icon.Uri);

        // DecodePixelWidth=32 against a multi-resolution .ico picks the 32 px frame rather than
        // scaling the 48 px one down.  §A.2: 32 x 32 at Margin 15,3,0,3.
        Assert.Equal(32, icon.Width);
        Assert.Equal(32, icon.Height);
    }

    /// <summary>Re-extracts one <c>TImage</c>'s icon from <c>MainQuickStat.dfm</c>.</summary>
    /// <param name="componentName">The <c>.dfm</c> component, e.g. <c>imgAppIcon</c>.</param>
    /// <returns>The <c>.ico</c> file's bytes.</returns>
    /// <remarks>
    /// A Delphi <c>TPicture</c> stream is the graphic's class name as a short string - one length
    /// byte, then <c>TIcon</c> - followed by the icon file verbatim, which is why the six bytes come
    /// off the front and the rest starts with the <c>ICONDIR</c> header <c>00 00 01 00</c>.
    /// </remarks>
    private static byte[] IconInsideTheForm(string componentName)
    {
        string form = File.ReadAllText(Path.Combine(RepositoryFiles.Root, "MainQuickStat.dfm"));

        int component = form.IndexOf($"object {componentName}: TImage", StringComparison.Ordinal);

        Assert.True(component >= 0, $"{componentName} is not in MainQuickStat.dfm.");

        int open = form.IndexOf("Picture.Data = {", component, StringComparison.Ordinal) + "Picture.Data = {".Length;
        int close = form.IndexOf('}', open);

        byte[] stream = Convert.FromHexString(
            string.Concat(form[open..close].Where(character => !char.IsWhiteSpace(character))));

        Assert.Equal(5, stream[0]);
        Assert.Equal("TIcon", Encoding.ASCII.GetString(stream, 1, 5));

        byte[] icon = stream[6..];

        // ICONDIR: reserved 0, type 1 (icon).  Proves the offset above rather than assuming it.
        Assert.Equal<byte[]>([0, 0, 1, 0], icon[..4]);

        return icon;
    }

    /// <summary>The bytes behind a pack URI, or a failure naming the URI that is missing.</summary>
    /// <param name="uri">A <c>pack://application:,,,</c> resource URI.</param>
    /// <returns>The resource's bytes.</returns>
    private static byte[] Resource(string uri)
    {
        StreamResourceInfo? info = Application.GetResourceStream(new Uri(uri, UriKind.Absolute));

        Assert.NotNull(info);

        using MemoryStream buffer = new();

        info.Stream.CopyTo(buffer);

        return buffer.ToArray();
    }

    /// <summary>The banner's 32 x 32 image, which is its only one.</summary>
    /// <param name="banner">The realised banner.</param>
    /// <returns>The image element.</returns>
    private static Image TheIcon(DependencyObject banner) =>
        Assert.Single(Descendants<Image>(banner));

    private static IEnumerable<T> Descendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        int children = VisualTreeHelper.GetChildrenCount(root);

        for (int index = 0; index < children; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);

            if (child is T match)
            {
                yield return match;
            }

            foreach (T deeper in Descendants<T>(child))
            {
                yield return deeper;
            }
        }
    }
}
