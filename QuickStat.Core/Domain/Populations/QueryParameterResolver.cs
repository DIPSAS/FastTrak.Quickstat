using System.Globalization;
using Microsoft.Extensions.Logging;
using QuickStat.Data;

namespace QuickStat.Domain.Populations;

/// <summary>
/// Resolves a population's <c>:Name</c> placeholders: the period from the user, everything else from
/// the session.
/// </summary>
/// <remarks>
/// Delphi: <c>TParameterDictionary.TryApplyParameters</c>
/// (<c>Emetra.Database.ParameterDictionary.pas:79-133</c>). Placeholders are discovered with step
/// 2.2's <see cref="ISqlTextRewriter"/> rather than a second scanner, because population SQL is
/// arbitrary server-authored T-SQL and a colon inside a literal, a bracketed identifier or a comment
/// must not be mistaken for a placeholder (PORT-PLAN.md R2).
/// </remarks>
internal sealed class QueryParameterResolver : IQueryParameterResolver
{
    /// <summary>
    /// Sub-header shown by the period dialog. <c>rsSelectPeriod</c>
    /// (<c>Emetra.Database.ParameterDictionary.pas:54</c>), in Norwegian, verbatim.
    /// </summary>
    internal const string PeriodPromptCaption = "Denne spørringen krever at du angir et tidsintervall.";

    /// <summary>Mirrors <c>LOG_UNRESOLVED_PARAMETER</c> (<c>…ParameterDictionary.pas:59</c>).</summary>
    private const string UnknownParameterFormat = "Unknown parameter name \"{0}\" found at position {1}.";

    private const string InvalidPeriodFormat =
        "The period prompt returned {0:yyyy-MM-dd HH:mm:ss} to {1:yyyy-MM-dd HH:mm:ss}, which is not a valid half-open period.";

    private const string NoSessionFormat =
        "Unknown parameter name \"{0}\" found at position {1}: no database session is open, so nothing can be resolved from it.";

    private readonly ISqlTextRewriter _rewriter;
    private readonly ISessionService _sessions;
    private readonly IPeriodPrompt _periodPrompt;
    private readonly ILogger<QueryParameterResolver> _log;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="rewriter">Step 2.2's placeholder scanner.</param>
    /// <param name="sessions">Supplies the current session, the source of every non-period value.</param>
    /// <param name="periodPrompt">Asks the user for a period; implemented by the UI.</param>
    /// <param name="log">Where diagnostics go.</param>
    public QueryParameterResolver(
        ISqlTextRewriter rewriter,
        ISessionService sessions,
        IPeriodPrompt periodPrompt,
        ILogger<QueryParameterResolver> log)
    {
        ArgumentNullException.ThrowIfNull(rewriter);
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(periodPrompt);
        ArgumentNullException.ThrowIfNull(log);

        _rewriter = rewriter;
        _sessions = sessions;
        _periodPrompt = periodPrompt;
        _log = log;
    }

    /// <inheritdoc />
    public async Task<ParameterResolution> ResolveAsync(string sqlText, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sqlText);

        RewrittenSql rewritten = _rewriter.Rewrite(sqlText);
        IReadOnlyList<string> names = rewritten.ParameterNames;
        Dictionary<string, object?> values = new(StringComparer.OrdinalIgnoreCase);

        // Emetra.Database.ParameterDictionary.pas:96-98. Both halves of the pair, or no prompt at
        // all: a population carrying only :StartDate is resolved from the session like any other
        // name, and therefore fails, exactly as it does today.
        if (Contains(names, IQueryParameterResolver.StartDateParameterName)
            && Contains(names, IQueryParameterResolver.StopDateParameterName))
        {
            ParameterResolution? failure = await TryApplyPeriodAsync(sqlText, values, cancellationToken)
                .ConfigureAwait(false);
            if (failure is not null)
            {
                return failure;
            }
        }

        SessionContext? session = _sessions.Current;

        for (int index = 0; index < names.Count; index++)
        {
            string name = names[index];
            if (values.ContainsKey(name))
            {
                continue;
            }

            if (session is null)
            {
                string reason = string.Format(CultureInfo.InvariantCulture, NoSessionFormat, name, index);
                _log.LogError("Population parameters could not be resolved. {Reason}", reason);
                return new ParameterResolution { Succeeded = false, FailureReason = reason };
            }

            if (!session.TryGetParameterValue(name, out object? value))
            {
                // Emetra.Database.ParameterDictionary.pas:125-127 logs a SilentError and stops at the
                // first unresolvable name. ParameterResolution can finally say which one it was.
                string reason = string.Format(CultureInfo.InvariantCulture, UnknownParameterFormat, name, index);
                _log.LogError("Population parameters could not be resolved. {Reason}", reason);
                return new ParameterResolution { Succeeded = false, FailureReason = reason };
            }

            values[name] = value;
        }

        // LOG_PARAMETER_SET logged the values too. Values are omitted here: a population parameter can
        // carry a national identity number and the log file is not access-controlled.
        _log.LogDebug("Resolved {Count} population parameters: {ParameterNames}.", values.Count, string.Join(", ", names));

        return new ParameterResolution { Succeeded = true, Values = values };
    }

    private static bool Contains(IReadOnlyList<string> names, string name)
    {
        for (int i = 0; i < names.Count; i++)
        {
            if (string.Equals(names[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Runs the period prompt and binds the pair.</summary>
    /// <returns><see langword="null"/> on success, or the failed resolution to return.</returns>
    private async Task<ParameterResolution?> TryApplyPeriodAsync(
        string sqlText,
        Dictionary<string, object?> values,
        CancellationToken cancellationToken)
    {
        HalfOpenPeriod? period = await _periodPrompt
            .TryGetPeriodAsync(PeriodSettingsKey.For(sqlText), PeriodPromptCaption, cancellationToken)
            .ConfigureAwait(false);

        if (period is null)
        {
            // PORT-PLAN.md §7.2: this must abort the load. The Delphi cleared the patient list only
            // inside the success branch, so the previous cohort stayed on screen under the new
            // population's title (CRF.Patient.List.pas:297-299).
            _log.LogInformation("The user cancelled the period prompt; the population load is aborted.");
            return new ParameterResolution { Succeeded = false, CancelledByUser = true };
        }

        HalfOpenPeriod value = period.Value;

        if (!value.IsValid)
        {
            // The dialog itself refuses to return an invalid period (Emetra.VclForm.Period.pas:52),
            // and IPeriodPrompt returns null for a cancel, so this is a broken prompt rather than a
            // user decision - and it must not be reported as a cancel.
            string reason = string.Format(CultureInfo.InvariantCulture, InvalidPeriodFormat, value.Start, value.Stop);
            _log.LogError("Population parameters could not be resolved. {Reason}", reason);
            return new ParameterResolution { Succeeded = false, FailureReason = reason };
        }

        // PORT-PLAN.md R8: [Start, Stop), end-exclusive. Bound exactly as chosen - no rounding, no
        // off-by-one day on either end.
        values[IQueryParameterResolver.StartDateParameterName] = value.Start;
        values[IQueryParameterResolver.StopDateParameterName] = value.Stop;
        return null;
    }
}
