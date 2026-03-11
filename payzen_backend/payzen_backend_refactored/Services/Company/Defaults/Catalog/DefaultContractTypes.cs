using System.Collections.Generic;

namespace payzen_backend.Services.Company.Defaults.Catalog
{
    /// <summary>
    /// Catalogue des types de contrat "par défaut" créés pour chaque nouvelle company.
    /// On se base sur les codes légaux marocains CDI / CDD / STAGE
    /// et éventuellement sur les programmes d'emploi (ANAPEC / IDMAJ / TAHFIZ).
    /// </summary>
    public static class DefaultContractTypes
    {
        public sealed class ContractTypeDefinition
        {
            public required string Name { get; init; }
            /// <summary>
            /// Code du <see cref="Models.Referentiel.LegalContractType"/> à lier (CDI, CDD, STAGE, ...).
            /// Peut être null si aucun mapping.
            /// </summary>
            public string? LegalContractTypeCode { get; init; }

            /// <summary>
            /// Code du <see cref="Models.Referentiel.StateEmploymentProgram"/> (NONE, ANAPEC, IDMAJ, TAHFIZ).
            /// Peut être null.
            /// </summary>
            public string? StateProgramCode { get; init; }
        }

        /// <summary>
        /// Retourne la liste de types de contrats à créer pour une nouvelle entreprise.
        /// </summary>
        public static IReadOnlyList<ContractTypeDefinition> GetDefaults()
        {
            return new List<ContractTypeDefinition>
            {
                new()
                {
                    Name = "CDI",
                    LegalContractTypeCode = "CDI",
                    StateProgramCode = null
                },
                new()
                {
                    Name = "CDD",
                    LegalContractTypeCode = "CDD",
                    StateProgramCode = null
                },
                new()
                {
                    Name = "ANAPEC",
                    LegalContractTypeCode = "STAGE",
                    StateProgramCode = null
                },
            };
        }
    }
}
