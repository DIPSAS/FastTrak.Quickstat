using System.IO;
using QuickStat.Services;
using Xunit;

namespace QuickStat.Tests.Ui.Services;

/// <summary>
/// Resolving <c>EXCEL.EXE</c> from its COM registration, which is what makes
/// <c>Open this dataset in Excel</c> open Excel.
/// </summary>
/// <remarks>
/// <para>
/// <b>The parsing is the part that can go wrong, so it is the part asserted as data.</b> Starting
/// Excel is not something a test may do, and a build agent's registry is not something a test may
/// assume; what is left is the string handling, and the string handling is where the Delphi's own
/// version is unsafe to transcribe. <c>TExcelAdapter</c> splits the <c>LocalServer32</c> value on a
/// space and takes token 0, which survives only because a 32-bit process reads a view where Office
/// wrote the path quoted.
/// </para>
/// <para>
/// Both shapes below were read off this machine's registry, one per view, for the same CLSID:
/// </para>
/// <code>
/// HKLM\Software\Classes\CLSID\{00024500-...}\LocalServer32
///   64-bit view:  C:\Program Files\Microsoft Office\Root\Office16\EXCEL.EXE /automation
///   32-bit view: "C:\Program Files\Microsoft Office\Root\Office16\EXCEL.EXE" /automation
/// </code>
/// </remarks>
public class ExcelLocatorTests
{
    /// <summary>Command line and the executable inside it.</summary>
    public static TheoryData<string, string> Commands =>
        new()
        {
            // What a 32-bit client sees.  The only shape the Delphi's space-split handles.
            {
                "\"C:\\Program Files\\Microsoft Office\\Root\\Office16\\EXCEL.EXE\" /automation",
                "C:\\Program Files\\Microsoft Office\\Root\\Office16\\EXCEL.EXE"
            },

            // What a 64-bit client sees.  Unquoted, and the directory has a space in it: a
            // space-split answers "C:\Program".
            {
                "C:\\Program Files\\Microsoft Office\\Root\\Office16\\EXCEL.EXE /automation",
                "C:\\Program Files\\Microsoft Office\\Root\\Office16\\EXCEL.EXE"
            },

            // Older Office writes the 8.3 form, which has no space and would have worked either way.
            {
                "C:\\PROGRA~1\\MICROS~1\\Office16\\EXCEL.EXE /automation",
                "C:\\PROGRA~1\\MICROS~1\\Office16\\EXCEL.EXE"
            },

            // No switch at all.
            {
                "C:\\Program Files\\Microsoft Office\\Root\\Office16\\EXCEL.EXE",
                "C:\\Program Files\\Microsoft Office\\Root\\Office16\\EXCEL.EXE"
            },

            // Trailing whitespace, which a registry value is free to carry.
            {
                "  C:\\Office\\EXCEL.EXE /automation  ",
                "C:\\Office\\EXCEL.EXE"
            },

            // Lower case; the registry is not consistent about it.
            {
                "c:\\office\\excel.exe /automation",
                "c:\\office\\excel.exe"
            },

            // More than one switch.
            {
                "C:\\Office\\EXCEL.EXE /automation /embedding",
                "C:\\Office\\EXCEL.EXE"
            },
        };

    /// <summary>Values that are not a command line, and must not be turned into one.</summary>
    public static TheoryData<string?> NotCommands =>
        [
            null,
            "",
            "   ",

            // A ProgId-style value, not a path: no .exe, so nothing to start.
            "Excel.Application.16",

            // An opening quote and no closing one.
            "\"C:\\Program Files\\Microsoft Office\\Root\\Office16\\EXCEL.EXE /automation",

            // ".exe" inside a word rather than at its end.
            "C:\\Office\\EXCEL.exefoo /automation",
        ];

    [Theory]
    [MemberData(nameof(Commands))]
    public void TheExecutableIsPulledOutOfTheCommandLine(string command, string expected) =>
        Assert.Equal(expected, ExcelLocator.ParseLocalServerCommand(command));

    [Theory]
    [MemberData(nameof(NotCommands))]
    public void AValueThatNamesNoExecutableAnswersNothing(string? command) =>
        Assert.Null(ExcelLocator.ParseLocalServerCommand(command));

    [Fact]
    public void ASplitOnTheFirstSpaceWouldGetTheSixtyFourBitViewWrong()
    {
        // The Delphi's rule, spelled out, so the reason this class is not a transcription of
        // Emetra.Adapters.Office.pas is in the suite rather than only in a comment.
        const string Command = "C:\\Program Files\\Microsoft Office\\Root\\Office16\\EXCEL.EXE /automation";

        Assert.Equal("C:\\Program", Command.Split(' ')[0]);
        Assert.Equal(
            "C:\\Program Files\\Microsoft Office\\Root\\Office16\\EXCEL.EXE",
            ExcelLocator.ParseLocalServerCommand(Command));
    }

    [Fact]
    public void WhateverThisMachineAnswersIsAnExcelThatExists()
    {
        // Deliberately not "Excel is installed": that would fail on an agent, and where Excel is
        // absent the answer must be null so ShellProcessLauncher can fall back.  What holds
        // everywhere is that a non-null answer is a real EXCEL.EXE - which is what Verify is for,
        // since an uninstalled Office routinely leaves its registration behind.
        string? found = ExcelLocator.Find();

        if (found is null)
        {
            return;
        }

        Assert.True(File.Exists(found), $"ExcelLocator answered '{found}', which is not there.");
        Assert.Equal("excel.exe", Path.GetFileName(found).ToLowerInvariant());
    }
}
