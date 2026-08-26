namespace QuickStat.Data;

/// <summary>
/// A statement failed for a reason that is neither a privilege problem nor a user-defined error,
/// and that retrying will not fix.
/// </summary>
/// <remarks>
/// Delphi: <c>EDatabaseCommandFailed</c> (<c>Emetra.Database.Simple.pas:652-656</c>). That version
/// was also raised on the <em>success</em> path, because the retry check ran unconditionally and
/// any entry in the ADO <c>Errors</c> collection satisfied it - including informational
/// <c>PRINT</c> output. A stored procedure that printed anything therefore "failed". The port
/// routes provider information messages to the log and never turns them into exceptions.
/// </remarks>
public sealed class SqlCommandFailedException : QuickStatDataException
{
    /// <summary>Initialises a new instance.</summary>
    public SqlCommandFailedException()
    {
    }

    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">Description of the failure.</param>
    public SqlCommandFailedException(string message)
        : base(message)
    {
    }

    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">Description of the failure.</param>
    /// <param name="innerException">The provider exception.</param>
    public SqlCommandFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
