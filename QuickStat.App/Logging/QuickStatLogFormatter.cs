using System.Globalization;
using System.IO;
using QuickStat.Diagnostics;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Parsing;

namespace QuickStat.Logging;

/// <summary>
/// Renders one log entry, with every personal identifier removed.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the R6 choke point, and it is the reason the port formats its own entries instead of
/// handing Serilog an output template.</b> Redaction happens here because this is the last place
/// before bytes reach the disk: no call site can route around it, and no future caller has to
/// remember it. <c>Docs/Port/01-data-access.md</c> §7.5 asks for exactly this. PORT-PLAN.md §9 R6
/// treats a privacy regression as release-blocking, which is why
/// <c>QuickStat.Tests/Logging/FileLoggerRedactionTests.cs</c> proves the behaviour by reading the
/// actual bytes of the actual file rather than by testing this class in isolation.
/// </para>
/// <para>
/// <b>The message and the exception are redacted differently, and the difference matters.</b> The
/// message goes through <see cref="PiiRedactor.ForLog"/>, which also folds it onto one line -
/// matching the Delphi's <c>AnonymizeLogMessage</c>, and incidentally denying a caller the ability
/// to forge log entries by embedding a newline. The exception goes through
/// <see cref="PiiRedactor.Redact"/> instead: it is written as a multi-line block below the entry,
/// and collapsing a stack trace onto one line would destroy the only thing it is for. It is
/// redacted all the same, because an exception message quotes its inputs.
/// </para>
/// <para>
/// <b>Serilog eats the <c>{{ }}</c> convention if you let it, and that would be a silent privacy
/// regression.</b> Serilog's template parser reads <c>{{</c> as an escaped <c>{</c>, so a call site
/// that wrote <c>"Loaded patient {{Ola Nordmann}}"</c> would arrive here already rendered as
/// <c>"Loaded patient {Ola Nordmann}"</c> - and <see cref="PiiRedactor"/>, which looks for the
/// doubled brace, would never fire. Microsoft.Extensions.Logging does not do this, so the behaviour
/// changed underneath the convention the moment Serilog went in. <see cref="RenderMessage"/>
/// therefore redacts the <em>raw</em> template text before it is parsed. Nothing in the port
/// currently writes handlebars into a log template - they arrive in <em>data</em>, where Serilog
/// renders property values verbatim and the ordinary path catches them - but the convention is
/// documented, it is what <see cref="PiiRedactor"/> exists for, and a convention that works only
/// until someone uses it is worse than none.
/// </para>
/// </remarks>
internal sealed class QuickStatLogFormatter : ITextFormatter
{
    /// <summary>Shown when an entry carries no <c>SourceContext</c>.</summary>
    internal const string NoCategory = "(none)";

    /// <summary>Stateless, and Serilog uses one the same way.</summary>
    private static readonly MessageTemplateParser Parser = new();

    /// <summary>
    /// Renders the message and nothing else, with string values <b>unquoted</b>.
    /// </summary>
    /// <remarks>
    /// The <c>l</c> in <c>{Message:lj}</c> is load-bearing. Serilog's default renders a string
    /// property as <c>"value"</c>, quotes included; Microsoft.Extensions.Logging does not, and every
    /// template in this port was written against that. Several add quotes of their own -
    /// <c>"Loaded population {ProcId} '{Title}'"</c>, <c>"Deleted package {RowId} \"{Title}\""</c> -
    /// which under Serilog's default would come out as <c>'"HbA1c"'</c> and <c>""Alfa""</c>. So this
    /// is not a matter of taste: literal rendering is what keeps a hundred existing call sites
    /// reading the way their authors wrote them. <c>j</c> keeps structured values as JSON.
    /// </remarks>
    private static readonly MessageTemplateTextFormatter MessageRenderer =
        new("{Message:lj}", CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public void Format(LogEvent logEvent, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(output);

        output.Write(logEvent.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
        output.Write(" [");
        output.Write(Abbreviate(logEvent.Level));

        // Serilog does not capture the thread, and Serilog.Enrichers.Thread is a package this does
        // not need: a non-async file sink formats on the thread that logged, so this IS that thread.
        output.Write("] [T");
        output.Write(Environment.CurrentManagedThreadId.ToString("00", CultureInfo.InvariantCulture));
        output.Write("] ");
        output.Write(Category(logEvent));

        int eventId = EventId(logEvent);

        if (eventId != 0)
        {
            output.Write('(');
            output.Write(eventId.ToString(CultureInfo.InvariantCulture));
            output.Write(')');
        }

        output.Write(": ");
        output.Write(RenderMessage(logEvent));
        output.Write(Environment.NewLine);

        if (logEvent.Exception is not null)
        {
            output.Write(PiiRedactor.Redact(logEvent.Exception.ToString()));
            output.Write(Environment.NewLine);
        }
    }

    /// <summary>Renders the message, redacted, with the handlebar convention intact.</summary>
    /// <param name="logEvent">The entry.</param>
    /// <returns>One line, safe to write.</returns>
    /// <remarks>
    /// The raw template is redacted <em>before</em> parsing only when it actually carries a
    /// <c>{{</c>, which is close to never - so the common path is one parse-free render. Redacting
    /// the template cannot damage the holes: <see cref="PiiRedactor"/> matches the doubled brace,
    /// and a property hole is a single one.
    /// </remarks>
    private static string RenderMessage(LogEvent logEvent)
    {
        LogEvent source = logEvent;

        if (logEvent.MessageTemplate.Text.Contains("{{", StringComparison.Ordinal))
        {
            source = new LogEvent(
                logEvent.Timestamp,
                logEvent.Level,
                logEvent.Exception,
                Parser.Parse(PiiRedactor.Redact(logEvent.MessageTemplate.Text)),
                [.. logEvent.Properties.Select(property => new LogEventProperty(property.Key, property.Value))]);
        }

        StringWriter buffer = new(CultureInfo.InvariantCulture);

        MessageRenderer.Format(source, buffer);

        return PiiRedactor.ForLog(buffer.ToString());
    }

    /// <summary>The Microsoft.Extensions.Logging category, which Serilog carries as <c>SourceContext</c>.</summary>
    private static string Category(LogEvent logEvent) =>
        logEvent.Properties.TryGetValue("SourceContext", out LogEventPropertyValue? value)
        && value is ScalarValue { Value: string category }
        && category.Length > 0
            ? category
            : NoCategory;

    /// <summary>The event id, or zero when there is none.</summary>
    /// <remarks>
    /// <c>Serilog.Extensions.Logging</c> attaches <c>EventId</c> as a structure with <c>Id</c> and
    /// <c>Name</c> members, and only when the caller supplied one.
    /// </remarks>
    private static int EventId(LogEvent logEvent)
    {
        if (!logEvent.Properties.TryGetValue("EventId", out LogEventPropertyValue? value)
            || value is not StructureValue structure)
        {
            return 0;
        }

        foreach (LogEventProperty property in structure.Properties)
        {
            if (string.Equals(property.Name, "Id", StringComparison.Ordinal)
                && property.Value is ScalarValue { Value: int id })
            {
                return id;
            }
        }

        return 0;
    }

    /// <summary>
    /// Three-letter level names, unchanged from the provider this replaced so that anyone reading an
    /// older log file reads the new one the same way.
    /// </summary>
    private static string Abbreviate(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "TRC",
        LogEventLevel.Debug => "DBG",
        LogEventLevel.Information => "INF",
        LogEventLevel.Warning => "WRN",
        LogEventLevel.Error => "ERR",
        LogEventLevel.Fatal => "CRT",
        _ => "NON",
    };
}
