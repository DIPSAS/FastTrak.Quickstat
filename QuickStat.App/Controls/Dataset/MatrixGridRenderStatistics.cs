namespace QuickStat.Controls.Dataset;

/// <summary>How much work the last <c>OnRender</c> did.</summary>
/// <param name="Rows">Data rows painted.</param>
/// <param name="Columns">Data columns painted.</param>
/// <param name="HeaderCells">Header cells painted, frozen and scrollable alike.</param>
/// <param name="FixedCells">Frozen identity cells painted.</param>
/// <param name="DataCells">Data cells painted.</param>
/// <remarks>
/// <para>
/// Recorded so virtualisation can be <b>proved</b> rather than asserted in prose. Each counter is
/// incremented exactly once per call into <see cref="QuickStat.Domain.Matrix.PersonMatrix.GetCell"/>
/// or <see cref="QuickStat.Domain.Matrix.PersonMatrix.GetFixedCell"/>, so a test can bind a
/// 1500 × 1000 matrix, render one frame, and check that the model was asked about a few hundred
/// cells rather than a million and a half.
/// </para>
/// <para>
/// Internal because it is a diagnostic, not part of the control's contract with step 3.1.
/// </para>
/// </remarks>
internal readonly record struct MatrixGridRenderStatistics(
    int Rows,
    int Columns,
    int HeaderCells,
    int FixedCells,
    int DataCells)
{
    /// <summary>Every cell the model was asked about.</summary>
    public int TotalCells => HeaderCells + FixedCells + DataCells;
}
