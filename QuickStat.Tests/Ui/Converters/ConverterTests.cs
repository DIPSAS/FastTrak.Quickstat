using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using QuickStat.Converters;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.DataPoints;
using Xunit;

namespace QuickStat.Tests.Ui.Converters;

/// <summary>
/// The four converters of <c>05-ui-spec.md</c> §H.1, plus the <see cref="Rgb"/> bridge the spec
/// does not name but the port needs, because <c>QuickStat.Core</c> is deliberately WPF-free.
/// </summary>
/// <remarks>
/// Every case passes <see cref="CultureInfo.InvariantCulture"/> explicitly. None of these converters
/// formats anything, so culture cannot change the answer - but writing it out is what proves that,
/// and this machine's culture is <c>nb-NO</c>.
/// </remarks>
public class ConverterTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    [Theory]
    [InlineData(true, false, true, "Visible")]
    [InlineData(false, false, true, "Collapsed")]
    [InlineData(false, false, false, "Hidden")]
    [InlineData(true, true, true, "Collapsed")]
    [InlineData(false, true, true, "Visible")]
    public void BoolToVisibilityHonoursInvertAndCollapse(bool value, bool invert, bool collapse, string expected)
    {
        BoolToVisibilityConverter converter = new() { Invert = invert, Collapse = collapse };

        Assert.Equal(Enum.Parse<Visibility>(expected), converter.Convert(value, typeof(Visibility), null, Culture));
    }

    [Fact]
    public void BoolToVisibilityTreatsANonBooleanAsFalse()
    {
        // Bindings do produce nulls - a DataContext arrives late, or a path does not resolve.
        // Collapsing is the safe answer; throwing would take the window down.
        Assert.Equal(Visibility.Collapsed, BoolToVisibilityConverter.Default.Convert(null, typeof(Visibility), null, Culture));
    }

    [Fact]
    public void EnumToBooleanMatchesTheParameterAndOnlyThat()
    {
        EnumToBooleanConverter converter = EnumToBooleanConverter.Default;

        Assert.Equal(
            true,
            converter.Convert(PersonIdentification.PersonIdOnly, typeof(bool), PersonIdentification.PersonIdOnly, Culture));
        Assert.Equal(
            false,
            converter.Convert(PersonIdentification.PersonIdOnly, typeof(bool), PersonIdentification.Full, Culture));
    }

    [Fact]
    public void EnumToBooleanAcceptsTheParameterAsAString()
    {
        // ConverterParameter=Full in XAML arrives as a string unless x:Static is used.
        Assert.Equal(
            true,
            EnumToBooleanConverter.Default.Convert(PersonIdentification.Full, typeof(bool), "Full", Culture));
    }

    [Fact]
    public void EnumToBooleanRejectsAMisspelledParameter()
    {
        // Case-sensitive on purpose: a typo should be a dead radio button, not a silently different
        // identification mode.
        Assert.Equal(
            false,
            EnumToBooleanConverter.Default.Convert(PersonIdentification.Full, typeof(bool), "full", Culture));
    }

    [Fact]
    public void EnumToBooleanWritesBackOnlyWhenTheButtonIsChecked()
    {
        EnumToBooleanConverter converter = EnumToBooleanConverter.Default;

        Assert.Equal(
            PersonIdentification.RandomPersonId,
            converter.ConvertBack(true, typeof(PersonIdentification), "RandomPersonId", Culture));

        // The crux: unchecking the outgoing radio must write nothing, or it overwrites the value the
        // incoming one just set and the group never changes.
        Assert.Same(
            Binding.DoNothing,
            converter.ConvertBack(false, typeof(PersonIdentification), "RandomPersonId", Culture));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("x", true)]
    [InlineData(3, true)]
    public void NullToBooleanReportsPresence(object? value, bool expected)
    {
        Assert.Equal(expected, NullToBooleanConverter.Default.Convert(value, typeof(bool), null, Culture));
    }

    [Fact]
    public void NullToBooleanInverts()
    {
        NullToBooleanConverter converter = new() { Invert = true };

        Assert.Equal(true, converter.Convert(null, typeof(bool), null, Culture));
        Assert.Equal(false, converter.Convert("x", typeof(bool), null, Culture));
    }

    [Fact]
    public void RgbConvertsToTheSameColourChannelForChannel()
    {
        // Delphi TColor literals are $00BBGGRR, so the channel order is the one thing that can go
        // wrong here and it only shows on asymmetric colours.
        Rgb teal = Rgb.FromDelphi(0x00918817);

        Assert.Equal("#178891", teal.ToHex());
        Assert.Equal(Color.FromRgb(0x17, 0x88, 0x91), RgbConverter.ToColor(teal));
    }

    [Fact]
    public void RgbBrushesAreCachedAndFrozen()
    {
        Rgb colour = new(0xFF, 0xED, 0xBF);

        SolidColorBrush first = RgbConverter.ToBrush(colour);
        SolidColorBrush second = RgbConverter.ToBrush(new Rgb(0xFF, 0xED, 0xBF));

        Assert.Same(first, second);
        Assert.True(first.IsFrozen);
    }

    [Fact]
    public void RgbConverterReturnsABrushForABrushTargetAndAColourForAColourTarget()
    {
        Rgb colour = new(1, 2, 3);

        Assert.IsType<SolidColorBrush>(RgbConverter.Default.Convert(colour, typeof(Brush), null, Culture));
        Assert.IsType<Color>(RgbConverter.Default.Convert(colour, typeof(Color), null, Culture));
    }

    [Fact]
    public void RgbConverterLeavesTheTargetAloneForANonColour()
    {
        // "No colour here" must not paint the cell transparent.
        Assert.Same(Binding.DoNothing, RgbConverter.Default.Convert(null, typeof(Brush), null, Culture));
    }
}
