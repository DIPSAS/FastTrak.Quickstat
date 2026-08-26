namespace QuickStat.Domain.Patients;

/// <summary>Biological sex as FastTrak's <c>GenderId</c> encodes it.</summary>
/// <remarks>
/// Delphi: <c>Emetra.Person.pas:363-377</c> and <c>EPR.QA.Matrix.Row.pas:210-217</c>. Any value
/// other than 1 or 2 - including a missing column, which reads as zero - is
/// <see cref="Unknown"/>.
/// </remarks>
public enum Sex
{
    /// <summary>Not recorded, or an unrecognised <c>GenderId</c>.</summary>
    Unknown = 0,

    /// <summary><c>GenderId = 1</c>.</summary>
    Male = 1,

    /// <summary><c>GenderId = 2</c>.</summary>
    Female = 2,
}
