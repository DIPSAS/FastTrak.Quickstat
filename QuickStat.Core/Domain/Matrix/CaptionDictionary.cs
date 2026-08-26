namespace QuickStat.Domain.Matrix;

/// <summary>The variable-caption lookup, with the Delphi's two-tier precedence.</summary>
/// <remarks>
/// <para>
/// Delphi: <c>TVarCaptions</c> (<c>EPR.QA.CaptionDictionary.pas</c>). Two paths write into it and
/// they disagree on purpose. <see cref="AddCaption"/> overwrites (<c>AddOrSetValue</c>,
/// <c>:149-154</c>) and is how the twelve hardcoded captions are installed; the database load uses
/// <c>if not ContainsKey</c> (<c>:110-111</c>) and so is first-wins. Because the hardcoded captions
/// are added before the query runs, they win - which is the whole point of the asymmetry.
/// </para>
/// <para>
/// QuickStat loads exactly one caption query, <c>QueryLabCaptions</c> over <c>dbo.LabClass</c>;
/// custom captions from <c>Report.ColumnCaption</c> and per-item captions are both switched off, so
/// every other variable falls back to its own name.
/// </para>
/// </remarks>
public sealed class CaptionDictionary : ITitleDictionary
{
    private readonly Dictionary<string, CaptionRecord> _captions = new(StringComparer.Ordinal);

    /// <summary>
    /// The twelve captions <c>MainQuickStat.AddCaptions</c> installs at the start of every collect
    /// run (<c>MainQuickStat.pas:453-469</c>).
    /// </summary>
    /// <remarks>
    /// Ten of the twelve are already dead: <c>TDrugCollector</c> emits
    /// <c>ATC_&lt;pattern&gt;.&lt;TreatType&gt;</c>, never <c>DRUG.F</c> and friends
    /// (<c>Docs/Port/04-matrix-export.md</c> §1.4). They are carried across unchanged because
    /// deciding what they should have been is a collector-registry question, not a caption one.
    /// </remarks>
    public static IReadOnlyList<CaptionRecord> QuickStatDefaults { get; } =
    [
        new() { VarName = "DRUID.RED", Title = "DDI-R", Description = "Drug-Drug interactions, red level" },
        new() { VarName = "DRUID.YELLOW", Title = "DDI-Y", Description = "Drug-Drug interactions, yellow level" },
        new() { VarName = "DRUID.ORANGE", Title = "DDI-O", Description = "Drug-Drug interactions, orange level" },
        new() { VarName = "DRUID.GREEN", Title = "DDI-G", Description = "Drug-Drug interactions, green level" },
        new() { VarName = "DRUG.F", Title = "Regular" },
        new() { VarName = "DRUG.B", Title = "AsNeeded" },
        new() { VarName = "DRUG.U", Title = "Weekly" },
        new() { VarName = "DRUG.X", Title = "Unspec" },
        new() { VarName = "DRUG.K", Title = "Cure" },
        new() { VarName = "DRUG.NOATC", Title = "NoAtc" },
        new() { VarName = "DRUG.RESISTANCE_DRIVING", Title = "Resist", Description = "Resistance-driving antibiotics" },
        new() { VarName = "DRUG.METFORMIN", Title = "Metform", Description = "Metformin" },
    ];

    /// <summary>How many captions are held.</summary>
    public int Count => _captions.Count;

    /// <summary>Creates a dictionary preloaded with <see cref="QuickStatDefaults"/>.</summary>
    /// <returns>The dictionary.</returns>
    public static CaptionDictionary WithQuickStatDefaults()
    {
        CaptionDictionary captions = new();

        foreach (CaptionRecord caption in QuickStatDefaults)
        {
            captions.AddCaption(caption);
        }

        return captions;
    }

    /// <summary>Adds a caption, replacing any existing one.</summary>
    /// <param name="caption">The caption. Its name and title must both be non-empty, as the Delphi asserts.</param>
    public void AddCaption(CaptionRecord caption)
    {
        ArgumentNullException.ThrowIfNull(caption);
        ArgumentException.ThrowIfNullOrEmpty(caption.VarName);
        ArgumentException.ThrowIfNullOrEmpty(caption.Title);

        _captions[caption.VarName] = caption;
    }

    /// <summary>Adds a caption only if the variable has none yet.</summary>
    /// <param name="caption">The caption.</param>
    /// <returns><see langword="false"/> when a caption was already present and was kept.</returns>
    /// <remarks>This is the database-load path, which must never override a hardcoded caption.</remarks>
    public bool TryAddCaption(CaptionRecord caption)
    {
        ArgumentNullException.ThrowIfNull(caption);
        ArgumentException.ThrowIfNullOrEmpty(caption.VarName);

        return _captions.TryAdd(caption.VarName, caption);
    }

    /// <summary>Adds captions on the first-wins rule.</summary>
    /// <param name="captions">The captions, in the order the query returned them.</param>
    /// <returns>How many were actually added.</returns>
    public int AddRange(IEnumerable<CaptionRecord> captions)
    {
        ArgumentNullException.ThrowIfNull(captions);

        int added = 0;

        foreach (CaptionRecord caption in captions)
        {
            if (TryAddCaption(caption))
            {
                added++;
            }
        }

        return added;
    }

    /// <summary>Removes every caption.</summary>
    public void Clear() => _captions.Clear();

    /// <summary>Looks a caption up.</summary>
    /// <param name="varName">Column name.</param>
    /// <param name="caption">The caption.</param>
    /// <returns><see langword="true"/> when the variable has one.</returns>
    public bool TryGetCaption(string varName, out CaptionRecord? caption)
    {
        ArgumentNullException.ThrowIfNull(varName);

        return _captions.TryGetValue(varName, out caption);
    }

    /// <inheritdoc />
    public string GetVarTitle(string varName)
    {
        ArgumentNullException.ThrowIfNull(varName);

        return _captions.TryGetValue(varName, out CaptionRecord? caption) ? caption.Title : varName;
    }

    /// <inheritdoc />
    public string GetVarDescription(string varName)
    {
        ArgumentNullException.ThrowIfNull(varName);

        return _captions.TryGetValue(varName, out CaptionRecord? caption) ? caption.Description : "";
    }
}
