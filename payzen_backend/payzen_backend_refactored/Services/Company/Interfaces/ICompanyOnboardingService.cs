namespace payzen_backend.Services.Company.Interfaces
{
    /// <summary>
    /// Service d'onboarding pour une nouvelle entreprise :
    /// - création des données par défaut (types de contrat, départements, postes, calendrier, congés, etc.)
    /// </summary>
    public interface ICompanyOnboardingService
    {
        /// <summary>
        /// Initialise toutes les données "par défaut" d'une nouvelle company.
        /// Cette méthode est idempotente : elle ne recrée pas ce qui existe déjà.
        /// </summary>
        /// <param name="companyId">Id de la société fraîchement créée</param>
        /// <param name="userId">Id de l'utilisateur qui déclenche l'onboarding</param>
        Task OnboardAsync(int companyId, int userId);
    }
}
