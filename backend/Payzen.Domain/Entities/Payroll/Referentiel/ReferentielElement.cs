using Payzen.Domain.Common;
using Payzen.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Payzen.Domain.Entities.Payroll.Referentiel;

/// <summary>
/// A compensation element in the référentiel library
/// (Transport, Panier, Représentation, etc.)
/// </summary>
public class ReferentielElement : BaseEntity
{

    [MaxLength(100)]
    public string? Code
    {
        get; set;
    }

    public required string Name
    {
        get; set;
    }
    public int CategoryId
    {
        get; set;
    }
    public string? Description
    {
        get; set;
    }
    public PaymentFrequency DefaultFrequency
    {
        get; set;
    }
    public ElementStatus Status { get; set; } = ElementStatus.DRAFT;
    public bool HasConvergence
    {
        get; set;
    }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ElementCategory Category { get; set; } = null!;
    public virtual ICollection<ElementRule> Rules { get; set; } = new List<ElementRule>();

    /// <summary>Get the rule for a specific authority (CNSS, IR, etc.) at a given date.</summary>
    public ElementRule? GetRuleForAuthority(string authorityCode, DateOnly? asOfDate = null)
    {
        var checkDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Rules.FirstOrDefault(r =>
            r.Authority.Code == authorityCode &&
            r.EffectiveFrom <= checkDate &&
            (r.EffectiveTo == null || r.EffectiveTo >= checkDate));
    }

    /// <summary>Check if CNSS and IR rules are in convergence (same treatment).</summary>
    public bool IsConvergence(DateOnly? asOfDate = null)
    {
        var cnssRule = GetRuleForAuthority("CNSS", asOfDate);
        var irRule = GetRuleForAuthority("IR", asOfDate);
        if (cnssRule == null || irRule == null)
            return false;
        return cnssRule.ExemptionType == irRule.ExemptionType;
    }
}
