using Microsoft.Extensions.Logging;

namespace QuickStat.Domain.Matrix;

/// <summary>Default <see cref="ICaptionLoader"/>.</summary>
internal sealed class CaptionLoader : ICaptionLoader
{
    private readonly ICaptionRepository _repository;
    private readonly CaptionDictionary _captions;
    private readonly ILogger<CaptionLoader> _log;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="repository">Where database captions come from.</param>
    /// <param name="captions">The one dictionary the matrix reads through <see cref="ITitleDictionary"/>.</param>
    /// <param name="log">Where diagnostics go.</param>
    public CaptionLoader(ICaptionRepository repository, CaptionDictionary captions, ILogger<CaptionLoader> log)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(captions);
        ArgumentNullException.ThrowIfNull(log);

        _repository = repository;
        _captions = captions;
        _log = log;
    }

    /// <inheritdoc />
    public async Task<int> LoadAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CaptionRecord> labCaptions;

        try
        {
            labCaptions = await _repository.GetLabCaptionsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // A cancelled login is not a caption failure; let the caller see it.
            throw;
        }
        catch (Exception exception)
        {
            // Deliberately not Clear(): see ICaptionLoader.LoadAsync.  Whatever is in the dictionary
            // now is better than nothing, and captions are cosmetic.
            _log.LogWarning(exception, "Could not read lab captions; keeping the captions already loaded.");

            return 0;
        }

        // Reset first, so a project switch cannot leave the previous database's captions winning on
        // the first-wins merge below.
        _captions.Clear();
        _captions.AddRange(CaptionDictionary.QuickStatDefaults);

        int added = _captions.AddRange(labCaptions);

        _log.LogInformation(
            "Loaded {Added} lab captions of {Returned} returned; {Total} captions in total.",
            added,
            labCaptions.Count,
            _captions.Count);

        return added;
    }
}
