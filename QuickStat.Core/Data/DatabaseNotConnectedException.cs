namespace QuickStat.Data;

/// <summary>
/// A statement was issued before any project was selected, or after the session was closed.
/// </summary>
/// <remarks>
/// Delphi: <c>CheckConnected</c> / <c>EDatabaseImplicitConnectError</c>
/// (<c>Emetra.Database.Simple.pas:340-345</c>). There is no auto-connect at startup - the project
/// combo starts unselected (<c>MainQuickStat.pas:382-406</c>) - so this is reachable from a
/// mis-gated command, and the view models must gate on
/// <see cref="ISessionService.IsConnected"/> rather than rely on catching it.
/// </remarks>
public sealed class DatabaseNotConnectedException : QuickStatDataException
{
    /// <summary>Initialises a new instance.</summary>
    public DatabaseNotConnectedException()
    {
    }

    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">Description of what was attempted.</param>
    public DatabaseNotConnectedException(string message)
        : base(message)
    {
    }

    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">Description of what was attempted.</param>
    /// <param name="innerException">The underlying failure.</param>
    public DatabaseNotConnectedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
