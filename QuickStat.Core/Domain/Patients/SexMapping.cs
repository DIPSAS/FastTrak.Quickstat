namespace QuickStat.Domain.Patients;

/// <summary>The one place <c>GenderId</c> becomes a <see cref="Sex"/>.</summary>
/// <remarks>
/// Delphi: <c>TPerson.Set_GenderId</c> (<c>Emetra.Person.pas:363-377</c>). A <c>case</c> with two
/// arms and an <c>else</c>, so every other value - including the <c>-1</c> a missing or null column
/// reads as - is <see cref="Sex.Unknown"/>.
/// </remarks>
internal static class SexMapping
{
    /// <summary>Interprets a raw <c>GenderId</c>.</summary>
    /// <param name="genderId">The value as returned.</param>
    /// <returns>The corresponding <see cref="Sex"/>.</returns>
    public static Sex FromGenderId(int genderId) => genderId switch
    {
        1 => Sex.Male,
        2 => Sex.Female,
        _ => Sex.Unknown,
    };
}
