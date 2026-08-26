using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuickStat.Configuration;
using QuickStat.Domain.Packages;
using QuickStat.Domain.Patients;

namespace QuickStat.Domain.Populations;

/// <summary>
/// Registers the population, patient and packaged-selection services (step 2.3).
/// </summary>
/// <remarks>
/// <para>
/// The composition root calls this under its own Phase 2.3 anchor; step 2.3 does not edit
/// <c>App.xaml.cs</c> itself, so that seven parallel agents never touch the same lines.
/// </para>
/// <para>
/// The file lives in <c>Domain/Populations/</c> - and therefore carries that namespace, because
/// <c>IDE0130</c> is an error - simply because every folder step 2.3 owns is a leaf. Nothing about it
/// is population-specific.
/// </para>
/// </remarks>
public static class DomainServiceCollectionExtensions
{
    /// <summary>Adds the step 2.3 services.</summary>
    /// <param name="services">The container being built.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Everything registered here is stateless, so singletons. Registration is additive - each entry
    /// is skipped if the composition root has already supplied its own - which matters for
    /// <see cref="SqlOptions"/>: it belongs to step 2.1 and is registered here only so that a
    /// container built from this one call is usable on its own.
    /// </para>
    /// <para>
    /// <see cref="IPeriodPrompt"/> is deliberately <em>not</em> registered. It shows a window, so the
    /// UI layer owns the implementation.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddQuickStatDomain(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(_ => new SqlOptions());

        services.TryAddSingleton<IPopulationRepository, PopulationRepository>();
        services.TryAddSingleton<IQueryParameterResolver, QueryParameterResolver>();
        services.TryAddSingleton<IPatientRepository, PatientRepository>();
        services.TryAddSingleton<IPackageRepository, PackageRepository>();

        return services;
    }
}
