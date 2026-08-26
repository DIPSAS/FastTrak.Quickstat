namespace QuickStat.Configuration;

/// <summary>
/// Configuration could not be read or translated: a malformed <c>QuickStat.config.xml</c>, an
/// unreadable or too-short <c>.UDL</c>, or a connection string with neither integrated security nor
/// credentials.
/// </summary>
/// <remarks>
/// One exception type for the whole of step 2.1 rather than three, because the caller's response is
/// the same in every case: tell the user which file is wrong and refuse to connect. The Delphi
/// silently did nothing when a UDL had fewer than three lines
/// (<c>Emetra.Database.ConnectionString.pas:193-194</c>), leaving an unusable
/// <c>FILE NAME=</c> string that ADO also accepted - an invisible failure mode.
/// </remarks>
public class QuickStatConfigurationException : Exception
{
    /// <summary>Initialises a new instance.</summary>
    public QuickStatConfigurationException()
    {
    }

    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">Message naming the offending file and what is wrong with it.</param>
    public QuickStatConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>Initialises a new instance.</summary>
    /// <param name="message">Message naming the offending file and what is wrong with it.</param>
    /// <param name="innerException">The underlying I/O or parse failure.</param>
    public QuickStatConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>The configuration or UDL file the failure relates to, when known.</summary>
    public string? FilePath { get; init; }
}
