using payzen_backend.Models.Payroll.Referentiel;

namespace payzen_backend.DTOs.Payroll.Referentiel
{
    /// <summary>
    /// DTO for Rule Cap (fixed or capped exemption)
    /// </summary>
    public class RuleCapDto
    {
        public int Id { get; set; }
        public decimal CapAmount { get; set; }
        public CapUnit CapUnit { get; set; }
    }

    /// <summary>
    /// DTO for creating/updating Rule Cap
    /// </summary>
    public class CreateRuleCapDto
    {
        public decimal CapAmount { get; set; }
        public CapUnit CapUnit { get; set; }
        public decimal? MinAmount { get; set; }
    }

    /// <summary>
    /// DTO for Rule Percentage (percentage-based exemption)
    /// </summary>
    public class RulePercentageDto
    {
        public int Id { get; set; }
        public decimal Percentage { get; set; }
        public BaseReference BaseReference { get; set; }
        public int? EligibilityId { get; set; }
        public string? EligibilityName { get; set; }
    }

    /// <summary>
    /// DTO for creating/updating Rule Percentage
    /// </summary>
    public class CreateRulePercentageDto
    {
        public decimal Percentage { get; set; }
        public BaseReference BaseReference { get; set; }
        public int? EligibilityId { get; set; }
    }

    /// <summary>
    /// DTO for Rule Formula (formula-based exemption like 2 × SMIG)
    /// </summary>
    public class RuleFormulaDto
    {
        public int Id { get; set; }
        public decimal Multiplier { get; set; }
        public int ParameterId { get; set; }
        public string ParameterName { get; set; } = string.Empty;
        public CapUnit ResultUnit { get; set; }
        public decimal CurrentCapValue { get; set; } // Calculated value
    }

    /// <summary>
    /// DTO for creating/updating Rule Formula
    /// </summary>
    public class CreateRuleFormulaDto
    {
        public decimal Multiplier { get; set; }
        public int ParameterId { get; set; }
        public CapUnit ResultUnit { get; set; }
    }

    /// <summary>
    /// DTO for Rule Dual Cap (fixed cap AND percentage cap, e.g., DGI ticket-restaurant)
    /// </summary>
    public class RuleDualCapDto
    {
        public int Id { get; set; }
        public decimal FixedCapAmount { get; set; }
        public CapUnit FixedCapUnit { get; set; }
        public decimal PercentageCap { get; set; }
        public BaseReference BaseReference { get; set; }
        public DualCapLogic Logic { get; set; }
    }

    /// <summary>
    /// DTO for creating/updating Rule Dual Cap
    /// </summary>
    public class CreateRuleDualCapDto
    {
        public decimal FixedCapAmount { get; set; }
        public CapUnit FixedCapUnit { get; set; }
        public decimal PercentageCap { get; set; }
        public BaseReference BaseReference { get; set; }
        public DualCapLogic Logic { get; set; } = DualCapLogic.MIN;
    }

    /// <summary>
    /// DTO for Rule Tier (tiered exemption rates)
    /// </summary>
    public class RuleTierDto
    {
        public int Id { get; set; }
        public int TierOrder { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public decimal ExemptionRate { get; set; }
    }

    /// <summary>
    /// DTO for creating/updating Rule Tier
    /// </summary>
    public class CreateRuleTierDto
    {
        public int TierOrder { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public decimal ExemptionRate { get; set; }
    }

    /// <summary>
    /// DTO for Rule Variant (zone/grade-specific caps)
    /// </summary>
    public class RuleVariantDto
    {
        public int Id { get; set; }
        public string VariantType { get; set; } = string.Empty;  // e.g., "ZONE", "GRADE"
        public string VariantKey { get; set; } = string.Empty;   // e.g., "URBAN", "HORS_URBAN"
        public string VariantLabel { get; set; } = string.Empty; // e.g., "Zone Urbaine"
        public decimal? OverrideCap { get; set; }
        public int? OverrideEligibilityId { get; set; }
        public string? OverrideEligibilityName { get; set; }
    }

    /// <summary>
    /// DTO for creating/updating Rule Variant.
    /// For zone-based rules (like transport allowance), just provide variants - no base cap needed.
    /// </summary>
    public class CreateRuleVariantDto
    {
        public string VariantType { get; set; } = "ZONE";  // Default to ZONE for simplicity
        public required string VariantKey { get; set; }
        public required string VariantLabel { get; set; }
        public decimal? OverrideCap { get; set; }
        public int? OverrideEligibilityId { get; set; }
    }
}
