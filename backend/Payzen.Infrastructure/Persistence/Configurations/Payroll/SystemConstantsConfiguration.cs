using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payzen.Domain.Entities.Payroll;

namespace Payzen.Infrastructure.Persistence.Configurations.Payroll;

public class SystemConstantConfiguration : IEntityTypeConfiguration<SystemConstant>
{
    public void Configure(EntityTypeBuilder<SystemConstant> entity)
    {
        entity.ToTable("SystemConstants");
        entity.Property(c => c.Key).IsRequired().HasMaxLength(100);
        entity.Property(c => c.Description).HasMaxLength(500);
        entity.Property(c => c.Value).HasColumnType("decimal(18,4)");
        entity.HasIndex(c => new { c.Key, c.EffectiveDate });

        var effectiveDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        entity.HasData(
            new SystemConstant { Id = 1, Key = "WorkDaysRef", Description = "Jours de travail référentiels", Value = 26.00m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 2, Key = "WorkHoursRef", Description = "Heures de travail référentielles", Value = 191.00m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 3, Key = "SmigHoraire", Description = "SMIG Horaire (MAD)", Value = 17.10m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 4, Key = "PlafondCnssMensuel", Description = "Plafond CNSS Mensuel", Value = 6000.00m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 5, Key = "CnssRgSalarial", Description = "Taux CNSS RG Salarial", Value = 0.0448m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 6, Key = "CnssRgPatronal", Description = "Taux CNSS RG Patronal", Value = 0.0898m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 7, Key = "CnssAmoSalarial", Description = "Taux CNSS AMO Salarial", Value = 0.0226m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 8, Key = "CnssAmoPatronal", Description = "Taux CNSS AMO Patronal", Value = 0.0226m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 9, Key = "CnssAmoParticipPatronal", Description = "Taux CNSS AMO Participation Patronale", Value = 0.0185m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 10, Key = "CnssAllocFamPatronal", Description = "Taux CNSS Allocations Familiales Patronal", Value = 0.0640m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 11, Key = "CnssFpPatronal", Description = "Taux CNSS FP Patronal", Value = 0.0160m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 12, Key = "PlafondNiTransport", Description = "Plafond NI Transport", Value = 500.00m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 13, Key = "PlafondNiTransportHu", Description = "Plafond NI Transport Hors Urbain", Value = 750.00m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 14, Key = "PlafondNiTournee", Description = "Plafond NI Tournée", Value = 1500.00m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 15, Key = "PlafondNiRepresentation", Description = "Plafond NI Représentation (Taux)", Value = 0.10m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 16, Key = "PlafondNiPanierJour", Description = "Plafond NI Panier/Jour", Value = 34.20m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 17, Key = "PlafondNiCaisse", Description = "Plafond NI Caisse", Value = 239.00m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 18, Key = "PlafondNiCaisseDgi", Description = "Plafond NI Caisse DGI", Value = 190.00m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 19, Key = "PlafondNiLait", Description = "Plafond NI Lait", Value = 196.00m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 20, Key = "PlafondNiLaitDgi", Description = "Plafond NI Lait DGI", Value = 150.00m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 21, Key = "PlafondNiOutillage", Description = "Plafond NI Outillage", Value = 119.00m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 22, Key = "PlafondNiOutillageDgi", Description = "Plafond NI Outillage DGI", Value = 100.00m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 23, Key = "PlafondNiSalissure", Description = "Plafond NI Salissure", Value = 239.00m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 24, Key = "PlafondNiSalissureDgi", Description = "Plafond NI Salissure DGI", Value = 210.00m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 25, Key = "PlafondNiGratifAnnuel", Description = "Plafond NI Gratif Annuelle", Value = 5000.00m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 26, Key = "PlafondNiGratifDgi", Description = "Plafond NI Gratif DGI", Value = 2500.00m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 27, Key = "IrDeductionFamille", Description = "Déduction Famille par enfant (IR)", Value = 30.00m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 28, Key = "FpSeuil35", Description = "Frais Pro: Seuil 35%", Value = 6500.00m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 29, Key = "FpTaux35", Description = "Frais Pro: Taux <= Seuil", Value = 0.35m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 30, Key = "FpPlafond35", Description = "Frais Pro: Plafond <= Seuil", Value = 2916.67m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 31, Key = "FpTaux25", Description = "Frais Pro: Taux > Seuil", Value = 0.25m, EffectiveDate = effectiveDate },
            new SystemConstant { Id = 32, Key = "FpPlafond25", Description = "Frais Pro: Plafond > Seuil", Value = 2916.67m, EffectiveDate = effectiveDate }
        );
    }
}

public class IrTaxBracketConfiguration : IEntityTypeConfiguration<IrTaxBracket>
{
    public void Configure(EntityTypeBuilder<IrTaxBracket> entity)
    {
        entity.ToTable("IrTaxBrackets");
        entity.Property(b => b.MinIncome).HasColumnType("decimal(18,2)");
        entity.Property(b => b.MaxIncome).HasColumnType("decimal(18,2)");
        entity.Property(b => b.Rate).HasColumnType("decimal(5,4)");
        entity.Property(b => b.Deduction).HasColumnType("decimal(18,2)");

        var effectiveDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        entity.HasData(
            new IrTaxBracket { Id = 1, MinIncome = 0, MaxIncome = 3333.33m, Rate = 0.00m, Deduction = 0.00m, EffectiveDate = effectiveDate },
            new IrTaxBracket { Id = 2, MinIncome = 3333.34m, MaxIncome = 5000.00m, Rate = 0.10m, Deduction = 333.33m, EffectiveDate = effectiveDate },
            new IrTaxBracket { Id = 3, MinIncome = 5000.01m, MaxIncome = 6666.67m, Rate = 0.20m, Deduction = 833.33m, EffectiveDate = effectiveDate },
            new IrTaxBracket { Id = 4, MinIncome = 6666.68m, MaxIncome = 8333.33m, Rate = 0.30m, Deduction = 1500.00m, EffectiveDate = effectiveDate },
            new IrTaxBracket { Id = 5, MinIncome = 8333.34m, MaxIncome = 15000.00m, Rate = 0.34m, Deduction = 1833.33m, EffectiveDate = effectiveDate },
            // MaxIncome cannot be decimal.MaxValue in HasData easily depending on SQL mapping provider, so let's use 999999999m
            new IrTaxBracket { Id = 6, MinIncome = 15000.01m, MaxIncome = 999999999.99m, Rate = 0.37m, Deduction = 2283.33m, EffectiveDate = effectiveDate }
        );
    }
}
