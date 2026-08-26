using System.Diagnostics.CodeAnalysis;

namespace QuickStat.Domain.DataPoints;

/// <summary>The default <see cref="IDataPointFactory"/>: a case-sensitive rule table.</summary>
/// <remarks>
/// <para>
/// Delphi: <c>TDataPointFactory</c> (<c>EPR.QA.PointFactory.pas</c>). There it resolved a
/// <c>TClass</c> and allocated it through the non-virtual <c>TObject.Create</c>, so the datapoint's
/// real constructor never ran and the caller had to finish the job. Here a rule is looked up but the
/// datapoint is always the same sealed type, so there is nothing to go wrong and no reflection.
/// </para>
/// <para>
/// A missing key is not an error: the Delphi fell back to a default class, and the equivalent here
/// is "no rule", which the matrix renders as a plain right-aligned <c>%g</c> number on white.
/// </para>
/// </remarks>
public sealed class DataPointFactory : IDataPointFactory
{
    private readonly Dictionary<string, DataPointRule> _rules;

    /// <summary>Creates a factory carrying <see cref="StandardDataPointRules.Registrations"/>.</summary>
    /// <remarks>
    /// The Delphi calls <c>RegisterCustomDatapoints</c> from <c>AfterConstruction</c>, so a factory
    /// is never empty in practice.
    /// </remarks>
    public DataPointFactory()
        : this(StandardDataPointRules.Registrations)
    {
    }

    /// <summary>Creates a factory carrying exactly the rules supplied.</summary>
    /// <param name="rules">The initial registrations. Later entries win, as <c>AddOrSetValue</c> does.</param>
    public DataPointFactory(IEnumerable<KeyValuePair<string, DataPointRule>> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        _rules = new Dictionary<string, DataPointRule>(StringComparer.Ordinal);

        foreach (KeyValuePair<string, DataPointRule> rule in rules)
        {
            _rules[rule.Key] = rule.Value;
        }
    }

    /// <summary>The registered variable names.</summary>
    public IReadOnlyCollection<string> RegisteredVariableNames => _rules.Keys;

    /// <inheritdoc />
    public DataPoint Create(string varName, double value, DateTime timestamp, int rowId)
    {
        ArgumentNullException.ThrowIfNull(varName);

        DataPoint dataPoint = new() { VarName = varName };

        dataPoint.Update(value, timestamp, rowId);

        return dataPoint;
    }

    /// <inheritdoc />
    public bool TryGetRule(string varName, [NotNullWhen(true)] out DataPointRule? rule)
    {
        ArgumentNullException.ThrowIfNull(varName);

        return _rules.TryGetValue(varName, out rule);
    }

    /// <inheritdoc />
    public void Register(string varName, DataPointRule rule)
    {
        ArgumentException.ThrowIfNullOrEmpty(varName);
        ArgumentNullException.ThrowIfNull(rule);

        _rules[varName] = rule;
    }
}
