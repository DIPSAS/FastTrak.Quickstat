using QuickStat.Data;
using Xunit;

namespace QuickStat.Tests.Data;

/// <summary>
/// Error-number to exception-type mapping (<c>Emetra.Database.NativeErrors.pas:73-83</c>,
/// <c>Emetra.Database.Simple.pas:606-668</c>).
/// </summary>
public class SqlErrorClassifierTests
{
    private static QuickStatDataException Classify(params SqlErrorInfo[] errors) =>
        SqlErrorClassifier.Classify(errors, "SELECT 1", innerException: null);

    private static SqlErrorInfo Error(int number, string message = "boom", byte severity = 16) =>
        new(number, severity, "dbo.SomeProc", message);

    [Theory]
    [InlineData(229)]
    [InlineData(230)]
    [InlineData(262)]
    [InlineData(300)]
    [InlineData(1971)]
    [InlineData(1972)]
    [InlineData(1991)]
    public void ClassifiesTheSevenPrivilegeErrors(int number)
    {
        QuickStatDataException exception = Classify(Error(number, "The EXECUTE permission was denied on the object 'GetFormData'."));

        SqlPrivilegeException privilege = Assert.IsType<SqlPrivilegeException>(exception);

        Assert.Equal(number, privilege.Number);
        Assert.Equal("dbo.SomeProc", privilege.Procedure);
        Assert.Equal("SELECT 1", privilege.CommandText);

        // The message must name the denied object; it is the only diagnosis support gets, because
        // QuickStat.exe never checks the role itself (Docs/Port/01-data-access.md §4).
        Assert.Contains("GetFormData", privilege.Message, StringComparison.Ordinal);
        Assert.Contains(SqlPrivilegeException.RequiredDatabaseRole, privilege.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(297)]
    [InlineData(916)]
    public void DoesNotWidenThePrivilegeSetBeyondTheDelphi(int number)
    {
        // Docs/Port/01-data-access.md §3.1 sketched these two in as well; the Delphi list and the
        // frozen contract both stop at seven, so widening it would be a behaviour change.
        Assert.IsType<SqlCommandFailedException>(Classify(Error(number)));
    }

    [Theory]
    [InlineData(50000)]
    [InlineData(50001)]
    [InlineData(2147483647)]
    public void ClassifiesUserDefinedErrors(int number)
    {
        QuickStatDataException exception = Classify(Error(number, "Utvalget kan ikke lagres uten tittel."));

        SqlUserDefinedException userDefined = Assert.IsType<SqlUserDefinedException>(exception);

        // The database team wrote that message for the user; it must survive untouched.
        Assert.Equal("Utvalget kan ikke lagres uten tittel.", userDefined.Message);
    }

    [Fact]
    public void JustBelowTheUserDefinedRangeIsNotAUserDefinedError() =>
        Assert.IsType<SqlCommandFailedException>(Classify(Error(49999)));

    [Fact]
    public void ClassifiesEverythingElseAsCommandFailed()
    {
        QuickStatDataException exception = Classify(Error(208, "Invalid object name 'dbo.Nope'."));

        SqlCommandFailedException failed = Assert.IsType<SqlCommandFailedException>(exception);

        Assert.Equal(208, failed.Number);
        Assert.Equal("Invalid object name 'dbo.Nope'.", failed.Message);
    }

    [Fact]
    public void PrivilegeWinsOverALaterUserDefinedError()
    {
        // The Delphi raised on the first match while walking the collection in order.
        Assert.IsType<SqlPrivilegeException>(Classify(Error(229), Error(50000)));
    }

    [Fact]
    public void AnEarlierUserDefinedErrorWinsOverALaterPrivilegeError() =>
        Assert.IsType<SqlUserDefinedException>(Classify(Error(50000), Error(229)));

    [Fact]
    public void ReportsTheCountAndTheLastMessageWhenSeveralErrorsArrive()
    {
        QuickStatDataException exception = Classify(Error(100, "first"), Error(101, "second"), Error(102, "third"));

        // Delphi SGeneralErrorMessage used the count and the *last* description.
        Assert.Contains("3", exception.Message, StringComparison.Ordinal);
        Assert.Contains("third", exception.Message, StringComparison.Ordinal);
        Assert.Equal(102, exception.Number);
    }

    [Fact]
    public void SurvivesAnEmptyErrorCollection()
    {
        InvalidOperationException inner = new("the provider failed before the server answered");

        QuickStatDataException exception = SqlErrorClassifier.Classify([], "SELECT 1", inner);

        Assert.IsType<SqlCommandFailedException>(exception);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void KeepsTheProviderExceptionAsTheInnerException()
    {
        InvalidOperationException inner = new("provider");

        QuickStatDataException exception = SqlErrorClassifier.Classify([Error(229)], "SELECT 1", inner);

        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void EverythingItRaisesDerivesFromTheOneRoot()
    {
        // The view models want exactly one catch clause per command.
        Assert.IsAssignableFrom<QuickStatDataException>(Classify(Error(229)));
        Assert.IsAssignableFrom<QuickStatDataException>(Classify(Error(50000)));
        Assert.IsAssignableFrom<QuickStatDataException>(Classify(Error(208)));
        Assert.IsAssignableFrom<QuickStatDataException>(new DatabaseNotConnectedException());
        Assert.IsAssignableFrom<QuickStatDataException>(new DatabaseVersionTooOldException());
        Assert.IsAssignableFrom<QuickStatDataException>(new SqlParameterBindingException());
    }

    [Fact]
    public void TheNorwegianPrivilegeMessageSurvivesTheSourceEncoding()
    {
        // The source files are UTF-8 without a BOM and carry Norwegian text; if that ever regresses
        // the message reaches the user as mojibake. Cheap to pin.
        QuickStatDataException exception = Classify(Error(229, "Permission denied."));

        Assert.StartsWith("Du mangler rettigheter til å utføre denne operasjonen:", exception.Message, StringComparison.Ordinal);
        Assert.Contains("brukerstøtte", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintOutputNeverReachesTheClassifier()
    {
        // Class 0 with number 0 is what a PRINT produces. SqlConnection.InfoMessage takes those, so
        // they are never in a SqlException and never in this list - which is the fix for
        // Emetra.Database.Simple.pas:652-656. This test pins the intent: an informational entry on
        // its own is not a privilege or user-defined error, so nothing about it is special-cased
        // here.
        Assert.False(SqlErrorClassifier.IsPrivilegeError(0));
        Assert.False(SqlErrorClassifier.IsUserDefinedError(0));
    }
}
