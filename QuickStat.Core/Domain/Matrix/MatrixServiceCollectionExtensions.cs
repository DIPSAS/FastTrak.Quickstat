using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuickStat.Domain.DataPoints;

namespace QuickStat.Domain.Matrix;

/// <summary>Registers the matrix and the datapoint machinery.</summary>
/// <remarks>
/// Everything here is a singleton because the Delphi has exactly one of each: one
/// <c>TPersonGridData</c>, one <c>TDataPointFactory</c> built in <c>AfterConstruction</c>, and one
/// <c>TVarCaptions</c> that survives every project switch. The matrix is mutable and stateful, so a
/// second instance would silently split the dataset in two.
/// </remarks>
public static class MatrixServiceCollectionExtensions
{
    /// <summary>Adds the result matrix, the datapoint factory and the caption machinery.</summary>
    /// <param name="services">The container.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddQuickStatMatrix(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IDataPointFactory, DataPointFactory>();

        // The concrete dictionary is registered as well as the interface, and ITitleDictionary
        // resolves to the very same instance.  ITitleDictionary is read-only by design, so a loader
        // needs the class to write into it - and if the two were registered independently the
        // matrix would read one dictionary while the loader filled another, which fails silently as
        // "every column shows its raw variable name".
        services.TryAddSingleton(static _ => CaptionDictionary.WithQuickStatDefaults());
        services.TryAddSingleton<ITitleDictionary>(static provider => provider.GetRequiredService<CaptionDictionary>());

        services.TryAddSingleton<ICaptionRepository, CaptionRepository>();
        services.TryAddSingleton<ICaptionLoader, CaptionLoader>();

        services.TryAddSingleton<PersonMatrix>();

        return services;
    }
}
