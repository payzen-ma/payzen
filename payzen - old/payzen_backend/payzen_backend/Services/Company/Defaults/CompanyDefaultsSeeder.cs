using payzen_backend.Services.Company.Defaults.Seeders;
using payzen_backend.Services.Company.Interfaces;

namespace payzen_backend.Services.Company.Defaults
{
    /// <summary>
    /// Orchestre le seed de toutes les données par défaut d'une company :
    /// types de contrat, départements, postes, catégories d'employés, calendrier de travail, types de congé et politiques.
    /// </summary>
    public class CompanyDefaultsSeeder : ICompanyDefaultsSeeder
    {
        private readonly ContractTypeSeeder _contractTypeSeeder;
        private readonly DepartmentSeeder _departmentSeeder;
        private readonly JobPositionSeeder _jobPositionSeeder;
        private readonly EmployeeCategorySeeder _employeeCategorySeeder;
        private readonly WorkingCalendarSeeder _workingCalendarSeeder;
        private readonly LeaveSeeder _leaveSeeder;

        public CompanyDefaultsSeeder(
            ContractTypeSeeder contractTypeSeeder,
            DepartmentSeeder departmentSeeder,
            JobPositionSeeder jobPositionSeeder,
            EmployeeCategorySeeder employeeCategorySeeder,
            WorkingCalendarSeeder workingCalendarSeeder,
            LeaveSeeder leaveSeeder)
        {
            _contractTypeSeeder = contractTypeSeeder;
            _departmentSeeder = departmentSeeder;
            _jobPositionSeeder = jobPositionSeeder;
            _employeeCategorySeeder = employeeCategorySeeder;
            _workingCalendarSeeder = workingCalendarSeeder;
            _leaveSeeder = leaveSeeder;
        }

        public async Task SeedDefaultsAsync(int companyId, int userId)
        {
            await _contractTypeSeeder.SeedAsync(companyId, userId);
            await _departmentSeeder.SeedAsync(companyId, userId);
            await _jobPositionSeeder.SeedAsync(companyId, userId);
            await _employeeCategorySeeder.SeedAsync(companyId, userId);
            await _workingCalendarSeeder.SeedAsync(companyId, userId);
            await _leaveSeeder.SeedAsync(companyId, userId);
        }
    }
}
