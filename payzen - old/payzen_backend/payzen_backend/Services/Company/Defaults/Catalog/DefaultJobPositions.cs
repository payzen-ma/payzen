using System.Collections.Generic;

namespace payzen_backend.Services.Company.Defaults.Catalog
{
    /// <summary>
    /// Catalogue des postes (job positions) créés par défaut pour chaque nouvelle company.
    /// </summary>
    public static class DefaultJobPositions
    {
        /// <summary>
        /// Retourne la liste des noms de postes à créer pour une nouvelle entreprise.
        /// </summary>
        public static IReadOnlyList<string> GetDefaults()
        {
            return new List<string>
            {
                "Directeur / Directrice",
                "Responsable RH",
                "Comptable",
                "Commercial(e)",
                "Développeur(se)",
                "Assistant(e)",
                "Chargé(e) de mission",
                "Manager"
            };
        }
    }
}
