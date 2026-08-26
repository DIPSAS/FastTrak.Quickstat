using QuickStat.Domain.Anonymisation;
using Xunit;

namespace QuickStat.Tests.Domain.Anonymisation;

/// <summary>The one derivation from mode to column set, and the one policy that holds the mode.</summary>
public class IdentificationColumnsTests
{
    [Fact]
    public void FullKeepsAllFourIdentityColumns()
    {
        IdentificationColumns columns = IdentificationColumns.For(PersonIdentification.Full);

        Assert.True(columns.IncludesPersonId);
        Assert.True(columns.IncludesDateOfBirth);
        Assert.True(columns.IncludesNationalId);
        Assert.True(columns.IncludesName);
        Assert.False(columns.UsesPseudonyms);
    }

    [Theory]
    [InlineData(PersonIdentification.PersonIdOnly, false)]
    [InlineData(PersonIdentification.RandomPersonId, true)]
    public void NonFullModesDropEverythingButThePersonId(
        PersonIdentification identification,
        bool expectsPseudonyms)
    {
        IdentificationColumns columns = IdentificationColumns.For(identification);

        Assert.True(columns.IncludesPersonId);
        Assert.False(columns.IncludesDateOfBirth);
        Assert.False(columns.IncludesNationalId);
        Assert.False(columns.IncludesName);
        Assert.Equal(expectsPseudonyms, columns.UsesPseudonyms);
    }

    [Fact]
    public void AnUndeclaredModeFailsRatherThanDefaulting()
    {
        // The Delphi raised EAbort('Unhandled identification strategy.') when no radio button was
        // checked. Every plausible default here is either a leak or silent data loss.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => IdentificationColumns.For((PersonIdentification)99));
    }

    [Fact]
    public void ThePolicyDefaultsToPersonIdOnly()
    {
        // MainQuickStat.dfm:1234-1263 - rbKeepPids is the checked radio button.
        var policy = new IdentificationPolicy();

        Assert.Equal(PersonIdentification.PersonIdOnly, policy.Mode);
        Assert.Equal(IdentificationColumns.For(PersonIdentification.PersonIdOnly), policy.Columns);
    }

    [Fact]
    public void ThePolicyRaisesModeChangedOncePerRealChange()
    {
        var policy = new IdentificationPolicy();
        var observed = new List<PersonIdentification>();

        policy.ModeChanged += (_, mode) => observed.Add(mode);

        policy.Mode = PersonIdentification.Full;
        policy.Mode = PersonIdentification.Full;
        policy.Mode = PersonIdentification.RandomPersonId;

        Assert.Equal(
            new[] { PersonIdentification.Full, PersonIdentification.RandomPersonId },
            observed);
    }

    [Fact]
    public void ThePolicyRejectsAnUndeclaredMode()
    {
        var policy = new IdentificationPolicy();

        Assert.Throws<ArgumentOutOfRangeException>(() => policy.Mode = (PersonIdentification)7);
        Assert.Equal(PersonIdentification.PersonIdOnly, policy.Mode);
    }

    [Fact]
    public void TheEnumValuesAreTheDelphiOrder()
    {
        // TPersonIdentification = ( pgiFull, pgiPersonIdOnly, pgiRandomPersonId ).
        Assert.Equal(0, (int)PersonIdentification.Full);
        Assert.Equal(1, (int)PersonIdentification.PersonIdOnly);
        Assert.Equal(2, (int)PersonIdentification.RandomPersonId);
    }
}
