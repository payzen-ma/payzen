using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payzen.Application.Interfaces;
using Payzen.Infrastructure.Persistence;
using Payzen.Infrastructure.Services;
using Payzen.Infrastructure.Services.Auth;
using Payzen.Infrastructure.Services.Company;
using Payzen.Infrastructure.Services.Company.Defaults;
using Payzen.Infrastructure.Services.Dashboard;
using Payzen.Infrastructure.Services.Documents;
using Payzen.Infrastructure.Services.Email;
using Payzen.Infrastructure.Services.Employee;
using Payzen.Infrastructure.Services.Employee.Breaks;
using Payzen.Infrastructure.Services.EventLog;
using Payzen.Infrastructure.Services.Leave;
using Payzen.Infrastructure.Services.LLM;
using Payzen.Infrastructure.Services.Payroll;
using Payzen.Infrastructure.Services.Public;
using Payzen.Infrastructure.Services.Referentiel;
using Payzen.Infrastructure.Services.Timesheet;
using Payzen.Infrastructure.Services.Absence;

namespace Payzen.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Enregistre toutes les implémentations Infrastructure.
    /// À appeler dans Payzen.Api/Program.cs : builder.Services.AddInfrastructure(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ── EF Core ──────────────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly("Payzen.Infrastructure")
            )
        );

        // ── HTTP Client (Gemini LLM) ──────────────────────────────────────────
        services.AddHttpClient(
            "Gemini",
            client =>
            {
                // L'URL de base n'est pas configurée ici car elle varie avec le modèle,
            // utilisée directement lors de l'appel HTTP.
                client.Timeout = TimeSpan.FromSeconds(120);
            }
        );

        // ── Auth ─────────────────────────────────────────────────────────────
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserInviteService, UserInviteService>();
        services.AddScoped<IInvitationService, InvitationService>();
        services.AddScoped<IIdentityProvisioningService, IdentityProvisioningService>();

        // ── Company ──────────────────────────────────────────────────────────
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<ICompanyOnboardingService, CompanyOnboardingService>();
        services.AddScoped<ICompanyDocumentService, CompanyDocumentService>();
        services.AddScoped<ICompanyDefaultsSeeder, CompanyDefaultsSeederService>();

        // ── Public signup / onboarding ──────────────────────────────────────
        services.AddScoped<IPublicSignupService, PublicSignupService>();

        // ── Employee ─────────────────────────────────────────────────────────
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IEmployeeContractService, EmployeeContractService>();
        services.AddScoped<IEmployeeSalaryService, EmployeeSalaryService>();
        services.AddScoped<IEmployeeDocumentService, EmployeeDocumentService>();
        services.AddScoped<IEmployeeAddressService, EmployeeAddressService>();
        services.AddScoped<IEmployeeFamilyService, EmployeeFamilyService>();
        services.AddScoped<IEmployeeAttendanceService, EmployeeAttendanceService>();
        services.AddScoped<IEmployeeAbsenceService, EmployeeAbsenceService>();
        services.AddScoped<IEmployeeOvertimeService, EmployeeOvertimeService>();
        services.AddScoped<IEmployeeAttendanceBreakService, AttendanceBreakService>();

        // ── Leave ─────────────────────────────────────────────────────────────
        services.AddScoped<ILeaveBalanceRecalculationService, LeaveBalanceRecalculationService>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<ILeaveTypeService, LeaveTypeService>();
        services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();
        services.AddScoped<ILeaveAuditLogService, LeaveAuditLogService>();
        // ── Payroll ───────────────────────────────────────────────────────────
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<ISalaryPackageService, SalaryPackageService>();
        services.AddScoped<IPayrollExportService, PayrollExportService>();
        services.AddScoped<IPayComponentService, PayComponentService>();
        services.AddScoped<IReferentielPayrollService, ReferentielPayrollService>();
        services.AddScoped<IConvergenceService, ConvergenceAnalysisService>();

        // ── Email ─────────────────────────────────────────────────────────────
        services.AddScoped<IEmailService, EmailService>();

        // ── Referentiel ───────────────────────────────────────────────────────
        services.AddScoped<IReferentielService, ReferentielService>();

        // ── Dashboard ─────────────────────────────────────────────────────────
        services.AddScoped<IDashboardService, DashboardService>();

        // ── LLM (Gemini) ───────────────────────────────────────────────────────
        services.AddScoped<ILlmService, GeminiService>();

        // ── Timesheet Import ───────────────────────────────────────────────────
        services.AddScoped<ITimesheetImportService, TimesheetImportService>();

        // ── Absence Import ────────────────────────────────────────────────────
        services.AddScoped<IAbsenceImportService, AbsenceImportService>();

        // ── Documents ─────────────────────────────────────────────────────────
        services.AddScoped<IDocumentService, IronPdfDocumentService>();

        // ── Event Logs ────────────────────────────────────────────────────────
        services.AddScoped<ICompanyEventLogService, CompanyEventLogService>();
        services.AddScoped<IEmployeeEventLogService, EmployeeEventLogService>();
        services.AddScoped<ILeaveEventLogService, LeaveEventLogService>();

        // ── Utilities ─────────────────────────────────────────────────────────
        services.AddScoped<IWorkingDaysCalculator, WorkingDaysCalculatorService>();
        services.AddScoped<IElementRuleResolutionService, ElementRuleResolutionService>();

        return services;
    }
}
