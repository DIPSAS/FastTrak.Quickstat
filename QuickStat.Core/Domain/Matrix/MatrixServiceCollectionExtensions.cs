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
    /// <summary>Adds the result matrix, the datapoint factory and the caption dictionary.</summary>
    /// <param name="services">The container.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddQuickStatMatrix(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IDataPointFactory, DataPointFactory>();
        services.TryAddSingleton<ITitleDictionary>(static _ => CaptionDictionary.WithQuickStatDefaults());
        services.TryAddSingleton<PersonMatrix>();

        return services;
    }
}
