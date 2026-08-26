using System.IO;
using QuickStat.Configuration;
using Xunit;

namespace QuickStat.Tests.Configuration;

/// <summary>
/// <see cref="XmlConnectionCatalog"/> against the shipped configuration file and against the
/// legacy parsing behaviours it has to reproduce.
/// </summary>
public class ConnectionCatalogTests
{
    [Fact]
    public void ReadsTheShippedConfigurationFileUntouched()
    {
        // PORT-PLAN.md §6: an existing QuickStat.config.xml must keep working with no edits.
        IReadOnlyList<QuickStatConnection> connections = new XmlConnectionCatalog().Load(RepositoryFiles.ConfigFile);

        QuickStatConnection connection = Assert.Single(connections);

        Assert.Equal("Testdatabase (NDV)", connection.Name);
        Assert.Equal("NDV", connection.StudyName);
        Assert.Equal(@"FILE NAME=.\FastTrak.UDL", connection.ConnectionString);
        Assert.Null(connection.SqlOptions);
    }

    [Fact]
    public void ReturnsEmptyWhenTheFileIsMissing()
    {
        // Delphi: MainQuickStat.pas:392-398 logged and carried on with an empty project list.
        using TemporaryDirectory directory = new();

        Assert.Empty(new XmlConnectionCatalog().Load(Path.Combine(directory.Path, "absent.config.xml")));
    }

    [Fact]
    public void FindsConnectionElementsAtAnyDepth()
    {
        // Delphi: TNodeList walks the whole subtree of the document element
        // (Emetra.Xml.NodeList.pas:33-51), so the nesting in the file is not fixed.
        using TemporaryDirectory directory = new();
        string path = directory.Write(
            "deep.config.xml",
            """
            <?xml version="1.0"?>
            <QuickStat>
              <Sites>
                <Site name="north">
                  <Connections>
                    <Connection>
                      <Name>Deep</Name>
                      <StudyName>NDV</StudyName>
                      <ConnectionString>Data Source=srv;Initial Catalog=db;Integrated Security=SSPI</ConnectionString>
                    </Connection>
                  </Connections>
                </Site>
              </Sites>
            </QuickStat>
            """);

        Assert.Equal("Deep", Assert.Single(new XmlConnectionCatalog().Load(path)).Name);
    }

    [Fact]
    public void KeepsTheFirstOfTwoConnectionsWithTheSameName()
    {
        // Delphi: QuickStat.Connections.pas:68-69 freed the later duplicate.
        using TemporaryDirectory directory = new();
        string path = directory.Write("duplicate.config.xml", BuildConfig(
            Entry("Same", "A", "Data Source=a;Initial Catalog=db;Integrated Security=SSPI"),
            Entry("Same", "B", "Data Source=b;Initial Catalog=db;Integrated Security=SSPI")));

        QuickStatConnection connection = Assert.Single(new XmlConnectionCatalog().Load(path));

        Assert.Equal("A", connection.StudyName);
    }

    [Fact]
    public void NamesAreComparedCaseSensitively()
    {
        // Delphi keyed the dictionary with TEqualityComparer<string>.Default, which is ordinal.
        using TemporaryDirectory directory = new();
        string path = directory.Write("case.config.xml", BuildConfig(
            Entry("Same", "A", "Data Source=a;Initial Catalog=db;Integrated Security=SSPI"),
            Entry("SAME", "B", "Data Source=b;Initial Catalog=db;Integrated Security=SSPI")));

        Assert.Equal(2, new XmlConnectionCatalog().Load(path).Count);
    }

    [Fact]
    public void ReturnsConnectionsInDocumentOrder()
    {
        using TemporaryDirectory directory = new();
        string path = directory.Write("order.config.xml", BuildConfig(
            Entry("Zulu", "Z", "Data Source=z;Initial Catalog=db;Integrated Security=SSPI"),
            Entry("Alpha", "A", "Data Source=a;Initial Catalog=db;Integrated Security=SSPI")));

        IReadOnlyList<QuickStatConnection> connections = new XmlConnectionCatalog().Load(path);

        Assert.Equal("Zulu", connections[0].Name);
        Assert.Equal("Alpha", connections[1].Name);
    }

    [Fact]
    public void ReadsTheOptionalSqlOptionsElement()
    {
        // PORT-PLAN.md §8.1: the .NET-only override the Delphi parser ignores, so one file serves
        // both builds.
        using TemporaryDirectory directory = new();
        string path = directory.Write(
            "options.config.xml",
            """
            <?xml version="1.0"?>
            <QuickStat>
              <Connections>
                <Connection>
                  <Name>With options</Name>
                  <StudyName>NDV</StudyName>
                  <ConnectionString>Data Source=srv;Initial Catalog=db;Integrated Security=SSPI</ConnectionString>
                  <SqlOptions>  Encrypt=False;TrustServerCertificate=True  </SqlOptions>
                </Connection>
              </Connections>
            </QuickStat>
            """);

        Assert.Equal(
            "Encrypt=False;TrustServerCertificate=True",
            Assert.Single(new XmlConnectionCatalog().Load(path)).SqlOptions);
    }

    [Fact]
    public void ThrowsWhenTheDocumentIsNotWellFormed()
    {
        using TemporaryDirectory directory = new();
        string path = directory.Write("broken.config.xml", "<QuickStat><Connections></QuickStat>");

        QuickStatConfigurationException exception =
            Assert.Throws<QuickStatConfigurationException>(() => new XmlConnectionCatalog().Load(path));

        Assert.Equal(path, exception.FilePath);
    }

    [Fact]
    public void ThrowsWhenAConnectionOmitsARequiredElement()
    {
        // Delphi read the three children through IXMLNode's default property, which raises when the
        // child is absent (QuickStat.Connections.pas:41-43). Failing loudly beats a connection whose
        // StudyName is silently empty - StudyName gates the whole collector registry.
        using TemporaryDirectory directory = new();
        string path = directory.Write(
            "incomplete.config.xml",
            """
            <?xml version="1.0"?>
            <QuickStat>
              <Connections>
                <Connection>
                  <Name>No study</Name>
                  <ConnectionString>Data Source=srv;Initial Catalog=db;Integrated Security=SSPI</ConnectionString>
                </Connection>
              </Connections>
            </QuickStat>
            """);

        QuickStatConfigurationException exception =
            Assert.Throws<QuickStatConfigurationException>(() => new XmlConnectionCatalog().Load(path));

        Assert.Equal(path, exception.FilePath);
        Assert.Contains("StudyName", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DefaultConfigFileNameFollowsTheExecutableName()
    {
        // Delphi: ChangeFileExt(ParamStr(0), '.config.xml') (MainQuickStat.pas:391). The smoke test
        // pins AssemblyName to QuickStat for exactly this reason.
        Assert.Equal(
            "QuickStat.config.xml",
            XmlConnectionCatalog.DeriveConfigFileName(@"C:\Program Files\FastTrak\bin\QuickStat.exe", null));
    }

    [Fact]
    public void DefaultConfigFileNameFallsBackToTheEntryAssemblyBehindTheDotnetHost()
    {
        Assert.Equal(
            "QuickStat.config.xml",
            XmlConnectionCatalog.DeriveConfigFileName(@"C:\Program Files\dotnet\dotnet.exe", "QuickStat"));

        Assert.Equal("QuickStat.config.xml", XmlConnectionCatalog.DeriveConfigFileName(null, null));
    }

    [Fact]
    public void DefaultConfigFilePathPrefersTheExecutableDirectory()
    {
        // PORT-PLAN.md §4.1: never the working directory, which a shortcut can set to anything.
        using TemporaryDirectory executableDirectory = new();
        using TemporaryDirectory workingDirectory = new();

        string expected = executableDirectory.Write("QuickStat.config.xml", "<QuickStat />");
        workingDirectory.Write("QuickStat.config.xml", "<QuickStat />");

        Assert.Equal(
            expected,
            XmlConnectionCatalog.ResolveDefaultConfigFilePath(
                executableDirectory.Path,
                workingDirectory.Path,
                @"C:\bin\QuickStat.exe",
                null));
    }

    [Fact]
    public void DefaultConfigFilePathFallsBackToTheWorkingDirectory()
    {
        using TemporaryDirectory executableDirectory = new();
        using TemporaryDirectory workingDirectory = new();

        string expected = workingDirectory.Write("QuickStat.config.xml", "<QuickStat />");

        Assert.Equal(
            expected,
            XmlConnectionCatalog.ResolveDefaultConfigFilePath(
                executableDirectory.Path,
                workingDirectory.Path,
                @"C:\bin\QuickStat.exe",
                null));
    }

    [Fact]
    public void DefaultConfigFilePathNamesTheExecutableDirectoryWhenNothingExists()
    {
        using TemporaryDirectory executableDirectory = new();
        using TemporaryDirectory workingDirectory = new();

        Assert.Equal(
            Path.Combine(executableDirectory.Path, "QuickStat.config.xml"),
            XmlConnectionCatalog.ResolveDefaultConfigFilePath(
                executableDirectory.Path,
                workingDirectory.Path,
                @"C:\bin\QuickStat.exe",
                null));
    }

    private static string Entry(string name, string studyName, string connectionString) =>
        $"""
           <Connection>
             <Name>{name}</Name>
             <StudyName>{studyName}</StudyName>
             <ConnectionString>{connectionString}</ConnectionString>
           </Connection>
         """;

    private static string BuildConfig(params string[] entries) =>
        string.Concat(
            "<?xml version=\"1.0\"?>\r\n<QuickStat>\r\n  <Connections>\r\n",
            string.Join("\r\n", entries),
            "\r\n  </Connections>\r\n</QuickStat>\r\n");
}
