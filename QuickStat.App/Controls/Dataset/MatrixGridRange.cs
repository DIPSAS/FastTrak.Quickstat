namespace QuickStat.Controls.Dataset;

/// <summary>A contiguous half-open run of row or column indices.</summary>
/// <param name="First">The first index in the run; zero when <paramref name="Count"/> is zero.</param>
/// <param name="Count">How many indices the run covers.</param>
/// <remarks>
/// This is what virtualisation actually <i>is</i> in this control: every frame resolves the
/// viewport to two of these and touches nothing outside them. A named type rather than a tuple so
/// the virtualisation tests can assert on it directly.
/// </remarks>
public readonly record struct MatrixGridRange(int First, int Count)
{
    /// <summary>The empty run.</summary>
    public static MatrixGridRange Empty { get; }

    /// <summary>One past the last index in the run.</summary>
    public int End => First + Count;

    /// <summary>The last index in the run; meaningless when <see cref="Count"/> is zero.</summary>
    public int Last => (First + Count) - 1;

    /// <summary>Whether the run covers nothing.</summary>
    public bool IsEmpty => Count <= 0;

    /// <summary>Whether an index falls inside the run.</summary>
    /// <param name="index">The index to test.</param>
    /// <returns><see langword="true"/> when the run covers it.</returns>
    public bool Contains(int index) => index >= First && index < End;
}
