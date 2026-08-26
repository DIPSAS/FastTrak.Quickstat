namespace QuickStat.Data;

/// <summary>
/// The FastTrak schema in the connected database predates what QuickStat supports.
/// </summary>
/// <remarks>
/// Delphi: <c>VerifyDbVersion</c> (<c>Emetra.Database.Info.pas:131-135</c>), which raised when
/// <c>0 &lt; DbVersion &lt; 510</c>. Zero means "not a FastTrak database" and is not rejected here;
/// <c>-1</c> means the info query itself failed and was swallowed, which is a different condition
/// (see <see cref="DatabaseInfo.DbVersion"/>).
/// </remarks>
public sealed class DatabaseVersionTooOldException : QuickStatDataException
{
    /// <summary>Initialises a new instance.</summary>
    public DatabaseVersionTooOldException()
    {
    }

    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">Description naming both versions.</param>
    public DatabaseVersionTooOldException(string message)
        : base(message)
    {
    }

    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">Description naming both versions.</param>
    /// <param name="innerException">The underlying failure.</param>
    public DatabaseVersionTooOldException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>The version the database reported.</summary>
    public int DbVersion { get; init; }

    /// <summary>The lowest version accepted, <see cref="DatabaseInfo.MinimumDbVersion"/>.</summary>
    public int MinimumDbVersion { get; init; } = DatabaseInfo.MinimumDbVersion;
}
