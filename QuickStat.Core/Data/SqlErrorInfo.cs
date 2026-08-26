namespace QuickStat.Data;

/// <summary>
/// One server-reported error, reduced to the four fields the classifier needs.
/// </summary>
/// <param name="Number">SQL Server error number - the ADO <c>NativeError</c> equivalent.</param>
/// <param name="Class">Severity class. Ten and below is informational; <c>PRINT</c> is class 0.</param>
/// <param name="Procedure">Stored procedure, when the server named one.</param>
/// <param name="Message">The server's own text.</param>
/// <remarks>
/// A separate type because <c>Microsoft.Data.SqlClient.SqlError</c> cannot be constructed from test
/// code and <c>SqlException</c> cannot be thrown from a fake. Classification is the part of the
/// error path with real logic in it, so it is the part that has to stay testable without a server
/// (PORT-PLAN.md §9 R9).
/// </remarks>
internal readonly record struct SqlErrorInfo(int Number, byte Class, string? Procedure, string Message);
