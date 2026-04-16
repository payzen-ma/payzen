using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payzen.Domain.Entities.Events;
using Payzen.Domain.Entities.Referentiel;

namespace Payzen.Infrastructure.Persistence.Configurations.Referentiel;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> entity)
    {
        entity.ToTable("Countries");
        entity.Property(c => c.CountryName).IsRequired().HasMaxLength(100);
        entity.Property(c => c.CountryNameAr).HasMaxLength(100);
        entity.Property(c => c.CountryCode).IsRequired().HasMaxLength(5);
        entity.Property(c => c.CountryPhoneCode).IsRequired().HasMaxLength(10);
        entity.HasIndex(c => c.CountryCode).IsUnique();
    }
}

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> entity)
    {
        entity.ToTable("Cities");
        entity.Property(c => c.CityName).IsRequired().HasMaxLength(100);
        entity
            .HasOne(c => c.Country)
            .WithMany(co => co.Cities)
            .HasForeignKey(c => c.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class GenderConfiguration : IEntityTypeConfiguration<Gender>
{
    public void Configure(EntityTypeBuilder<Gender> entity)
    {
        entity.ToTable("Genders");
        entity.Property(g => g.Code).IsRequired().HasMaxLength(10);
        entity.Property(g => g.NameFr).IsRequired().HasMaxLength(50);
        entity.Property(g => g.NameAr).IsRequired().HasMaxLength(50);
        entity.Property(g => g.NameEn).IsRequired().HasMaxLength(50);
        entity.HasIndex(g => g.Code).IsUnique();
    }
}

public class StatusConfiguration : IEntityTypeConfiguration<Status>
{
    public void Configure(EntityTypeBuilder<Status> entity)
    {
        entity.ToTable("Statuses");
        entity.Property(s => s.Code).IsRequired().HasMaxLength(20);
        entity.Property(s => s.NameFr).IsRequired().HasMaxLength(100);
        entity.Property(s => s.NameAr).IsRequired().HasMaxLength(100);
        entity.Property(s => s.NameEn).IsRequired().HasMaxLength(100);
        entity.HasIndex(s => s.Code).IsUnique();
    }
}

public class EducationLevelConfiguration : IEntityTypeConfiguration<EducationLevel>
{
    public void Configure(EntityTypeBuilder<EducationLevel> entity)
    {
        entity.ToTable("EducationLevels");
        entity.Property(el => el.Code).IsRequired().HasMaxLength(20);
        entity.Property(el => el.NameFr).IsRequired().HasMaxLength(100);
        entity.Property(el => el.NameAr).IsRequired().HasMaxLength(100);
        entity.Property(el => el.NameEn).IsRequired().HasMaxLength(100);
        entity.HasIndex(el => el.Code).IsUnique();
    }
}

public class MaritalStatusConfiguration : IEntityTypeConfiguration<MaritalStatus>
{
    public void Configure(EntityTypeBuilder<MaritalStatus> entity)
    {
        entity.ToTable("MaritalStatuses");
        entity.Property(ms => ms.Code).IsRequired().HasMaxLength(20);
        entity.Property(ms => ms.NameFr).IsRequired().HasMaxLength(100);
        entity.Property(ms => ms.NameAr).IsRequired().HasMaxLength(100);
        entity.Property(ms => ms.NameEn).IsRequired().HasMaxLength(100);
        entity.HasIndex(ms => ms.Code).IsUnique();
    }
}

public class NationalityConfiguration : IEntityTypeConfiguration<Nationality>
{
    public void Configure(EntityTypeBuilder<Nationality> entity)
    {
        entity.ToTable("Nationalities");
        entity.Property(n => n.Name).IsRequired().HasMaxLength(100);
    }
}

public class LegalContractTypeConfiguration : IEntityTypeConfiguration<LegalContractType>
{
    public void Configure(EntityTypeBuilder<LegalContractType> entity)
    {
        entity.ToTable("LegalContractTypes");
        entity.Property(lct => lct.Code).IsRequired().HasMaxLength(20);
        entity.Property(lct => lct.Name).IsRequired().HasMaxLength(100);
        entity.HasIndex(lct => lct.Code).IsUnique();
    }
}

public class StateEmploymentProgramConfiguration : IEntityTypeConfiguration<StateEmploymentProgram>
{
    public void Configure(EntityTypeBuilder<StateEmploymentProgram> entity)
    {
        entity.ToTable("StateEmploymentPrograms");
        entity.Property(sep => sep.Code).IsRequired().HasMaxLength(20);
        entity.Property(sep => sep.Name).IsRequired().HasMaxLength(100);
        entity.Property(sep => sep.SalaryCeiling).HasColumnType("decimal(18,2)");
        entity.HasIndex(sep => sep.Code).IsUnique();
    }
}

public class OvertimeRateRuleConfiguration : IEntityTypeConfiguration<OvertimeRateRule>
{
    public void Configure(EntityTypeBuilder<OvertimeRateRule> entity)
    {
        entity.ToTable("OvertimeRateRules");
        entity.Property(orr => orr.Code).IsRequired().HasMaxLength(50);
        entity.Property(orr => orr.NameAr).IsRequired().HasMaxLength(200);
        entity.Property(orr => orr.NameFr).IsRequired().HasMaxLength(200);
        entity.Property(orr => orr.NameEn).IsRequired().HasMaxLength(200);
        entity.Property(orr => orr.Description).HasMaxLength(1000);
        entity.Property(orr => orr.Multiplier).HasColumnType("decimal(5,2)");
        entity.Property(orr => orr.MinimumDurationHours).HasColumnType("decimal(5,2)");
        entity.Property(orr => orr.MaximumDurationHours).HasColumnType("decimal(5,2)");
        entity.Property(orr => orr.LegalReference).HasMaxLength(500);
        entity.Property(orr => orr.DocumentationUrl).HasMaxLength(500);
        entity.Property(orr => orr.Category).HasMaxLength(50);
        entity.Property(orr => orr.RowVersion).IsRowVersion();
        entity.HasIndex(orr => orr.Code).IsUnique().HasFilter("[DeletedAt] IS NULL");
    }
}

// ── Events ───────────────────────────────────────────────────────────────────

public class EmployeeEventLogConfiguration : IEntityTypeConfiguration<EmployeeEventLog>
{
    public void Configure(EntityTypeBuilder<EmployeeEventLog> entity)
    {
        entity.ToTable("EmployeeEventLogs");
        entity.Property(e => e.eventName).IsRequired().HasMaxLength(200);
        entity.Property(e => e.oldValue).HasMaxLength(2000);
        entity.Property(e => e.newValue).HasMaxLength(2000);
    }
}

public class CompanyEventLogConfiguration : IEntityTypeConfiguration<CompanyEventLog>
{
    public void Configure(EntityTypeBuilder<CompanyEventLog> entity)
    {
        entity.ToTable("CompanyEventLogs");
        entity.Property(e => e.eventName).IsRequired().HasMaxLength(200);
        entity.Property(e => e.oldValue).HasMaxLength(2000);
        entity.Property(e => e.newValue).HasMaxLength(2000);
    }
}
