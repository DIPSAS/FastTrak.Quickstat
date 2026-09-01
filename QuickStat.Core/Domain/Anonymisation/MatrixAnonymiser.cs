using System.Buffers.Binary;
using System.Security.Cryptography;

namespace QuickStat.Domain.Anonymisation;

/// <summary>
/// Assigns pseudonymous person ids by keyed derivation from a per-dataset secret.
/// </summary>
/// <remarks>
/// <para>
/// Replaces <c>TMatrixAnonymizer</c> (<c>EPR.QA.Matrix.Anoymizer.pas</c>). The Delphi drew from the
/// global <c>Random</c> and never called <c>Randomize</c>, so <c>RandSeed</c> stayed at the RTL's
/// initial 0. That produced the worst of both worlds, and both halves are fixed here.
/// </para>
/// <list type="table">
///   <listheader><term>Property</term><description>How it is obtained</description></listheader>
///   <item>
///     <term>Stable within one dataset</term>
///     <description>
///       Every pseudonym is memoised in <c>_personIdToPseudonym</c> for the lifetime of the space,
///       so a second export of the same loaded dataset reproduces the first exactly. The Delphi
///       instead built a new anonymiser per <c>SaveToFile</c> and let the RNG stream run on, so the
///       same patient changed pseudonym between two exports in one session.
///     </description>
///   </item>
///   <item>
///     <term>Unlinkable across datasets</term>
///     <description>
///       <see cref="Reset"/> - called from the population load, and nowhere else - draws a fresh
///       256-bit key from the operating system CSPRNG
///       (<see cref="RandomNumberGenerator"/>). Pseudonyms are
///       <c>scale + HMAC-SHA256(key, personId || counter) mod 9*scale</c>. HMAC-SHA256 under an
///       independent, uniformly random, never-persisted key is a pseudo-random function, so the
///       pseudonyms a patient receives in two different datasets are computationally independent:
///       joining two exports reveals nothing, not even that the same patient appears in both. The
///       Delphi's unseeded stream, by contrast, produced the <em>same</em> sequence in every process
///       on every machine, so two anonymised cohorts of equal size could be joined by position.
///     </description>
///   </item>
///   <item>
///     <term>No re-identification from the file alone</term>
///     <description>
///       The key never leaves this object, is never written to disk, and is zeroed when replaced.
///       Only <see cref="PseudonymToPersonId"/> can undo a pseudonym, and writing that to disk is
///       opt-in (<c>DatasetExportOptions.WriteKeyFile</c>).
///     </description>
///   </item>
/// </list>
/// <para>
/// The <em>width</em> of a pseudonym is Delphi parity and is reproduced exactly: the scale factor is
/// the smallest power of ten at or above <c>1 + max(personCount, 1)</c> (the Delphi passed the grid's
/// <c>RowCount</c>, which is one header row plus at least one data row), and the value lies in
/// <c>[scale, 10 * scale - 1]</c>. Seventeen people therefore give three-digit ids in 100-999.
/// </para>
/// </remarks>
public sealed class MatrixAnonymiser : IAnonymiser
{
    /// <summary>Smallest scale factor, i.e. the Delphi's initial <c>fScaleFactor</c>.</summary>
    public const int MinimumScaleFactor = 10;

    /// <summary>
    /// Largest scale factor, so that <c>10 * scale - 1</c> still fits in an <see cref="int"/>.
    /// </summary>
    /// <remarks>
    /// The Delphi's <c>fScaleFactor</c> was an <c>integer</c> and would have overflowed silently
    /// somewhere past a hundred million people. Cohorts are hundreds to a few thousand
    /// (<c>Docs/Port/04-matrix-export.md</c> §9.1), so this ceiling is unreachable in practice and
    /// exists only to turn a silent wrap into a diagnosable exception.
    /// </remarks>
    public const int MaximumScaleFactor = 100_000_000;

    /// <summary>Length of the derivation key, in bytes.</summary>
    private const int KeySizeInBytes = 32;

    /// <summary>
    /// Derivation attempts per person before giving up.
    /// </summary>
    /// <remarks>
    /// At most <c>scale - 1</c> people share a space of <c>9 * scale</c> pseudonyms, so the load
    /// factor never exceeds 1/9 and the expected number of attempts is below 1.2. Reaching this
    /// bound means the key or the range is wrong, not that the caller was unlucky.
    /// </remarks>
    private const int MaximumAttemptsPerPerson = 10_000;

    private readonly Lock _gate = new();
    private readonly Dictionary<int, int> _personIdToPseudonym = [];
    private readonly Dictionary<int, int> _pseudonymToPersonId = [];

    private byte[]? _key;
    private int _scaleFactor;

    /// <summary>
    /// Current scale factor, or zero when no pseudonym space has been established.
    /// </summary>
    /// <remarks>
    /// Pseudonyms lie in <c>[ScaleFactor, 10 * ScaleFactor - 1]</c>, so this is also the digit
    /// count: 100 means three digits.
    /// </remarks>
    public int ScaleFactor
    {
        get
        {
            lock (_gate)
            {
                return _scaleFactor;
            }
        }
    }

    /// <summary>Whether <see cref="Reset"/> or <see cref="EnsureSpaceFor"/> has run.</summary>
    public bool HasPseudonymSpace
    {
        get
        {
            lock (_gate)
            {
                return _key is not null;
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<int, int> PseudonymToPersonId
    {
        get
        {
            lock (_gate)
            {
                // A snapshot, not a live view. The map is the re-identification key: handing out a
                // reference to mutable internal state and letting a caller enumerate it while
                // another export writes into it is not a risk worth taking for a dictionary that is
                // at most a few thousand entries.
                return new Dictionary<int, int>(_pseudonymToPersonId);
            }
        }
    }

    /// <summary>
    /// The scale factor the Delphi would have chosen for a cohort of this size.
    /// </summary>
    /// <param name="personCount">Number of people.</param>
    /// <returns>The smallest power of ten at or above <c>1 + max(personCount, 1)</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="personCount"/> is negative, or so large that the scale factor would exceed
    /// <see cref="MaximumScaleFactor"/>.
    /// </exception>
    public static int ScaleFactorFor(int personCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(personCount);

        // EPR.QA.Matrix.pas:456 passes fGridComponent.RowCount, which is FixedRows + max(DataRows, 1)
        // = 1 + max(personCount, 1). EPR.QA.Matrix.Anoymizer.pas:37-39 then multiplies by ten until
        // the factor reaches it.
        long rowCount = 1L + Math.Max(personCount, 1);
        long scaleFactor = MinimumScaleFactor;

        while (scaleFactor < rowCount)
        {
            scaleFactor *= 10;

            if (scaleFactor > MaximumScaleFactor)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(personCount),
                    personCount,
                    $"A cohort this large needs a pseudonym scale factor above {MaximumScaleFactor}.");
            }
        }

        return (int)scaleFactor;
    }

    /// <inheritdoc />
    public void Reset(int personCount)
    {
        int scaleFactor = ScaleFactorFor(personCount);

        lock (_gate)
        {
            ResetCore(scaleFactor);
        }
    }

    /// <inheritdoc />
    public bool EnsureSpaceFor(int personCount)
    {
        int scaleFactor = ScaleFactorFor(personCount);

        lock (_gate)
        {
            // A space that is at least as wide still produces pseudonyms of the agreed width and
            // still contains everyone mapped so far, so leaving it alone is what keeps two exports
            // of one loaded dataset identical. A narrower one cannot serve this cohort at all.
            if (_key is not null && _scaleFactor >= scaleFactor)
            {
                return false;
            }

            ResetCore(scaleFactor);
            return true;
        }
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// No pseudonym space exists. Call <see cref="Reset"/> when the dataset is loaded, or
    /// <see cref="EnsureSpaceFor"/> immediately before exporting.
    /// </exception>
    public int GetPseudonym(int personId)
    {
        lock (_gate)
        {
            if (_key is null)
            {
                throw new InvalidOperationException(
                    $"No pseudonym space. Call {nameof(Reset)} or {nameof(EnsureSpaceFor)} first.");
            }

            if (_personIdToPseudonym.TryGetValue(personId, out int existing))
            {
                return existing;
            }

            int pseudonym = Derive(_key, _scaleFactor, personId, _pseudonymToPersonId);

            _personIdToPseudonym.Add(personId, pseudonym);
            _pseudonymToPersonId.Add(pseudonym, personId);

            return pseudonym;
        }
    }

    /// <summary>
    /// Draws a pseudonym for one person, rejecting both modulo bias and collisions.
    /// </summary>
    private static int Derive(
        byte[] key,
        int scaleFactor,
        int personId,
        Dictionary<int, int> taken)
    {
        // The Delphi range: fScaleFactor + Random(9 * fScaleFactor), i.e. [scale, 10*scale - 1].
        ulong range = 9UL * (ulong)(uint)scaleFactor;

        // Accept only draws below the largest multiple of the range that fits in 64 bits, so every
        // residue is equally likely. The rejection probability is at most range / 2^64.
        ulong limit = ulong.MaxValue - (ulong.MaxValue % range);

        Span<byte> message = stackalloc byte[8];
        Span<byte> digest = stackalloc byte[HMACSHA256.HashSizeInBytes];

        BinaryPrimitives.WriteInt32LittleEndian(message[..4], personId);

        for (uint counter = 0; counter < MaximumAttemptsPerPerson; counter++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(message[4..], counter);
            HMACSHA256.HashData(key, message, digest);

            ulong draw = BinaryPrimitives.ReadUInt64LittleEndian(digest);

            if (draw >= limit)
            {
                continue;
            }

            int candidate = scaleFactor + (int)(draw % range);

            if (!taken.ContainsKey(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"Could not find a free pseudonym for person {personId} in {MaximumAttemptsPerPerson} attempts.");
    }

    private void ResetCore(int scaleFactor)
    {
        if (_key is not null)
        {
            CryptographicOperations.ZeroMemory(_key);
        }

        _personIdToPseudonym.Clear();
        _pseudonymToPersonId.Clear();
        _scaleFactor = scaleFactor;
        _key = RandomNumberGenerator.GetBytes(KeySizeInBytes);
    }
}
