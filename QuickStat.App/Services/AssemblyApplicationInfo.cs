using System.Reflection;

namespace QuickStat.Services;

/// <summary>Reads <see cref="IApplicationInfo"/> out of the assembly's own metadata.</summary>
public sealed class AssemblyApplicationInfo : IApplicationInfo
{
    /// <summary>Window and taskbar title. Delphi <c>frmQuickStat.Caption</c>.</summary>
    public const string ApplicationTitle = "FastTrak QuickStat";

    /// <summary>The wordmark. Delphi <c>lblAppName.Caption</c>.</summary>
    public const string Wordmark = "QuickStat";

    /// <summary>What <see cref="Version"/> falls back to when the assembly carries no version at all.</summary>
    public const string UnknownVersion = "0.0.0.0";

    private readonly string _version;

    /// <summary>Reads the version from the assembly this type is defined in.</summary>
    public AssemblyApplicationInfo()
        : this(typeof(AssemblyApplicationInfo).Assembly)
    {
    }

    /// <summary>Reads the version from a specific assembly.</summary>
    /// <param name="assembly">The assembly whose version to display.</param>
    /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is <see langword="null"/>.</exception>
    public AssemblyApplicationInfo(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        _version = ReadVersion(assembly);
    }

    /// <inheritdoc />
    public string Title => ApplicationTitle;

    /// <inheritdoc />
    public string ProductName => Wordmark;

    /// <inheritdoc />
    public string Version => _version;

    /// <summary>Picks the most specific version the assembly carries.</summary>
    /// <param name="assembly">The assembly.</param>
    /// <returns>The version string.</returns>
    /// <remarks>
    /// <see cref="AssemblyFileVersionAttribute"/> first, because that is the field the Delphi banner
    /// binds to (<c>vifFileVersion</c>). The informational version is second and has its build
    /// metadata - everything from the first <c>+</c> - stripped, so a source-linked build does not
    /// print a commit hash in the banner.
    /// </remarks>
    private static string ReadVersion(Assembly assembly)
    {
        string? fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;

        if (!string.IsNullOrWhiteSpace(fileVersion))
        {
            return fileVersion;
        }

        string? informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            int plus = informational.IndexOf('+', StringComparison.Ordinal);

            return plus < 0 ? informational : informational[..plus];
        }

        return assembly.GetName().Version?.ToString() ?? UnknownVersion;
    }
}
