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

    /// <summary>The file version, as displayed - e.g. <c>22.12.21.547</c>.</summary>
    /// <remarks>
    /// <b>This reads whatever the assembly reports, and nothing sets it.</b> No
    /// <c>&lt;Version&gt;</c>, <c>&lt;FileVersion&gt;</c> or <c>&lt;InformationalVersion&gt;</c>
    /// appears in <c>Directory.Build.props</c> or in any <c>.csproj</c>, so today it is
    /// <c>1.0.0.0</c> while the shipped Delphi build is <c>22.12.21.547</c> - a date-derived number
    /// produced by the FinalBuilder project. Inventing one here would put a false version in front
    /// of users and in every log line; it is a packaging decision, and it belongs in the build
    /// definition.
    /// </remarks>
    string Version { get; }
}
