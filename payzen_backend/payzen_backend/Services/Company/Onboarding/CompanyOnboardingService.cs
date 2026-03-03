using payzen_backend.Services.Company.Interfaces;

namespace payzen_backend.Services.Company.Onboarding
{
    /// <summary>
    /// Service d'onboarding : initialise les données par défaut d'une nouvelle company
    /// (types de contrat, départements, postes, calendrier, congés).
    /// </summary>
    public class CompanyOnboardingService : ICompanyOnboardingService
    {
        private readonly ICompanyDefaultsSeeder _defaultsSeeder;

        public CompanyOnboardingService(ICompanyDefaultsSeeder defaultsSeeder)
        {
            _defaultsSeeder = defaultsSeeder;
        }

        public async Task OnboardAsync(int companyId, int userId)
        {
            await _defaultsSeeder.SeedDefaultsAsync(companyId, userId);
        }
    }
}
