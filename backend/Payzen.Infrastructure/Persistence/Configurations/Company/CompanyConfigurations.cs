using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payzen.Domain.Entities.Company;

namespace Payzen.Infrastructure.Persistence.Configurations.Company;

public class CompanyConfiguration : IEntityTypeConfiguration<Payzen.Domain.Entities.Company.Company>
{
    public void Configure(EntityTypeBuilder<Payzen.Domain.Entities.Company.Company> entity)
    {
        entity.ToTable("Companies");
        entity.Property(c => c.CompanyName).IsRequired().HasMaxLength(500);
        entity.Property(c => c.Email).IsRequired().HasMaxLength(500);
        entity.Property(c => c.PhoneNumber).IsRequired().HasMaxLength(20);
        entity.Property(c => c.CountryPhoneCode).HasMaxLength(10);
        entity.Property(c => c.CompanyAddress).IsRequired().HasMaxLength(1000);
        entity.Property(c => c.CnssNumber).IsRequired().HasMaxLength(100);
        entity.Property(c => c.IceNumber).HasMaxLength(100);
        entity.Property(c => c.IfNumber).HasMaxLength(100);
        entity.Property(c => c.RcNumber).HasMaxLength(100);
        entity.Property(c => c.RibNumber).HasMaxLength(100);
        entity.Property(c => c.LegalForm).HasMaxLength(50);
        entity.Property(c => c.SignatoryName).HasMaxLength(200);
        entity.Property(c => c.SignatoryTitle).HasMaxLength(100);
        entity.Property(c => c.Currency).HasMaxLength(10).HasDefaultValue("MAD");
        entity.Property(c => c.PayrollPeriodicity).HasMaxLength(50).HasDefaultValue("Mensuelle");
        entity.Property(c => c.BusinessSector).HasMaxLength(200);
        entity.Property(c => c.PaymentMethod).HasMaxLength(100);
        entity.Property(c => c.AuthType).HasMaxLength(20).HasDefaultValue("JWT");

        entity.HasOne(c => c.City).WithMany(ct => ct.Companies).HasForeignKey(c => c.CityId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(c => c.Country).WithMany(co => co.Companies).HasForeignKey(c => c.CountryId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(c => c.ManagedByCompany).WithMany(c => c.ManagedCompanies).HasForeignKey(c => c.ManagedByCompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DepartementConfiguration : IEntityTypeConfiguration<Departement>
{
    public void Configure(EntityTypeBuilder<Departement> entity)
    {
        entity.ToTable("Departements");
        entity.Property(d => d.DepartementName).IsRequired().HasMaxLength(200);
        entity.HasOne(d => d.Company).WithMany().HasForeignKey(d => d.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ContractTypeConfiguration : IEntityTypeConfiguration<ContractType>
{
    public void Configure(EntityTypeBuilder<ContractType> entity)
    {
        entity.ToTable("ContractTypes");
        entity.Property(ct => ct.ContractTypeName).IsRequired().HasMaxLength(200);
        entity.HasOne(ct => ct.Company).WithMany().HasForeignKey(ct => ct.CompanyId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(ct => ct.LegalContractType).WithMany().HasForeignKey(ct => ct.LegalContractTypeId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(ct => ct.StateEmploymentProgram).WithMany().HasForeignKey(ct => ct.StateEmploymentProgramId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class JobPositionConfiguration : IEntityTypeConfiguration<JobPosition>
{
    public void Configure(EntityTypeBuilder<JobPosition> entity)
    {
        entity.ToTable("JobPositions");
        entity.Property(jp => jp.Name).IsRequired().HasMaxLength(200);
        entity.HasOne(jp => jp.Company).WithMany().HasForeignKey(jp => jp.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> entity)
    {
        entity.ToTable("Holidays");
        entity.Property(h => h.NameFr).IsRequired().HasMaxLength(200);
        entity.Property(h => h.NameAr).IsRequired().HasMaxLength(200);
        entity.Property(h => h.NameEn).IsRequired().HasMaxLength(200);
        entity.Property(h => h.HolidayType).HasMaxLength(50);
        entity.Property(h => h.RecurrenceRule).HasMaxLength(200);
        entity.HasOne(h => h.Company).WithMany().HasForeignKey(h => h.CompanyId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(h => h.Country).WithMany(c => c.Holidays).HasForeignKey(h => h.CountryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkingCalendarConfiguration : IEntityTypeConfiguration<WorkingCalendar>
{
    public void Configure(EntityTypeBuilder<WorkingCalendar> entity)
    {
        entity.ToTable("WorkingCalendars");
        entity.HasOne(wc => wc.Company).WithMany().HasForeignKey(wc => wc.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CompanyDocumentConfiguration : IEntityTypeConfiguration<CompanyDocument>
{
    public void Configure(EntityTypeBuilder<CompanyDocument> entity)
    {
        entity.ToTable("CompanyDocuments");
        entity.Property(cd => cd.Name).IsRequired().HasMaxLength(500);
        entity.Property(cd => cd.FilePath).IsRequired().HasMaxLength(1000);
        entity.Property(cd => cd.DocumentType).HasMaxLength(100);
        entity.HasOne(cd => cd.Company).WithMany(c => c.Documents).HasForeignKey(cd => cd.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}
