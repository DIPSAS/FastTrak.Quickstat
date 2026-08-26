namespace QuickStat.Diagnostics;

/// <summary>How loudly to tell the user something.</summary>
/// <remarks>
/// The three levels of <c>TLogLevel</c> that actually produced a dialog in the Delphi -
/// <c>ltMessage</c>, <c>ltWarning</c> and <c>ltError</c>/<c>ltException</c>
/// (<c>Emetra.Logging.Interfaces.pas:56-85</c>). The other three levels were logging only and now
/// go to <c>ILogger</c>, which is the whole point of the split.
/// </remarks>
public enum NotificationSeverity
{
    /// <summary>Information. Delphi <c>ltMessage</c>, information icon.</summary>
    Information = 0,

    /// <summary>Warning. Delphi <c>ltWarning</c>.</summary>
    Warning = 1,

    /// <summary>Error. Delphi <c>ltError</c> / <c>ltException</c>.</summary>
    Error = 2,
}
