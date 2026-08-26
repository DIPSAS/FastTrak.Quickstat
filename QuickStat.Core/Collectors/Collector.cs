namespace QuickStat.Collectors;

/// <summary>
/// The one concrete <see cref="ICollector"/>: a descriptor paired with a SQL builder.
/// </summary>
/// <remarks>
/// <para>
/// The Delphi's thirteen-class <c>TDataCollector</c> hierarchy collapses to this. Its subclasses
/// varied in nothing but <c>(name, title, varPrefix, batchSize, sql)</c>, and the shape of the read
/// is identical for every collector, so the variation belongs in data and a delegate rather than in
/// types (<c>Docs/Port/03-collectors.md</c> §G.1).
/// </para>
/// <para>
/// A delegate rather than an <c>ISqlSource</c> interface because the three cases it has to cover
/// are "a constant string", "a constant string with one or two arguments folded in at construction"
/// and "a string that needs the study id" - all of which a closure expresses without ceremony, and
/// all of which stay pure.
/// </para>
/// </remarks>
public sealed class Collector : ICollector
{
    private readonly Func<CollectorSqlContext, string> _buildSql;

    /// <summary>Creates a collector.</summary>
    /// <param name="descriptor">Name, title, prefix, batch size, gate and availability.</param>
    /// <param name="buildSql">
    /// Produces the statement for one batch. Must be pure and deterministic; see
    /// <see cref="ICollector.BuildSql"/>.
    /// </param>
    public Collector(CollectorDescriptor descriptor, Func<CollectorSqlContext, string> buildSql)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(buildSql);

        Descriptor = descriptor;
        _buildSql = buildSql;
    }

    /// <inheritdoc />
    public CollectorDescriptor Descriptor { get; }

    /// <inheritdoc />
    public string BuildSql(CollectorSqlContext context) => _buildSql(context);

    /// <inheritdoc />
    public override string ToString() => Descriptor.Name;
}
