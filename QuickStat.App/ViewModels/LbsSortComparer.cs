using System.Globalization;
using System.Runtime.InteropServices;

namespace QuickStat.ViewModels;

/// <summary>
/// Orders strings exactly as a Win32 list box with <c>LBS_SORT</c> does, by calling the same
/// comparison the list box calls.
/// </summary>
/// <remarks>
/// <para>
/// This exists because <see cref="StringComparer.CurrentCultureIgnoreCase"/> is <em>not</em> the
/// .NET equivalent of <c>LBS_SORT</c>, which an earlier revision of
/// <see cref="DataElementViewModel.TitleOrder"/> assumed. Both read the user's locale and both are
/// linguistic and case-insensitive, but since .NET 5 the framework collates with <b>ICU</b> while
/// the list box collates with <b>NLS</b>, and the two disagree about punctuation.
/// </para>
/// <para>
/// Phase 5 measured the disagreement against the running <c>22.12.21.547</c> build on a real
/// database. Of 213 data elements, 208 agreed and five did not: NLS orders
/// <c>Skjema: Antall totalt per type</c> <em>before</em> <c>Skjema-alder: …</c>, ICU orders it
/// after. The five <c>Skjema: Antall …</c> elements consequently sat at positions 41-45 in the
/// Delphi and 209-213 here. Because the collect loop walks the list from index 0 and column order
/// is insertion order, that is five columns landing in the wrong place in <b>every exported
/// file</b> - the drift PORT-PLAN.md §6 forbids.
/// </para>
/// <para>
/// <b>The 131-entry static catalog sorts identically under either comparer</b>, which is why no
/// existing test caught this and why it needed a database: the divergence only appears once the
/// per-form elements from <c>Report.GetFormClasses</c> are in the list, because they are what
/// introduce the <c>Skjema-alder:</c> and <c>Skjema-data:</c> titles that collide with
/// <c>Skjema:</c>.
/// </para>
/// <para>
/// The locale comes from <see cref="CultureInfo.CurrentCulture"/> rather than from
/// <c>LOCALE_NAME_USER_DEFAULT</c> (which is what passing <see langword="null"/> would select).
/// The Delphi's <c>LOCALE_USER_DEFAULT</c> and the process's current culture are the same thing on
/// a normally-configured machine, and reading the managed culture is what keeps
/// <c>CultureSweep</c> able to sweep this code and the tests deterministic.
/// </para>
/// </remarks>
internal sealed class LbsSortComparer : IComparer<string>
{
    /// <summary>The shared instance; the comparer holds no state.</summary>
    internal static LbsSortComparer Instance { get; } = new();

    /// <summary><c>NORM_IGNORECASE</c>. The flag <c>LBS_SORT</c> passes.</summary>
    private const uint NormIgnoreCase = 0x00000001;

    /// <summary><c>CSTR_EQUAL</c>. Subtracting it turns the API's 1/2/3 into -1/0/1.</summary>
    private const int CompareStringEqual = 2;

    private LbsSortComparer()
    {
    }

    /// <inheritdoc />
    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        // Nulls cannot reach CompareStringEx, and they cannot reach a list box either. Ordering them
        // first matches what every StringComparer does, so a caller that somehow has one behaves the
        // same as it did before.
        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        int result = CompareStringEx(
            CultureInfo.CurrentCulture.Name,
            NormIgnoreCase,
            x,
            x.Length,
            y,
            y.Length,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);

        // Zero means the call failed - an invalid locale name is the only realistic cause, and an
        // unsorted list is a worse outcome than a list sorted by the framework's own rule. Fall back
        // rather than throw: this runs while a window is being populated.
        return result == 0
            ? StringComparer.CurrentCultureIgnoreCase.Compare(x, y)
            : result - CompareStringEqual;
    }

    // DllImport rather than LibraryImport (SYSLIB1054): the source generator emits unsafe code, and
    // turning AllowUnsafeBlocks on for the whole application to reach one comparison function is a
    // worse trade than the marshalling this saves. Every argument is blittable or a UTF-16 string.
    [DllImport("kernel32.dll", EntryPoint = "CompareStringEx", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int CompareStringEx(
        string localeName,
        uint flags,
        string string1,
        int count1,
        string string2,
        int count2,
        IntPtr versionInformation,
        IntPtr reserved,
        IntPtr parameter);
}
