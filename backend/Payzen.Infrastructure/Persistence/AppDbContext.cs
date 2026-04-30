using Microsoft.EntityFrameworkCore;
using Payzen.Domain.Common;
using Payzen.Domain.Entities.Auth;
using Payzen.Domain.Entities.Company;
using Payzen.Domain.Entities.Employee;
using Payzen.Domain.Entities.Events;
using Payzen.Domain.Entities.Leave;
using Payzen.Domain.Entities.Payroll;
using Payzen.Domain.Entities.Payroll.Referentiel;
using Payzen.Domain.Entities.Referentiel;

namespace Payzen.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    // ── Auth ─────────────────────────────────────────────────────────────────
    public DbSet<Users> Users { get; set; }
    public DbSet<Roles> Roles { get; set; }
    public DbSet<Permissions> Permissions { get; set; }
    public DbSet<RolesPermissions> RolesPermissions { get; set; }
    public DbSet<UsersRoles> UsersRoles { get; set; }
    public DbSet<Invitation> Invitations { get; set; }

    // ── Company ──────────────────────────────────────────────────────────────
    public DbSet<Company> Companies { get; set; }
    public DbSet<Departement> Departements { get; set; }
    public DbSet<ContractType> ContractTypes { get; set; }
    public DbSet<JobPosition> JobPositions { get; set; }
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<WorkingCalendar> WorkingCalendars { get; set; }
    public DbSet<CompanyDocument> CompanyDocuments { get; set; }

    // ── Referentiel ──────────────────────────────────────────────────────────
    public DbSet<Country> Countries { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<Gender> Genders { get; set; }
    public DbSet<Status> Statuses { get; set; }
    public DbSet<EducationLevel> EducationLevels { get; set; }
    public DbSet<MaritalStatus> MaritalStatuses { get; set; }
    public DbSet<Nationality> Nationalities { get; set; }
    public DbSet<LegalContractType> LegalContractTypes { get; set; }
    public DbSet<StateEmploymentProgram> StateEmploymentPrograms { get; set; }
    public DbSet<OvertimeRateRule> OvertimeRateRules { get; set; }

    // ── Employee ─────────────────────────────────────────────────────────────
    public DbSet<Employee> Employees { get; set; }
    public DbSet<EmployeeCategory> EmployeeCategories { get; set; }
    public DbSet<EmployeeContract> EmployeeContracts { get; set; }
    public DbSet<EmployeeSalary> EmployeeSalaries { get; set; }
    public DbSet<EmployeeSalaryComponent> EmployeeSalaryComponents { get; set; }
    public DbSet<EmployeeAddress> EmployeeAddresses { get; set; }
    public DbSet<EmployeeDocument> EmployeeDocuments { get; set; }
    public DbSet<EmployeeAttendance> EmployeeAttendances { get; set; }
    public DbSet<EmployeeAttendanceBreak> EmployeeAttendanceBreaks { get; set; }
    public DbSet<EmployeeAbsence> EmployeeAbsences { get; set; }
    public DbSet<EmployeeChild> EmployeeChildren { get; set; }
    public DbSet<EmployeeSpouse> EmployeeSpouses { get; set; }
    public DbSet<EmployeeOvertime> EmployeeOvertimes { get; set; }

    // ── Leave ─────────────────────────────────────────────────────────────────
    public DbSet<LeaveType> LeaveTypes { get; set; }
    public DbSet<LeaveTypePolicy> LeaveTypePolicies { get; set; }
    public DbSet<LeaveTypeLegalRule> LeaveTypeLegalRules { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<LeaveRequestApprovalHistory> LeaveRequestApprovalHistories { get; set; }
    public DbSet<LeaveRequestExemption> LeaveRequestExemptions { get; set; }
    public DbSet<LeaveRequestAttachment> LeaveRequestAttachments { get; set; }
    public DbSet<LeaveAuditLog> LeaveAuditLogs { get; set; }
    public DbSet<LeaveBalance> LeaveBalances { get; set; }
    public DbSet<LeaveCarryOverAgreement> LeaveCarryOverAgreements { get; set; }

    // ── Payroll ───────────────────────────────────────────────────────────────
    public DbSet<PayrollResult> PayrollResults { get; set; }
    public DbSet<PayrollResultPrime> PayrollResultPrimes { get; set; }
    public DbSet<PayrollCalculationAuditStep> PayrollCalculationAuditSteps { get; set; }
    public DbSet<PayrollCustomRule> PayrollCustomRules { get; set; }
    public DbSet<SalaryPackage> SalaryPackages { get; set; }
    public DbSet<SalaryPackageItem> SalaryPackageItems { get; set; }
    public DbSet<SalaryPackageAssignment> SalaryPackageAssignments { get; set; }
    public DbSet<PayComponent> PayComponents { get; set; }
    public DbSet<SystemConstant> SystemConstants { get; set; }
    public DbSet<IrTaxBracket> IrTaxBrackets { get; set; }
    public DbSet<CnssPreetabliImport> CnssPreetabliImports { get; set; }
    public DbSet<CnssPreetabliLine> CnssPreetabliLines { get; set; }
    public DbSet<PayrollTaxSnapshot> PayrollTaxSnapshots { get; set; }

    // ── Payroll Referentiel ───────────────────────────────────────────────────
    public DbSet<Authority> Authorities { get; set; }
    public DbSet<ElementCategory> ElementCategories { get; set; }
    public DbSet<EligibilityCriteria> EligibilityCriteria { get; set; }
    public DbSet<LegalParameter> LegalParameters { get; set; }
    public DbSet<ReferentielElement> ReferentielElements { get; set; }
    public DbSet<ElementRule> ElementRules { get; set; }
    public DbSet<RuleCap> RuleCaps { get; set; }
    public DbSet<RulePercentage> RulePercentages { get; set; }
    public DbSet<RuleFormula> RuleFormulas { get; set; }
    public DbSet<RuleDualCap> RuleDualCaps { get; set; }
    public DbSet<RuleTier> RuleTiers { get; set; }
    public DbSet<RuleVariant> RuleVariants { get; set; }
    public DbSet<AncienneteRateSet> AncienneteRateSets { get; set; }
    public DbSet<AncienneteRate> AncienneteRates { get; set; }
    public DbSet<BusinessSector> BusinessSectors { get; set; }

    // ── Events ────────────────────────────────────────────────────────────────
    public DbSet<EmployeeEventLog> EmployeeEventLogs { get; set; }
    public DbSet<CompanyEventLog> CompanyEventLogs { get; set; }

    // ─────────────────────────────────────────────────────────────────────────
    // SAVE — auto-audit timestamps
    // ─────────────────────────────────────────────────────────────────────────

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
        return await base.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MODEL CREATING — Fluent API configurations + global soft-delete filter
    // ─────────────────────────────────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global soft-delete query filter for every entity that has DeletedAt
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var deletedAtProp = entityType.FindProperty("DeletedAt");
            if (deletedAtProp == null || deletedAtProp.ClrType != typeof(DateTimeOffset?))
                continue;

            var param = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
            var property = System.Linq.Expressions.Expression.Property(param, "DeletedAt");
            var isNull = System.Linq.Expressions.Expression.Equal(
                property,
                System.Linq.Expressions.Expression.Constant(null, typeof(DateTimeOffset?))
            );
            var lambda = System.Linq.Expressions.Expression.Lambda(isNull, param);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
