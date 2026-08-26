namespace QuickStat.Domain.Anonymisation;

/// <summary>
/// The one shared identification mode. Register as a singleton; never construct a second one.
/// </summary>
/// <remarks>
/// <para>
/// PORT-PLAN.md §7.2 lists "display anonymity and export anonymity are independent paths" as a bug
/// being fixed. The structural half of that fix is this class existing exactly once in the
/// container; the derivational half is <see cref="IdentificationColumns.For"/> being the only place
/// a mode becomes a column set. Neither the grid nor the exporter may branch on
/// <see cref="PersonIdentification"/> directly.
/// </para>
/// <para>
/// Deliberately not thread-safe beyond the usual "set it from the UI thread": the Delphi read the
/// radio buttons at export time from the same thread that owned them, and nothing in the port needs
/// more.
/// </para>
/// </remarks>
public sealed class IdentificationPolicy : IIdentificationPolicy
{
    private PersonIdentification _mode = PersonIdentification.PersonIdOnly;

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">The value is not a declared mode.</exception>
    public PersonIdentification Mode
    {
        get => _mode;
        set
        {
            // Validate through the single derivation, so an undeclared value can never be stored
            // and then silently produce a column set that nobody chose.
            _ = IdentificationColumns.For(value);

            if (_mode == value)
            {
                return;
            }

            _mode = value;
            ModeChanged?.Invoke(this, value);
        }
    }

    /// <inheritdoc />
    public IdentificationColumns Columns => IdentificationColumns.For(_mode);

    /// <inheritdoc />
    public event EventHandler<PersonIdentification>? ModeChanged;
}
