using Microsoft.Extensions.Logging;

namespace QuickStat.Logging;

/// <summary>
/// The <see cref="ILogger"/> handed out by <see cref="FileLoggerProvider"/>.
/// </summary>
/// <remarks>
/// Level filtering is left to the <see cref="ILoggerFactory"/>, which applies the rules configured
/// on the logging builder before calling <see cref="Log{TState}"/>. Scopes are not recorded.
/// </remarks>
internal sealed class FileLogger : ILogger
{
    private readonly FileLoggerProvider _provider;
    private readonly string _category;

    internal FileLogger(FileLoggerProvider provider, string category)
    {
        _provider = provider;
        _category = string.IsNullOrEmpty(category) ? "(none)" : category;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel) || formatter is null)
        {
            return;
        }

        string message;

        try
        {
            message = formatter(state, exception);
        }
        catch (Exception)
        {
            // A broken message formatter must not take the application down either.
            return;
        }

        if (string.IsNullOrEmpty(message) && exception is null)
        {
            return;
        }

        _provider.Write(logLevel, _category, eventId, message, exception);
    }
}
