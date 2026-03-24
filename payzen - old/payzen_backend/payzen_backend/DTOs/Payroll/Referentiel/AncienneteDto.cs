namespace payzen_backend.DTOs.Payroll.Referentiel
{
    /// <summary>
    /// DTO for Ancienneté Rate (seniority bonus tier)
    /// </summary>
    public class AncienneteRateDto
    {
        public int Id { get; set; }
        public int MinYears { get; set; }
        public int? MaxYears { get; set; }
        public decimal Rate { get; set; }
    }

    /// <summary>
    /// DTO for creating/updating Ancienneté Rate
    /// </summary>
    public class CreateAncienneteRateDto
    {
        public int MinYears { get; set; }
        public int? MaxYears { get; set; }
        public decimal Rate { get; set; }
    }

    /// <summary>
    /// DTO for Ancienneté Rate Set (collection of seniority tiers)
    /// </summary>
    public class AncienneteRateSetDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsLegalDefault { get; set; }
        public string? Source { get; set; }
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public bool IsActive { get; set; }

        /// <summary>
        /// null for legal default, otherwise the company ID
        /// </summary>
        public int? CompanyId { get; set; }

        /// <summary>
        /// If this is a company-specific copy, which rate set it was cloned from
        /// </summary>
        public int? ClonedFromId { get; set; }

        public List<AncienneteRateDto> Rates { get; set; } = new();
    }

    /// <summary>
    /// DTO for creating Ancienneté Rate Set (Backoffice - Legal Default)
    /// </summary>
    public class CreateAncienneteRateSetDto
    {
        public required string Name { get; set; }
        public bool IsLegalDefault { get; set; } = true;
        public string? Source { get; set; }
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public List<CreateAncienneteRateDto> Rates { get; set; } = new();
    }

    /// <summary>
    /// DTO for updating Ancienneté Rate Set (creates a new version)
    /// </summary>
    public class UpdateAncienneteRateSetDto
    {
        public required string Name { get; set; }
        public bool IsLegalDefault { get; set; }
        public string? Source { get; set; }
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public List<CreateAncienneteRateDto> Rates { get; set; } = new();
    }

    /// <summary>
    /// DTO for updating Ancienneté Rates with auto-versioning (simplified workflow)
    /// Dates are handled automatically by the payzen_backend
    /// </summary>
    public class UpdateAncienneteRatesDto
    {
        public string? Name { get; set; }
        public List<CreateAncienneteRateDto> Rates { get; set; } = new();
    }

    /// <summary>
    /// DTO for Frontoffice: Company wants to customize rates (Copy-on-Write)
    /// </summary>
    public class CustomizeCompanyRatesDto
    {
        public int CompanyId { get; set; }
        public List<CreateAncienneteRateDto> Rates { get; set; } = new();
    }

    /// <summary>
    /// Result of rate validation against legal minimum
    /// </summary>
    public class RateValidationResultDto
    {
        public bool IsValid { get; set; }
        public List<string> Violations { get; set; } = new();
    }
}
