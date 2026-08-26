using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Diagnostics;
using QuickStat.Domain.Anonymisation;

namespace QuickStat.Export;

/// <summary>Registers step 2.6 - anonymisation and export.</summary>
public static class ExportServiceCollectionExtensions
{
    /// <summary>Adds the anonymisation and export services.</summary>
    /// <param name="services">The container.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Every registration is a <b>singleton</b>, and for two of them that is a correctness
    /// requirement rather than a performance choice:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="IIdentificationPolicy"/> is the one shared answer to "how identified is this
    ///     dataset?". A second instance would restore exactly the defect PORT-PLAN.md §7.2 records,
    ///     where the grid and the file could disagree.
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="IAnonymiser"/> holds the pseudonym map and its key. A scoped or transient
    ///     registration would hand every export a new key, which is the Delphi behaviour being
    ///     fixed: the same patient would change pseudonym between two exports of one loaded dataset.
    ///   </description></item>
    /// </list>
    /// <para>
    /// <see cref="Microsoft.Extensions.Logging.ILogger{TCategoryName}"/> and
    /// <see cref="IUserNotifier"/> are resolved optionally, so this method works in a bare container
    /// - a unit test does not have to stand up logging to get an exporter.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddQuickStatExport(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IIdentificationPolicy, IdentificationPolicy>();
        services.TryAddSingleton<IAnonymiser, MatrixAnonymiser>();

        services.TryAddSingleton<ITempFileTracker>(provider => new TempFileTracker(
            provider.GetService<ILogger<TempFileTracker>>()));

        services.TryAddSingleton<IDatasetExporter>(provider => new DatasetExporter(
            provider.GetRequiredService<IAnonymiser>(),
            provider.GetService<ILogger<DatasetExporter>>() ?? NullLogger<DatasetExporter>.Instance,
            provider.GetService<IUserNotifier>()));

        return services;
    }
}
