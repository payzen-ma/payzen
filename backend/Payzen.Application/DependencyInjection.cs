using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Payzen.Application.Payroll;

namespace Payzen.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Enregistre uniquement ce qui appartient à la couche Application pure :
    ///   - FluentValidation (scan de l'assembly)
    ///   - PayrollCalculationEngine (moteur pur — aucune dépendance DB)
    ///
    /// Tous les services métier (IAuthService, ICompanyService, etc.) sont enregistrés
    /// dans Payzen.Infrastructure via AddInfrastructure().
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<PayrollCalculationEngine>();
        return services;
    }
}
