namespace QuickStat.Data;

/// <summary>
/// The login is missing a <c>GRANT</c>. Almost always: the user is not a member of the
/// <c>QuickStat</c> database role.
/// </summary>
/// <remarks>
/// <para>
/// Raised for SQL Server error numbers 229, 230, 262, 300, 1971, 1972 and 1991, matching
/// <c>Emetra.Database.NativeErrors.pas:73-83</c>.
/// </para>
/// <para>
/// QuickStat.exe never checks the role itself - enforcement is entirely object-level <c>GRANT</c>s
/// on the <c>Report.*</c> and <c>QuickStat.*</c> programmability
/// (<c>Docs/Port/01-data-access.md</c> §4). That makes this exception the only diagnosis a support
/// engineer gets, so the message must name the object that was denied.
/// </para>
/// </remarks>
public sealed class SqlPrivilegeException : QuickStatDataException
{
    /// <summary>Initialises a new instance.</summary>
    public SqlPrivilegeException()
    {
    }

    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">Description naming the denied object where possible.</param>
    public SqlPrivilegeException(string message)
        : base(message)
    {
    }

    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">Description naming the denied object where possible.</param>
    /// <param name="innerException">The provider exception.</param>
    public SqlPrivilegeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>The database role QuickStat expects its users to hold.</summary>
    /// <remarks>Delphi: <c>ROLE_QUICKSTAT</c> (<c>Emetra.AccessControl.Constants.pas:51</c>).</remarks>
    public const string RequiredDatabaseRole = "QuickStat";
}
