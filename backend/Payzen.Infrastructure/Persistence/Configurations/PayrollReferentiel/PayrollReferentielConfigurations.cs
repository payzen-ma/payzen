using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payzen.Domain.Entities.Payroll.Referentiel;

namespace Payzen.Infrastructure.Persistence.Configurations.PayrollReferentiel;

public class AuthorityConfiguration : IEntityTypeConfiguration<Authority>
{
    public void Configure(EntityTypeBuilder<Authority> entity)
    {
        entity.ToTable("Authorities");
        entity.Property(a => a.Code).IsRequired().HasMaxLength(50);
        entity.Property(a => a.Name).IsRequired().HasMaxLength(200);
        entity.Property(a => a.Description).HasMaxLength(500);
        entity.HasIndex(a => a.Code).IsUnique().HasFilter("[DeletedAt] IS NULL");
    }
}

public class ElementCategoryConfiguration : IEntityTypeConfiguration<ElementCategory>
{
    public void Configure(EntityTypeBuilder<ElementCategory> entity)
    {
        entity.ToTable("ElementCategories");
        entity.Property(ec => ec.Name).IsRequired().HasMaxLength(200);
        entity.Property(ec => ec.Description).HasMaxLength(500);
    }
}

public class EligibilityCriteriaConfiguration : IEntityTypeConfiguration<EligibilityCriteria>
{
    public void Configure(EntityTypeBuilder<EligibilityCriteria> entity)
    {
        entity.ToTable("EligibilityCriteria");
        entity.Property(ec => ec.Code).IsRequired().HasMaxLength(50);
        entity.Property(ec => ec.Name).IsRequired().HasMaxLength(200);
        entity.Property(ec => ec.Description).HasMaxLength(500);
        entity.HasIndex(ec => ec.Code).IsUnique();
    }
}

public class LegalParameterConfiguration : IEntityTypeConfiguration<LegalParameter>
{
    public void Configure(EntityTypeBuilder<LegalParameter> entity)
    {
        entity.ToTable("LegalParameters");
        entity.Property(lp => lp.Code).IsRequired().HasMaxLength(100);
        entity.Property(lp => lp.Label).IsRequired().HasMaxLength(200);
        entity.Property(lp => lp.Value).HasColumnType("decimal(18,6)");
        entity.Property(lp => lp.Unit).IsRequired().HasMaxLength(50);
        entity.Property(lp => lp.Source).HasMaxLength(200);
        entity.HasIndex(lp => new { lp.Code, lp.EffectiveFrom }).IsUnique();
    }
}

public class ReferentielElementConfiguration : IEntityTypeConfiguration<ReferentielElement>
{
    public void Configure(EntityTypeBuilder<ReferentielElement> entity)
    {
        entity.ToTable("ReferentielElements");
        entity.Property(re => re.Code).HasMaxLength(100);
        entity.Property(re => re.Name).IsRequired().HasMaxLength(200);
        entity.Property(re => re.Description).HasMaxLength(500);
        entity
            .HasOne(re => re.Category)
            .WithMany(ec => ec.Elements)
            .HasForeignKey(re => re.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ElementRuleConfiguration : IEntityTypeConfiguration<ElementRule>
{
    public void Configure(EntityTypeBuilder<ElementRule> entity)
    {
        entity.ToTable("ElementRules");
        entity.Property(er => er.RuleDetails).HasMaxLength(2000).HasDefaultValue("{}");
        entity.Property(er => er.SourceRef).HasMaxLength(200);
        entity
            .HasOne(er => er.Element)
            .WithMany(re => re.Rules)
            .HasForeignKey(er => er.ElementId)
            .OnDelete(DeleteBehavior.Restrict);
        entity
            .HasOne(er => er.Authority)
            .WithMany(a => a.ElementRules)
            .HasForeignKey(er => er.AuthorityId)
            .OnDelete(DeleteBehavior.Restrict);
        // 1-to-1 navigation children
        entity
            .HasOne(er => er.Cap)
            .WithOne(c => c.Rule)
            .HasForeignKey<RuleCap>(c => c.RuleId)
            .OnDelete(DeleteBehavior.Cascade);
        entity
            .HasOne(er => er.Percentage)
            .WithOne(p => p.Rule)
            .HasForeignKey<RulePercentage>(p => p.RuleId)
            .OnDelete(DeleteBehavior.Cascade);
        entity
            .HasOne(er => er.Formula)
            .WithOne(f => f.Rule)
            .HasForeignKey<RuleFormula>(f => f.RuleId)
            .OnDelete(DeleteBehavior.Cascade);
        entity
            .HasOne(er => er.DualCap)
            .WithOne(dc => dc.Rule)
            .HasForeignKey<RuleDualCap>(dc => dc.RuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RuleCapConfiguration : IEntityTypeConfiguration<RuleCap>
{
    public void Configure(EntityTypeBuilder<RuleCap> entity)
    {
        entity.ToTable("RuleCaps");
        entity.Property(rc => rc.CapAmount).HasColumnType("decimal(18,2)");
        entity.Property(rc => rc.MinAmount).HasColumnType("decimal(18,2)");
    }
}

public class RulePercentageConfiguration : IEntityTypeConfiguration<RulePercentage>
{
    public void Configure(EntityTypeBuilder<RulePercentage> entity)
    {
        entity.ToTable("RulePercentages");
        entity.Property(rp => rp.Percentage).HasColumnType("decimal(5,2)");
        entity
            .HasOne(rp => rp.Eligibility)
            .WithMany(ec => ec.RulePercentages)
            .HasForeignKey(rp => rp.EligibilityId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class RuleFormulaConfiguration : IEntityTypeConfiguration<RuleFormula>
{
    public void Configure(EntityTypeBuilder<RuleFormula> entity)
    {
        entity.ToTable("RuleFormulas");
        entity.Property(rf => rf.Multiplier).HasColumnType("decimal(10,4)");
        entity
            .HasOne(rf => rf.Parameter)
            .WithMany(lp => lp.RuleFormulas)
            .HasForeignKey(rf => rf.ParameterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RuleDualCapConfiguration : IEntityTypeConfiguration<RuleDualCap>
{
    public void Configure(EntityTypeBuilder<RuleDualCap> entity)
    {
        entity.ToTable("RuleDualCaps");
        entity.Property(dc => dc.FixedCapAmount).HasColumnType("decimal(18,2)");
        entity.Property(dc => dc.PercentageCap).HasColumnType("decimal(5,2)");
    }
}

public class RuleTierConfiguration : IEntityTypeConfiguration<RuleTier>
{
    public void Configure(EntityTypeBuilder<RuleTier> entity)
    {
        entity.ToTable("RuleTiers");
        entity.Property(rt => rt.FromAmount).HasColumnType("decimal(18,2)");
        entity.Property(rt => rt.ToAmount).HasColumnType("decimal(18,2)");
        entity.Property(rt => rt.ExemptPercent).HasColumnType("decimal(5,2)");
        entity
            .HasOne(rt => rt.Rule)
            .WithMany(er => er.Tiers)
            .HasForeignKey(rt => rt.RuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RuleVariantConfiguration : IEntityTypeConfiguration<RuleVariant>
{
    public void Configure(EntityTypeBuilder<RuleVariant> entity)
    {
        entity.ToTable("RuleVariants");
        entity.Property(rv => rv.VariantType).IsRequired().HasMaxLength(50);
        entity.Property(rv => rv.VariantKey).IsRequired().HasMaxLength(50);
        entity.Property(rv => rv.VariantLabel).IsRequired().HasMaxLength(200);
        entity.Property(rv => rv.OverrideCap).HasColumnType("decimal(18,2)");
        entity.Property(rv => rv.OverridePercentage).HasColumnType("decimal(5,2)");
        entity
            .HasOne(rv => rv.Rule)
            .WithMany(er => er.Variants)
            .HasForeignKey(rv => rv.RuleId)
            .OnDelete(DeleteBehavior.Restrict);
        entity
            .HasOne(rv => rv.Eligibility)
            .WithMany(ec => ec.RuleVariants)
            .HasForeignKey(rv => rv.EligibilityId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class AncienneteRateSetConfiguration : IEntityTypeConfiguration<AncienneteRateSet>
{
    public void Configure(EntityTypeBuilder<AncienneteRateSet> entity)
    {
        entity.ToTable("AncienneteRateSets");
        entity.Property(ars => ars.Code).IsRequired().HasMaxLength(100);
        entity.Property(ars => ars.Name).IsRequired().HasMaxLength(255);
        entity.Property(ars => ars.Source).HasMaxLength(500);
        entity.HasIndex(ars => new { ars.CompanyId, ars.EffectiveFrom }).IsUnique().HasFilter("[DeletedAt] IS NULL");
        entity
            .HasOne(ars => ars.Company)
            .WithMany()
            .HasForeignKey(ars => ars.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
        entity
            .HasOne(ars => ars.ClonedFrom)
            .WithMany(ars => ars.Clones)
            .HasForeignKey(ars => ars.ClonedFromId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AncienneteRateConfiguration : IEntityTypeConfiguration<AncienneteRate>
{
    public void Configure(EntityTypeBuilder<AncienneteRate> entity)
    {
        entity.ToTable("AncienneteRates");
        entity.Property(ar => ar.Rate).HasColumnType("decimal(5,4)");
        entity.HasIndex(ar => new { ar.RateSetId, ar.SortOrder }).IsUnique();
        entity
            .HasOne(ar => ar.RateSet)
            .WithMany(rs => rs.Rates)
            .HasForeignKey(ar => ar.RateSetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BusinessSectorConfiguration : IEntityTypeConfiguration<BusinessSector>
{
    public void Configure(EntityTypeBuilder<BusinessSector> entity)
    {
        entity.ToTable("BusinessSectors");
        entity.Property(bs => bs.Code).IsRequired().HasMaxLength(50);
        entity.Property(bs => bs.Name).IsRequired().HasMaxLength(200);
        entity.HasIndex(bs => bs.Code).IsUnique().HasFilter("[DeletedAt] IS NULL");
    }
}
