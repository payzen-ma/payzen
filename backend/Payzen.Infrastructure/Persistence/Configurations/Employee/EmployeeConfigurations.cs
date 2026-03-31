using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payzen.Domain.Entities.Employee;

namespace Payzen.Infrastructure.Persistence.Configurations.Employee;

public class EmployeeConfiguration : IEntityTypeConfiguration<Payzen.Domain.Entities.Employee.Employee>
{
    public void Configure(EntityTypeBuilder<Payzen.Domain.Entities.Employee.Employee> entity)
    {
        entity.ToTable("Employees");
        entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        entity.Property(e => e.CinNumber).IsRequired().HasMaxLength(20);
        entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
        entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
        entity.Property(e => e.PersonalEmail).HasMaxLength(200);
        entity.Property(e => e.CnssNumber).HasMaxLength(50);
        entity.Property(e => e.CimrNumber).HasMaxLength(50);
        entity.Property(e => e.CimrEmployeeRate).HasColumnType("decimal(5,4)");
        entity.Property(e => e.CimrCompanyRate).HasColumnType("decimal(5,4)");
        entity.Property(e => e.PrivateInsuranceNumber).HasMaxLength(50);
        entity.Property(e => e.PrivateInsuranceRate).HasColumnType("decimal(5,4)");
        entity.Property(e => e.PaymentMethod).HasMaxLength(50);
        entity.Property(e => e.AnnualLeaveOpeningDays).HasColumnType("decimal(10,2)");
        entity.Property(e => e.AnnualLeaveOpeningEffectiveFrom).HasColumnType("date");

        entity.HasOne(e => e.Company).WithMany(c => c.Employees).HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.Manager).WithMany(e => e.Subordinates).HasForeignKey(e => e.ManagerId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.Departement).WithMany(d => d.Employees).HasForeignKey(e => e.DepartementId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(e => e.Status).WithMany(s => s.Employees).HasForeignKey(e => e.StatusId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(e => e.Gender).WithMany(g => g.Employees).HasForeignKey(e => e.GenderId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(e => e.Nationality).WithMany(n => n.Employees).HasForeignKey(e => e.NationalityId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(e => e.EducationLevel).WithMany(el => el.Employees).HasForeignKey(e => e.EducationLevelId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(e => e.MaritalStatus).WithMany(ms => ms.Employees).HasForeignKey(e => e.MaritalStatusId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(e => e.Category).WithMany(c => c.Employees).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class EmployeeCategoryConfiguration : IEntityTypeConfiguration<EmployeeCategory>
{
    public void Configure(EntityTypeBuilder<EmployeeCategory> entity)
    {
        entity.ToTable("EmployeeCategories");
        entity.Property(ec => ec.Name).IsRequired().HasMaxLength(100);
        entity.Property(ec => ec.PayrollPeriodicity).HasMaxLength(50).HasDefaultValue("Mensuelle");
        entity.HasOne(ec => ec.Company).WithMany().HasForeignKey(ec => ec.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeContractConfiguration : IEntityTypeConfiguration<EmployeeContract>
{
    public void Configure(EntityTypeBuilder<EmployeeContract> entity)
    {
        entity.ToTable("EmployeeContracts");
        entity.HasOne(ec => ec.Employee).WithMany(e => e.Contracts).HasForeignKey(ec => ec.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(ec => ec.Company).WithMany().HasForeignKey(ec => ec.CompanyId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(ec => ec.JobPosition).WithMany(jp => jp.EmployeeContracts).HasForeignKey(ec => ec.JobPositionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(ec => ec.ContractType).WithMany(ct => ct.Employees).HasForeignKey(ec => ec.ContractTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeSalaryConfiguration : IEntityTypeConfiguration<EmployeeSalary>
{
    public void Configure(EntityTypeBuilder<EmployeeSalary> entity)
    {
        entity.ToTable("EmployeeSalaries");
        entity.Property(es => es.BaseSalary).HasColumnType("decimal(18,2)");
        entity.Property(es => es.BaseSalaryHourly).HasColumnType("decimal(18,4)");
        entity.HasOne(es => es.Employee).WithMany(e => e.Salaries).HasForeignKey(es => es.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(es => es.Contract).WithMany(c => c.Salaries).HasForeignKey(es => es.ContractId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeSalaryComponentConfiguration : IEntityTypeConfiguration<EmployeeSalaryComponent>
{
    public void Configure(EntityTypeBuilder<EmployeeSalaryComponent> entity)
    {
        entity.ToTable("EmployeeSalaryComponents");
        entity.Property(esc => esc.ComponentType).IsRequired().HasMaxLength(100);
        entity.Property(esc => esc.Amount).HasColumnType("decimal(18,2)");
        entity.HasOne(esc => esc.EmployeeSalary).WithMany(es => es.Components).HasForeignKey(esc => esc.EmployeeSalaryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeAddressConfiguration : IEntityTypeConfiguration<EmployeeAddress>
{
    public void Configure(EntityTypeBuilder<EmployeeAddress> entity)
    {
        entity.ToTable("EmployeeAddresses");
        entity.Property(ea => ea.AddressLine1).IsRequired().HasMaxLength(500);
        entity.Property(ea => ea.AddressLine2).HasMaxLength(500);
        entity.Property(ea => ea.ZipCode).IsRequired().HasMaxLength(20);
        entity.HasOne(ea => ea.Employee).WithMany(e => e.Addresses).HasForeignKey(ea => ea.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(ea => ea.City).WithMany().HasForeignKey(ea => ea.CityId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> entity)
    {
        entity.ToTable("EmployeeDocuments");
        entity.Property(ed => ed.Name).IsRequired().HasMaxLength(500);
        entity.Property(ed => ed.FilePath).IsRequired().HasMaxLength(1000);
        entity.Property(ed => ed.DocumentType).IsRequired().HasMaxLength(100);
        entity.HasOne(ed => ed.Employee).WithMany(e => e.Documents).HasForeignKey(ed => ed.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeAttendanceConfiguration : IEntityTypeConfiguration<EmployeeAttendance>
{
    public void Configure(EntityTypeBuilder<EmployeeAttendance> entity)
    {
        entity.ToTable("EmployeeAttendances");
        entity.Property(ea => ea.WorkedHours).HasColumnType("decimal(5,2)");
        entity.HasOne(ea => ea.Employee).WithMany(e => e.Attendances).HasForeignKey(ea => ea.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeAttendanceBreakConfiguration : IEntityTypeConfiguration<EmployeeAttendanceBreak>
{
    public void Configure(EntityTypeBuilder<EmployeeAttendanceBreak> entity)
    {
        entity.ToTable("EmployeeAttendanceBreaks");
        entity.Property(eab => eab.BreakType).HasMaxLength(50);
        entity.HasOne(eab => eab.EmployeeAttendance).WithMany(ea => ea.Breaks).HasForeignKey(eab => eab.EmployeeAttendanceId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeAbsenceConfiguration : IEntityTypeConfiguration<EmployeeAbsence>
{
    public void Configure(EntityTypeBuilder<EmployeeAbsence> entity)
    {
        entity.ToTable("EmployeeAbsences");
        entity.Property(ea => ea.AbsenceType).IsRequired().HasMaxLength(100);
        entity.Property(ea => ea.Reason).HasMaxLength(500);
        entity.Property(ea => ea.DecisionComment).HasMaxLength(500);
        entity.HasOne(ea => ea.Employee).WithMany().HasForeignKey(ea => ea.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeChildConfiguration : IEntityTypeConfiguration<EmployeeChild>
{
    public void Configure(EntityTypeBuilder<EmployeeChild> entity)
    {
        entity.ToTable("EmployeeChildren");
        entity.Property(ec => ec.FirstName).IsRequired().HasMaxLength(100);
        entity.Property(ec => ec.LastName).IsRequired().HasMaxLength(100);
        entity.HasOne(ec => ec.Employee).WithMany(e => e.Children).HasForeignKey(ec => ec.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(ec => ec.Gender).WithMany().HasForeignKey(ec => ec.GenderId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class EmployeeSpouseConfiguration : IEntityTypeConfiguration<EmployeeSpouse>
{
    public void Configure(EntityTypeBuilder<EmployeeSpouse> entity)
    {
        entity.ToTable("EmployeeSpouses");
        entity.Property(es => es.FirstName).IsRequired().HasMaxLength(100);
        entity.Property(es => es.LastName).IsRequired().HasMaxLength(100);
        entity.Property(es => es.CinNumber).HasMaxLength(20);
        entity.HasOne(es => es.Employee).WithMany(e => e.Spouses).HasForeignKey(es => es.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(es => es.Gender).WithMany().HasForeignKey(es => es.GenderId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class EmployeeOvertimeConfiguration : IEntityTypeConfiguration<EmployeeOvertime>
{
    public void Configure(EntityTypeBuilder<EmployeeOvertime> entity)
    {
        entity.ToTable("EmployeeOvertimes");
        entity.Property(eo => eo.DurationInHours).HasColumnType("decimal(5,2)");
        entity.Property(eo => eo.StandardDayHours).HasColumnType("decimal(5,2)");
        entity.Property(eo => eo.RateMultiplierApplied).HasColumnType("decimal(5,2)");
        entity.Property(eo => eo.RateRuleCodeApplied).HasMaxLength(50);
        entity.Property(eo => eo.RateRuleNameApplied).HasMaxLength(200);
        entity.Property(eo => eo.MultiplierCalculationDetails).HasMaxLength(1000);
        entity.Property(eo => eo.EmployeeComment).HasMaxLength(500);
        entity.Property(eo => eo.ManagerComment).HasMaxLength(500);
        entity.Property(eo => eo.RowVersion).IsRowVersion();
        entity.HasOne(eo => eo.Employee).WithMany().HasForeignKey(eo => eo.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(eo => eo.Holiday).WithMany().HasForeignKey(eo => eo.HolidayId).OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(eo => eo.RateRule).WithMany(rr => rr.OvertimeRecords).HasForeignKey(eo => eo.RateRuleId).OnDelete(DeleteBehavior.SetNull);
    }
}
