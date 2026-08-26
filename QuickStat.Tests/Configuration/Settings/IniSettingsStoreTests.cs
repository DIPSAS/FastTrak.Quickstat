using System.Globalization;
using System.IO;
using System.Text;
using QuickStat.Configuration.Settings;
using Xunit;

namespace QuickStat.Tests.Configuration.Settings;

/// <summary>
/// Round-tripping, deleting, flushing, and every degraded file the store has to survive.
/// </summary>
public class IniSettingsStoreTests
{
    /// <summary>
    /// Values chosen to break a Win32 INI file: the characters <c>WritePrivateProfileString</c>
    /// cannot store in a key, the section brackets, line breaks in both conventions, edge
    /// whitespace, and the Norwegian characters the whole application is written in.
    /// </summary>
    private static readonly string[] Hostile =
    [
        string.Empty,
        "plain",
        "with = equals",
        "=leading equals",
        "trailing equals=",
        "[bracketed]",
        "]close[",
        "a\nb",
        "a\r\nb",
        "a\rb",
        "trailing newline\n",
        "\nleading newline",
        "a\tb",
        " leading space",
        "trailing space ",
        " ",
        "   ",
        "back\\slash",
        "double\\\\slash",
        "trailing backslash\\",
        @"\n not a newline",
        "; not a comment",
        "# not a comment either",
        "Fødselsdato, Ålesund, blåbærsyltetøy ÆØÅ",
        "Labdata: Interleukiner (siste)",
        "Autommunitet (siste)",
        "quote\"and'apostrophe",
        "null\0byte",
        "emoji \U0001F52C og en gresk π",
    ];

    /// <summary>Every hostile string, used as a value.</summary>
    public static TheoryData<string> HostileStrings => Build(Hostile);

    /// <summary>Every hostile string that can also be a name; a name may not be empty.</summary>
    public static TheoryData<string> HostileNames => Build(Hostile.Where(value => value.Length > 0));

    [Theory]
    [MemberData(nameof(HostileStrings))]
    public void AStringRoundTripsThroughTheFile(string value)
    {
        using TemporarySettingsFile file = new();

        using (IniSettingsStore writing = file.Open())
        {
            writing.SetString("Section", "Key", value);
            writing.Flush();
        }

        using IniSettingsStore reading = file.Open();

        Assert.Equal(value, reading.GetString("Section", "Key", "not read"));
    }

    [Theory]
    [MemberData(nameof(HostileNames))]
    public void ASectionAndAKeyRoundTripThroughTheFile(string name)
    {
        // The Delphi could not do this at all: WritePrivateProfileString rejects "=" and newlines in
        // a key, which is precisely why the remembered period - keyed on the whole SQL text - never
        // came back (Docs/Port/01-data-access.md §6.3).
        using TemporarySettingsFile file = new();

        using (IniSettingsStore writing = file.Open())
        {
            writing.SetString(name, name, "value");
            writing.Flush();
        }

        using IniSettingsStore reading = file.Open();

        Assert.True(reading.Contains(name, name));
        Assert.Equal("value", reading.GetString(name, name, "not read"));
    }

    [Fact]
    public void AWholeMultiLineSqlQueryWorksAsAKey()
    {
        // The shape step 2.3 will hash, tried unhashed to prove the store is not the constraint.
        const string Sql = """
            SELECT p.PersonId, p.FullName
            FROM   dbo.Person p
            WHERE  p.Included >= :PeriodStart
               AND p.Included <  :PeriodEnd
               AND p.[Group]   = 'GBD'
            """;

        using TemporarySettingsFile file = new();

        using (IniSettingsStore writing = file.Open())
        {
            writing.SetDateTime("PeriodStart", Sql, new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            writing.Flush();
        }

        using IniSettingsStore reading = file.Open();

        Assert.Equal(
            new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            reading.GetDateTime("PeriodStart", Sql, DateTime.MinValue));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    [InlineData(1920)]
    public void AnInt32RoundTrips(int value)
    {
        using TemporarySettingsFile file = new();

        using (IniSettingsStore writing = file.Open())
        {
            writing.SetInt32("S", "K", value);
            writing.Flush();
        }

        using IniSettingsStore reading = file.Open();

        Assert.Equal(value, reading.GetInt32("S", "K", 4711));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ABooleanRoundTrips(bool value)
    {
        using TemporarySettingsFile file = new();

        using (IniSettingsStore writing = file.Open())
        {
            writing.SetBoolean("S", "K", value);
            writing.Flush();
        }

        using IniSettingsStore reading = file.Open();

        Assert.Equal(value, reading.GetBoolean("S", "K", !value));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1d)]
    [InlineData(-1d)]
    [InlineData(3.14159265358979d)]
    [InlineData(0.1d)]
    [InlineData(1e-300d)]
    [InlineData(1.7976931348623157E+308d)]
    [InlineData(-1.7976931348623157E+308d)]
    [InlineData(double.Epsilon)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ADoubleRoundTripsExactly(double value)
    {
        using TemporarySettingsFile file = new();

        using (IniSettingsStore writing = file.Open())
        {
            writing.SetDouble("S", "K", value);
            writing.Flush();
        }

        using IniSettingsStore reading = file.Open();

        Assert.Equal(value, reading.GetDouble("S", "K", 4711d));
    }

    [Fact]
    public void ADoubleIsNotWrittenWithTheOperatingSystemDecimalSeparator()
    {
        // The exported CSV deliberately uses the locale separator (PORT-PLAN.md §6), so
        // InvariantGlobalization is off and CurrentCulture may well be nb-NO. A settings file must
        // not move with it, or the file stops parsing when the user changes their locale.
        using TemporarySettingsFile file = new();

        using (IniSettingsStore writing = file.Open())
        {
            writing.SetDouble("S", "K", 3.5d);
            writing.Flush();
        }

        Assert.Contains("3.5", file.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ADateTimeRoundTripsIncludingKindAndSubSeconds()
    {
        DateTime value = new(2026, 8, 26, 10, 42, 33, 123, DateTimeKind.Utc);

        using TemporarySettingsFile file = new();

        using (IniSettingsStore writing = file.Open())
        {
            writing.SetDateTime("S", "K", value);
            writing.Flush();
        }

        using IniSettingsStore reading = file.Open();

        DateTime read = reading.GetDateTime("S", "K", DateTime.MinValue);

        Assert.Equal(value, read);
        Assert.Equal(DateTimeKind.Utc, read.Kind);
    }

    [Fact]
    public void ALocalDateTimeKeepsItsKind()
    {
        DateTime value = new(2026, 8, 26, 10, 42, 33, DateTimeKind.Local);

        using TemporarySettingsFile file = new();

        using (IniSettingsStore writing = file.Open())
        {
            writing.SetDateTime("S", "K", value);
            writing.Flush();
        }

        using IniSettingsStore reading = file.Open();

        Assert.Equal(DateTimeKind.Local, reading.GetDateTime("S", "K", DateTime.MinValue).Kind);
    }

    [Fact]
    public void ALegacyDelphiDateTimeIsStillReadable()
    {
        // Delphi's WriteDateTime used the thread locale, forced to the user default at
        // Emetra.Settings.IniFile.pas:495-496. On a Norwegian machine that is dd.MM.yyyy HH:mm:ss.
        using TemporarySettingsFile file = new("[S]\r\nK=26.08.2026 10:42:33\r\n");
        using IniSettingsStore store = file.Open();

        Assert.Equal(
            new DateTime(2026, 8, 26, 10, 42, 33, DateTimeKind.Unspecified),
            store.GetDateTime("S", "K", DateTime.MinValue));
    }

    [Fact]
    public void ALegacyDelphiBooleanAndFloatAreStillReadable()
    {
        // TIniFile.WriteBool wrote 1 and 0; WriteFloat used the locale decimal separator.
        using TemporarySettingsFile file = new("[S]\r\nYes=1\r\nNo=0\r\nPi=3,25\r\n");
        using IniSettingsStore store = file.Open();

        Assert.True(store.GetBoolean("S", "Yes"));
        Assert.False(store.GetBoolean("S", "No", defaultValue: true));
        Assert.Equal(3.25d, store.GetDouble("S", "Pi"));
    }

    [Fact]
    public void AMissingFileIsAnEmptyStoreRatherThanAnError()
    {
        using TemporarySettingsFile file = new();

        Assert.False(file.Exists);

        using IniSettingsStore store = file.Open();

        Assert.False(store.Contains("S", "K"));
        Assert.Equal("fallback", store.GetString("S", "K", "fallback"));
        Assert.Equal(0, store.SkippedLineCount);
    }

    [Fact]
    public void FlushingAnUnchangedStoreDoesNotCreateAFile()
    {
        using TemporarySettingsFile file = new();
        using IniSettingsStore store = file.Open();

        store.Flush();

        Assert.False(file.Exists);
    }

    [Fact]
    public void AMissingSectionAndAMissingKeyBothReturnTheDefault()
    {
        using TemporarySettingsFile file = new("[Present]\r\nHere=1\r\n");
        using IniSettingsStore store = file.Open();

        Assert.Equal("d", store.GetString("Absent", "Here", "d"));
        Assert.Equal("d", store.GetString("Present", "Absent", "d"));
        Assert.False(store.Contains("Absent", "Here"));
        Assert.False(store.Contains("Present", "Absent"));
    }

    [Theory]
    [InlineData("Value that is not a number")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("3.5")]
    public void AnUnparsableValueFallsBackToTheDefault(string stored)
    {
        using TemporarySettingsFile file = new();

        using (IniSettingsStore writing = file.Open())
        {
            writing.SetString("S", "K", stored);
            writing.Flush();
        }

        using IniSettingsStore reading = file.Open();

        Assert.Equal(4711, reading.GetInt32("S", "K", 4711));
        Assert.True(reading.GetBoolean("S", "K", defaultValue: true));
        Assert.Equal(DateTime.MaxValue, reading.GetDateTime("S", "K", DateTime.MaxValue));
    }

    [Fact]
    public void ACorruptFileLoadsWhatItCanAndCountsTheRest()
    {
        // Hand-edited, half-written, or a completely different file that happens to share the name.
        const string Corrupt = """
            [Good]
            Kept=1
            this line has no equals sign
            [Unterminated
            AlsoKept=2

            ; a comment
            # another comment
            =value with no key
            [Second]
            Third=3
            """;

        using TemporarySettingsFile file = new(Corrupt);
        using IniSettingsStore store = file.Open();

        Assert.Equal("1", store.GetString("Good", "Kept"));
        Assert.Equal("2", store.GetString("Good", "AlsoKept"));
        Assert.Equal("3", store.GetString("Second", "Third"));
        Assert.Equal(3, store.SkippedLineCount);
    }

    [Fact]
    public void BinaryRubbishDoesNotThrow()
    {
        using TemporarySettingsFile file = new();

        File.WriteAllBytes(file.FilePath, [0x00, 0xFF, 0xFE, 0x01, 0x02, 0x03, 0x41, 0x42]);

        using IniSettingsStore store = file.Open();

        Assert.Equal("d", store.GetString("S", "K", "d"));
    }

    [Fact]
    public void ADirectoryWhereTheFileShouldBeDoesNotThrow()
    {
        using TemporarySettingsFile file = new();

        Directory.CreateDirectory(file.FilePath);

        using IniSettingsStore store = file.Open();

        Assert.Equal("d", store.GetString("S", "K", "d"));

        store.SetString("S", "K", "v");

        // Flush must never throw, whatever the file system says.
        store.Flush();

        Assert.True(store.HasUnsavedChanges);
    }

    [Fact]
    public void RemoveDeletesAKeyTheDelphiCouldNeverClear()
    {
        using TemporarySettingsFile file = new();

        using (IniSettingsStore writing = file.Open())
        {
            writing.SetString("S", "First", "1");
            writing.SetString("S", "Second", "2");
            writing.Remove("S", "First");
            writing.Flush();
        }

        using IniSettingsStore reading = file.Open();

        Assert.False(reading.Contains("S", "First"));
        Assert.True(reading.Contains("S", "Second"));
    }

    [Fact]
    public void RemovingTheLastKeyRemovesTheSectionToo()
    {
        using TemporarySettingsFile file = new();

        using (IniSettingsStore writing = file.Open())
        {
            writing.SetString("S", "Only", "1");
            writing.Flush();
        }

        using (IniSettingsStore removing = file.Open())
        {
            removing.Remove("S", "Only");
            removing.Flush();
        }

        Assert.DoesNotContain("[S]", file.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void RemovingSomethingAbsentIsNotAnError()
    {
        using TemporarySettingsFile file = new();
        using IniSettingsStore store = file.Open();

        store.Remove("Nowhere", "Nothing");

        Assert.False(store.HasUnsavedChanges);
    }

    [Fact]
    public void NothingReachesDiskBeforeFlush()
    {
        // Unlike the Delphi, which committed on every WritePrivateProfileString call.
        using TemporarySettingsFile file = new();
        using IniSettingsStore store = file.Open();

        store.SetString("S", "K", "v");

        Assert.False(file.Exists);
        Assert.True(store.HasUnsavedChanges);

        store.Flush();

        Assert.True(file.Exists);
        Assert.False(store.HasUnsavedChanges);
    }

    [Fact]
    public void DisposeFlushes()
    {
        // The container disposes singletons at shutdown, which is the ".. and on process exit" half
        // of Docs/Port/01-data-access.md §3.5.
        using TemporarySettingsFile file = new();

        using (IniSettingsStore store = file.Open())
        {
            store.SetString("S", "K", "v");
        }

        using IniSettingsStore reading = file.Open();

        Assert.Equal("v", reading.GetString("S", "K"));
    }

    [Fact]
    public void FlushCreatesTheDirectoryItNeeds()
    {
        // The Delphi never created LOGS\ and lost every log line (PORT-PLAN.md §7.2). Same class of
        // defect, same fix.
        using TemporarySettingsFile file = new();
        using IniSettingsStore store = new(file.NestedFilePath);

        store.SetString("S", "K", "v");
        store.Flush();

        Assert.True(File.Exists(file.NestedFilePath));
    }

    [Fact]
    public void FlushLeavesNoTemporaryFileBehind()
    {
        using TemporarySettingsFile file = new();
        using IniSettingsStore store = file.Open();

        store.SetString("S", "K", "v");
        store.Flush();

        Assert.False(File.Exists(file.FilePath + ".tmp"));
    }

    [Fact]
    public void TheFileIsWrittenAsUtf8WithoutAByteOrderMark()
    {
        using TemporarySettingsFile file = new();
        using IniSettingsStore store = file.Open();

        store.SetString("S", "Norsk", "Fødselsnummer");
        store.Flush();

        byte[] bytes = file.Bytes;

        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "No BOM expected.");
        Assert.Contains("Fødselsnummer", Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void AFileWithAByteOrderMarkIsStillReadable()
    {
        using TemporarySettingsFile file = new();

        File.WriteAllText(file.FilePath, "[S]\r\nK=Fødselsdato\r\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        using IniSettingsStore store = file.Open();

        Assert.Equal("Fødselsdato", store.GetString("S", "K"));
    }

    [Fact]
    public void SectionsAndKeysAreCaseInsensitive()
    {
        // Win32 INI semantics, and what Docs/Port/01-data-access.md §3.5 specifies.
        using TemporarySettingsFile file = new();
        using IniSettingsStore store = file.Open();

        store.SetString("Section", "Key", "v");

        Assert.True(store.Contains("SECTION", "KEY"));
        Assert.Equal("v", store.GetString("section", "key"));
    }

    [Fact]
    public void WritingTheSameValueTwiceDoesNotDirtyTheStore()
    {
        using TemporarySettingsFile file = new();

        using (IniSettingsStore writing = file.Open())
        {
            writing.SetString("S", "K", "v");
            writing.Flush();
        }

        using IniSettingsStore reading = file.Open();

        reading.SetString("S", "K", "v");

        Assert.False(reading.HasUnsavedChanges);
    }

    [Fact]
    public void SectionOrderAndKeyOrderAreStable()
    {
        // A settings file that reshuffles itself on every save is unreadable in a diff and looks
        // like tampering to anyone watching the roaming profile.
        using TemporarySettingsFile file = new();

        using (IniSettingsStore writing = file.Open())
        {
            writing.SetString("Zeta", "b", "1");
            writing.SetString("Zeta", "a", "2");
            writing.SetString("Alpha", "z", "3");
            writing.Flush();
        }

        string first = file.Text;

        using (IniSettingsStore rewriting = file.Open())
        {
            rewriting.SetString("Zeta", "b", "changed");
            rewriting.Flush();
        }

        string second = file.Text;

        Assert.True(first.IndexOf("[Zeta]", StringComparison.Ordinal) < first.IndexOf("[Alpha]", StringComparison.Ordinal));
        Assert.Equal(
            first.Replace("b=1", "b=changed", StringComparison.Ordinal),
            second);
    }

    [Fact]
    public void ManyLongKeysAreFine()
    {
        // "Long keys must be safe" - the hashed period key is short, but nothing enforces that.
        string longName = new('k', 8000);

        using TemporarySettingsFile file = new();

        using (IniSettingsStore writing = file.Open())
        {
            writing.SetString(longName, longName, longName);
            writing.Flush();
        }

        using IniSettingsStore reading = file.Open();

        Assert.Equal(longName, reading.GetString(longName, longName));
    }

    [Fact]
    public void AHandWrittenWindowsPathIsNotMangled()
    {
        // The most likely thing anyone would type into this file by hand, and the reason unknown
        // backslash escapes are kept rather than dropped.
        using TemporarySettingsFile file = new(@"[QuickStat]" + "\r\n" + @"LastExportFolder=C:\Users\ola\Eksport" + "\r\n");
        using IniSettingsStore store = file.Open();

        Assert.Equal(@"C:\Users\ola\Eksport", store.GetString("QuickStat", "LastExportFolder"));
    }

    [Fact]
    public void TheUnnamedLeadingBlockIsAddressable()
    {
        using TemporarySettingsFile file = new("Loose=1\r\n[Named]\r\nInside=2\r\n");
        using IniSettingsStore store = file.Open();

        Assert.Equal("1", store.GetString(string.Empty, "Loose"));
        Assert.Equal("2", store.GetString("Named", "Inside"));
    }

    [Fact]
    public void ArgumentsAreValidated()
    {
        using TemporarySettingsFile file = new();
        using IniSettingsStore store = file.Open();

        Assert.Throws<ArgumentNullException>(() => { _ = store.GetString(null!, "K"); });
        Assert.Throws<ArgumentNullException>(() => store.SetString("S", "K", null!));
        Assert.Throws<ArgumentException>(() => { _ = store.GetString("S", string.Empty); });
        Assert.Throws<ArgumentException>(() => { _ = store.Contains("S", string.Empty); });
        Assert.Throws<ArgumentException>(() => store.SetInt32("S", string.Empty, 1));
        Assert.Throws<ArgumentException>(() => store.Remove("S", string.Empty));
        Assert.Throws<ArgumentException>(() => { _ = new IniSettingsStore(string.Empty); });
        Assert.Throws<ArgumentNullException>(() => { _ = new IniSettingsStore(null!); });
    }

    private static TheoryData<string> Build(IEnumerable<string> values)
    {
        TheoryData<string> data = new();

        foreach (string value in values)
        {
            data.Add(value);
        }

        return data;
    }

    [Fact]
    public void FilePathIsAlwaysAbsolute()
    {
        using TemporarySettingsFile file = new();
        using IniSettingsStore store = file.Open();

        Assert.True(Path.IsPathFullyQualified(store.FilePath));
        Assert.Equal(file.FilePath, store.FilePath);
    }

    [Fact]
    public void ConcurrentWritersDoNotCorruptTheStore()
    {
        using TemporarySettingsFile file = new();
        using IniSettingsStore store = file.Open();

        Parallel.For(0, 200, index =>
        {
            store.SetInt32("Concurrent", index.ToString(CultureInfo.InvariantCulture), index);
            store.Flush();
        });

        using IniSettingsStore reading = file.Open();

        for (int index = 0; index < 200; index++)
        {
            Assert.Equal(index, reading.GetInt32("Concurrent", index.ToString(CultureInfo.InvariantCulture), -1));
        }
    }
}
