using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using Xunit;

namespace QuickStat.Tests.Ui;

/// <summary>
/// Forces the static constructor of every WPF type in <c>QuickStat.App</c>, because that is where
/// dependency properties are registered and nothing else in a test run touches them.
/// </summary>
/// <remarks>
/// <para>
/// This exists because of a real failure. <c>MatrixGrid</c> re-owned three inherited text properties
/// with <c>new FrameworkPropertyMetadata(SomeFlags)</c> — and
/// <see cref="FrameworkPropertyMetadata"/> has <b>no constructor taking only</b>
/// <see cref="FrameworkPropertyMetadataOptions"/>. The call bound to the
/// <c>(object defaultValue)</c> overload instead and boxed the flags enum <i>as the default value</i>.
/// It compiled without a warning, passed every existing test, and then threw
/// <c>ArgumentException: Default value type does not match type of property 'FontFamily'</c> out of
/// the type initialiser the first moment any XAML mentioned the control — taking down the whole
/// window with a <see cref="System.Windows.Markup.XamlParseException"/> whose message named the
/// wrong culprit.
/// </para>
/// <para>
/// A dependency property is only validated when its declaring type is first touched, so the entire
/// class of registration bug is invisible until the application starts. Sweeping every type is
/// cheap and catches it for controls that do not exist yet, which is the point: this is a net for
/// future steps, not a regression test for one fixed bug.
/// </para>
/// <para>
/// Shared across Phase 3 steps and not owned by any of them — extend it rather than copying it.
/// </para>
/// </remarks>
public class DependencyPropertyRegistrationTests
{
    private static IEnumerable<Type> WpfTypes() =>
        typeof(QuickStat.App).Assembly
            .GetTypes()
            .Where(type => typeof(DependencyObject).IsAssignableFrom(type))
            .Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition)
            .OrderBy(type => type.FullName, StringComparer.Ordinal);

    [Fact]
    public void EveryDependencyPropertyRegistrationIsValid()
    {
        // On an STA thread because a default value can itself be a DispatcherObject - a Brush, a
        // FontFamily - and DependencyProperty validates thread affinity while registering.
        List<string> failures = StaTestRunner.Run(() =>
        {
            List<string> broken = [];

            foreach (Type type in WpfTypes())
            {
                try
                {
                    RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                }
                catch (TypeInitializationException exception)
                {
                    // Report every offender in one run rather than stopping at the first: a bad
                    // metadata overload is usually copied across several properties at once.
                    broken.Add($"{type.FullName}: {exception.InnerException?.Message ?? exception.Message}");
                }
            }

            return broken;
        });

        Assert.Empty(failures);
    }

    [Fact]
    public void TheSweepActuallyReachesTheControls()
    {
        // Guards the guard.  If the filter above ever stops matching - a renamed namespace, a
        // control that becomes generic - the test would keep passing while checking nothing.
        IReadOnlyList<Type> types = [.. WpfTypes()];

        // Root-qualified: step 3.5's tests live in QuickStat.Tests.Ui.Controls, which shadows
        // QuickStat.Controls for every unqualified lookup made from this namespace.
        Assert.Contains(types, type => type == typeof(global::QuickStat.Controls.Dataset.MatrixGrid));
        Assert.Contains(types, type => type == typeof(MainWindow));
    }

    [Theory]
    [InlineData("FontFamily")]
    [InlineData("FontSize")]
    [InlineData("Foreground")]
    public void TheReOwnedTextPropertiesKeepTheirMetadataFlags(string propertyName)
    {
        // The second, quieter half of the same bug, and the half that survives a careless fix.
        // Binding to the (object) overload did not just poison the default value - it meant the
        // flags were never passed at all, so Inherits and AffectsRender were silently false. The
        // control would then have compiled, started, and simply not repainted when the theme
        // changed the font, with nothing to explain why.
        StaTestRunner.Run(() =>
        {
            Type grid = typeof(global::QuickStat.Controls.Dataset.MatrixGrid);

            DependencyProperty property = (DependencyProperty)grid
                .GetField($"{propertyName}Property", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)!
                .GetValue(null)!;

            FrameworkPropertyMetadata metadata = Assert.IsType<FrameworkPropertyMetadata>(property.GetMetadata(grid));

            Assert.True(metadata.Inherits, $"{propertyName} must inherit from the surrounding theme.");
            Assert.True(metadata.AffectsRender, $"{propertyName} must repaint the grid when it changes.");
            Assert.NotNull(metadata.DefaultValue);
            Assert.IsAssignableFrom(property.PropertyType, metadata.DefaultValue);
        });
    }

    [Fact]
    public void EveryRegisteredPropertyAcceptsItsOwnDefaultValue()
    {
        // The complementary half: RunClassConstructor proves registration succeeded, this proves the
        // registered default would survive being set back.  A default that fails its own validation
        // callback is legal at registration time and throws on first assignment.
        List<string> failures = StaTestRunner.Run(() =>
        {
            List<string> broken = [];

            foreach (Type type in WpfTypes())
            {
                RuntimeHelpers.RunClassConstructor(type.TypeHandle);

                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
                {
                    if (field.GetValue(null) is not DependencyProperty property)
                    {
                        continue;
                    }

                    object? defaultValue = property.GetMetadata(type).DefaultValue;

                    if (defaultValue is not null && !property.PropertyType.IsInstanceOfType(defaultValue))
                    {
                        broken.Add(
                            $"{type.FullName}.{property.Name}: default is {defaultValue.GetType().Name}, "
                            + $"property is {property.PropertyType.Name}");
                    }
                }
            }

            return broken;
        });

        Assert.Empty(failures);
    }
}
