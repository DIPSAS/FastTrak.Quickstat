using QuickStat.Domain.Patients;
using Xunit;

namespace QuickStat.Tests.Domain.Patients;

/// <summary>
/// <c>TPerson.Set_FullName</c> (<c>Emetra.Person.pas:328-361</c>) splits on a comma and nothing else.
/// The result is what the grid and every export render, so the lossy branches are reproduced rather
/// than improved.
/// </summary>
public class PersonNameTests
{
    [Fact]
    public void ACommaSeparatedNameSplitsIntoLastAndFirst()
    {
        PersonName name = PersonName.Parse("Nordmann, Ola");

        Assert.Equal("Nordmann", name.LastName);
        Assert.Equal("Ola", name.FirstName);
    }

    [Fact]
    public void BothPartsAreTrimmed()
    {
        PersonName name = PersonName.Parse("  Nordmann ,  Ola Kari  ");

        Assert.Equal("Nordmann", name.LastName);
        Assert.Equal("Ola Kari", name.FirstName);
    }

    [Fact]
    public void ANameWithoutACommaBecomesTheLastNameWithNoFirstName()
    {
        // The lossy branch: StrictDelimiter means a space is not a delimiter, so the whole string is
        // one part and it lands in the last name.
        PersonName name = PersonName.Parse("Ola Nordmann");

        Assert.Equal("Ola Nordmann", name.LastName);
        Assert.Equal("", name.FirstName);
    }

    [Fact]
    public void AnEmptyNameLeavesBothPartsEmpty()
    {
        PersonName name = PersonName.Parse("");

        Assert.Equal("", name.LastName);
        Assert.Equal("", name.FirstName);
    }

    [Fact]
    public void WhitespaceOnlyIsTreatedAsEmpty()
    {
        PersonName name = PersonName.Parse("   ");

        Assert.Equal("", name.LastName);
        Assert.Equal("", name.FirstName);
    }

    [Fact]
    public void NullIsTreatedAsEmpty()
    {
        PersonName name = PersonName.Parse(null);

        Assert.Equal("", name.LastName);
        Assert.Equal("", name.FirstName);
    }

    [Fact]
    public void ThreePartsPutTheLastPartInTheLastNameAndRejoinTheRest()
    {
        // Delphi's else branch: FLastName := lstNames[Count-1], then FFirstName := the remainder's
        // DelimitedText. Neither is trimmed there, unlike the two-part branch.
        PersonName name = PersonName.Parse("Von, Der, Berg");

        Assert.Equal(" Berg", name.LastName);
        Assert.Equal("Von, Der", name.FirstName);
    }

    [Fact]
    public void ATrailingCommaYieldsAnEmptyFirstName()
    {
        PersonName name = PersonName.Parse("Nordmann,");

        Assert.Equal("Nordmann", name.LastName);
        Assert.Equal("", name.FirstName);
    }

    [Fact]
    public void NorwegianCharactersSurviveTheSplit()
    {
        PersonName name = PersonName.Parse("Bjørnstad, Åse");

        Assert.Equal("Bjørnstad", name.LastName);
        Assert.Equal("Åse", name.FirstName);
    }

    [Fact]
    public void DisplayNameIsLastThenFirst()
    {
        // EPR.QA.Matrix.Row.pas:90-97. Note the trailing comma when the name did not split.
        Patient split = new() { PersonId = 1, LastName = "Nordmann", FirstName = "Ola" };
        Patient unsplit = new() { PersonId = 2, LastName = "Ola Nordmann", FirstName = "" };

        Assert.Equal("Nordmann, Ola", split.DisplayName);
        Assert.Equal("Ola Nordmann, ", unsplit.DisplayName);
    }
}
