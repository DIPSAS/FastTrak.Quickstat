using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace QuickStat.Export;

/// <summary>Best-effort deletion of temporary export files. Register as a singleton.</summary>
/// <remarks>
/// Paths are compared case-insensitively, matching the file system this ships on, so tracking the
/// same file under two spellings does not leave a second entry that fails to delete.
/// </remarks>
public sealed class TempFileTracker : ITempFileTracker
{
    private readonly Lock _gate = new();
    private readonly HashSet<string> _paths = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<TempFileTracker> _logger;

    private bool _disposed;

    /// <summary>Creates a tracker.</summary>
    /// <param name="logger">Where deletion failures go, or <see langword="null"/> for none.</param>
    public TempFileTracker(ILogger<TempFileTracker>? logger = null) =>
        _logger = logger ?? NullLogger<TempFileTracker>.Instance;

    /// <inheritdoc />
    public IReadOnlyCollection<string> TrackedPaths
    {
        get
        {
            lock (_gate)
            {
                return _paths.ToArray();
            }
        }
    }

    /// <inheritdoc />
    public void Track(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        lock (_gate)
        {
            _paths.Add(Path.GetFullPath(path));
        }
    }

    /// <inheritdoc />
    public bool Delete(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string full = Path.GetFullPath(path);

        lock (_gate)
        {
            _paths.Remove(full);
        }

        return TryDelete(full);
    }

    /// <inheritdoc />
    public int DeleteAll()
    {
        string[] snapshot;

        lock (_gate)
        {
            snapshot = [.. _paths];
            _paths.Clear();
        }

        int deleted = 0;

        foreach (string path in snapshot)
        {
            if (TryDelete(path))
            {
                deleted++;
            }
        }

        return deleted;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ = DeleteAll();
    }

    private bool TryDelete(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch (IOException exception)
        {
            // Excel keeps the temporary CSV open for as long as it is showing it, so this is an
            // ordinary outcome rather than a fault. The Delphi swallowed it too (MainQuickStat.pas:326-337).
            _logger.LogWarning(exception, "Could not delete the temporary file {Path}.", path);
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Not allowed to delete the temporary file {Path}.", path);
            return false;
        }
    }
}
