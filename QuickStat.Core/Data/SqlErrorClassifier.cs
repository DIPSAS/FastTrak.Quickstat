using System.Globalization;

namespace QuickStat.Data;

/// <summary>
/// Maps server-reported errors onto the typed exception hierarchy.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>ShouldRetryLastOperation</c> (<c>Emetra.Database.Simple.pas:606-668</c>), which walked
/// the ADO <c>Errors</c> collection and raised on the way through. The order of the checks is
/// reproduced exactly - privilege before user-defined, both before the catch-all - because a batch
/// can report several errors and the Delphi's first match won.
/// </para>
/// <para>
/// What is <em>not</em> reproduced: the Delphi ran this on the success path as well
/// (<c>Emetra.Database.Simple.pas:508</c>), and informational entries such as <c>PRINT</c> output
/// satisfied its "any errors at all" test, so a stored procedure that printed anything appeared to
/// fail. Here class 10 and below never reaches this method: <c>SqlConnection.InfoMessage</c> takes
/// those and they are logged, never raised (PORT-PLAN.md §7.2).
/// </para>
/// </remarks>
internal static class SqlErrorClassifier
{
    /// <summary>
    /// Error numbers that mean "permission denied", verbatim from
    /// <c>TPrivilegeErrors.AfterConstruction</c> (<c>Emetra.Database.NativeErrors.pas:73-83</c>).
    /// </summary>
    /// <remarks>
    /// The design sketch in <c>Docs/Port/01-data-access.md</c> §3.1 also listed 297 and 916. The
    /// contract in <c>Docs/Port/06-contracts.md</c> and the Delphi source agree on these seven, so
    /// these seven are what ships; widening the set is a behaviour change, not a transcription.
    /// </remarks>
    public static readonly int[] PrivilegeErrorNumbers = [229, 230, 262, 300, 1971, 1972, 1991];

    /// <summary>Delphi <c>SDatabasePrivilegeError</c> (<c>Emetra.Database.Simple.pas:123-125</c>).</summary>
    private const string PrivilegeMessageFormat =
        "Du mangler rettigheter til å utføre denne operasjonen:\r\n{0}\r\n" +
        "Kontakt superbruker/brukerstøtte hvis du mener dette er en feil. " +
        "Tilgang til QuickStat krever medlemskap i databaserollen {1}.";

    /// <summary>Delphi <c>SGeneralErrorMessage</c> (<c>Emetra.Database.Simple.pas:126-128</c>).</summary>
    private const string SeveralErrorsMessageFormat =
        "Operasjonen medførte {0} feil:\r\n{1}\r\nKontroller loggen hvis det oppstod flere feil.";

    /// <summary>Whether the number is one of the seven privilege errors.</summary>
    /// <param name="number">SQL Server error number.</param>
    /// <returns><see langword="true"/> for a privilege error.</returns>
    public static bool IsPrivilegeError(int number) => Array.IndexOf(PrivilegeErrorNumbers, number) >= 0;

    /// <summary>Whether the number is in SQL Server's user-defined range.</summary>
    /// <param name="number">SQL Server error number.</param>
    /// <returns><see langword="true"/> at or above 50 000.</returns>
    public static bool IsUserDefinedError(int number) => number >= SqlUserDefinedException.FirstUserDefinedErrorNumber;

    /// <summary>Classifies a batch of server errors into one exception.</summary>
    /// <param name="errors">The errors, in the order the server reported them.</param>
    /// <param name="commandText">Statement that failed, for the log.</param>
    /// <param name="innerException">The provider exception, when there is one.</param>
    /// <returns>The exception to raise.</returns>
    public static QuickStatDataException Classify(
        IReadOnlyList<SqlErrorInfo> errors,
        string? commandText,
        Exception? innerException)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Count == 0)
        {
            string fallback = innerException?.Message ?? "The statement failed and the server reported no detail.";

            return innerException is null
                ? new SqlCommandFailedException(fallback) { CommandText = commandText }
                : new SqlCommandFailedException(fallback, innerException) { CommandText = commandText };
        }

        foreach (SqlErrorInfo error in errors)
        {
            if (IsPrivilegeError(error.Number))
            {
                string message = string.Format(
                    CultureInfo.CurrentCulture,
                    PrivilegeMessageFormat,
                    error.Message,
                    SqlPrivilegeException.RequiredDatabaseRole);

                return innerException is null
                    ? new SqlPrivilegeException(message)
                    {
                        Number = error.Number,
                        Procedure = error.Procedure,
                        Severity = error.Class,
                        CommandText = commandText,
                    }
                    : new SqlPrivilegeException(message, innerException)
                    {
                        Number = error.Number,
                        Procedure = error.Procedure,
                        Severity = error.Class,
                        CommandText = commandText,
                    };
            }

            if (IsUserDefinedError(error.Number))
            {
                // The message was written for the user by whoever wrote the RAISERROR; pass it
                // through untouched rather than wrapping it in "an unexpected error occurred".
                return innerException is null
                    ? new SqlUserDefinedException(error.Message)
                    {
                        Number = error.Number,
                        Procedure = error.Procedure,
                        Severity = error.Class,
                        CommandText = commandText,
                    }
                    : new SqlUserDefinedException(error.Message, innerException)
                    {
                        Number = error.Number,
                        Procedure = error.Procedure,
                        Severity = error.Class,
                        CommandText = commandText,
                    };
            }
        }

        // The Delphi reported the *last* error in the collection, plus the count.
        SqlErrorInfo last = errors[^1];

        string text = errors.Count == 1
            ? last.Message
            : string.Format(CultureInfo.CurrentCulture, SeveralErrorsMessageFormat, errors.Count, last.Message);

        return innerException is null
            ? new SqlCommandFailedException(text)
            {
                Number = last.Number,
                Procedure = last.Procedure,
                Severity = last.Class,
                CommandText = commandText,
            }
            : new SqlCommandFailedException(text, innerException)
            {
                Number = last.Number,
                Procedure = last.Procedure,
                Severity = last.Class,
                CommandText = commandText,
            };
    }
}
