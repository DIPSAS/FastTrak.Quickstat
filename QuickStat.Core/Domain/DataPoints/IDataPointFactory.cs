using System.Diagnostics.CodeAnalysis;

namespace QuickStat.Domain.DataPoints;

/// <summary>Creates datapoints and resolves the display rule for a variable.</summary>
/// <remarks>
/// <para>
/// Delphi: <c>TDataPointFactory</c> (<c>EPR.QA.PointFactory.pas</c>), which looked a variable name
/// up in a class dictionary and then called <c>datapointClass.Create</c> on a bare <c>TClass</c> -
/// resolving to the non-virtual <c>TObject.Create</c>, so the type was allocated but its real
/// constructor never ran and the caller had to finish initialising it. There is no reflection here.
/// </para>
/// <para>
/// Lookup is <b>case-sensitive</b>, matching the Delphi's default dictionary comparer. That is not
/// a detail to tidy away: two of the sixteen registrations are <c>DB_VERSION</c> and
/// <c>DbVersion</c>, and they map to different rules.
/// </para>
/// </remarks>
public interface IDataPointFactory
{
    /// <summary>Creates a datapoint for a variable.</summary>
    /// <param name="varName">Matrix column name, prefix included.</param>
    /// <param name="value">The value.</param>
    /// <param name="timestamp">Observation time.</param>
    /// <param name="rowId">Source row identity.</param>
    /// <returns>The datapoint.</returns>
    DataPoint Create(string varName, double value, DateTime timestamp, int rowId);

    /// <summary>Looks up the display rule for a variable.</summary>
    /// <param name="varName">Matrix column name.</param>
    /// <param name="rule">The rule.</param>
    /// <returns><see langword="false"/> when the variable has no rule and defaults apply.</returns>
    bool TryGetRule(string varName, [NotNullWhen(true)] out DataPointRule? rule);

    /// <summary>Registers or replaces a rule.</summary>
    /// <param name="varName">Matrix column name, matched case-sensitively.</param>
    /// <param name="rule">The rule.</param>
    void Register(string varName, DataPointRule rule);
}
