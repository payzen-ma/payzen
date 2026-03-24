using System.Collections.Generic;

namespace payzen_backend.Services.Company.Defaults.Catalog
{
    /// <summary>
    /// Catalogue des départements créés par défaut pour chaque nouvelle company.
    /// </summary>
    public static class DefaultDepartments
    {
        /// <summary>
        /// Retourne la liste des noms de départements à créer pour une nouvelle entreprise.
        /// </summary>
        public static IReadOnlyList<string> GetDefaults()
        {
            return new List<string>
            {
                "Direction",
                "Ressources Humaines",
                "Comptabilité",
                "Commercial",
                "Technique / IT",
                "Administration",
                "Logistique"
            };
        }
    }
}
