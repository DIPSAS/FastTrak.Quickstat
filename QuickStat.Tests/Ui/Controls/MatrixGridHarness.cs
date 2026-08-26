using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QuickStat.Controls.Dataset;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.Matrix;

namespace QuickStat.Tests.Ui.Controls;

/// <summary>Builds, lays out and renders a <see cref="MatrixGrid"/> with no window.</summary>
/// <remarks>
/// <para>
/// Everything here must run inside <see cref="StaTestRunner"/>: <c>Measure</c> throws on the MTA
/// test thread, and a <see cref="RenderTargetBitmap"/> needs an apartment. The pixel readback is
/// what proves the painting priority order actually reaches the screen - a unit test of
/// <see cref="MatrixGridCellPainter"/> proves the decision, not the drawing.
/// </para>
/// <para>
/// The bitmap is created at 96 DPI so one device-independent unit is one pixel and every probe
/// coordinate can be worked out from the documented widths, whatever the machine's display scaling.
/// </para>
/// </remarks>
internal sealed class MatrixGridHarness
{
    private readonly uint[] _pixels;

    private MatrixGridHarness(MatrixGrid grid, uint[] pixels, int width, int height)
    {
        Grid = grid;
        _pixels = pixels;
        Width = width;
        Height = height;
    }

    /// <summary>The laid-out grid.</summary>
    public MatrixGrid Grid { get; }

    /// <summary>Bitmap width in pixels, which equals its width in device-independent units.</summary>
    public int Width { get; }

    /// <summary>Bitmap height in pixels.</summary>
    public int Height { get; }

    /// <summary>Creates a grid and lays it out, without rendering.</summary>
    /// <param name="matrix">The dataset, or <see langword="null"/>.</param>
    /// <param name="width">Viewport width.</param>
    /// <param name="height">Viewport height.</param>
    /// <param name="identification">Which identity columns to show.</param>
    /// <returns>The grid.</returns>
    public static MatrixGrid CreateGrid(
        PersonMatrix? matrix,
        double width = 400,
        double height = 200,
        PersonIdentification identification = PersonIdentification.PersonIdOnly)
    {
        MatrixGrid grid = new()
        {
            Matrix = matrix,
            Identification = identification,
            CellCulture = MatrixGridTestData.Culture,
        };

        LayOut(grid, width, height);

        return grid;
    }

    /// <summary>Measures and arranges a grid at a given size.</summary>
    /// <param name="grid">The grid.</param>
    /// <param name="width">Viewport width.</param>
    /// <param name="height">Viewport height.</param>
    public static void LayOut(MatrixGrid grid, double width, double height)
    {
        grid.Measure(new Size(width, height));
        grid.Arrange(new Rect(0, 0, width, height));
        grid.UpdateLayout();
    }

    /// <summary>Lays a grid out and renders it into a bitmap.</summary>
    /// <param name="grid">The grid.</param>
    /// <param name="width">Viewport width.</param>
    /// <param name="height">Viewport height.</param>
    /// <returns>A harness holding the rendered pixels.</returns>
    public static MatrixGridHarness Render(MatrixGrid grid, int width = 400, int height = 200)
    {
        LayOut(grid, width, height);

        RenderTargetBitmap bitmap = new(width, height, 96, 96, PixelFormats.Pbgra32);

        bitmap.Render(grid);

        uint[] pixels = new uint[width * height];

        bitmap.CopyPixels(pixels, width * 4, 0);

        return new MatrixGridHarness(grid, pixels, width, height);
    }

    /// <summary>Builds a grid over a dataset and renders it in one step.</summary>
    /// <param name="matrix">The dataset.</param>
    /// <param name="width">Viewport width.</param>
    /// <param name="height">Viewport height.</param>
    /// <param name="identification">Which identity columns to show.</param>
    /// <param name="configure">Optional extra setup applied before rendering.</param>
    /// <returns>A harness holding the rendered pixels.</returns>
    public static MatrixGridHarness RenderMatrix(
        PersonMatrix? matrix,
        int width = 400,
        int height = 200,
        PersonIdentification identification = PersonIdentification.PersonIdOnly,
        Action<MatrixGrid>? configure = null)
    {
        MatrixGrid grid = CreateGrid(matrix, width, height, identification);

        configure?.Invoke(grid);

        return Render(grid, width, height);
    }

    /// <summary>The opaque colour of one pixel.</summary>
    /// <param name="x">Column, in pixels from the left.</param>
    /// <param name="y">Row, in pixels from the top.</param>
    /// <returns>The colour.</returns>
    public Color PixelAt(int x, int y)
    {
        uint pixel = _pixels[(y * Width) + x];

        return Color.FromRgb((byte)((pixel >> 16) & 0xFF), (byte)((pixel >> 8) & 0xFF), (byte)(pixel & 0xFF));
    }
}
