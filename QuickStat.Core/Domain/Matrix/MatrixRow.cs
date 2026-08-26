using System.Diagnostics.CodeAnalysis;
using QuickStat.Domain.DataPoints;
using QuickStat.Domain.Patients;

namespace QuickStat.Domain.Matrix;

/// <summary>One person and every datapoint collected for them.</summary>
/// <remarks>
/// Delphi: <c>TPersonGridRow</c> (<c>EPR.QA.Matrix.Row.pas:23-60</c>). Built from the patient list,
/// never from a dataset - <c>TPersonGridRow.Load(TDataset)</c> exists but is unreachable in
/// QuickStat.
/// </remarks>
public sealed class MatrixRow
{
    /// <summary>The person. Also the matrix's row key, and the default sort key.</summary>
    public required int PersonId { get; init; }

    /// <summary>Date of birth, or <see langword="null"/> when unknown.</summary>
    public DateTime? DateOfBirth { get; init; }

    /// <summary>
    /// <c>"Last, First"</c>, snapshotted from <see cref="Patient.DisplayName"/>.
    /// </summary>
    public string FullName { get; init; } = "";

    /// <summary>
    /// National identity number, or <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// Writable because the Delphi's <c>AddNationalIds</c> fills it after the row exists. Note the
    /// grid row snapshots the value and never refreshes it
    /// (<c>EPR.QA.Matrix.Row.pas:96</c>), so switching identification mode after loading must
    /// either re-fetch or rebuild.
    /// </remarks>
    public string? NationalId { get; set; }

    /// <summary>Raw <c>GenderId</c>.</summary>
    public int GenderId { get; init; }

    /// <summary>Interpreted sex.</summary>
    public Sex Sex { get; init; }

    /// <summary>The person's datapoints, keyed by column name.</summary>
    /// <remarks>Ordinal comparison, matching the Delphi's case-sensitive row dictionary.</remarks>
    public IReadOnlyDictionary<string, DataPoint> DataPoints => _dataPoints;

    private readonly Dictionary<string, DataPoint> _dataPoints = new(StringComparer.Ordinal);

    /// <summary>Looks a datapoint up by column name.</summary>
    /// <param name="varName">Column name, prefix included.</param>
    /// <param name="dataPoint">The datapoint.</param>
    /// <returns><see langword="true"/> when the cell has a value.</returns>
    public bool TryGetDataPoint(string varName, [NotNullWhen(true)] out DataPoint? dataPoint)
    {
        ArgumentNullException.ThrowIfNull(varName);

        return _dataPoints.TryGetValue(varName, out dataPoint);
    }

    /// <summary>Adds a datapoint if the cell is empty.</summary>
    /// <param name="dataPoint">The datapoint; <see cref="DataPoint.VarName"/> is the key.</param>
    /// <returns>
    /// <see langword="false"/> when the cell already held one, in which case the existing datapoint
    /// is updated instead - matching <c>TPersonGridRow.AddDatapoint</c>
    /// (<c>EPR.QA.Matrix.Row.pas:135-152</c>), where the loser was freed by the collector.
    /// </returns>
    public bool TryAddDataPoint(DataPoint dataPoint)
    {
        ArgumentNullException.ThrowIfNull(dataPoint);

        if (_dataPoints.TryGetValue(dataPoint.VarName, out DataPoint? existing))
        {
            // The incoming point loses, but its value is folded into the survivor - note this keeps
            // the *last* row read, not the newest by timestamp.  Only the non-factory path
            // (TPersonGridRow.AddData) compares timestamps, and no collector uses it.
            existing.Update(dataPoint.Value, dataPoint.Timestamp, dataPoint.RowId);

            return false;
        }

        _dataPoints.Add(dataPoint.VarName, dataPoint);

        return true;
    }

    /// <summary>Drops every datapoint, keeping the person.</summary>
    /// <remarks>
    /// Used by <see cref="PersonMatrix.ClearVariables"/>. The Delphi left the datapoints in place and
    /// only cleared the column list, so values from a previous collect run survived invisibly until
    /// the row itself was freed.
    /// </remarks>
    internal void ClearDataPoints() => _dataPoints.Clear();
}
