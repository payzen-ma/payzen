using Microsoft.EntityFrameworkCore;
using payzen_backend.Models.Users;
using payzen_backend.Models;
using payzen_backend.Models.Permissions;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Employee.Dtos;
using payzen_backend.Models.Company;
using payzen_backend.Models.Referentiel;
using payzen_backend.Models.Event;
using payzen_backend.Models.Leave;
using payzen_backend.Models.Payroll;
using payzen_backend.Models.Payroll.Referentiel;
using System.Reflection.Emit;

namespace payzen_backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ========== Tables Users & Permissions ==========
        public DbSet<Users> Users { get; set; }
        public DbSet<Permissions> Permissions { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<RolesPermissions> RolesPermissions { get; set; }
        public DbSet<UsersRoles> UsersRoles { get; set; }

        // ========== Tables Company ==========
        public DbSet<Company> Companies { get; set; }
        public DbSet<Departement> Departement { get; set; }
        public DbSet<ContractType> ContractTypes { get; set; }
        public DbSet<JobPosition> JobPositions { get; set; }
        public DbSet<Holiday> Holidays { get; set; }
        public DbSet<WorkingCalendar> WorkingCalendars { get; set; }
        public DbSet<CompanyDocument> CompanyDocuments { get; set; }

        // ========== Tables Referentiel ==========
        public DbSet<Country> Countries { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<Gender> Genders { get; set; }
        public DbSet<EducationLevel> EducationLevels { get; set; }
        public DbSet<MaritalStatus> MaritalStatuses { get; set; }
        public DbSet<Nationality> Nationalities { get; set; }
        public DbSet<LegalContractType> LegalContractTypes { get; set; }
        public DbSet<StateEmploymentProgram> StateEmploymentPrograms { get; set; }
        public DbSet<OvertimeRateRule> OvertimeRateRules { get; set; }

        // ========== Tables Employee ==========
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

        public DbSet<PayrollResult> PayrollResults { get; set; }
        public DbSet<PayrollResultPrime> PayrollResultPrimes { get; set; }
        public DbSet<PayrollCalculationAuditStep> PayrollCalculationAuditSteps { get; set; }

        // ========== Tables Payroll ==========
        public DbSet<SalaryPackage> SalaryPackages { get; set; }
        public DbSet<SalaryPackageItem> SalaryPackageItems { get; set; }
        public DbSet<SalaryPackageAssignment> SalaryPackageAssignments { get; set; }
        public DbSet<PayComponent> PayComponents { get; set; }

        // =========== Tables Events Log ================
        public DbSet<EmployeeEventLog> EmployeeEventLogs { get; set; }
        public DbSet<CompanyEventLog> CompanyEventLogs { get; set; }

        // ========== Tables Leave ==========
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

        // =========== Tables Payroll Referentiel ================
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply global soft-delete query filters
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var deletedAtProperty = entityType.FindProperty("DeletedAt");
                if (deletedAtProperty != null && deletedAtProperty.ClrType == typeof(DateTimeOffset?))
                {
                    var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                    var property = System.Linq.Expressions.Expression.Property(parameter, "DeletedAt");
                    var condition = System.Linq.Expressions.Expression.Equal(
                        property,
                        System.Linq.Expressions.Expression.Constant(null, typeof(DateTimeOffset?))
                    );
                    var lambda = System.Linq.Expressions.Expression.Lambda(condition, parameter);

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }
            modelBuilder.Entity<Users>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Email).IsUnique(false);
                entity.HasIndex(u => u.EmailPersonal).IsUnique(false).HasFilter("[DeletedAt] IS NULL AND [EmailPersonal] IS NOT NULL");
                entity.HasIndex(u => u.Username).IsUnique(true).HasFilter("[DeletedAt] IS NULL");

                entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(100);

                entity.HasOne(u => u.Employee)
                    .WithMany()
                    .HasForeignKey(u => u.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Permissions>(entity =>
            {
                entity.ToTable("Permissions");
                entity.HasKey(p => p.Id);
                entity.HasIndex(p => p.Name).IsUnique(true).HasFilter("[DeletedAt] IS NULL");

                entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Description).IsRequired().HasMaxLength(500);
            });

            modelBuilder.Entity<Roles>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasKey(r => r.Id);
                entity.HasIndex(r => r.Name).IsUnique(true).HasFilter("[DeletedAt] IS NULL");

                entity.Property(r => r.Name).IsRequired().HasMaxLength(50);
                entity.Property(r => r.Description).IsRequired().HasMaxLength(500);
            });

            modelBuilder.Entity<RolesPermissions>(entity =>
            {
                entity.ToTable("RolesPermissions");
                entity.HasKey(rp => rp.Id);
                entity.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique(true).HasFilter("[DeletedAt] IS NULL");

                entity.HasOne(rp => rp.Role)
                    .WithMany()
                    .HasForeignKey(rp => rp.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rp => rp.Permission)
                    .WithMany()
                    .HasForeignKey(rp => rp.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UsersRoles>(entity =>
            {
                entity.ToTable("UsersRoles");
                entity.HasKey(ur => ur.Id);
                entity.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique(true).HasFilter("[DeletedAt] IS NULL");

                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.UsersRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ur => ur.Role)
                    .WithMany()
                    .HasForeignKey(ur => ur.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ========================================
            // CONFIGURATION REFERENTIEL
            // ========================================

            modelBuilder.Entity<Country>(entity =>
            {
                entity.ToTable("Country");
                entity.HasKey(c => c.Id);
                entity.HasIndex(c => c.CountryCode).IsUnique().HasFilter("[DeletedAt] IS NULL");

                entity.Property(c => c.CountryName).IsRequired().HasMaxLength(500);
                entity.Property(c => c.CountryNameAr).HasMaxLength(500);
                entity.Property(c => c.CountryCode).IsRequired().HasMaxLength(3);
                entity.Property(c => c.CountryPhoneCode).IsRequired().HasMaxLength(10);
            });

            modelBuilder.Entity<City>(entity =>
            {
                entity.ToTable("City");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.CityName).IsRequired().HasMaxLength(500);

                entity.HasOne(c => c.Country)
                    .WithMany(co => co.Cities)
                    .HasForeignKey(c => c.CountryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Status>(entity =>
            {
                entity.ToTable("Status");
                entity.HasKey(s => s.Id);

                entity.HasIndex(s => s.Code).IsUnique(true);

                entity.Property(s => s.Code)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(s => s.NameFr)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(s => s.NameAr)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(s => s.NameEn)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(s => s.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.Property(s => s.AffectsAccess)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(s => s.AffectsPayroll)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(s => s.AffectsAttendance)
                    .IsRequired()
                    .HasDefaultValue(false);
            });

            modelBuilder.Entity<Gender>(entity =>
            {
                entity.ToTable("Gender");
                entity.HasKey(g => g.Id);

                // Index unique sur Code
                entity.HasIndex(g => g.Code).IsUnique(true);

                entity.Property(g => g.Code)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(g => g.NameFr)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(g => g.NameAr)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(g => g.NameEn)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(g => g.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);
            });

            modelBuilder.Entity<EducationLevel>(entity =>
            {
                entity.ToTable("EducationLevel");
                entity.HasKey(e => e.Id);

                // Code unique
                entity.HasIndex(e => e.Code).IsUnique(true);

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.NameFr)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.NameAr)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.NameEn)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.LevelOrder)
                    .IsRequired();

                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);
            });

            modelBuilder.Entity<MaritalStatus>(entity =>
            {
                entity.ToTable("MaritalStatus");
                entity.HasKey(ms => ms.Id);

                // Index unique sur Code
                entity.HasIndex(ms => ms.Code).IsUnique(true);

                entity.Property(ms => ms.Code)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(ms => ms.NameFr)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(ms => ms.NameAr)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(ms => ms.NameEn)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(ms => ms.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);
            });

            modelBuilder.Entity<Nationality>(entity =>
            {
                entity.ToTable("Nationality");
                entity.HasKey(n => n.Id);
                entity.HasIndex(n => n.Name).IsUnique().HasFilter("[DeletedAt] IS NULL");
            });
            
            modelBuilder.Entity<LegalContractType>(entity =>
            {
                entity.ToTable("LegalContractType");
                entity.HasKey(lct => lct.Id);
                
                entity.Property(lct => lct.Code).IsRequired().HasMaxLength(30);
                entity.Property(lct => lct.Name).IsRequired().HasMaxLength(200);

                entity.HasIndex(lct => lct.Name).IsUnique().HasFilter("[DeletedAt] IS NULL");
                entity.HasIndex(lct => lct.Code).IsUnique();
            });
            
            modelBuilder.Entity<StateEmploymentProgram>(entity =>
            {
                entity.ToTable("StateEmploymentProgram");
                entity.HasKey(sep => sep.Id);

                entity.Property(sep => sep.Code).IsRequired().HasMaxLength(30);
                entity.Property(sep => sep.Name).IsRequired().HasMaxLength(200);

                entity.Property(sep => sep.SalaryCeiling).HasColumnType("decimal(18,2)");

                entity.HasIndex(sep => sep.Code).IsUnique();
            });

            modelBuilder.Entity<OvertimeRateRule>(entity =>
            {
                entity.ToTable("OvertimeRateRules");
                entity.HasKey(e => e.Id);

                // Index unique : un code ne peut exister qu'une seule fois par période
                // Exemple : "NORMAL_DAY" peut avoir plusieurs versions historiques avec EffectiveFrom différents
                entity.HasIndex(e => new { e.Code })
                      .IsUnique()
                      .HasDatabaseName("IX_OvertimeRateRule_Code");

                // Index de lookup pour recherche rapide des règles actives
                entity.HasIndex(e => new { e.AppliesTo, e.IsActive, e.Priority })
                      .HasDatabaseName("IX_OvertimeRateRule_Lookup");

                // Index sur dates d'effectivité
                entity.HasIndex(e => new { e.EffectiveFrom, e.EffectiveTo })
                      .HasDatabaseName("IX_OvertimeRateRule_EffectiveDates");

                // Index sur Code pour recherche rapide
                entity.HasIndex(e => e.Code)
                      .HasDatabaseName("IX_OvertimeRateRule_Code");

                // Contraintes de longueur
                entity.Property(e => e.Code)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.NameEn)
                      .IsRequired()
                      .HasMaxLength(200);
                entity.Property(e => e.NameFr)
                      .IsRequired()
                      .HasMaxLength(200);
                entity.Property(e => e.NameAr)
                        .IsRequired()
                        .HasMaxLength(200);

                entity.Property(e => e.Description)
                      .HasMaxLength(2000); // Plus long pour inclure références légales

                // Précision décimale pour le multiplicateur
                entity.Property(e => e.Multiplier)
                      .HasPrecision(5, 2)
                      .IsRequired();

                // Pas de relation avec Company (règles globales)
            });

            // ========================================
            // CONFIGURATION COMPANY
            // ========================================

            modelBuilder.Entity<Company>(entity =>
            {
                entity.ToTable("Company");
                entity.HasKey(c => c.Id);

                // ========== INFORMATIONS DE BASE (Obligatoires à la création) ==========

                entity.Property(c => c.CompanyName)
                    .HasColumnName("company_name")
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(c => c.Email)
                    .HasColumnName("email")
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(c => c.PhoneNumber)
                    .HasColumnName("phone_number")
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(c => c.CountryPhoneCode)
                    .HasColumnName("country_phone_code")
                    .HasMaxLength(10);

                entity.Property(c => c.CompanyAddress)
                    .HasColumnName("company_address")
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.Property(c => c.CityId)
                    .HasColumnName("city_id")
                    .IsRequired();

                entity.Property(c => c.CountryId)
                    .HasColumnName("country_id")
                    .IsRequired();

                entity.Property(c => c.CnssNumber)
                    .HasColumnName("cnss_number")
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(c => c.IsCabinetExpert)
                    .HasColumnName("is_cabinet_expert")
                    .HasDefaultValue(false);

                // ========== INFORMATIONS LÉGALES & FISCALES (Optionnelles) ==========

                entity.Property(c => c.IceNumber)
                    .HasColumnName("ice_number")
                    .HasMaxLength(100);

                entity.Property(c => c.IfNumber)
                    .HasColumnName("if_number")
                    .HasMaxLength(100);

                entity.Property(c => c.RcNumber)
                    .HasColumnName("rc_number")
                    .HasMaxLength(100);

                entity.Property(c => c.RibNumber)
                    .HasColumnName("rib_number")
                    .HasMaxLength(100);

                entity.Property(c => c.PatenteNumber)
                    .HasColumnName("patente_number")
                    .HasMaxLength(100);

                entity.Property(c => c.WebsiteUrl)
                    .HasColumnName("website_url")
                    .HasMaxLength(500);

                entity.Property(c => c.LegalForm)
                    .HasColumnName("legal_form")
                    .HasMaxLength(50);

                entity.Property(c => c.FoundingDate)
                    .HasColumnName("founding_date")
                    .HasColumnType("date");

                // ========== PARAMÉTRAGE PAIE (Optionnels avant 1ère paie) ==========

                entity.Property(c => c.Currency)
                    .HasColumnName("currency")
                    .HasMaxLength(10)
                    .HasDefaultValue("MAD");

                entity.Property(c => c.PayrollPeriodicity)
                    .HasColumnName("payroll_periodicity")
                    .HasMaxLength(50)
                    .HasDefaultValue("Mensuelle");

                entity.Property(c => c.FiscalYearStartMonth)
                    .HasColumnName("fiscal_year_start_month")
                    .HasDefaultValue(1);

                entity.Property(c => c.BusinessSector)
                    .HasColumnName("business_sector")
                    .HasMaxLength(200);

                entity.Property(c => c.PaymentMethod)
                    .HasColumnName("payment_method")
                    .HasMaxLength(100);

                // ========== SIGNATAIRE ==========

                entity.Property(c => c.SignatoryName)
                    .HasColumnName("signatory_name")
                    .HasMaxLength(200);

                entity.Property(c => c.SignatoryTitle)
                    .HasColumnName("signatory_title")
                    .HasMaxLength(100);

                // ========== GESTION MULTI-ENTREPRISES ==========

                entity.Property(c => c.ManagedByCompanyId)
                    .HasColumnName("managedby_company_id");

                // ========== CHAMPS D'AUDIT ==========

                entity.Property(c => c.CreatedAt).HasColumnName("created_at");
                entity.Property(c => c.CreatedBy).HasColumnName("created_by");
                entity.Property(c => c.ModifiedAt).HasColumnName("modified_at");
                entity.Property(c => c.ModifiedBy).HasColumnName("modified_by");
                entity.Property(c => c.DeletedAt).HasColumnName("deleted_at");
                entity.Property(c => c.DeletedBy).HasColumnName("deleted_by");

                // ========== RELATIONS ==========

                entity.HasOne(c => c.ManagedByCompany)
                    .WithMany(c => c.ManagedCompanies)
                    .HasForeignKey(c => c.ManagedByCompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.City)
                    .WithMany(ci => ci.Companies)
                    .HasForeignKey(c => c.CityId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Country)
                    .WithMany(co => co.Companies)
                    .HasForeignKey(c => c.CountryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Departement>(entity =>
            {
                entity.ToTable("Departement");
                entity.HasKey(d => d.Id);
                entity.Property(d => d.DepartementName).IsRequired().HasMaxLength(100);

                entity.HasOne(d => d.Company)
                    .WithMany()
                    .HasForeignKey(d => d.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ContractType>(entity =>
            {
                entity.ToTable("ContractType");
                entity.HasKey(ct => ct.Id);
                entity.Property(ct => ct.ContractTypeName).IsRequired().HasMaxLength(100);
                entity.Property(ct => ct.LegalContractTypeId).IsRequired(false);
                entity.Property(ct => ct.StateEmploymentProgramId).IsRequired(false);

                entity.HasOne(ct => ct.Company)
                    .WithMany()
                    .HasForeignKey(ct => ct.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ct => ct.LegalContractType)
                    .WithMany()
                    .HasForeignKey(ct => ct.LegalContractTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ct => ct.StateEmploymentProgram)
                    .WithMany()
                    .HasForeignKey(ct => ct.StateEmploymentProgramId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<JobPosition>(entity =>
            {
                entity.ToTable("JobPosition");
                entity.HasKey(jp => jp.Id);
                entity.Property(jp => jp.Name).IsRequired().HasMaxLength(200);

                entity.HasOne(jp => jp.Company)
                    .WithMany()
                    .HasForeignKey(jp => jp.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Holiday>(entity =>
            {
                entity.ToTable("Holiday");
                entity.HasKey(h => h.Id);

                // Multilingue (cohérent avec autres tables)
                entity.Property(h => h.NameFr).IsRequired().HasMaxLength(500);
                entity.Property(h => h.NameAr).IsRequired().HasMaxLength(500);
                entity.Property(h => h.NameEn).IsRequired().HasMaxLength(500);

                entity.Property(h => h.HolidayDate).HasColumnType("date");
                entity.Property(h => h.Description).HasMaxLength(1000);

                // Type et catégorisation
                entity.Property(h => h.HolidayType).IsRequired().HasMaxLength(50); // National, Religious, Company, Regional
                entity.Property(h => h.IsMandatory).HasDefaultValue(true);
                entity.Property(h => h.IsPaid).HasDefaultValue(true);

                // Récurrence
                entity.Property(h => h.IsRecurring).HasDefaultValue(false);
                entity.Property(h => h.RecurrenceRule).HasMaxLength(200);

                // Impact système
                entity.Property(h => h.AffectPayroll).HasDefaultValue(true);
                entity.Property(h => h.AffectAttendance).HasDefaultValue(true);

                entity.HasIndex(h => new { h.CompanyId, h.HolidayDate })
                    .IsUnique()
                    .HasFilter("[DeletedAt] IS NULL");

                entity.HasOne(h => h.Company)
                    .WithMany()
                    .HasForeignKey(h => h.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(h => h.Country)
                    .WithMany(c => c.Holidays)
                    .HasForeignKey(h => h.CountryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WorkingCalendar>(entity =>
            {
                entity.ToTable("WorkingCalendar");
                entity.HasKey(wc => wc.Id);
                entity.Property(wc => wc.StartTime).HasColumnType("time");
                entity.Property(wc => wc.EndTime).HasColumnType("time");
                entity.HasIndex(wc => new { wc.CompanyId, wc.DayOfWeek }).IsUnique().HasFilter("[DeletedAt] IS NULL");

                entity.HasOne(wc => wc.Company)
                    .WithMany()
                    .HasForeignKey(wc => wc.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CompanyEventLog>(entity =>
            {
                entity.ToTable("CompanyEventLog");
                entity.HasKey(cel => cel.Id);

                entity.Property(cel => cel.eventName).IsRequired().HasMaxLength(200);
                entity.Property(cel => cel.oldValue).HasMaxLength(1000);
                entity.Property(cel => cel.newValue).HasMaxLength(1000);
                entity.Property(cel => cel.createdAt).IsRequired();
                entity.HasOne<Users>()
                    .WithMany()
                    .HasForeignKey(cel => cel.createdBy)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Company>()
                    .WithMany()
                    .HasForeignKey(cel => cel.companyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ========================================
            // CONFIGURATION EMPLOYEE
            // ========================================

            modelBuilder.Entity<EmployeeCategory>(entity =>
            {
                entity.ToTable("EmployeeCategory");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.CompanyId)
                    .IsRequired();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Mode)
                    .IsRequired()
                    .HasConversion<int>();

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.Property(e => e.CreatedBy)
                    .IsRequired();

                entity.Property(e => e.ModifiedAt)
                    .IsRequired(false);

                entity.Property(e => e.ModifiedBy)
                    .IsRequired(false);

                entity.Property(e => e.DeletedAt)
                    .IsRequired(false);

                entity.Property(e => e.DeletedBy)
                    .IsRequired(false);

                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Employees)
                    .WithOne(e => e.Category)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.CompanyId, e.Name })
                    .IsUnique()
                    .HasFilter("[DeletedAt] IS NULL"); ;
            });

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.ToTable("Employee");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
                entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
                entity.Property(e => e.CinNumber).HasColumnName("cin_number").HasMaxLength(50).IsRequired();
                entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth").IsRequired();
                entity.Property(e => e.Phone).HasColumnName("phone").IsRequired();
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(100).IsRequired();
                entity.Property(e => e.PersonalEmail).HasColumnName("personal_email").HasMaxLength(100);
                entity.Property(e => e.CompanyId).HasColumnName("company_id").IsRequired();
                entity.Property(e => e.ManagerId).HasColumnName("manager_id");
                entity.Property(e => e.DepartementId).HasColumnName("departement_id");
                entity.Property(e => e.StatusId).HasColumnName("status_id");
                entity.Property(e => e.GenderId).HasColumnName("gender_id");
                entity.Property(e => e.NationalityId).HasColumnName("nationality_id");
                entity.Property(e => e.EducationLevelId).HasColumnName("education_level_id");
                entity.Property(e => e.MaritalStatusId).HasColumnName("marital_status_id");
                entity.Property(e => e.CategoryId).HasColumnName("employee_category_id").IsRequired(false);
                entity.Property(e => e.CnssNumber).HasColumnName("cnss_number");
                entity.Property(e => e.CimrNumber).HasColumnName("cimr_number");
                entity.Property(e => e.CimrEmployeeRate).HasColumnName("cimr_employee_rate").HasColumnType("decimal(5,2)");
                entity.Property(e => e.CimrCompanyRate).HasColumnName("cimr_company_rate").HasColumnType("decimal(5,2)");
                entity.Property(e => e.HasPrivateInsurance).HasColumnName("has_private_insurance").IsRequired().HasDefaultValue(false);
                entity.Property(e => e.PrivateInsuranceNumber).HasColumnName("private_insurance_number");
                entity.Property(e => e.PrivateInsuranceRate).HasColumnName("private_insurance_rate").HasColumnType("decimal(5,2)");
                entity.Property(e => e.DisableAmo).HasColumnName("disable_amo").IsRequired().HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.ModifiedAt).HasColumnName("modified_at");
                entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
                entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
                entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");

                entity.HasIndex(e => e.CinNumber).IsUnique().HasFilter("[deleted_at] IS NULL");
                entity.HasIndex(e => e.Email).IsUnique().HasFilter("[deleted_at] IS NULL");
                entity.HasIndex(e => e.PersonalEmail).HasFilter("[deleted_at] IS NULL AND [personal_email] IS NOT NULL");
                entity.HasIndex(e => e.CategoryId);

                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Employees)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Manager)
                    .WithMany(m => m.Subordinates)
                    .HasForeignKey(e => e.ManagerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Departement)
                    .WithMany(d => d.Employees)
                    .HasForeignKey(e => e.DepartementId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Status)
                    .WithMany(s => s.Employees)
                    .HasForeignKey(e => e.StatusId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Gender)
                    .WithMany(g => g.Employees)
                    .HasForeignKey(e => e.GenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Nationality)
                    .WithMany(n => n.Employees)
                    .HasForeignKey(e => e.NationalityId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.EducationLevel)
                    .WithMany(el => el.Employees)
                    .HasForeignKey(e => e.EducationLevelId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.MaritalStatus)
                    .WithMany(ms => ms.Employees)
                    .HasForeignKey(e => e.MaritalStatusId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Category)
                    .WithMany(ec => ec.Employees)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EmployeeContract>(entity =>
            {
                entity.ToTable("EmployeeContract");
                entity.HasKey(ec => ec.Id);
                entity.Property(ec => ec.StartDate).IsRequired();

                entity.HasOne(ec => ec.Employee)
                    .WithMany(e => e.Contracts)
                    .HasForeignKey(ec => ec.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ec => ec.Company)
                    .WithMany()
                    .HasForeignKey(ec => ec.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ec => ec.JobPosition)
                    .WithMany(jp => jp.EmployeeContracts)
                    .HasForeignKey(ec => ec.JobPositionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ec => ec.ContractType)
                    .WithMany(ct => ct.Employees)
                    .HasForeignKey(ec => ec.ContractTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EmployeeSalary>(entity =>
            {
                entity.ToTable("EmployeeSalary");
                entity.HasKey(es => es.Id);
                entity.Property(es => es.BaseSalary).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(es => es.EffectiveDate).IsRequired();

                entity.HasOne(es => es.Employee)
                    .WithMany(e => e.Salaries)
                    .HasForeignKey(es => es.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(es => es.Contract)
                    .WithMany(ec => ec.Salaries)
                    .HasForeignKey(es => es.ContractId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EmployeeSalaryComponent>(entity =>
            {
                entity.ToTable("EmployeeSalaryComponent");
                entity.HasKey(esc => esc.Id);
                entity.Property(esc => esc.ComponentType).IsRequired().HasMaxLength(100);
                entity.Property(esc => esc.Amount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(esc => esc.EffectiveDate).IsRequired();

                entity.HasOne(esc => esc.EmployeeSalary)
                    .WithMany(es => es.Components)
                    .HasForeignKey(esc => esc.EmployeeSalaryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PayrollResult>(entity =>
            {
                entity.ToTable("PayrollResults");
                entity.HasKey(pr => pr.Id);

                // Unicité : un seul résultat par employé par mois/année
                entity.HasIndex(pr => new { pr.EmployeeId, pr.Month, pr.Year })
                    .IsUnique()
                    .HasFilter("[DeletedAt] IS NULL")
                    .HasDatabaseName("UX_PayrollResult_Employee_Period");

                // Index pour listing par company + période
                entity.HasIndex(pr => new { pr.CompanyId, pr.Year, pr.Month })
                    .HasDatabaseName("IX_PayrollResult_Company_Period");

                // Index sur statut pour monitoring
                entity.HasIndex(pr => pr.Status)
                    .HasDatabaseName("IX_PayrollResult_Status");

                // JSON brut (peut être grand)
                entity.Property(pr => pr.ResultatJson)
                    .HasColumnType("nvarchar(max)");

                entity.Property(pr => pr.ErrorMessage)
                    .HasMaxLength(2000);

                entity.Property(pr => pr.ClaudeModel)
                    .HasMaxLength(100);

                // Montants
                entity.Property(pr => pr.SalaireBase).HasColumnType("decimal(18,2)");
                entity.Property(pr => pr.TotalBrut).HasColumnType("decimal(18,2)");
                entity.Property(pr => pr.TotalCotisationsSalariales).HasColumnType("decimal(18,2)");
                entity.Property(pr => pr.TotalCotisationsPatronales).HasColumnType("decimal(18,2)");
                entity.Property(pr => pr.ImpotRevenu).HasColumnType("decimal(18,2)");
                entity.Property(pr => pr.TotalNet).HasColumnType("decimal(18,2)");
                entity.Property(pr => pr.TotalNet2).HasColumnType("decimal(18,2)");

                entity.Property(pr => pr.Status)
                    .HasConversion<int>();

                // Relations
                entity.HasOne(pr => pr.Employee)
                    .WithMany()
                    .HasForeignKey(pr => pr.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(pr => pr.Company)
                    .WithMany()
                    .HasForeignKey(pr => pr.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(pr => pr.CalculationAuditSteps)
                    .WithOne(s => s.PayrollResult)
                    .HasForeignKey(s => s.PayrollResultId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<PayrollCalculationAuditStep>(entity =>
            {
                entity.ToTable("PayrollCalculationAuditSteps");
                entity.HasKey(s => s.Id);
                entity.Property(s => s.ModuleName).IsRequired().HasMaxLength(100);
                entity.Property(s => s.FormulaDescription).IsRequired().HasMaxLength(2000);
                entity.Property(s => s.InputsJson).HasColumnType("nvarchar(max)");
                entity.Property(s => s.OutputsJson).HasColumnType("nvarchar(max)");
                entity.HasIndex(s => s.PayrollResultId);
            });

            // ========================================
            // CONFIGURATION SALARY PACKAGES
            // ========================================

            modelBuilder.Entity<PayComponent>(entity =>
            {
                entity.ToTable("PayComponent");
                entity.HasKey(pc => pc.Id);
                
                entity.Property(pc => pc.Code).IsRequired().HasMaxLength(50);
                entity.Property(pc => pc.NameFr).IsRequired().HasMaxLength(200);
                entity.Property(pc => pc.NameAr).HasMaxLength(200);
                entity.Property(pc => pc.NameEn).HasMaxLength(200);
                entity.Property(pc => pc.Type).IsRequired().HasMaxLength(50);
                entity.Property(pc => pc.ExemptionLimit).HasColumnType("decimal(18,2)");
                entity.Property(pc => pc.ExemptionRule).HasMaxLength(100);
                entity.Property(pc => pc.DefaultAmount).HasColumnType("decimal(18,2)");
                entity.Property(pc => pc.Version).IsRequired().HasDefaultValue(1);
                entity.Property(pc => pc.ValidFrom).IsRequired();
                entity.Property(pc => pc.IsRegulated).IsRequired().HasDefaultValue(false);
                entity.Property(pc => pc.IsActive).IsRequired().HasDefaultValue(true);
                entity.Property(pc => pc.SortOrder).IsRequired().HasDefaultValue(0);

                // Unique constraint on Code + Version
                entity.HasIndex(pc => new { pc.Code, pc.Version }).IsUnique().HasFilter("[DeletedAt] IS NULL");
            });

            modelBuilder.Entity<SalaryPackage>(entity =>
            {
                entity.ToTable("SalaryPackage");
                entity.HasKey(sp => sp.Id);
                entity.Property(sp => sp.Name).IsRequired().HasMaxLength(200);
                entity.Property(sp => sp.Category).IsRequired().HasMaxLength(100);
                entity.Property(sp => sp.Description).HasMaxLength(1000);
                entity.Property(sp => sp.BaseSalary).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(sp => sp.Status).IsRequired().HasMaxLength(20).HasDefaultValue("draft");
                
                // Template type distinction (OFFICIAL or COMPANY)
                entity.Property(sp => sp.TemplateType).IsRequired().HasMaxLength(20).HasDefaultValue("OFFICIAL");
                
                // Moroccan regulation version
                entity.Property(sp => sp.RegulationVersion).IsRequired().HasMaxLength(20).HasDefaultValue("MA_2025");
                
                // Auto rules configuration (stored as JSON)
                entity.Property(sp => sp.AutoRulesJson).HasColumnType("nvarchar(max)");
                
                // CIMR configuration (stored as JSON)
                entity.Property(sp => sp.CimrConfigJson).HasColumnType("nvarchar(max)");
                
                // Origin tracking for copied templates
                entity.Property(sp => sp.OriginType).HasMaxLength(30);
                entity.Property(sp => sp.SourceTemplateNameSnapshot).HasMaxLength(200);
                
                // CIMR and insurance (Moroccan 2025 compliance) - Legacy fields
                entity.Property(sp => sp.CimrRate).HasColumnType("decimal(5,4)");
                entity.Property(sp => sp.HasPrivateInsurance).IsRequired().HasDefaultValue(false);
                
                // Versioning and template tracking
                entity.Property(sp => sp.Version).IsRequired().HasDefaultValue(1);
                entity.Property(sp => sp.IsLocked).IsRequired().HasDefaultValue(false);

                entity.HasOne(sp => sp.Company)
                    .WithMany()
                    .HasForeignKey(sp => sp.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(sp => sp.SourceTemplate)
                    .WithMany(sp => sp.ClonedPackages)
                    .HasForeignKey(sp => sp.SourceTemplateId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SalaryPackageItem>(entity =>
            {
                entity.ToTable("SalaryPackageItem");
                entity.HasKey(spi => spi.Id);
                entity.Property(spi => spi.Label).IsRequired().HasMaxLength(200);
                entity.Property(spi => spi.DefaultValue).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(spi => spi.SortOrder).IsRequired();
                
                // Moroccan regulatory fields (2026 compliance)
                entity.Property(spi => spi.Type).IsRequired().HasMaxLength(50).HasDefaultValue("allowance");
                entity.Property(spi => spi.IsTaxable).IsRequired().HasDefaultValue(true);
                entity.Property(spi => spi.IsSocial).IsRequired().HasDefaultValue(true);
                entity.Property(spi => spi.IsCIMR).IsRequired().HasDefaultValue(false);
                entity.Property(spi => spi.IsVariable).IsRequired().HasDefaultValue(false);
                entity.Property(spi => spi.ExemptionLimit).HasColumnType("decimal(18,2)");

                entity.HasOne(spi => spi.SalaryPackage)
                    .WithMany(sp => sp.Items)
                    .HasForeignKey(spi => spi.SalaryPackageId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(spi => spi.PayComponent)
                    .WithMany(pc => pc.PackageItems)
                    .HasForeignKey(spi => spi.PayComponentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(spi => spi.ReferentielElement)
                    .WithMany()
                    .HasForeignKey(spi => spi.ReferentielElementId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SalaryPackageAssignment>(entity =>
            {
                entity.ToTable("SalaryPackageAssignment");
                entity.HasKey(spa => spa.Id);
                entity.Property(spa => spa.EffectiveDate).IsRequired();
                entity.Property(spa => spa.PackageVersion).IsRequired().HasDefaultValue(1);

                entity.HasOne(spa => spa.SalaryPackage)
                    .WithMany(sp => sp.Assignments)
                    .HasForeignKey(spa => spa.SalaryPackageId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(spa => spa.Employee)
                    .WithMany()
                    .HasForeignKey(spa => spa.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(spa => spa.Contract)
                    .WithMany()
                    .HasForeignKey(spa => spa.ContractId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(spa => spa.EmployeeSalary)
                    .WithMany()
                    .HasForeignKey(spa => spa.EmployeeSalaryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EmployeeAddress>(entity =>
            {
                entity.ToTable("EmployeeAddress");
                entity.HasKey(ea => ea.Id);
                entity.Property(ea => ea.AddressLine1).IsRequired().HasMaxLength(500);
                entity.Property(ea => ea.AddressLine2).HasMaxLength(500);
                entity.Property(ea => ea.ZipCode).IsRequired().HasMaxLength(20);

                entity.HasOne(ea => ea.Employee)
                    .WithMany(e => e.Addresses)
                    .HasForeignKey(ea => ea.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ea => ea.City)
                    .WithMany()
                    .HasForeignKey(ea => ea.CityId)
                    .OnDelete(DeleteBehavior.Restrict);

            });

            modelBuilder.Entity<EmployeeDocument>(entity =>
            {
                entity.ToTable("EmployeeDocument");
                entity.HasKey(ed => ed.Id);
                entity.Property(ed => ed.Name).IsRequired().HasMaxLength(500);
                entity.Property(ed => ed.FilePath).IsRequired().HasMaxLength(1000);
                entity.Property(ed => ed.DocumentType).IsRequired().HasMaxLength(100);

                entity.HasOne(ed => ed.Employee)
                    .WithMany(e => e.Documents)
                    .HasForeignKey(ed => ed.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EmployeeEventLog>(entity =>
            {
                entity.ToTable("EmployeeEventLog");
                entity.HasKey(eel => eel.Id);

                entity.Property(eel => eel.eventName).IsRequired().HasMaxLength(200);
                entity.Property(eel => eel.oldValue).HasMaxLength(1000);
                entity.Property(eel => eel.newValue).HasMaxLength(1000);
                entity.Property(eel => eel.createdAt).IsRequired();
                entity.HasOne<Users>()
                    .WithMany()
                    .HasForeignKey(eel => eel.createdBy)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<Employee>()
                    .WithMany()
                    .HasForeignKey(eel => eel.employeeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EmployeeOvertime>(entity =>
            {
                entity.ToTable("EmployeeOvertimes");
                entity.HasKey(e => e.Id);

                // Index pour recherche par employé et date
                entity.HasIndex(e => new { e.EmployeeId, e.OvertimeDate, e.Status })
                      .HasDatabaseName("IX_EmployeeOvertime_EmployeeDate");

                // Index pour recherche par batch (split automatique)
                entity.HasIndex(e => e.SplitBatchId)
                      .HasDatabaseName("IX_EmployeeOvertime_SplitBatchId");

                // Index pour workflow
                entity.HasIndex(e => new { e.Status, e.CreatedAt })
                      .HasDatabaseName("IX_EmployeeOvertime_StatusDate");

                // Index soft delete
                entity.HasIndex(e => e.DeletedAt)
                      .HasDatabaseName("IX_EmployeeOvertime_DeletedAt");

                // Précisions décimales
                entity.Property(e => e.DurationInHours)
                      .HasPrecision(5, 2)
                      .IsRequired();

                entity.Property(e => e.RateMultiplierApplied)
                      .HasPrecision(5, 2)
                      .IsRequired();

                // Relation avec Employee
                entity.HasOne(e => e.Employee)
                      .WithMany()
                      .HasForeignKey(e => e.EmployeeId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relation avec OvertimeRateRule (obligatoire conceptuellement, nullable techniquement)
                entity.HasOne(e => e.RateRule)
                      .WithMany(r => r.OvertimeRecords)
                      .HasForeignKey(e => e.RateRuleId)
                      .OnDelete(DeleteBehavior.Restrict); // Ne JAMAIS supprimer une règle si overtimes l'utilisent

                // Query filter pour soft delete
                entity.HasQueryFilter(e => e.DeletedAt == null);
            });

            modelBuilder.Entity<EmployeeChild>(entity =>
            {
                entity.ToTable("EmployeeChild");
                entity.HasKey(ec => ec.Id);

                entity.Property(ec => ec.EmployeeId)
                    .HasColumnName("employee_id")
                    .IsRequired();

                entity.Property(ec => ec.FirstName)
                    .HasColumnName("first_name")
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(ec => ec.LastName)
                    .HasColumnName("last_name")
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(ec => ec.DateOfBirth)
                    .HasColumnName("date_of_birth")
                    .HasColumnType("date")
                    .IsRequired();

                entity.Property(ec => ec.GenderId)
                    .HasColumnName("gender_id");

                entity.Property(ec => ec.IsDependent)
                    .HasColumnName("is_dependent")
                    .HasDefaultValue(true);

                entity.Property(ec => ec.IsStudent)
                    .HasColumnName("is_student")
                    .HasDefaultValue(false);

                // Champs d'audit
                entity.Property(ec => ec.CreatedAt)
                    .HasColumnName("created_at")
                    .IsRequired();

                entity.Property(ec => ec.CreatedBy)
                    .HasColumnName("created_by")
                    .IsRequired();

                entity.Property(ec => ec.ModifiedAt)
                    .HasColumnName("modified_at");

                entity.Property(ec => ec.ModifiedBy)
                    .HasColumnName("modified_by");

                entity.Property(ec => ec.DeletedAt)
                    .HasColumnName("deleted_at");

                entity.Property(ec => ec.DeletedBy)
                    .HasColumnName("deleted_by");

                // Index
                entity.HasIndex(ec => ec.EmployeeId)
                    .HasDatabaseName("IX_EmployeeChild_EmployeeId");

                entity.HasIndex(ec => ec.DeletedAt)
                    .HasDatabaseName("IX_EmployeeChild_DeletedAt")
                    .HasFilter("[deleted_at] IS NULL");

                // Relations
                entity.HasOne(ec => ec.Employee)
                    .WithMany(e => e.Children)
                    .HasForeignKey(ec => ec.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_EmployeeChild_Employee");

                entity.HasOne(ec => ec.Gender)
                    .WithMany()
                    .HasForeignKey(ec => ec.GenderId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_EmployeeChild_Gender");
            });

            modelBuilder.Entity<EmployeeSpouse>(entity =>
            {
                entity.ToTable("EmployeeSpouse");
                entity.HasKey(es => es.Id);

                entity.Property(es => es.EmployeeId)
                    .HasColumnName("employee_id")
                    .IsRequired();

                entity.Property(es => es.FirstName)
                    .HasColumnName("first_name")
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(es => es.LastName)
                    .HasColumnName("last_name")
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(es => es.DateOfBirth)
                    .HasColumnName("date_of_birth")
                    .HasColumnType("date")
                    .IsRequired();

                entity.Property(es => es.GenderId)
                    .HasColumnName("gender_id");

                entity.Property(es => es.CinNumber)
                    .HasColumnName("cin_number")
                    .HasMaxLength(50);

                entity.Property(es => es.IsDependent)
                    .HasColumnName("is_dependent")
                    .HasDefaultValue(false);

                // Champs d'audit
                entity.Property(es => es.CreatedAt)
                    .HasColumnName("created_at")
                    .IsRequired();

                entity.Property(es => es.CreatedBy)
                    .HasColumnName("created_by")
                    .IsRequired();

                entity.Property(es => es.ModifiedAt)
                    .HasColumnName("modified_at");

                entity.Property(es => es.ModifiedBy)
                    .HasColumnName("modified_by");

                entity.Property(es => es.DeletedAt)
                    .HasColumnName("deleted_at");

                entity.Property(es => es.DeletedBy)
                    .HasColumnName("deleted_by");

                // Index
                entity.HasIndex(es => es.EmployeeId)
                    .HasDatabaseName("IX_EmployeeSpouse_EmployeeId");

                entity.HasIndex(es => es.CinNumber)
                    .IsUnique()
                    .HasDatabaseName("IX_EmployeeSpouse_CinNumber")
                    .HasFilter("[deleted_at] IS NULL AND [cin_number] IS NOT NULL");


                entity.HasIndex(es => es.DeletedAt)
                    .HasDatabaseName("IX_EmployeeSpouse_DeletedAt")
                    .HasFilter("[deleted_at] IS NULL");

                // Relations
                entity.HasOne(es => es.Employee)
                    .WithMany(e => e.Spouses)
                    .HasForeignKey(es => es.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_EmployeeSpouse_Employee");

                entity.HasOne(es => es.Gender)
                    .WithMany()
                    .HasForeignKey(es => es.GenderId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_EmployeeSpouse_Gender");
            });

            modelBuilder.Entity<EmployeeAttendance>(entity =>
            {
                entity.ToTable("EmployeeAttendance");
                entity.HasKey(ea => ea.Id);

                // Configuration des propriétés
                entity.Property(ea => ea.EmployeeId)
                    .HasColumnName("employee_id")
                    .IsRequired();

                entity.Property(ea => ea.WorkDate)
                    .HasColumnName("work_date")
                    .HasColumnType("date")
                    .IsRequired();

                entity.Property(ea => ea.CheckIn)
                    .HasColumnName("check_in")
                    .HasColumnType("time");

                entity.Property(ea => ea.CheckOut)
                    .HasColumnName("check_out")
                    .HasColumnType("time");

                entity.Property(ea => ea.BreakMinutesApplied)
                    .HasColumnName("break_minutes_applied")
                    .IsRequired()
                    .HasDefaultValue(0);

                entity.Property(ea => ea.WorkedHours)
                    .HasColumnName("worked_hours")
                    .HasColumnType("decimal(5,2)")
                    .IsRequired()
                    .HasDefaultValue(0);

                entity.Property(ea => ea.Status)
                    .HasColumnName("status")
                    .IsRequired()
                    .HasConversion<int>();

                entity.Property(ea => ea.Source)
                    .HasColumnName("source")
                    .IsRequired()
                    .HasConversion<int>();

                // Champs d'audit
                entity.Property(ea => ea.CreatedAt)
                    .HasColumnName("created_at")
                    .IsRequired();

                entity.Property(ea => ea.CreatedBy)
                    .HasColumnName("created_by")
                    .IsRequired();

                entity.Property(ea => ea.ModifiedAt)
                    .HasColumnName("modified_at");

                entity.Property(ea => ea.ModifiedBy)
                    .HasColumnName("modified_by");

                // Index pour contrainte d'unicité : un seul enregistrement par employé par date
                entity.HasIndex(ea => new { ea.EmployeeId, ea.WorkDate })
                    .IsUnique()
                    .HasDatabaseName("IX_EmployeeAttendance_Employee_WorkDate");

                // Index pour les requêtes par date
                entity.HasIndex(ea => ea.WorkDate)
                    .HasDatabaseName("IX_EmployeeAttendance_WorkDate");

                // Index pour les requêtes par employé
                entity.HasIndex(ea => ea.EmployeeId)
                    .HasDatabaseName("IX_EmployeeAttendance_EmployeeId");

                // Relation avec Employee
                entity.HasOne(ea => ea.Employee)
                    .WithMany(e => e.Attendances)
                    .HasForeignKey(ea => ea.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_EmployeeAttendance_Employee");
            });

            modelBuilder.Entity<EmployeeAttendanceBreak>(entity =>
            {
                entity.ToTable("EmployeeAttendanceBreak");
                entity.HasKey(eab => eab.Id);

                // Configuration des propriétés
                entity.Property(eab => eab.EmployeeAttendanceId)
                    .HasColumnName("employee_attendance_id")
                    .IsRequired();

                entity.Property(eab => eab.BreakStart)
                    .HasColumnName("break_start")
                    .HasColumnType("time")
                    .IsRequired();

                entity.Property(eab => eab.BreakEnd)
                    .HasColumnName("break_end")
                    .HasColumnType("time");

                entity.Property(eab => eab.BreakType)
                    .HasColumnName("break_type")
                    .HasMaxLength(100);

                // Champs d'audit
                entity.Property(eab => eab.CreatedAt)
                    .HasColumnName("created_at")
                    .IsRequired();

                entity.Property(eab => eab.CreatedBy)
                    .HasColumnName("created_by")
                    .IsRequired();

                entity.Property(eab => eab.ModifiedAt)
                    .HasColumnName("modified_at");

                entity.Property(eab => eab.ModifiedBy)
                    .HasColumnName("modified_by");

                // Index pour les requêtes par assiduité
                entity.HasIndex(eab => eab.EmployeeAttendanceId)
                    .HasDatabaseName("IX_EmployeeAttendanceBreak_AttendanceId");

                // Relation avec EmployeeAttendance
                entity.HasOne(eab => eab.EmployeeAttendance)
                    .WithMany(ea => ea.Breaks)
                    .HasForeignKey(eab => eab.EmployeeAttendanceId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_EmployeeAttendanceBreak_EmployeeAttendance");
            });

            modelBuilder.Entity<EmployeeAbsence>(entity =>
            {
                entity.ToTable("EmployeeAbsence");
                entity.HasKey(ea => ea.Id);

                // Propriétés principales
                entity.Property(ea => ea.EmployeeId)
                    .HasColumnName("employee_id")
                    .IsRequired();

                entity.Property(ea => ea.AbsenceDate)
                    .HasColumnName("absence_date")
                    .HasColumnType("date")
                    .IsRequired();

                entity.Property(ea => ea.DurationType)
                    .HasColumnName("duration_type")
                    .IsRequired()
                    .HasConversion<int>();

                // Decision d'absence
                entity.Property(ea => ea.Status)
                    .HasColumnName("status")
                    .IsRequired()
                    .HasConversion<int>()
                    .HasDefaultValue(AbsenceStatus.Draft);

                entity.Property(ea => ea.DecisionAt)
                    .HasColumnName("decision_at");

                entity.Property(ea => ea.DecisionBy)
                    .HasColumnName("decision_by");

                entity.Property(ea => ea.DecisionComment)
                    .HasColumnName("decision_comment")
                    .HasMaxLength(1000);

                // ---- Demi-journée ----
                entity.Property(ea => ea.IsMorning)
                    .HasColumnName("is_morning");

                // ---- Tranche horaire ----
                entity.Property(ea => ea.StartTime)
                    .HasColumnName("start_time")
                    .HasColumnType("time");

                entity.Property(ea => ea.EndTime)
                    .HasColumnName("end_time")
                    .HasColumnType("time");

                entity.Property(ea => ea.AbsenceType)
                    .HasColumnName("absence_type")
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(ea => ea.Reason)
                    .HasColumnName("reason")
                    .HasMaxLength(500);

                // Champs d'audit
                entity.Property(ea => ea.CreatedAt)
                    .HasColumnName("created_at")
                    .IsRequired();

                entity.Property(ea => ea.CreatedBy)
                    .HasColumnName("created_by")
                    .IsRequired();

                // Indexes utiles
                entity.HasIndex(ea => new { ea.EmployeeId, ea.AbsenceDate })
                    .HasDatabaseName("IX_EmployeeAbsence_Employee_Date");

                entity.HasIndex(ea => ea.AbsenceDate)
                    .HasDatabaseName("IX_EmployeeAbsence_Date");

                entity.HasIndex(ea => ea.EmployeeId)
                    .HasDatabaseName("IX_EmployeeAbsence_EmployeeId");

                entity.HasIndex(ea => ea.AbsenceType)
                    .HasDatabaseName("IX_EmployeeAbsence_Type");

                // Relation avec Employee
                entity.HasOne(ea => ea.Employee)
                    .WithMany()
                    .HasForeignKey(ea => ea.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_EmployeeAbsence_Employee");
            });

            // ======================================================
            // ============ Configuration des tables Leave ==========
            // ======================================================

            modelBuilder.Entity<LeaveType>(entity =>
            {
                entity.ToTable("LeaveType");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.LeaveCode)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.LeaveNameAr)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.LeaveNameEn)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.LeaveNameFr)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.LeaveDescription)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.HasIndex(e => e.LeaveCode)
                    .HasDatabaseName("idx_LeaveType_LeaveCode");

                entity.HasIndex(e => new { e.CompanyId, e.LeaveCode })
                    .IsUnique()
                    .HasDatabaseName("ux_LeaveType_Company_LeaveCode");

                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =====================================================
            // LeaveTypeLegalRule
            // =====================================================
            modelBuilder.Entity<LeaveTypeLegalRule>(entity =>
            {
                entity.ToTable("LeaveTypeLegalRule");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.EventCaseCode)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(300);

                entity.Property(e => e.LegalArticle)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(e => new { e.LeaveTypeId, e.EventCaseCode })
                    .IsUnique()
                    .HasDatabaseName("ux_LegalRule_Type_Case");

                entity.HasOne(e => e.LeaveType)
                    .WithMany(t => t.LegalRules)
                    .HasForeignKey(e => e.LeaveTypeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =====================================================
            // LeaveTypePolicy
            // =====================================================
            modelBuilder.Entity<LeaveTypePolicy>(entity =>
            {
                entity.ToTable("LeaveTypePolicy");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.CompanyId, e.LeaveTypeId })
                    .IsUnique()
                    .HasDatabaseName("ux_LeaveTypePolicy_Company_LeaveType");

                entity.HasIndex(e => e.CompanyId)
                    .HasDatabaseName("idx_LeaveTypePolicy_CompanyId");

                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.LeaveType)
                    .WithMany(t => t.Policies)
                    .HasForeignKey(e => e.LeaveTypeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =====================================================
            // LeaveRequest
            // =====================================================
            modelBuilder.Entity<LeaveRequest>(entity =>
            {
                entity.ToTable("LeaveRequests");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.CompanyId, e.Status })
                    .HasDatabaseName("idx_LeaveRequests_Company_Status");

                entity.HasIndex(e => e.EmployeeId)
                    .HasDatabaseName("idx_LeaveRequests_EmployeeId");

                entity.HasIndex(e => new { e.CompanyId, e.StartDate })
                    .HasDatabaseName("idx_LeaveRequests_Company_StartDate");

                entity.HasOne(e => e.Employee)
                    .WithMany()
                    .HasForeignKey(e => e.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.LeaveType)
                    .WithMany()
                    .HasForeignKey(e => e.LeaveTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.LegalRule)
                    .WithMany()
                    .HasForeignKey(e => e.LegalRuleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Policy)
                    .WithMany()
                    .HasForeignKey(e => e.PolicyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =====================================================
            // LeaveRequestApprovalHistory
            // =====================================================
            modelBuilder.Entity<LeaveRequestApprovalHistory>(entity =>
            {
                entity.ToTable("LeaveRequestApprovalHistory");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.LeaveRequestId)
                    .HasDatabaseName("idx_LeaveRequestApprovalHistory_RequestId");

                entity.HasOne(e => e.LeaveRequest)
                    .WithMany(r => r.ApprovalHistory)
                    .HasForeignKey(e => e.LeaveRequestId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =====================================================
            // LeaveRequestAttachment
            // =====================================================
            modelBuilder.Entity<LeaveRequestAttachment>(entity =>
            {
                entity.ToTable("LeaveRequestAttachments");

                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.LeaveRequest)
                    .WithMany(r => r.Attachments)
                    .HasForeignKey(e => e.LeaveRequestId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =====================================================
            // LeaveRequestExemption
            // =====================================================
            modelBuilder.Entity<LeaveRequestExemption>(entity =>
            {
                entity.ToTable("LeaveRequestExemptions");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.LeaveRequestId, e.ExemptionDate })
                    .IsUnique()
                    .HasDatabaseName("ux_LeaveRequestExemptions_Request_Date");

                entity.HasIndex(e => e.ExemptionDate)
                    .HasDatabaseName("idx_LeaveRequestExemptions_Date");

                entity.HasOne(e => e.LeaveRequest)
                    .WithMany(r => r.Exemptions)
                    .HasForeignKey(e => e.LeaveRequestId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // =====================================================
            // LeaveBalance
            // =====================================================
            modelBuilder.Entity<LeaveBalance>(entity =>
            {
                entity.ToTable("LeaveBalance");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.CompanyId, e.Year, e.Month })
                    .HasDatabaseName("idx_LeaveBalance_Company_Year_Month");

                entity.HasOne(e => e.Employee)
                    .WithMany()
                    .HasForeignKey(e => e.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.LeaveType)
                    .WithMany()
                    .HasForeignKey(e => e.LeaveTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =====================================================
            // LeaveCarryOverAgreement
            // =====================================================
            modelBuilder.Entity<LeaveCarryOverAgreement>(entity =>
            {
                entity.ToTable("LeaveCarryOverAgreements");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.EmployeeId, e.LeaveTypeId, e.FromYear, e.ToYear })
                    .IsUnique()
                    .HasDatabaseName("ux_LeaveCarryOverAgreements_Emp_Type_Years");

                entity.HasIndex(e => e.CompanyId)
                    .HasDatabaseName("idx_LeaveCarryOverAgreements_CompanyId");

                entity.HasOne(e => e.Employee)
                    .WithMany()
                    .HasForeignKey(e => e.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.LeaveType)
                    .WithMany()
                    .HasForeignKey(e => e.LeaveTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // =====================================================
            // LeaveAuditLog
            // =====================================================
            modelBuilder.Entity<LeaveAuditLog>(entity =>
            {
                entity.ToTable("LeaveAuditLog");

                entity.HasKey(e => e.Id);

                entity.HasIndex(e => new { e.CompanyId, e.CreatedAt })
                    .HasDatabaseName("idx_LeaveAuditLog_Company_CreatedAt");

                entity.HasIndex(e => e.LeaveRequestId)
                    .HasDatabaseName("idx_LeaveAuditLog_LeaveRequestId");

                entity.HasOne(e => e.Company)
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Employee)
                    .WithMany()
                    .HasForeignKey(e => e.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.LeaveRequest)
                    .WithMany()
                    .HasForeignKey(e => e.LeaveRequestId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ========================================
            // CONFIGURATION PAYROLL REFERENTIEL
            // ========================================

            modelBuilder.Entity<Authority>(entity =>
            {
                entity.ToTable("Authorities");
                entity.HasKey(a => a.Id);
                
                entity.Property(a => a.Code).IsRequired().HasMaxLength(50);
                entity.Property(a => a.Name).IsRequired().HasMaxLength(255);
                entity.Property(a => a.Description).HasColumnType("nvarchar(max)");
                entity.Property(a => a.SortOrder).HasDefaultValue(0);
                entity.Property(a => a.IsActive).HasDefaultValue(true);
                entity.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasIndex(a => a.Code).IsUnique().HasFilter("[DeletedAt] IS NULL");
            });

            modelBuilder.Entity<ElementCategory>(entity =>
            {
                entity.ToTable("ElementCategories");
                entity.HasKey(ec => ec.Id);
                
                entity.Property(ec => ec.Name).IsRequired().HasMaxLength(255);
                entity.Property(ec => ec.Description).HasColumnType("nvarchar(max)");
                entity.Property(ec => ec.SortOrder).HasDefaultValue(0);
                entity.Property(ec => ec.IsActive).HasDefaultValue(true);
                entity.Property(ec => ec.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<EligibilityCriteria>(entity =>
            {
                entity.ToTable("EligibilityCriteria");
                entity.HasKey(ec => ec.Id);
                
                entity.Property(ec => ec.Code).IsRequired().HasMaxLength(50);
                entity.Property(ec => ec.Name).IsRequired().HasMaxLength(255);
                entity.Property(ec => ec.Description).HasColumnType("nvarchar(max)");
                entity.Property(ec => ec.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasIndex(ec => ec.Code).IsUnique().HasFilter("[DeletedAt] IS NULL");
            });

            modelBuilder.Entity<LegalParameter>(entity =>
            {
                entity.ToTable("LegalParameters");
                entity.HasKey(lp => lp.Id);

                entity.Property(lp => lp.Code).IsRequired().HasMaxLength(50);
                entity.Property(lp => lp.Label).IsRequired().HasMaxLength(255);
                entity.Property(lp => lp.Value).HasColumnType("decimal(18,4)");
                entity.Property(lp => lp.Unit).IsRequired().HasMaxLength(50);
                entity.Property(lp => lp.Source).HasMaxLength(500);
                entity.Property(lp => lp.EffectiveFrom).HasColumnType("date");
                entity.Property(lp => lp.EffectiveTo).HasColumnType("date");
                entity.Property(lp => lp.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(lp => new { lp.Code, lp.EffectiveFrom }).IsUnique().HasFilter("[DeletedAt] IS NULL");
            });

            modelBuilder.Entity<ReferentielElement>(entity =>
            {
                entity.ToTable("ReferentielElements");
                entity.HasKey(re => re.Id);
                
                entity.Property(re => re.Name).IsRequired().HasMaxLength(255);
                entity.Property(re => re.Description).HasColumnType("nvarchar(max)");
                entity.Property(re => re.DefaultFrequency).HasConversion<string>().HasMaxLength(20);
                entity.Property(re => re.IsActive).HasDefaultValue(true);
                entity.Property(re => re.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(re => re.Category)
                    .WithMany(c => c.Elements)
                    .HasForeignKey(re => re.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Phase 1.3: unique (Name, CategoryId) within non-deleted elements
                entity.HasIndex(re => new { re.Name, re.CategoryId })
                    .IsUnique()
                    .HasFilter("[DeletedAt] IS NULL");
            });

            modelBuilder.Entity<ElementRule>(entity =>
            {
                entity.ToTable("ElementRules");
                entity.HasKey(er => er.Id);
                
                entity.Property(er => er.ExemptionType).HasConversion<string>().HasMaxLength(30);
                entity.Property(er => er.SourceRef).HasMaxLength(500);
                entity.Property(er => er.EffectiveFrom).HasColumnType("date");
                entity.Property(er => er.EffectiveTo).HasColumnType("date");
                entity.Property(er => er.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasIndex(er => new { er.ElementId, er.AuthorityId, er.EffectiveFrom }).IsUnique().HasFilter("[DeletedAt] IS NULL");
                
                entity.HasOne(er => er.Element)
                    .WithMany(e => e.Rules)
                    .HasForeignKey(er => er.ElementId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(er => er.Authority)
                    .WithMany(a => a.ElementRules)
                    .HasForeignKey(er => er.AuthorityId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RuleCap>(entity =>
            {
                entity.ToTable("RuleCaps");
                entity.HasKey(rc => rc.Id);
                
                entity.Property(rc => rc.CapAmount).HasColumnType("decimal(18,4)");
                entity.Property(rc => rc.CapUnit).HasConversion<string>().HasMaxLength(20);
                entity.Property(rc => rc.MinAmount).HasColumnType("decimal(18,4)");
                
                entity.HasIndex(rc => rc.RuleId).IsUnique();
                
                entity.HasOne(rc => rc.Rule)
                    .WithOne(r => r.Cap)
                    .HasForeignKey<RuleCap>(rc => rc.RuleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RulePercentage>(entity =>
            {
                entity.ToTable("RulePercentages");
                entity.HasKey(rp => rp.Id);
                
                entity.Property(rp => rp.Percentage).HasColumnType("decimal(5,4)");
                entity.Property(rp => rp.BaseReference).HasConversion<string>().HasMaxLength(30);
                
                entity.HasIndex(rp => rp.RuleId).IsUnique();
                
                entity.HasOne(rp => rp.Rule)
                    .WithOne(r => r.Percentage)
                    .HasForeignKey<RulePercentage>(rp => rp.RuleId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(rp => rp.Eligibility)
                    .WithMany(e => e.RulePercentages)
                    .HasForeignKey(rp => rp.EligibilityId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<RuleFormula>(entity =>
            {
                entity.ToTable("RuleFormulas");
                entity.HasKey(rf => rf.Id);

                entity.Property(rf => rf.Multiplier).HasColumnType("decimal(10,4)");
                entity.Property(rf => rf.ResultUnit).HasConversion<string>().HasMaxLength(20);

                entity.HasIndex(rf => rf.RuleId).IsUnique();

                entity.HasOne(rf => rf.Rule)
                    .WithOne(r => r.Formula)
                    .HasForeignKey<RuleFormula>(rf => rf.RuleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rf => rf.Parameter)
                    .WithMany(p => p.RuleFormulas)
                    .HasForeignKey(rf => rf.ParameterId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RuleDualCap>(entity =>
            {
                entity.ToTable("RuleDualCaps");
                entity.HasKey(rdc => rdc.Id);

                entity.Property(rdc => rdc.FixedCapAmount).HasColumnType("decimal(18,4)");
                entity.Property(rdc => rdc.FixedCapUnit).HasConversion<string>().HasMaxLength(20);
                entity.Property(rdc => rdc.PercentageCap).HasColumnType("decimal(5,4)");
                entity.Property(rdc => rdc.BaseReference).HasConversion<string>().HasMaxLength(30);
                entity.Property(rdc => rdc.Logic).HasConversion<string>().HasMaxLength(10);

                entity.HasIndex(rdc => rdc.RuleId).IsUnique();

                entity.HasOne(rdc => rdc.Rule)
                    .WithOne(r => r.DualCap)
                    .HasForeignKey<RuleDualCap>(rdc => rdc.RuleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RuleTier>(entity =>
            {
                entity.ToTable("RuleTiers");
                entity.HasKey(rt => rt.Id);
                
                entity.Property(rt => rt.FromAmount).HasColumnType("decimal(18,4)");
                entity.Property(rt => rt.ToAmount).HasColumnType("decimal(18,4)");
                entity.Property(rt => rt.ExemptPercent).HasColumnType("decimal(5,4)");
                
                entity.HasIndex(rt => new { rt.RuleId, rt.TierOrder }).IsUnique();
                
                entity.HasOne(rt => rt.Rule)
                    .WithMany(r => r.Tiers)
                    .HasForeignKey(rt => rt.RuleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RuleVariant>(entity =>
            {
                entity.ToTable("RuleVariants");
                entity.HasKey(rv => rv.Id);
                
                entity.Property(rv => rv.VariantType).IsRequired().HasMaxLength(50);
                entity.Property(rv => rv.VariantKey).IsRequired().HasMaxLength(50);
                entity.Property(rv => rv.VariantLabel).IsRequired().HasMaxLength(255);
                entity.Property(rv => rv.OverrideCap).HasColumnType("decimal(18,4)");
                entity.Property(rv => rv.OverridePercentage).HasColumnType("decimal(5,4)");
                entity.Property(rv => rv.SortOrder).HasDefaultValue(0);
                
                entity.HasIndex(rv => new { rv.RuleId, rv.VariantType, rv.VariantKey }).IsUnique();
                
                entity.HasOne(rv => rv.Rule)
                    .WithMany(r => r.Variants)
                    .HasForeignKey(rv => rv.RuleId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(rv => rv.Eligibility)
                    .WithMany(e => e.RuleVariants)
                    .HasForeignKey(rv => rv.EligibilityId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<AncienneteRateSet>(entity =>
            {
                entity.ToTable("AncienneteRateSets");
                entity.HasKey(ars => ars.Id);

                entity.Property(ars => ars.Name).IsRequired().HasMaxLength(255);
                entity.Property(ars => ars.Code).IsRequired().HasMaxLength(100);
                entity.Property(ars => ars.IsLegalDefault).HasDefaultValue(false);
                entity.Property(ars => ars.Source).HasMaxLength(500);
                entity.Property(ars => ars.EffectiveFrom).HasColumnType("date");
                entity.Property(ars => ars.EffectiveTo).HasColumnType("date");
                entity.Property(ars => ars.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // CompanyId: null for legal default, non-null for company-specific
                entity.HasOne(ars => ars.Company)
                    .WithMany()
                    .HasForeignKey(ars => ars.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                // ClonedFromId: reference to the source rate set
                entity.HasOne(ars => ars.ClonedFrom)
                    .WithMany(ars => ars.ClonedTo)
                    .HasForeignKey(ars => ars.ClonedFromId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Unique constraint: one active rate set per company (or one legal default)
                // CompanyId + EffectiveFrom must be unique (for non-deleted records)
                entity.HasIndex(ars => new { ars.CompanyId, ars.EffectiveFrom })
                    .IsUnique()
                    .HasFilter("[DeletedAt] IS NULL");
            });

            modelBuilder.Entity<AncienneteRate>(entity =>
            {
                entity.ToTable("AncienneteRates");
                entity.HasKey(ar => ar.Id);

                entity.Property(ar => ar.Rate).HasColumnType("decimal(5,4)");

                entity.HasIndex(ar => new { ar.RateSetId, ar.SortOrder }).IsUnique();

                entity.HasOne(ar => ar.RateSet)
                    .WithMany(rs => rs.Rates)
                    .HasForeignKey(ar => ar.RateSetId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CompanyDocument>(entity =>
            {
                entity.ToTable("CompanyDocument");
                entity.HasKey(cd => cd.Id);

                entity.Property(cd => cd.Name).IsRequired().HasMaxLength(500);
                entity.Property(cd => cd.FilePath).IsRequired().HasMaxLength(1000);
                entity.Property(cd => cd.DocumentType).HasMaxLength(100);

                entity.Property(cd => cd.CreatedAt).HasColumnName("created_at");
                entity.Property(cd => cd.CreatedBy).HasColumnName("created_by");
                entity.Property(cd => cd.DeletedAt).HasColumnName("deleted_at");
                entity.Property(cd => cd.DeletedBy).HasColumnName("deleted_by");

                entity.HasOne(cd => cd.Company)
                      .WithMany(c => c.Documents)
                      .HasForeignKey(cd => cd.CompanyId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BusinessSector>(entity =>
            {
                entity.ToTable("BusinessSectors");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.IsStandard)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.SortOrder)
                    .IsRequired()
                    .HasDefaultValue(0);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.CreatedBy)
                    .IsRequired();

                // Unique index on Code (case-insensitive, excluding soft-deleted)
                entity.HasIndex(e => e.Code)
                    .IsUnique()
                    .HasDatabaseName("IX_BusinessSectors_Code")
                    .HasFilter("[DeletedAt] IS NULL");
            });
        }
    }
}
