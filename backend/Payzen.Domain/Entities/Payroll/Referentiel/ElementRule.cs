using Payzen.Domain.Common;
using Payzen.Domain.Enums;

namespace Payzen.Domain.Entities.Payroll.Referentiel;

/// <summary>The exemption rule for an element under a specific authority</summary>
public class ElementRule : BaseEntity
{
    public int ElementId { get; set; }
    public int AuthorityId { get; set; }
    public ExemptionType ExemptionType { get; set; }
    public string RuleDetails { get; set; } = "{}";
    public string? SourceRef { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public ElementStatus Status { get; set; } = ElementStatus.DRAFT;

    // Navigation
    public virtual ReferentielElement Element { get; set; } = null!;
    public virtual Authority Authority { get; set; } = null!;

    // Rule details (0 or 1 of each, depending on ExemptionType)
    public virtual RuleCap? Cap { get; set; }
    public virtual RulePercentage? Percentage { get; set; }
    public virtual RuleFormula? Formula { get; set; }
    public virtual RuleDualCap? DualCap { get; set; }
    public virtual ICollection<RuleTier> Tiers { get; set; } = new List<RuleTier>();
    public virtual ICollection<RuleVariant> Variants { get; set; } = new List<RuleVariant>();

    public bool IsActive(DateOnly? asOfDate = null)
    {
        var checkDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return EffectiveFrom <= checkDate && (EffectiveTo == null || EffectiveTo >= checkDate);
    }

    /// <summary>
    /// Calculate the exemption cap for a given variant (if applicable).
    /// When variants are defined they ARE the caps — no base cap needed.
    /// </summary>
    public decimal? GetCapForVariant(string? variantKey = null)
    {
        if (Variants.Any(v => v.OverrideCap != null))
        {
            if (variantKey != null)
            {
                var variant = Variants.FirstOrDefault(v => v.VariantKey == variantKey);
                return variant?.OverrideCap;
            }
            return null;
        }
        return Cap?.CapAmount;
    }
}
