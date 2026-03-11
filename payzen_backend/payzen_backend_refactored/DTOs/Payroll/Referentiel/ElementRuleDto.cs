using payzen_backend.Models.Payroll.Referentiel;

namespace payzen_backend.DTOs.Payroll.Referentiel
{
    /// <summary>
    /// DTO for Element Rule (exemption rule for CNSS, IR, etc.)
    /// </summary>
    public class ElementRuleDto
    {
        public int Id { get; set; }
        public int ElementId { get; set; }
        public int AuthorityId { get; set; }
        public string AuthorityName { get; set; } = string.Empty;
        public ExemptionType ExemptionType { get; set; }
        public string? SourceRef { get; set; }
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public bool IsActive { get; set; }
        public ElementStatus Status { get; set; }
        public string RuleDetails { get; set; } = "{}";

        // Legacy: Rule details (only one will be populated based on ExemptionType)
        // Kept for backward compatibility during migration
        public RuleCapDto? Cap { get; set; }
        public RulePercentageDto? Percentage { get; set; }
        public RuleFormulaDto? Formula { get; set; }
        public RuleDualCapDto? DualCap { get; set; }
        public List<RuleTierDto> Tiers { get; set; } = new();
        public List<RuleVariantDto> Variants { get; set; } = new();
    }

    /// <summary>
    /// DTO for creating Element Rule
    /// </summary>
    public class CreateElementRuleDto
    {
        public int ElementId { get; set; }
        public int AuthorityId { get; set; }
        public string? AuthorityCode { get; set; }
        public ExemptionType ExemptionType { get; set; }
        public string? SourceRef { get; set; }
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public ElementStatus Status { get; set; } = ElementStatus.DRAFT;
        public string RuleDetails { get; set; } = "{}";

        // Legacy: Rule details (provide only one based on ExemptionType)
        // Kept for backward compatibility during migration
        public CreateRuleCapDto? Cap { get; set; }
        public CreateRulePercentageDto? Percentage { get; set; }
        public CreateRuleFormulaDto? Formula { get; set; }
        public CreateRuleDualCapDto? DualCap { get; set; }
        public List<CreateRuleTierDto> Tiers { get; set; } = new();
        public List<CreateRuleVariantDto> Variants { get; set; } = new();
    }

    /// <summary>
    /// DTO for updating Element Rule
    /// </summary>
    public class UpdateElementRuleDto
    {
        public ExemptionType? ExemptionType { get; set; }
        public string? SourceRef { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public ElementStatus? Status { get; set; }
        public string? RuleDetails { get; set; }

        // Legacy: Rule details (provide only one based on ExemptionType)
        // Kept for backward compatibility during migration
        public CreateRuleCapDto? Cap { get; set; }
        public CreateRulePercentageDto? Percentage { get; set; }
        public CreateRuleFormulaDto? Formula { get; set; }
        public CreateRuleDualCapDto? DualCap { get; set; }
        public List<CreateRuleTierDto>? Tiers { get; set; }
        public List<CreateRuleVariantDto>? Variants { get; set; }
    }

    /// <summary>
    /// DTO for validating rule details
    /// </summary>
    public class ValidateRuleDto
    {
        public int? ElementId { get; set; }
        public int? AuthorityId { get; set; }
        public ExemptionType ExemptionType { get; set; }
        public string RuleDetails { get; set; } = "{}";
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
    }
}
