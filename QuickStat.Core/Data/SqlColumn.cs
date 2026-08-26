namespace QuickStat.Data;

/// <summary>One column of a <see cref="SqlResultSet"/>.</summary>
/// <param name="Ordinal">Zero-based position, which is how collector results are read.</param>
/// <param name="Name">Column name as returned by the server.</param>
/// <param name="ClrType">The CLR type the reader reported for the column.</param>
public readonly record struct SqlColumn(int Ordinal, string Name, Type ClrType);
