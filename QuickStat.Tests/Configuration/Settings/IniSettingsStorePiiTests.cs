using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using QuickStat.Configuration.Settings;
using QuickStat.Diagnostics;
using QuickStat.Tests.Diagnostics;
using Xunit;

namespace QuickStat.Tests.Configuration.Settings;

/// <summary>
/// The privacy guarantee of the settings store: an identifier cannot reach the file, by any public
/// path, in any position.
/// </summary>
/// <remarks>
/// R6 in PORT-PLAN.md §9 treats a privacy regression as release-blocking. A settings file lives in
/// a roaming profile, is backed up, and is never reviewed by anyone, so anything that accumulates
/// there stays there.
/// </remarks>
public class IniSettingsStorePiiTests
{
    private const string Fnr = PiiRedactorTests.ValidFodselsnummer;
    private const string OtherFnr = PiiRedactorTests.AnotherFodselsnummer;
    private const string ThirdFnr = PiiRedactorTests.ThirdFodselsnummer;

    [Fact]
    public void NoPublicWritePathCanPutAnIdentifierInTheFile()
    {
        // Exhaustive by construction rather than by inspection: every Set* method declared on the
        // contract is invoked with an identifier in the section, in the key, and - where the
        // parameter type can carry one - in the value. A setter added later is covered
        // automatically, and this test fails if it is not redacted.
        MethodInfo[] setters = typeof(ISettingsStore)
            .GetMethods()
            .Where(method => method.Name.StartsWith("Set", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(5, setters.Length);

        using TemporarySettingsFile file = new();
        using IniSettingsStore store = file.Open();

        foreach (MethodInfo setter in setters)
        {
            ParameterInfo[] parameters = setter.GetParameters();

            Assert.Equal(3, parameters.Length);

            setter.Invoke(store, [$"Section {Fnr}", $"Key {OtherFnr}", ValueFor(parameters[2].ParameterType)]);
        }

        store.Flush();

        string content = file.Text;

        Assert.DoesNotContain(Fnr, content, StringComparison.Ordinal);
        Assert.DoesNotContain(OtherFnr, content, StringComparison.Ordinal);
        Assert.DoesNotContain(ThirdFnr, content, StringComparison.Ordinal);
        Assert.Contains(PiiRedactor.Replacement, content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PiiRedactorTests.ValidFodselsnummer)]
    [InlineData(PiiRedactorTests.AnotherFodselsnummer)]
    [InlineData(PiiRedactorTests.ValidDNumber)]
    [InlineData(PiiRedactorTests.ValidHNumber)]
    [InlineData(PiiRedactorTests.ValidSyntheticNumber)]
    [InlineData(PiiRedactorTests.ValidFhNumber)]
    public void EveryKindOfIdentityNumberIsRedacted(string identifier)
    {
        using TemporarySettingsFile file = new();
        using IniSettingsStore store = file.Open();

        store.SetString("S", "K", $"noted: {identifier}");
        store.Flush();

        Assert.DoesNotContain(identifier, file.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void AMarkedNameIsRedacted()
    {
        // A patient name cannot be recognised by shape, so the {{ }} convention is the only defence.
        using TemporarySettingsFile file = new();
        using IniSettingsStore store = file.Open();

        store.SetString("LastExport", "Comment", "Eksport for {{Ola Nordmann}}");
        store.Flush();

        Assert.DoesNotContain("Ola Nordmann", file.Text, StringComparison.Ordinal);
        Assert.Contains(PiiRedactor.Replacement, file.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void AnIdentifierInsidePopulationSqlIsRedacted()
    {
        // The realistic route: 2.3 stores a period against a query, and someone has hard-coded a
        // patient into the query's WHERE clause.
        string sql = $"SELECT * FROM dbo.Person WHERE NationalId = '{Fnr}'";

        using TemporarySettingsFile file = new();
        using IniSettingsStore store = file.Open();

        store.SetDateTime("PeriodStart", sql, DateTime.UnixEpoch);
        store.Flush();

        Assert.DoesNotContain(Fnr, file.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void AHashedKeyCarriesNoFragmentOfTheQuery()
    {
        // Why 2.3 hashes rather than relying on the store surviving the raw text.
        string sql = $"SELECT * FROM dbo.Person WHERE NationalId = '{Fnr}'";
        string section = SettingsKey.ForText("Period", sql);

        using TemporarySettingsFile file = new();
        using IniSettingsStore store = file.Open();

        store.SetDateTime(section, "Start", DateTime.UnixEpoch);
        store.Flush();

        Assert.DoesNotContain(Fnr, file.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("NationalId", file.Text, StringComparison.Ordinal);
        Assert.Contains(section, file.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ARedactedValueIsWhatComesBackOut()
    {
        // Redaction is not cosmetic: the identifier is gone from memory as well as from the file,
        // so nothing downstream can recover it from the store.
        using TemporarySettingsFile file = new();
        using IniSettingsStore store = file.Open();

        store.SetString("S", "K", Fnr);

        Assert.Equal(PiiRedactor.Replacement, store.GetString("S", "K"));
    }

    [Fact]
    public void AWriteAndAReadWithTheSameIdentifierStillAgree()
    {
        // Both are redacted, so the redacted name is the lookup name and the entry is still
        // findable. Anything else would make redaction quietly lose settings.
        using TemporarySettingsFile file = new();
        using IniSettingsStore store = file.Open();

        store.SetInt32($"Window {Fnr}", $"Left {OtherFnr}", 100);

        Assert.True(store.Contains($"Window {Fnr}", $"Left {OtherFnr}"));
        Assert.Equal(100, store.GetInt32($"Window {Fnr}", $"Left {OtherFnr}", -1));

        store.Remove($"Window {Fnr}", $"Left {OtherFnr}");

        Assert.False(store.Contains($"Window {Fnr}", $"Left {OtherFnr}"));
    }

    [Fact]
    public void AnIdentifierAlreadyInTheFileIsRemovedOnLoadAndScrubbedOnTheNextSave()
    {
        // An inherited file, or one edited by hand. Redacting only on write would let the store read
        // an identifier back into memory and write it out again unchanged.
        using TemporarySettingsFile file = new($"[S]\r\nK={Fnr}\r\n[Window {OtherFnr}]\r\nLeft=1\r\n");

        using (IniSettingsStore store = file.Open())
        {
            Assert.Equal(PiiRedactor.Replacement, store.GetString("S", "K", "not found"));
            Assert.True(store.HasUnsavedChanges, "Finding an identifier should schedule a clean-up.");

            store.Flush();
        }

        Assert.DoesNotContain(Fnr, file.Text, StringComparison.Ordinal);
        Assert.DoesNotContain(OtherFnr, file.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void AFileWithoutIdentifiersIsNotRewrittenJustForBeingRead()
    {
        using TemporarySettingsFile file = new("[S]\r\nK=1\r\n");
        using IniSettingsStore store = file.Open();

        Assert.False(store.HasUnsavedChanges);
    }

    [Fact]
    public void RedactionIsNotSomethingACallerCanTurnOff()
    {
        // Structural: no property, no constructor argument and no options type on the store mentions
        // redaction, so there is no switch to find.
        string[] settables = typeof(IniSettingsStore)
            .GetProperties()
            .Where(property => property.CanWrite && property.SetMethod?.IsPublic == true)
            .Select(property => property.Name)
            .ToArray();

        Assert.Empty(settables);

        Type[] constructorParameters = typeof(IniSettingsStore)
            .GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .Distinct()
            .ToArray();

        Assert.All(constructorParameters, type => Assert.True(
            type == typeof(string) || type == typeof(ILogger<IniSettingsStore>),
            $"Unexpected constructor parameter type {type}."));
    }

    [Fact]
    public void OrdinarySettingsAreUntouched()
    {
        // The other half of getting redaction right: it must not corrupt what it is protecting.
        using TemporarySettingsFile file = new();

        using (IniSettingsStore store = file.Open())
        {
            store.SetInt32("frmQuickStat.1920x1080", "Left", 100);
            store.SetInt32("frmQuickStat.1920x1080", "Width", 1920);
            store.SetBoolean("QuickStat", "ExportDates", value: true);
            store.SetString("QuickStat", "LastExportFolder", @"C:\Temp\Eksport");
            store.SetDouble("QuickStat", "SplitterPosition", 0.375d);
            store.Flush();
        }

        using IniSettingsStore reading = file.Open();

        Assert.Equal(100, reading.GetInt32("frmQuickStat.1920x1080", "Left"));
        Assert.Equal(1920, reading.GetInt32("frmQuickStat.1920x1080", "Width"));
        Assert.True(reading.GetBoolean("QuickStat", "ExportDates"));
        Assert.Equal(@"C:\Temp\Eksport", reading.GetString("QuickStat", "LastExportFolder"));
        Assert.Equal(0.375d, reading.GetDouble("QuickStat", "SplitterPosition"));
    }

    [Fact]
    public void TheFileBytesNeverContainTheIdentifierInAnyEncoding()
    {
        // Belt and braces: assert on the raw bytes, not on a decoded string, so an encoding quirk
        // cannot hide a leak.
        using TemporarySettingsFile file = new();
        using IniSettingsStore store = file.Open();

        store.SetString($"S{Fnr}", $"K{OtherFnr}", $"V{ThirdFnr}");
        store.Flush();

        byte[] bytes = file.Bytes;

        foreach (string identifier in new[] { Fnr, OtherFnr, ThirdFnr })
        {
            Assert.DoesNotContain(identifier, Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
            Assert.False(
                ContainsAscii(bytes, identifier),
                $"The file bytes contain {identifier}.");
        }
    }

    /// <summary>A value of the right type for a discovered setter, carrying an identifier where the type allows one.</summary>
    private static object ValueFor(Type valueType)
    {
        if (valueType == typeof(string))
        {
            return $"Value {ThirdFnr}";
        }

        if (valueType == typeof(double))
        {
            // A double really can render as an eleven-digit identity number, so this is not a
            // theoretical hole: 31128500181 is exactly representable and formats back as itself.
            return double.Parse(OtherFnr, CultureInfo.InvariantCulture);
        }

        if (valueType == typeof(int))
        {
            return 1920;
        }

        if (valueType == typeof(bool))
        {
            return true;
        }

        if (valueType == typeof(DateTime))
        {
            return DateTime.UnixEpoch;
        }

        throw new InvalidOperationException($"Unhandled setter value type {valueType}.");
    }

    private static bool ContainsAscii(byte[] haystack, string needle)
    {
        byte[] pattern = Encoding.ASCII.GetBytes(needle);

        for (int start = 0; start + pattern.Length <= haystack.Length; start++)
        {
            if (haystack.AsSpan(start, pattern.Length).SequenceEqual(pattern))
            {
                return true;
            }
        }

        return false;
    }
}
