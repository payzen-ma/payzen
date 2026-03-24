using payzen_backend.Models.Payroll.Referentiel;

namespace payzen_backend.DTOs.Payroll.Referentiel
{
    /// <summary>
    /// DTO for Referentiel Element (Transport, Panier, Représentation, etc.)
    /// </summary>
    public class ReferentielElementDto
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public PaymentFrequency DefaultFrequency { get; set; }
        public ElementStatus Status { get; set; }
        public bool IsActive { get; set; }
        public bool HasConvergence { get; set; } // CNSS & DGI rules aligned?
        public List<ElementRuleDto> Rules { get; set; } = new();
    }

    /// <summary>
    /// DTO for listing Referentiel Elements (summary view)
    /// </summary>
    public class ReferentielElementListDto
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public PaymentFrequency DefaultFrequency { get; set; }
        public ElementStatus Status { get; set; }
        public bool IsActive { get; set; }
        public bool HasConvergence { get; set; }
        public int RuleCount { get; set; }
        public bool HasCnssRule { get; set; }
        public bool HasDgiRule { get; set; }
    }

    /// <summary>
    /// DTO for creating Referentiel Element
    /// </summary>
    public class CreateReferentielElementDto
    {
        public string? Code { get; set; }
        public required string Name { get; set; }
        public int CategoryId { get; set; }
        public string? Description { get; set; }
        public PaymentFrequency DefaultFrequency { get; set; }
        public ElementStatus Status { get; set; } = ElementStatus.DRAFT;
    }

    /// <summary>
    /// DTO for updating Referentiel Element
    /// </summary>
    public class UpdateReferentielElementDto
    {
        public string? Code { get; set; }
        public required string Name { get; set; }
        public int CategoryId { get; set; }
        public string? Description { get; set; }
        public PaymentFrequency DefaultFrequency { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO for changing element status
    /// </summary>
    public class UpdateElementStatusDto
    {
        public ElementStatus Status { get; set; }
    }
}
