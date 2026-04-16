using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payzen.Domain.Entities.Leave;

namespace Payzen.Infrastructure.Persistence.Configurations.Leave;

public class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> entity)
    {
        entity.ToTable("LeaveTypes");
        entity.Property(lt => lt.LeaveCode).IsRequired().HasMaxLength(50);
        entity.Property(lt => lt.LeaveNameAr).IsRequired().HasMaxLength(100);
        entity.Property(lt => lt.LeaveNameEn).IsRequired().HasMaxLength(100);
        entity.Property(lt => lt.LeaveNameFr).IsRequired().HasMaxLength(100);
        entity.Property(lt => lt.LeaveDescription).IsRequired().HasMaxLength(500);
        entity.HasIndex(lt => lt.LeaveCode).IsUnique().HasFilter("[DeletedAt] IS NULL");
        entity.HasOne(lt => lt.Company).WithMany().HasForeignKey(lt => lt.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeaveTypePolicyConfiguration : IEntityTypeConfiguration<LeaveTypePolicy>
{
    public void Configure(EntityTypeBuilder<LeaveTypePolicy> entity)
    {
        entity.ToTable("LeaveTypePolicies");
        entity.Property(p => p.DaysPerMonthAdult).HasColumnType("decimal(5,2)");
        entity.Property(p => p.DaysPerMonthMinor).HasColumnType("decimal(5,2)");
        entity.Property(p => p.BonusDaysPerYearAfter5Years).HasColumnType("decimal(5,2)");
        entity.HasOne(p => p.Company).WithMany().HasForeignKey(p => p.CompanyId).OnDelete(DeleteBehavior.Restrict);
        entity
            .HasOne(p => p.LeaveType)
            .WithMany(lt => lt.Policies)
            .HasForeignKey(p => p.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeaveTypeLegalRuleConfiguration : IEntityTypeConfiguration<LeaveTypeLegalRule>
{
    public void Configure(EntityTypeBuilder<LeaveTypeLegalRule> entity)
    {
        entity.ToTable("LeaveTypeLegalRules");
        entity.Property(lr => lr.EventCaseCode).IsRequired().HasMaxLength(50);
        entity.Property(lr => lr.Description).IsRequired().HasMaxLength(300);
        entity.Property(lr => lr.LegalArticle).IsRequired().HasMaxLength(50);
        entity
            .HasOne(lr => lr.LeaveType)
            .WithMany(lt => lt.LegalRules)
            .HasForeignKey(lr => lr.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> entity)
    {
        entity.ToTable("LeaveRequests");
        entity.Property(lr => lr.WorkingDaysDeducted).HasColumnType("decimal(10,2)");
        entity.Property(lr => lr.DecisionComment).HasMaxLength(1000);
        entity.Property(lr => lr.ComputationVersion).HasMaxLength(50);
        entity.Property(lr => lr.EmployeeNote).HasMaxLength(1000);
        entity.Property(lr => lr.ManagerNote).HasMaxLength(1000);
        entity
            .HasOne(lr => lr.Employee)
            .WithMany()
            .HasForeignKey(lr => lr.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(lr => lr.Company).WithMany().HasForeignKey(lr => lr.CompanyId).OnDelete(DeleteBehavior.Restrict);
        entity
            .HasOne(lr => lr.LeaveType)
            .WithMany()
            .HasForeignKey(lr => lr.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        entity
            .HasOne(lr => lr.LegalRule)
            .WithMany()
            .HasForeignKey(lr => lr.LegalRuleId)
            .OnDelete(DeleteBehavior.SetNull);
        entity.HasOne(lr => lr.Policy).WithMany().HasForeignKey(lr => lr.PolicyId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class LeaveRequestApprovalHistoryConfiguration : IEntityTypeConfiguration<LeaveRequestApprovalHistory>
{
    public void Configure(EntityTypeBuilder<LeaveRequestApprovalHistory> entity)
    {
        entity.ToTable("LeaveRequestApprovalHistories");
        entity.Property(h => h.Comment).HasMaxLength(1000);
        entity
            .HasOne(h => h.LeaveRequest)
            .WithMany(lr => lr.ApprovalHistory)
            .HasForeignKey(h => h.LeaveRequestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeaveRequestExemptionConfiguration : IEntityTypeConfiguration<LeaveRequestExemption>
{
    public void Configure(EntityTypeBuilder<LeaveRequestExemption> entity)
    {
        entity.ToTable("LeaveRequestExemptions");
        entity.Property(e => e.Note).HasMaxLength(500);
        entity
            .HasOne(e => e.LeaveRequest)
            .WithMany(lr => lr.Exemptions)
            .HasForeignKey(e => e.LeaveRequestId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.Holiday).WithMany().HasForeignKey(e => e.HolidayId).OnDelete(DeleteBehavior.SetNull);
        entity
            .HasOne(e => e.EmployeeAbsence)
            .WithMany()
            .HasForeignKey(e => e.EmployeeAbsenceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class LeaveRequestAttachmentConfiguration : IEntityTypeConfiguration<LeaveRequestAttachment>
{
    public void Configure(EntityTypeBuilder<LeaveRequestAttachment> entity)
    {
        entity.ToTable("LeaveRequestAttachments");
        entity.Property(a => a.FileName).IsRequired().HasMaxLength(255);
        entity.Property(a => a.FilePath).IsRequired().HasMaxLength(1000);
        entity.Property(a => a.FileType).HasMaxLength(100);
        entity
            .HasOne(a => a.LeaveRequest)
            .WithMany(lr => lr.Attachments)
            .HasForeignKey(a => a.LeaveRequestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeaveAuditLogConfiguration : IEntityTypeConfiguration<LeaveAuditLog>
{
    public void Configure(EntityTypeBuilder<LeaveAuditLog> entity)
    {
        entity.ToTable("LeaveAuditLogs");
        entity.Property(l => l.EventName).IsRequired().HasMaxLength(200);
        entity.Property(l => l.OldValue).HasMaxLength(2000);
        entity.Property(l => l.NewValue).HasMaxLength(2000);
        entity.HasOne(l => l.Company).WithMany().HasForeignKey(l => l.CompanyId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(l => l.Employee).WithMany().HasForeignKey(l => l.EmployeeId).OnDelete(DeleteBehavior.SetNull);
        entity
            .HasOne(l => l.LeaveRequest)
            .WithMany()
            .HasForeignKey(l => l.LeaveRequestId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> entity)
    {
        entity.ToTable("LeaveBalances");
        entity.Property(lb => lb.OpeningDays).HasColumnType("decimal(10,2)");
        entity.Property(lb => lb.AccruedDays).HasColumnType("decimal(10,2)");
        entity.Property(lb => lb.UsedDays).HasColumnType("decimal(10,2)");
        entity.Property(lb => lb.CarryInDays).HasColumnType("decimal(10,2)");
        entity.Property(lb => lb.CarryOutDays).HasColumnType("decimal(10,2)");
        entity.Property(lb => lb.ClosingDays).HasColumnType("decimal(10,2)");
        entity
            .HasIndex(lb => new
            {
                lb.EmployeeId,
                lb.LeaveTypeId,
                lb.Year,
                lb.Month,
            })
            .IsUnique()
            .HasFilter("[DeletedAt] IS NULL");
        entity
            .HasOne(lb => lb.Employee)
            .WithMany()
            .HasForeignKey(lb => lb.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(lb => lb.Company).WithMany().HasForeignKey(lb => lb.CompanyId).OnDelete(DeleteBehavior.Restrict);
        entity
            .HasOne(lb => lb.LeaveType)
            .WithMany()
            .HasForeignKey(lb => lb.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeaveCarryOverAgreementConfiguration : IEntityTypeConfiguration<LeaveCarryOverAgreement>
{
    public void Configure(EntityTypeBuilder<LeaveCarryOverAgreement> entity)
    {
        entity.ToTable("LeaveCarryOverAgreements");
        entity.Property(a => a.AgreementDocRef).HasMaxLength(500);
        entity.HasOne(a => a.Employee).WithMany().HasForeignKey(a => a.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.Company).WithMany().HasForeignKey(a => a.CompanyId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.LeaveType).WithMany().HasForeignKey(a => a.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}
