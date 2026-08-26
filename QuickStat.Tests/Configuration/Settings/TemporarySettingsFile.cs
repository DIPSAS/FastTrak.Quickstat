using System.IO;
using QuickStat.Configuration.Settings;

namespace QuickStat.Tests.Configuration.Settings;

/// <summary>
/// A settings file in its own temporary directory, deleted when the test finishes.
/// </summary>
/// <remarks>
/// Settings files are never committed - <c>*.ini</c> is in <c>.gitignore</c> deliberately
/// (PORT-PLAN.md §5 step 0.1) - so there are no fixture files in the repository and every test that
/// needs one writes it here first.
/// </remarks>
internal sealed class TemporarySettingsFile : IDisposable
{
    private readonly string _root;

    internal TemporarySettingsFile(string? initialContent = null, string fileName = "QuickStat.ini")
    {
        _root = Path.Combine(Path.GetTempPath(), "QuickStat.Tests", Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_root);

        FilePath = Path.Combine(_root, fileName);

        if (initialContent is not null)
        {
            File.WriteAllText(FilePath, initialContent);
        }
    }

    /// <summary>The absolute path of the settings file, which may or may not exist yet.</summary>
    internal string FilePath { get; }

    /// <summary>A path inside the temporary directory, for tests about directory creation.</summary>
    internal string NestedFilePath => Path.Combine(_root, "nested", "deeper", "QuickStat.ini");

    /// <summary>Whether the file is on disk.</summary>
    internal bool Exists => File.Exists(FilePath);

    /// <summary>The file's bytes, exactly as written.</summary>
    internal byte[] Bytes => File.ReadAllBytes(FilePath);

    /// <summary>The file's text.</summary>
    internal string Text => File.ReadAllText(FilePath);

    /// <summary>Opens a store over this file.</summary>
    internal IniSettingsStore Open() => new(FilePath);

    /// <summary>Replaces the file's content, for corrupt-file and legacy-format tests.</summary>
    internal void Write(string content) => File.WriteAllText(FilePath, content);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch (IOException)
        {
            // A leftover temporary directory is not a test failure.
        }
    }
}
