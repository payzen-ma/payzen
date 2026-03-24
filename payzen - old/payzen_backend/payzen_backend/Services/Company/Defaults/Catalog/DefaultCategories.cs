using System.Collections.Generic;
using payzen_backend.Models.Employee;

namespace payzen_backend.Services.Company.Defaults.Catalog
{
    /// <summary>
    /// Catalogue des catégories d'employés créées par défaut pour chaque nouvelle company.
    /// </summary>
    public static class DefaultCategories
    {
        public sealed class CategoryDefinition
        {
            public required string Name { get; init; }
            public EmployeeCategoryMode Mode { get; init; }
            public string PayrollPeriodicity { get; init; } = "Mensuelle";
        }

        /// <summary>
        /// Retourne la liste des catégories à créer pour une nouvelle entreprise.
        /// </summary>
        public static IReadOnlyList<CategoryDefinition> GetDefaults()
        {
            return new List<CategoryDefinition>
            {
                new() { Name = "Cadre", Mode = EmployeeCategoryMode.Attendance, PayrollPeriodicity = "Mensuelle" },
                new() { Name = "Employé", Mode = EmployeeCategoryMode.Attendance, PayrollPeriodicity = "Mensuelle" },
                new() { Name = "Ouvrier", Mode = EmployeeCategoryMode.Attendance, PayrollPeriodicity = "Bimensuelle" }
            };
        }
    }
}
