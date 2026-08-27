namespace QuickStat.Services;

/// <summary>What the banner says about the running build.</summary>
/// <remarks>
/// Delphi: <c>TRzVersionInfoStatus</c> bound to <c>vifFileVersion</c>, rendered as the word
/// <c>version</c> in <c>#0078D7</c> followed by the number in black
/// (<c>05-ui-spec.md</c> §A.2, §F.2). Behind an interface so the banner can be asserted without
/// depending on whatever version this particular build happens to carry.
/// </remarks>
public interface IApplicationInfo
{
    /// <summary>Window and taskbar title. <c>FastTrak QuickStat</c>.</summary>
    string Title { get; }

    /// <summary>The wordmark next to the icon. <c>QuickStat</c>.</summary>
    string ProductName { get; }

    /// <summary>The file version, as displayed - e.g. <c>26.0.0.0</c>.</summary>
    /// <remarks>
    /// <b>This reads whatever the assembly reports, and one property sets it.</b>
    /// <c>&lt;Version&gt;26.0.0.0&lt;/Version&gt;</c> in <c>Directory.Build.props</c>, decided by the
    /// product owner at the start of Phase 4 (PORT-PLAN.md §8.9 b). MSBuild derives
    /// <c>AssemblyVersion</c>, <c>FileVersion</c> and <c>InformationalVersion</c> from it, and this
    /// reads <c>AssemblyFileVersion</c> - which is what the Delphi bound to - so the single property
    /// covers the banner, the file properties, the <c>@AppVer</c> sent to <c>dbo.AddSession</c> and
    /// the start-up log line. The scheme continues the Delphi build's year-led numbering without
    /// pretending to be one of its dated builds; the shipped Delphi build is <c>22.12.21.547</c>.
    /// </remarks>
    string Version { get; }
}
