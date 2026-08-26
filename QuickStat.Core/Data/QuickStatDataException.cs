namespace QuickStat.Data;

/// <summary>
/// Base for every failure the data layer raises, so one <c>catch</c> covers the whole surface.
/// </summary>
/// <remarks>
/// Deriving everything - including <see cref="DatabaseNotConnectedException"/> - from a single root
/// is a deliberate departure from the analysis document, which proposed
/// <see cref="InvalidOperationException"/> for that one case. The view models need exactly one
/// catch clause per command, and splitting the hierarchy at the root buys nothing.
/// </remarks>
public class QuickStatDataException : Exception
{
    /// <summary>Initialises a new instance.</summary>
    public QuickStatDataException()
    {
    }

    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">Human-readable description.</param>
    public QuickStatDataException(string message)
        : base(message)
    {
    }

    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">Human-readable description.</param>
    /// <param name="innerException">The provider exception this was classified from.</param>
    public QuickStatDataException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>SQL Server error number - the ADO <c>NativeError</c> equivalent.</summary>
    public int? Number { get; init; }

    /// <summary>Stored procedure the error came from, when the server reported one.</summary>
    public string? Procedure { get; init; }

    /// <summary>Server-reported severity class.</summary>
    public byte? Severity { get; init; }

    /// <summary>The statement that failed, for the log. May be <see langword="null"/>.</summary>
    public string? CommandText { get; init; }
}
