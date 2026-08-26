namespace QuickStat.Data;

/// <summary>
/// A stored procedure raised a business error with <c>RAISERROR</c> / <c>THROW</c>, i.e. an error
/// number of 50000 or above.
/// </summary>
/// <remarks>
/// Delphi: <c>EDatabaseUserDefinedError</c> (<c>Emetra.Database.Simple.pas:642-643</c>). Separate
/// from <see cref="SqlCommandFailedException"/> because the message is written for the user by the
/// database team and should be shown, not wrapped in "an unexpected error occurred".
/// </remarks>
public sealed class SqlUserDefinedException : QuickStatDataException
{
    /// <summary>Initialises a new instance.</summary>
    public SqlUserDefinedException()
    {
    }

    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">The server's own message, verbatim.</param>
    public SqlUserDefinedException(string message)
        : base(message)
    {
    }

    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">The server's own message, verbatim.</param>
    /// <param name="innerException">The provider exception.</param>
    public SqlUserDefinedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Lowest error number SQL Server reserves for user-defined messages.</summary>
    public const int FirstUserDefinedErrorNumber = 50000;
}
