namespace QuickStat.Data;

/// <summary>
/// A <see cref="SqlRequest"/> could not be bound: wrong number of positional values, a named value
/// missing for a discovered placeholder, both binding styles supplied at once, or positional
/// binding attempted against a statement that repeats a placeholder.
/// </summary>
/// <remarks>
/// This class of error had no representation in the Delphi at all. <c>PrepareQueryParameters</c>
/// looped to <c>Parameters.Count</c> rather than to the length of the supplied array
/// (<c>Emetra.Database.Simple.pas:415-433</c>), so too few values read past the end of an open
/// array - undefined behaviour with no diagnostic. Positional binding also makes argument-order
/// mistakes silent; there is a live example in the library where a last name lands in the
/// <c>:MidName</c> slot (<c>Emetra.Database.Simple.pas:140</c> against <c>:459</c>).
/// </remarks>
public sealed class SqlParameterBindingException : QuickStatDataException
{
    /// <summary>Initialises a new instance.</summary>
    public SqlParameterBindingException()
    {
    }

    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">Description naming the placeholder or the expected count.</param>
    public SqlParameterBindingException(string message)
        : base(message)
    {
    }

    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">Description naming the placeholder or the expected count.</param>
    /// <param name="innerException">The underlying failure.</param>
    public SqlParameterBindingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Placeholder the failure relates to, when it is about one specific name.</summary>
    public string? ParameterName { get; init; }
}
