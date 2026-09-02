using System.Data.Common;
using System.IO;
using System.Text;
using QuickStat.Configuration;
using Xunit;

namespace QuickStat.Tests.Configuration;

/// <summary>
/// <see cref="OleDbConnectionStringTranslator"/>: data link expansion, the OLE DB keyword map, the
/// override order, the injected defaults and the redaction.
/// </summary>
/// <remarks>
/// One class on purpose. Several of these tests set <c>QUICKSTAT_SQL_OPTIONS</c>, which is
/// process-global; xUnit runs the tests of a single class one at a time, so keeping them together is
/// what makes that safe. The constructor clears the variable and <see cref="Dispose"/> restores
/// whatever the developer's own environment had, so a machine that happens to define it does not
/// change the outcome.
/// </remarks>
public sealed class ConnectionStringTranslatorTests : IDisposable
{
    private const string TrustedLocal = "Data Source=srv;Initial Catalog=db;Integrated Security=SSPI";

    private readonly string? _originalEnvironmentOptions =
        Environment.GetEnvironmentVariable(OleDbConnectionStringTranslator.OptionsEnvironmentVariable);

    public ConnectionStringTranslatorTests() => SetEnvironmentOptions(null);

    public void Dispose() => SetEnvironmentOptions(_originalEnvironmentOptions);

    [Fact]
    public void TranslatesTheShippedConfigurationAndDataLinkFileEndToEnd()
    {
        // The one test that runs the real pair of files a site already has on disk, through the real
        // path resolution: the shipped <ConnectionString> is "FILE NAME=.\FastTrak.UDL", so the copy
        // beside the test assembly is what AppContext.BaseDirectory resolution has to find.
        string deployed = Path.Combine(AppContext.BaseDirectory, "FastTrak.UDL");

        File.Copy(RepositoryFiles.UdlFile, deployed, overwrite: true);

        try
        {
            QuickStatConnection connection = Assert.Single(new XmlConnectionCatalog().Load(RepositoryFiles.ConfigFile));

            Assert.Equal(@"FILE NAME=.\FastTrak.UDL", connection.ConnectionString);

            ResolvedConnectionString resolved = Translate(connection);

            Assert.Equal(deployed, resolved.UdlPath);
            Assert.Same(connection, resolved.Source);

            Assert.Equal("localhost", Value(resolved.Value, "Data Source"));
            Assert.Equal("EFT00028_BEHOVPOL_PRODSETTING", Value(resolved.Value, "Initial Catalog"));
            Assert.Equal("True", Value(resolved.Value, "Integrated Security"));
            Assert.Equal("False", Value(resolved.Value, "Persist Security Info"));

            // Provider=SQLOLEDB.1 makes SqlConnectionStringBuilder throw, so it has to be gone.
            Assert.Null(Value(resolved.Value, "Provider"));
            Assert.Null(Value(resolved.Value, "File Name"));

            // Pinned in full, because this exact string is what R1 is about: it is what an existing
            // installation will actually send on its first run of the ported build. If a
            // Microsoft.Data.SqlClient upgrade changes the rendering, that is worth a human look
            // rather than a silent pass.
            Assert.Equal(
                "Data Source=localhost;Initial Catalog=EFT00028_BEHOVPOL_PRODSETTING;Integrated Security=True;"
                + "Persist Security Info=False;Connect Timeout=15;Encrypt=True;Trust Server Certificate=True;"
                + "Application Name=\"DIPS QuickStat\";Command Timeout=300",
                resolved.Value);

            // Nothing secret in it, so the two renderings are the same string.
            Assert.Equal(resolved.Value, resolved.Redacted);
        }
        finally
        {
            File.Delete(deployed);
        }
    }

    [Fact]
    public void InjectsTheEncryptionDefaultsWhenTheLegacyStringSaysNothing()
    {
        // PORT-PLAN.md §8.2 and R1. Without this the first connection of every existing installation
        // fails against an on-premise server with a self-signed certificate.
        ResolvedConnectionString resolved = Translate(Connection(TrustedLocal));

        Assert.Equal("True", Value(resolved.Value, "Encrypt"));
        Assert.Equal("True", Value(resolved.Value, "Trust Server Certificate"));
    }

    [Fact]
    public void InjectsTheApplicationNameAndTimeouts()
    {
        SqlOptions options = new();
        ResolvedConnectionString resolved = Translate(Connection(TrustedLocal), options);

        Assert.Equal(options.ApplicationName, Value(resolved.Value, "Application Name"));
        Assert.Equal("15", Value(resolved.Value, "Connect Timeout"));
        Assert.Equal("300", Value(resolved.Value, "Command Timeout"));
    }

    [Fact]
    public void HonoursANonDefaultSqlOptionsInstance()
    {
        SqlOptions options = new()
        {
            ApplicationName = "QuickStat parity run",
            ConnectTimeout = TimeSpan.FromSeconds(45),
            DefaultCommandTimeout = TimeSpan.FromMinutes(10),
            DefaultEncryptionOptions = "Encrypt=False",
        };

        ResolvedConnectionString resolved = Translate(Connection(TrustedLocal), options);

        Assert.Equal("QuickStat parity run", Value(resolved.Value, "Application Name"));
        Assert.Equal("45", Value(resolved.Value, "Connect Timeout"));
        Assert.Equal("600", Value(resolved.Value, "Command Timeout"));
        Assert.Equal("False", Value(resolved.Value, "Encrypt"));
        Assert.Null(Value(resolved.Value, "Trust Server Certificate"));
    }

    [Fact]
    public void KeepsAnExplicitValueInsteadOfTheInjectedDefault()
    {
        ResolvedConnectionString resolved = Translate(Connection(
            $"{TrustedLocal};Application Name=Something else;Connect Timeout=42;Command Timeout=90;Encrypt=Strict"));

        Assert.Equal("Something else", Value(resolved.Value, "Application Name"));
        Assert.Equal("42", Value(resolved.Value, "Connect Timeout"));
        Assert.Equal("90", Value(resolved.Value, "Command Timeout"));
        Assert.Equal("Strict", Value(resolved.Value, "Encrypt"));
    }

    [Fact]
    public void TheSqlOptionsElementOverridesTheInjectedEncryptionDefault()
    {
        // PORT-PLAN.md §8.1: the escape hatch for a server too old to negotiate TLS 1.2.
        ResolvedConnectionString resolved = Translate(Connection(TrustedLocal, sqlOptions: "Encrypt=False"));

        Assert.Equal("False", Value(resolved.Value, "Encrypt"));
    }

    [Fact]
    public void ExpandingTheDataLinkFileDiscardsEveryOtherKeyword()
    {
        // Delphi: Set_Value assigned the third line of the UDL to DelimitedText, replacing the whole
        // key set (Emetra.Database.ConnectionString.pas:184-198, :261-268).
        using TemporaryDirectory directory = new();
        string udl = directory.WriteUdl("FastTrak.UDL", TrustedLocal);

        ResolvedConnectionString resolved = Translate(Connection($"FILE NAME={udl};Application Name=Discarded"));

        Assert.Equal(new SqlOptions().ApplicationName, Value(resolved.Value, "Application Name"));
    }

    [Fact]
    public void TheSqlOptionsElementSurvivesTheDataLinkExpansion()
    {
        // Which is the whole reason the override lives in the XML rather than beside FILE NAME=.
        using TemporaryDirectory directory = new();
        string udl = directory.WriteUdl("FastTrak.UDL", $"Provider=SQLOLEDB.1;{TrustedLocal}");

        ResolvedConnectionString resolved = Translate(
            Connection($"FILE NAME={udl}", sqlOptions: "Encrypt=False;Application Name=Overridden"));

        Assert.Equal("False", Value(resolved.Value, "Encrypt"));
        Assert.Equal("Overridden", Value(resolved.Value, "Application Name"));
        Assert.Equal(udl, resolved.UdlPath);
    }

    [Fact]
    public void TheEnvironmentVariableOverridesTheSqlOptionsElement()
    {
        SetEnvironmentOptions("Application Name=From the environment");

        ResolvedConnectionString resolved =
            Translate(Connection(TrustedLocal, sqlOptions: "Application Name=From the XML"));

        Assert.Equal("From the environment", Value(resolved.Value, "Application Name"));
    }

    [Fact]
    public void DropsTheProviderKeywordWithoutThrowing()
    {
        // Docs/Port/01-data-access.md §3.4: SqlConnectionStringBuilder throws for "Provider".
        ResolvedConnectionString resolved = Translate(Connection($"Provider=SQLNCLI11.1;{TrustedLocal}"));

        Assert.Null(Value(resolved.Value, "Provider"));
        Assert.Equal("srv", Value(resolved.Value, "Data Source"));
    }

    [Theory]
    [InlineData("Auto Translate=True")]
    [InlineData("Tag with column collation when possible=False")]
    [InlineData("Use Procedure for Prepare=1")]
    [InlineData("OLE DB Services=-13")]
    [InlineData("General Timeout=0")]
    [InlineData("Prompt=4")]
    [InlineData("Window Handle=0")]
    [InlineData("Mode=Read")]
    [InlineData("Asynchronous Processing=False")]
    [InlineData("Extended Properties=whatever")]
    [InlineData("Locale Identifier=1044")]
    [InlineData("Replication=False")]
    [InlineData("Data Provider=SQLOLEDB")]
    public void DropsOleDbOnlyKeywords(string keyword)
    {
        ResolvedConnectionString resolved = Translate(Connection($"{TrustedLocal};{keyword}"));

        string name = keyword[..keyword.IndexOf('=', StringComparison.Ordinal)];

        Assert.Null(Value(resolved.Value, name, Unspaced(name)));

        // "Extended Properties" is the trap: SqlConnectionStringBuilder treats that spelling as a
        // synonym for AttachDBFilename, which is not what OLE DB meant by it.
        Assert.Null(Value(resolved.Value, "AttachDbFilename"));
    }

    [Theory]
    [InlineData("Integrated Security=SSPI")]
    [InlineData("Integrated Security=true")]
    [InlineData("Trusted_Connection=Yes")]
    public void EveryLegacySpellingOfIntegratedSecurityIsAccepted(string keyword)
    {
        ResolvedConnectionString resolved = Translate(Connection($"Data Source=srv;Initial Catalog=db;{keyword}"));

        Assert.Equal("True", Value(resolved.Value, "Integrated Security"));
    }

    [Theory]
    [InlineData("Server=srv2", "Data Source", "srv2")]
    [InlineData("Address=srv2", "Data Source", "srv2")]
    [InlineData("Addr=srv2", "Data Source", "srv2")]
    [InlineData("Database=db2", "Initial Catalog", "db2")]
    [InlineData("Persist Security Info=True", "Persist Security Info", "True")]
    [InlineData("Application Name=Report tool", "Application Name", "Report tool")]
    [InlineData("App=Report tool", "Application Name", "Report tool")]
    [InlineData("Workstation ID=WS1", "Workstation ID", "WS1")]
    [InlineData("WSID=WS1", "Workstation ID", "WS1")]
    [InlineData("Connect Timeout=42", "Connect Timeout", "42")]
    [InlineData("Connection Timeout=42", "Connect Timeout", "42")]
    [InlineData("Current Language=Norwegian", "Current Language", "Norwegian")]
    [InlineData("Language=Norwegian", "Current Language", "Norwegian")]
    [InlineData("Packet Size=8192", "Packet Size", "8192")]
    [InlineData("Failover Partner=mirror", "Failover Partner", "mirror")]
    [InlineData("MARS Connection=True", "MultipleActiveResultSets", "True")]
    [InlineData(@"Initial File Name=C:\db.mdf", "AttachDbFilename", @"C:\db.mdf")]
    [InlineData("Use Encryption for Data=False", "Encrypt", "False")]
    public void MapsEveryOleDbKeywordInPlay(string keyword, string expectedName, string expectedValue)
    {
        ResolvedConnectionString resolved = Translate(Connection($"{TrustedLocal};{keyword}"));

        Assert.Equal(expectedValue, Value(resolved.Value, expectedName, Spaced(expectedName), Unspaced(expectedName)));
    }

    [Fact]
    public void AnEmptyKeywordFromTheDataLinkDialogNeverReachesTheConnectionString()
    {
        // The regression this exists for, observed 2026-09-02 against a real server. The Windows data
        // link dialog writes every property it knows about and spells the unset ones "", so
        // Initial File Name="" arrived as AttachDbFilename='""'; SqlClient attaches a database file
        // for any non-empty AttachDbFilename, so the login ran an implicit CREATE DATABASE … FOR
        // ATTACH and the server answered error 262, which SqlErrorClassifier reports - correctly, and
        // very confusingly - as a missing QuickStat database role.
        // Keyword for keyword what the dialog wrote, with the site's own server and catalog replaced.
        const string dataLinkDialogOutput =
            "Provider=MSOLEDBSQL.1;Integrated Security=SSPI;Persist Security Info=False;User ID=\"\";"
            + "Initial Catalog=db;Data Source=srv;Initial File Name=\"\";"
            + "Server SPN=\"\";Authentication=\"\";Access Token=\"\"";

        ResolvedConnectionString resolved = Translate(Connection(dataLinkDialogOutput));

        Assert.Null(Value(resolved.Value, "AttachDbFilename", "AttachDBFilename", "Initial File Name"));
        Assert.Null(Value(resolved.Value, "User ID", "UserID"));
        Assert.Null(Value(resolved.Value, "Server SPN", "ServerSPN"));

        // And what the file is actually for still arrives.
        Assert.Equal("srv", Value(resolved.Value, "Data Source"));
        Assert.Equal("db", Value(resolved.Value, "Initial Catalog"));
        Assert.Equal("True", Value(resolved.Value, "Integrated Security"));
    }

    [Theory]
    [InlineData("Application Name=\"Report tool\"", "Application Name", "Report tool")]
    [InlineData("Application Name='Report tool'", "Application Name", "Report tool")]
    [InlineData(@"Initial File Name=""C:\db.mdf""", "AttachDbFilename", @"C:\db.mdf")]
    public void AQuotedValueArrivesWithoutItsQuotes(string keyword, string expectedName, string expectedValue)
    {
        // Not a new liberty: the Delphi handed the whole initialisation string to the OLE DB
        // provider, which unquoted it. This port re-emits keyword by keyword, so it has to.
        ResolvedConnectionString resolved = Translate(Connection($"{TrustedLocal};{keyword}"));

        Assert.Equal(expectedValue, Value(resolved.Value, expectedName, Spaced(expectedName), Unspaced(expectedName)));
    }

    [Theory]
    [InlineData("\"\"", "")]
    [InlineData("''", "")]
    [InlineData("\"p@ss\"", "p@ss")]
    [InlineData("'p@ss'", "p@ss")]
    [InlineData("\"a\"\"b\"", "a\"b")]
    [InlineData("plain", "plain")]
    [InlineData("\"mismatched'", "\"mismatched'")]
    [InlineData("\"", "\"")]
    [InlineData("", "")]
    public void UnquotingFollowsTheOleDbRule(string value, string expected) =>
        Assert.Equal(expected, OleDbKeywords.Unquote(value));

    [Fact]
    public void UserIdAndPasswordSurviveUnderBothSpellings()
    {
        ResolvedConnectionString resolved =
            Translate(Connection("Data Source=srv;Initial Catalog=db;UID=reader;PWD=s3cret"));

        Assert.Equal("reader", Value(resolved.Value, "User ID"));
        Assert.Equal("s3cret", Value(resolved.Value, "Password"));
    }

    [Fact]
    public void TheTcpNetworkLibraryBecomesADataSourcePrefix()
    {
        ResolvedConnectionString resolved = Translate(Connection($"Network Library=DBMSSOCN;{TrustedLocal}"));

        Assert.Equal("tcp:srv", Value(resolved.Value, "Data Source"));
        Assert.Null(Value(resolved.Value, "Network Library", "NetworkLibrary", "Net"));
    }

    [Fact]
    public void ANonTcpNetworkLibraryIsDroppedWithoutRewritingTheDataSource()
    {
        ResolvedConnectionString resolved = Translate(Connection($"Network Library=DBNMPNTW;{TrustedLocal}"));

        Assert.Equal("srv", Value(resolved.Value, "Data Source"));
        Assert.Null(Value(resolved.Value, "Network Library", "NetworkLibrary", "Net"));
    }

    [Fact]
    public void AnUnknownKeywordIsDroppedRatherThanFailingTheTranslation()
    {
        ResolvedConnectionString resolved = Translate(Connection($"{TrustedLocal};Something Invented=1"));

        Assert.Null(Value(resolved.Value, "Something Invented", "SomethingInvented"));
        Assert.Equal("srv", Value(resolved.Value, "Data Source"));
    }

    [Fact]
    public void TheDataLinkPathFallsBackToTheWorkingDirectory()
    {
        using TemporaryDirectory executableDirectory = new();
        using TemporaryDirectory workingDirectory = new();

        string expected = workingDirectory.WriteUdl("FastTrak.UDL", TrustedLocal);

        Assert.True(OleDbConnectionStringTranslator.TryResolveUdlPath(
            @".\FastTrak.UDL",
            executableDirectory.Path,
            workingDirectory.Path,
            out string path,
            out bool usedWorkingDirectory));

        Assert.Equal(expected, path);
        Assert.True(usedWorkingDirectory);
    }

    [Fact]
    public void TheExecutableDirectoryWinsOverTheWorkingDirectory()
    {
        // PORT-PLAN.md §4.1: the Delphi resolved against the working directory only, which a shortcut
        // can set to anything.
        using TemporaryDirectory executableDirectory = new();
        using TemporaryDirectory workingDirectory = new();

        string expected = executableDirectory.WriteUdl("FastTrak.UDL", TrustedLocal);
        workingDirectory.WriteUdl("FastTrak.UDL", "Data Source=wrong;Initial Catalog=db");

        Assert.True(OleDbConnectionStringTranslator.TryResolveUdlPath(
            @".\FastTrak.UDL",
            executableDirectory.Path,
            workingDirectory.Path,
            out string path,
            out bool usedWorkingDirectory));

        Assert.Equal(expected, path);
        Assert.False(usedWorkingDirectory);
    }

    [Fact]
    public void AnAbsoluteDataLinkPathIsUsedAsIs()
    {
        using TemporaryDirectory directory = new();
        string expected = directory.WriteUdl("FastTrak.UDL", TrustedLocal);

        Assert.True(OleDbConnectionStringTranslator.TryResolveUdlPath(
            expected,
            AppContext.BaseDirectory,
            AppContext.BaseDirectory,
            out string path,
            out bool usedWorkingDirectory));

        Assert.Equal(expected, path);
        Assert.False(usedWorkingDirectory);
    }

    [Fact]
    public void ThrowsWhenTheDataLinkFileIsNowhere()
    {
        using TemporaryDirectory directory = new();
        string absent = Path.Combine(directory.Path, "absent.UDL");

        QuickStatConfigurationException exception =
            Assert.Throws<QuickStatConfigurationException>(() => Translate(Connection($"FILE NAME={absent}")));

        Assert.Contains("absent.UDL", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Initial Catalog=db;Integrated Security=SSPI", "Data Source")]
    [InlineData("Data Source=srv;Integrated Security=SSPI", "Initial Catalog")]
    public void ThrowsWhenTheServerOrDatabaseIsMissing(string connectionString, string expectedInMessage)
    {
        QuickStatConfigurationException exception =
            Assert.Throws<QuickStatConfigurationException>(() => Translate(Connection(connectionString)));

        Assert.Contains(expectedInMessage, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Data Source=srv;Initial Catalog=db")]
    [InlineData("Data Source=srv;Initial Catalog=db;User ID=reader")]
    [InlineData("Data Source=srv;Initial Catalog=db;Password=s3cret")]
    public void ThrowsWhenNeitherIntegratedSecurityNorACompleteLoginIsPresent(string connectionString)
    {
        // Delphi: EDatabaseCredentialsMissing at Emetra.Database.Simple.pas:379, raised at connect
        // time. Raised here at translation time so the message can name the configuration.
        QuickStatConfigurationException exception =
            Assert.Throws<QuickStatConfigurationException>(() => Translate(Connection(connectionString)));

        Assert.Contains("credentials", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ASqlLoginPassesValidation()
    {
        ResolvedConnectionString resolved =
            Translate(Connection("Data Source=srv;Initial Catalog=db;User ID=reader;Password=s3cret"));

        Assert.Equal("reader", Value(resolved.Value, "User ID"));
    }

    [Fact]
    public void ThePasswordNeverAppearsInTheRedactedRendering()
    {
        const string password = "Correct-Horse-Battery-Staple";

        ResolvedConnectionString resolved =
            Translate(Connection($"Data Source=srv;Initial Catalog=db;User ID=reader;Password={password}"));

        Assert.Contains(password, resolved.Value, StringComparison.Ordinal);
        Assert.DoesNotContain(password, resolved.Redacted, StringComparison.Ordinal);
        Assert.Contains(
            OleDbConnectionStringTranslator.RedactedPasswordPlaceholder,
            resolved.Redacted,
            StringComparison.Ordinal);

        // The rest has to stay legible, or the redaction is useless for support.
        Assert.Equal("srv", Value(resolved.Redacted, "Data Source"));
        Assert.Equal("reader", Value(resolved.Redacted, "User ID"));
    }

    [Fact]
    public void ThePwdSpellingIsRedactedToo()
    {
        const string password = "another-secret";

        ResolvedConnectionString resolved =
            Translate(Connection($"Data Source=srv;Initial Catalog=db;UID=reader;PWD={password}"));

        Assert.DoesNotContain(password, resolved.Redacted, StringComparison.Ordinal);
    }

    [Fact]
    public void APasswordSuppliedThroughTheEnvironmentIsRedactedToo()
    {
        const string password = "environment-secret";

        SetEnvironmentOptions($"User ID=reader;Password={password}");

        ResolvedConnectionString resolved = Translate(Connection("Data Source=srv;Initial Catalog=db"));

        Assert.DoesNotContain(password, resolved.Redacted, StringComparison.Ordinal);
    }

    [Fact]
    public void ToStringIsTheRedactedRenderingSoLoggingTheRecordIsSafe()
    {
        const string password = "yet-another-secret";

        ResolvedConnectionString resolved =
            Translate(Connection($"Data Source=srv;Initial Catalog=db;User ID=reader;Password={password}"));

        Assert.Equal(resolved.Redacted, resolved.ToString());
        Assert.DoesNotContain(password, $"{resolved}", StringComparison.Ordinal);
    }

    [Fact]
    public void RedactionIsAPassThroughWhenThereIsNoPassword()
    {
        ResolvedConnectionString resolved = Translate(Connection(TrustedLocal));

        Assert.Equal(resolved.Value, resolved.Redacted);
    }

    [Fact]
    public void AValueContainingASemicolonIsTruncated()
    {
        // Not a regression: Delphi held the string in a TStringList with StrictDelimiter := true, so
        // it could not represent a semicolon in a value either. Pinned so the limitation is a
        // decision on record rather than a surprise, and so the redaction is shown to still hold for
        // the truncated remainder.
        ResolvedConnectionString resolved =
            Translate(Connection("Data Source=srv;Initial Catalog=db;User ID=reader;Password=p@ss;word"));

        Assert.Equal("p@ss", Value(resolved.Value, "Password"));
        Assert.DoesNotContain("p@ss", resolved.Redacted, StringComparison.Ordinal);
    }

    [Fact]
    public void UdlPathIsNullWhenTheConnectionStringCarriesNoDataLinkReference() =>
        Assert.Null(Translate(Connection(TrustedLocal)).UdlPath);

    private static string Unspaced(string keyword) => keyword.Replace(" ", string.Empty, StringComparison.Ordinal);

    private static string Spaced(string keyword)
    {
        // "MultipleActiveResultSets" -> "Multiple Active Result Sets", so a test can name either
        // spelling without depending on which one the SqlClient version in use emits.
        string compact = Unspaced(keyword);
        StringBuilder builder = new(compact.Length + 8);

        for (int index = 0; index < compact.Length; index++)
        {
            if (index > 0 && char.IsUpper(compact[index]) && !char.IsUpper(compact[index - 1]))
            {
                builder.Append(' ');
            }

            builder.Append(compact[index]);
        }

        return builder.ToString();
    }

    private static string? Value(string connectionString, params string[] keywords)
    {
        DbConnectionStringBuilder parsed = new()
        {
            ConnectionString = connectionString,
        };

        foreach (string keyword in keywords)
        {
            if (parsed.TryGetValue(keyword, out object? value))
            {
                return value as string;
            }
        }

        return null;
    }

    private static QuickStatConnection Connection(string connectionString, string? sqlOptions = null) =>
        new()
        {
            Name = "Testdatabase (NDV)",
            StudyName = "NDV",
            ConnectionString = connectionString,
            SqlOptions = sqlOptions,
        };

    private static ResolvedConnectionString Translate(QuickStatConnection connection, SqlOptions? options = null) =>
        new OleDbConnectionStringTranslator(options ?? new SqlOptions(), new UdlReader()).Translate(connection);

    private static void SetEnvironmentOptions(string? value) =>
        Environment.SetEnvironmentVariable(OleDbConnectionStringTranslator.OptionsEnvironmentVariable, value);
}
