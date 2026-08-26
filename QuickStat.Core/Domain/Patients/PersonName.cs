namespace QuickStat.Domain.Patients;

/// <summary>
/// A patient's name split the way <c>FullName</c> is split, which is not the way a reader would
/// expect.
/// </summary>
/// <param name="FirstName">Given name, empty when the source could not be split.</param>
/// <param name="LastName">Family name, or the whole unsplittable name.</param>
/// <remarks>
/// Ports <c>TPerson.Set_FullName</c> (<c>Emetra.Person.pas:328-361</c>). The parse is lossy and the
/// loss is visible on screen, so it is reproduced rather than improved: the grid renders
/// <see cref="Patient.DisplayName"/> as <c>"Last, First"</c>, so a name that fails to split shows a
/// trailing comma.
/// </remarks>
public readonly record struct PersonName(string FirstName, string LastName)
{
    /// <summary>Splits a <c>FullName</c> exactly as the Delphi does.</summary>
    /// <param name="fullName">The <c>FullName</c> column, which may be <see langword="null"/>.</param>
    /// <returns>The split name.</returns>
    /// <remarks>
    /// <para>The three branches of <c>Set_FullName</c>, in order:</para>
    /// <list type="bullet">
    /// <item><description>
    /// Empty after trimming: both parts empty.
    /// </description></item>
    /// <item><description>
    /// Exactly one comma: <c>"Nordmann, Ola"</c> becomes <c>Nordmann</c> / <c>Ola</c>, both trimmed.
    /// </description></item>
    /// <item><description>
    /// Anything else: the last comma-separated part becomes the last name and the rest, rejoined with
    /// commas, becomes the first name - <em>without</em> trimming, because the Delphi's else branch
    /// does not trim. So <c>"Ola Nordmann"</c>, which has no comma at all, yields a last name of
    /// <c>"Ola Nordmann"</c> and an empty first name.
    /// </description></item>
    /// </list>
    /// <para>
    /// Delphi's <c>TStringList.DelimitedText</c> also honours a double-quote as a quoting character
    /// even with <c>StrictDelimiter</c>. That is not reproduced: a person name containing a quote
    /// character is not a case any FastTrak database produces, and reproducing it would make the
    /// parse harder to reason about than the thing it models.
    /// </para>
    /// </remarks>
    public static PersonName Parse(string? fullName)
    {
        string trimmed = (fullName ?? "").Trim();
        if (trimmed.Length == 0)
        {
            return new PersonName("", "");
        }

        string[] parts = trimmed.Split(',');
        if (parts.Length == 2)
        {
            return new PersonName(parts[1].Trim(), parts[0].Trim());
        }

        string last = parts[^1];
        string first = string.Join(",", parts, 0, parts.Length - 1);
        return new PersonName(first, last);
    }
}
