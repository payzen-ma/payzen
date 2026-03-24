using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payzen.Domain.Entities.Payroll;

namespace Payzen.Infrastructure.Persistence.Configurations.Payroll;

public class PayrollResultConfiguration : IEntityTypeConfiguration<PayrollResult>
{
    public void Configure(EntityTypeBuilder<PayrollResult> entity)
    {
        entity.ToTable("PayrollResults");
        // Tous les montants en decimal(18,2)
        var decimalProps = new[]
        {
            nameof(PayrollResult.SalaireBase), nameof(PayrollResult.HeuresSupp25),
            nameof(PayrollResult.HeuresSupp50), nameof(PayrollResult.HeuresSupp100),
            nameof(PayrollResult.Conges), nameof(PayrollResult.JoursFeries),
            nameof(PayrollResult.PrimeAnciennete), nameof(PayrollResult.PrimeAnciennteRate),
            nameof(PayrollResult.PrimeImposable1), nameof(PayrollResult.PrimeImposable2),
            nameof(PayrollResult.PrimeImposable3), nameof(PayrollResult.TotalPrimesImposables),
            nameof(PayrollResult.TotalBrut), nameof(PayrollResult.FraisProfessionnels),
            nameof(PayrollResult.IndemniteRepresentation), nameof(PayrollResult.PrimeTransport),
            nameof(PayrollResult.PrimePanier), nameof(PayrollResult.IndemniteDeplacement),
            nameof(PayrollResult.IndemniteCaisse), nameof(PayrollResult.PrimeSalissure),
            nameof(PayrollResult.GratificationsFamilial), nameof(PayrollResult.PrimeVoyageMecque),
            nameof(PayrollResult.IndemniteLicenciement), nameof(PayrollResult.IndemniteKilometrique),
            nameof(PayrollResult.PrimeTourne), nameof(PayrollResult.PrimeOutillage),
            nameof(PayrollResult.AideMedicale), nameof(PayrollResult.AutresPrimesNonImposable),
            nameof(PayrollResult.TotalIndemnites), nameof(PayrollResult.TotalNiExcedentImposable),
            nameof(PayrollResult.CnssPartSalariale), nameof(PayrollResult.CnssBase),
            nameof(PayrollResult.CimrPartSalariale), nameof(PayrollResult.CimrBase),
            nameof(PayrollResult.AmoPartSalariale), nameof(PayrollResult.AmoBase),
            nameof(PayrollResult.MutuellePartSalariale), nameof(PayrollResult.MutuelleBase),
            nameof(PayrollResult.TotalCotisationsSalariales),
            nameof(PayrollResult.CnssPartPatronale), nameof(PayrollResult.CimrPartPatronale),
            nameof(PayrollResult.AmoPartPatronale), nameof(PayrollResult.MutuellePartPatronale),
            nameof(PayrollResult.TotalCotisationsPatronales),
            nameof(PayrollResult.ImpotRevenu), nameof(PayrollResult.IrTaux),
            nameof(PayrollResult.Arrondi), nameof(PayrollResult.AvanceSurSalaire),
            nameof(PayrollResult.InteretSurLogement), nameof(PayrollResult.BrutImposable),
            nameof(PayrollResult.NetImposable), nameof(PayrollResult.TotalGains),
            nameof(PayrollResult.TotalRetenues), nameof(PayrollResult.NetAPayer),
            nameof(PayrollResult.TotalNet), nameof(PayrollResult.TotalNet2),
        };
        foreach (var prop in decimalProps)
            entity.Property(prop).HasColumnType("decimal(18,2)");

        entity.Property(pr => pr.ErrorMessage).HasMaxLength(2000);
        entity.Property(pr => pr.ClaudeModel).HasMaxLength(100);

        entity.HasOne(pr => pr.Employee).WithMany().HasForeignKey(pr => pr.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(pr => pr.Company).WithMany().HasForeignKey(pr => pr.CompanyId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(pr => new { pr.EmployeeId, pr.Year, pr.Month, pr.PayHalf }).HasFilter("[DeletedAt] IS NULL");
    }
}

public class PayrollResultPrimeConfiguration : IEntityTypeConfiguration<PayrollResultPrime>
{
    public void Configure(EntityTypeBuilder<PayrollResultPrime> entity)
    {
        entity.ToTable("PayrollResultPrimes");
        entity.Property(p => p.Label).IsRequired().HasMaxLength(200);
        entity.Property(p => p.Montant).HasColumnType("decimal(18,2)");
        entity.HasOne(p => p.PayrollResult).WithMany(pr => pr.Primes).HasForeignKey(p => p.PayrollResultId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayrollCalculationAuditStepConfiguration : IEntityTypeConfiguration<PayrollCalculationAuditStep>
{
    public void Configure(EntityTypeBuilder<PayrollCalculationAuditStep> entity)
    {
        entity.ToTable("PayrollCalculationAuditSteps");
        entity.Property(s => s.ModuleName).IsRequired().HasMaxLength(100);
        entity.Property(s => s.FormulaDescription).IsRequired().HasMaxLength(1000);
        entity.HasOne(s => s.PayrollResult).WithMany(pr => pr.CalculationAuditSteps).HasForeignKey(s => s.PayrollResultId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SalaryPackageConfiguration : IEntityTypeConfiguration<SalaryPackage>
{
    public void Configure(EntityTypeBuilder<SalaryPackage> entity)
    {
        entity.ToTable("SalaryPackages");
        entity.Property(sp => sp.Name).IsRequired().HasMaxLength(200);
        entity.Property(sp => sp.Category).IsRequired().HasMaxLength(100);
        entity.Property(sp => sp.Description).HasMaxLength(500);
        entity.Property(sp => sp.BaseSalary).HasColumnType("decimal(18,2)");
        entity.Property(sp => sp.Status).IsRequired().HasMaxLength(20);
        entity.Property(sp => sp.TemplateType).HasMaxLength(50);
        entity.Property(sp => sp.RegulationVersion).HasMaxLength(20);
        entity.Property(sp => sp.OriginType).HasMaxLength(50);
        entity.Property(sp => sp.SourceTemplateNameSnapshot).HasMaxLength(200);
        entity.Property(sp => sp.CimrRate).HasColumnType("decimal(5,4)");
        entity.HasOne(sp => sp.Company).WithMany().HasForeignKey(sp => sp.CompanyId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(sp => sp.BusinessSector).WithMany(bs => bs.SalaryPackages).HasForeignKey(sp => sp.BusinessSectorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SalaryPackageItemConfiguration : IEntityTypeConfiguration<SalaryPackageItem>
{
    public void Configure(EntityTypeBuilder<SalaryPackageItem> entity)
    {
        entity.ToTable("SalaryPackageItems");
        entity.Property(i => i.Label).IsRequired().HasMaxLength(200);
        entity.Property(i => i.DefaultValue).HasColumnType("decimal(18,2)");
        entity.Property(i => i.Type).IsRequired().HasMaxLength(50);
        entity.Property(i => i.ExemptionLimit).HasColumnType("decimal(18,2)");
        entity.HasOne(i => i.SalaryPackage).WithMany(sp => sp.Items).HasForeignKey(i => i.SalaryPackageId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(i => i.PayComponent).WithMany(pc => pc.PackageItems).HasForeignKey(i => i.PayComponentId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(i => i.ReferentielElement).WithMany().HasForeignKey(i => i.ReferentielElementId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class SalaryPackageAssignmentConfiguration : IEntityTypeConfiguration<SalaryPackageAssignment>
{
    public void Configure(EntityTypeBuilder<SalaryPackageAssignment> entity)
    {
        entity.ToTable("SalaryPackageAssignments");
        entity.HasOne(a => a.SalaryPackage).WithMany(sp => sp.Assignments).HasForeignKey(a => a.SalaryPackageId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.Employee).WithMany().HasForeignKey(a => a.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.Contract).WithMany().HasForeignKey(a => a.ContractId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.EmployeeSalary).WithMany().HasForeignKey(a => a.EmployeeSalaryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayComponentConfiguration : IEntityTypeConfiguration<PayComponent>
{
    public void Configure(EntityTypeBuilder<PayComponent> entity)
    {
        entity.ToTable("PayComponents");
        entity.Property(pc => pc.Code).IsRequired().HasMaxLength(100);
        entity.Property(pc => pc.NameFr).IsRequired().HasMaxLength(200);
        entity.Property(pc => pc.NameAr).HasMaxLength(200);
        entity.Property(pc => pc.NameEn).HasMaxLength(200);
        entity.Property(pc => pc.Type).IsRequired().HasMaxLength(50);
        entity.Property(pc => pc.ExemptionLimit).HasColumnType("decimal(18,2)");
        entity.HasIndex(pc => pc.Code).IsUnique().HasFilter("[DeletedAt] IS NULL");
    }
}
