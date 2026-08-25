using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace QuickStat.Logging;

/// <summary>
/// Registration helpers for <see cref="FileLoggerProvider"/>.
/// </summary>
public static class FileLoggerBuilderExtensions
{
    /// <summary>
    /// Adds a file logger writing to <c>&lt;executable directory&gt;\LOGS</c>.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static ILoggingBuilder AddFile(this ILoggingBuilder builder) =>
        builder.AddFile(FileLoggerProvider.DefaultLogDirectory);

    /// <summary>
    /// Adds a file logger writing to an explicit directory.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="logDirectory">Directory the log files are written to. Created if missing.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string logDirectory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>(_ => new FileLoggerProvider(logDirectory)));

        return builder;
    }
}
