using System.Collections;

namespace QuickStat.Data;

/// <summary>
/// A fully materialised result set. Replaces the single shared <c>TADOQuery</c> the Delphi handed
/// back from every call.
/// </summary>
/// <remarks>
/// Buffering is not a change of behaviour: the legacy cursor was <c>clUseClient</c>, so ADO already
/// pulled the whole result set to the client before the first <c>Next</c>
/// (<c>Emetra.Database.Simple.pas:167-170</c>).
/// </remarks>
public sealed class SqlResultSet : IReadOnlyList<SqlRow>
{
    private static readonly SqlColumn[] NoColumns = [];
    private static readonly object?[][] NoRows = [];

    private readonly SqlColumn[] _columns;
    private readonly object?[][] _rows;
    private readonly Dictionary<string, int> _ordinals;

    /// <summary>Builds a result set from already-materialised columns and rows.</summary>
    /// <param name="columns">Columns in ordinal order.</param>
    /// <param name="rows">
    /// One array per row, each as wide as <paramref name="columns"/>. <c>NULL</c> is
    /// <see langword="null"/>; <see cref="DBNull"/> is accepted and normalised.
    /// </param>
    /// <remarks>
    /// Public on purpose. Every other Phase 2 step fakes <see cref="ISqlExecutor"/> in its own
    /// tests, and a fake has to be able to hand back a result set without a database
    /// (PORT-PLAN.md §9 R9). <see cref="Create(IReadOnlyList{string}, object?[][])"/> is the
    /// convenient form.
    /// </remarks>
    public SqlResultSet(IReadOnlyList<SqlColumn> columns, IReadOnlyList<object?[]> rows)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        _columns = [.. columns];
        _rows = new object?[rows.Count][];
        _ordinals = new Dictionary<string, int>(_columns.Length, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < _columns.Length; i++)
        {
            // A duplicated column name resolves to the first, which is what TDataset.FieldByName
            // does with its FieldList.
            _ordinals.TryAdd(_columns[i].Name, i);
        }

        for (int r = 0; r < rows.Count; r++)
        {
            object?[] source = rows[r] ?? throw new ArgumentException("A row must not be null.", nameof(rows));

            if (source.Length > _columns.Length)
            {
                // A short row reads as NULL, which is useful; a long one is always a mistake in a
                // fake, and silently dropping the tail would hide it.
                throw new ArgumentException(
                    $"Row {r} has {source.Length} value(s) but there are only {_columns.Length} column(s).",
                    nameof(rows));
            }

            object?[] copy = new object?[_columns.Length];

            for (int c = 0; c < source.Length; c++)
            {
                copy[c] = source[c] is DBNull ? null : source[c];
            }

            _rows[r] = copy;
        }
    }

    private SqlResultSet(SqlColumn[] columns, object?[][] rows, Dictionary<string, int> ordinals)
    {
        _columns = columns;
        _rows = rows;
        _ordinals = ordinals;
    }

    /// <summary>A result set with no columns and no rows.</summary>
    public static SqlResultSet Empty { get; } =
        new(NoColumns, NoRows, new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));

    /// <summary>Builds a result set from column names and row values, inferring the CLR types.</summary>
    /// <param name="columnNames">Column names in ordinal order.</param>
    /// <param name="rows">One array per row.</param>
    /// <returns>The result set.</returns>
    /// <remarks>
    /// The column type is taken from the first non-null value in that column, falling back to
    /// <see cref="object"/>. Intended for tests and for fakes, not for the execution path.
    /// </remarks>
    public static SqlResultSet Create(IReadOnlyList<string> columnNames, params object?[][] rows)
    {
        ArgumentNullException.ThrowIfNull(columnNames);
        ArgumentNullException.ThrowIfNull(rows);

        SqlColumn[] columns = new SqlColumn[columnNames.Count];

        for (int c = 0; c < columns.Length; c++)
        {
            Type type = typeof(object);

            foreach (object?[] row in rows)
            {
                if (c < row.Length && row[c] is not null and not DBNull)
                {
                    type = row[c]!.GetType();
                    break;
                }
            }

            columns[c] = new SqlColumn(c, columnNames[c], type);
        }

        return new SqlResultSet(columns, rows);
    }

    /// <summary>The columns, in ordinal order.</summary>
    public IReadOnlyList<SqlColumn> Columns => _columns;

    /// <inheritdoc />
    public int Count => _rows.Length;

    /// <summary><see langword="true"/> when the statement returned no rows.</summary>
    public bool IsEmpty => _rows.Length == 0;

    /// <inheritdoc />
    public SqlRow this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_rows.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"The result set has {_rows.Length} row(s).");
            }

            return new SqlRow(_rows[index]);
        }
    }

    /// <summary>Case-insensitive column lookup that tolerates absence.</summary>
    /// <param name="columnName">Column to find.</param>
    /// <returns>The ordinal, or <c>-1</c>.</returns>
    /// <remarks>
    /// The equivalent of <c>TDataset.FindField</c>, which returns <c>nil</c> rather than raising.
    /// Collector results rely on it for the optional <c>ItemId</c> and <c>Caption</c> columns
    /// (<c>EPR.QA.Collector.Base.pas:149-150</c>).
    /// </remarks>
    public int IndexOf(string columnName)
    {
        ArgumentNullException.ThrowIfNull(columnName);

        return _ordinals.TryGetValue(columnName, out int ordinal) ? ordinal : -1;
    }

    /// <summary>Case-insensitive column lookup that insists on presence.</summary>
    /// <param name="columnName">Column to find.</param>
    /// <returns>The ordinal.</returns>
    /// <remarks>The equivalent of <c>TDataset.FieldByName</c>, which raises when absent.</remarks>
    /// <exception cref="SqlCommandFailedException">The column is not in the result set.</exception>
    public int GetOrdinal(string columnName)
    {
        ArgumentNullException.ThrowIfNull(columnName);

        if (_ordinals.TryGetValue(columnName, out int ordinal))
        {
            return ordinal;
        }

        throw new SqlCommandFailedException(
            $"The result set has no column named '{columnName}'. Columns: {string.Join(", ", _columns.Select(c => c.Name))}.");
    }

    /// <inheritdoc />
    public IEnumerator<SqlRow> GetEnumerator()
    {
        foreach (object?[] values in _rows)
        {
            yield return new SqlRow(values);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
