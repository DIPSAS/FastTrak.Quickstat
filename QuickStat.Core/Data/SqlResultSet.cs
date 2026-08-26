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
    /// <summary>The columns, in ordinal order.</summary>
    public IReadOnlyList<SqlColumn> Columns => throw new NotImplementedException();

    /// <inheritdoc />
    public int Count => throw new NotImplementedException();

    /// <summary><see langword="true"/> when the statement returned no rows.</summary>
    public bool IsEmpty => throw new NotImplementedException();

    /// <inheritdoc />
    public SqlRow this[int index] => throw new NotImplementedException();

    /// <summary>Case-insensitive column lookup that tolerates absence.</summary>
    /// <param name="columnName">Column to find.</param>
    /// <returns>The ordinal, or <c>-1</c>.</returns>
    /// <remarks>
    /// The equivalent of <c>TDataset.FindField</c>, which returns <c>nil</c> rather than raising.
    /// Collector results rely on it for the optional <c>ItemId</c> and <c>Caption</c> columns
    /// (<c>EPR.QA.Collector.Base.pas:149-150</c>).
    /// </remarks>
    public int IndexOf(string columnName) => throw new NotImplementedException();

    /// <summary>Case-insensitive column lookup that insists on presence.</summary>
    /// <param name="columnName">Column to find.</param>
    /// <returns>The ordinal.</returns>
    /// <remarks>The equivalent of <c>TDataset.FieldByName</c>, which raises when absent.</remarks>
    /// <exception cref="SqlCommandFailedException">The column is not in the result set.</exception>
    public int GetOrdinal(string columnName) => throw new NotImplementedException();

    /// <inheritdoc />
    public IEnumerator<SqlRow> GetEnumerator() => throw new NotImplementedException();

    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
}
