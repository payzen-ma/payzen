using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Common.LeaveStatus;
using payzen_backend.Models.Common.OvertimeEnums;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Employee.Dtos;
using payzen_backend.Models.Leave;
using payzen_backend.Services;

namespace payzen_backend.Services.Payroll
{
    public class EmployeePayrollDataService
{
    private readonly AppDbContext _db;
    private readonly WorkingDaysCalculator _workingDaysCalculator;

    public EmployeePayrollDataService(AppDbContext db, WorkingDaysCalculator workingDaysCalculator)
    {
        _db = db;
        _workingDaysCalculator = workingDaysCalculator;
    }

    public async Task<EmployeePayrollDto> BuildPayrollDataAsync(int employeeId, int month, int year)
    {
        var startOfMonth = new DateTime(year, month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        // 1. Données de base de l'employé
        var employee = await _db.Employees
            .Include(e => e.MaritalStatus)
            .Include(e => e.Children)
            .Include(e => e.Spouses)
            .FirstOrDefaultAsync(e => e.Id == employeeId)
            ?? throw new Exception($"Employé {employeeId} introuvable.");

        // 2. Contrat actif
        var contract = await _db.EmployeeContracts
            .Include(ec => ec.ContractType)
                .ThenInclude(ct => ct.LegalContractType)
            .Include(ec => ec.ContractType)
                .ThenInclude(ct => ct.StateEmploymentProgram)
            .Include(ec => ec.JobPosition)
            .Where(ec => ec.EmployeeId == employeeId && ec.StartDate <= startOfMonth
                         && (ec.EndDate == null || ec.EndDate >= startOfMonth))
            .OrderByDescending(ec => ec.StartDate)
            .FirstOrDefaultAsync();
        
        if (contract == null)
        {
            throw new Exception($"Aucun contrat trouvé pour l'employé {employee.FirstName} {employee.LastName} au cours du mois de paie.");
        }

        // 3. Salaire actif (ou ayant été actif) au cours du mois de paie
        var salary = await _db.EmployeeSalaries
            .Include(es => es.Components)
            .Where(es =>
                es.EmployeeId == employeeId
                && es.EffectiveDate <= endOfMonth
                && (es.EndDate == null || es.EndDate >= startOfMonth))
            .OrderByDescending(es => es.EffectiveDate)
            .FirstOrDefaultAsync();
        
        if (salary == null)
        {
            throw new Exception($"Aucun salaire trouvé pour l'employé {employee.FirstName} {employee.LastName} au cours du mois de paie.");
        }

        // 4. Package salarial actif (ou ayant été actif) au cours du mois de paie
        // LOGIQUE : On prend le package dont l'EffectiveDate est <= à la FIN du mois
        // et dont la date de fin est nulle ou postérieure au début du mois
        var packageAssignment = await _db.SalaryPackageAssignments
            .Include(spa => spa.SalaryPackage)
                .ThenInclude(sp => sp.Items)
                    .ThenInclude(spi => spi.PayComponent)
            .Where(spa => spa.EmployeeId == employeeId 
                && spa.EffectiveDate <= endOfMonth
                && (spa.EndDate == null || spa.EndDate >= startOfMonth)
                && spa.DeletedAt == null)
            .OrderByDescending(spa => spa.EffectiveDate)
            .FirstOrDefaultAsync();
        
        // 📊id SALARY COMPONENTS (ancien système)
        if (salary?.Components != null && salary.Components.Any())
        {
            var compImposables = salary.Components.Where(c => c.IsTaxable).ToList();
            var compNonImposables = salary.Components.Where(c => !c.IsTaxable).ToList();
        }
        
        // 📦 PACKAGE ITEMS (nouveau système)
        if (packageAssignment?.SalaryPackage?.Items != null && packageAssignment.SalaryPackage.Items.Any())
        {
            var imposables = packageAssignment.SalaryPackage.Items.Where(i => i.IsTaxable).ToList();
            var nonImposables = packageAssignment.SalaryPackage.Items.Where(i => !i.IsTaxable).ToList();
        }
        
        if ((salary?.Components == null || !salary.Components.Any()) && 
            (packageAssignment?.SalaryPackage?.Items == null || !packageAssignment.SalaryPackage.Items.Any()))
        {
            // Aucune prime trouvée (ni SalaryComponents, ni PackageItems)
        }

        // 5. Absences du mois (approuvées uniquement — impact sur la paie)
        var absences = await _db.EmployeeAbsences
            .Where(ea => ea.EmployeeId == employeeId
                         && ea.AbsenceDate >= DateOnly.FromDateTime(startOfMonth)
                         && ea.AbsenceDate <= DateOnly.FromDateTime(endOfMonth)
                         && ea.Status == AbsenceStatus.Approved
                         && ea.DeletedAt == null)
            .ToListAsync();
        // 6. Heures supplémentaires du mois
        var overtimes = await _db.EmployeeOvertimes
            .Where(eo => eo.EmployeeId == employeeId
                         && eo.OvertimeDate >= DateOnly.FromDateTime(startOfMonth)
                         && eo.OvertimeDate <= DateOnly.FromDateTime(endOfMonth)
                         && eo.Status == OvertimeStatus.Approved)
            .ToListAsync();

        // 7. Congés approuvés qui chevauchent le mois (jours proratisés au mois de paie)
        var startDateMonth = DateOnly.FromDateTime(startOfMonth);
        var endDateMonth = DateOnly.FromDateTime(endOfMonth);
        var leaves = await _db.LeaveRequests
            .Include(lr => lr.LeaveType)
            .Where(lr => lr.EmployeeId == employeeId
                         && lr.Status == LeaveRequestStatus.Approved
                         && lr.DeletedAt == null
                         && lr.StartDate <= endDateMonth
                         && lr.EndDate >= startDateMonth)
            .ToListAsync();

        // 8. Ancienneté
        var ancienneteYears = contract != null
            ? (int)((startOfMonth - contract.StartDate).TotalDays / 365)
            : 0;

        // 9. Assembler le DTO
        // Règle : la source de vérité pour les primes est EmployeeSalaryComponent (copie du package ou saisie manuelle).
        // Si l'employé a des composants, on n'envoie QUE ceux-ci au calcul pour éviter la double addition
        // (PackageItems + SalaryComponents qui seraient les mêmes après copie du package).
        var hasSalaryComponents = salary?.Components != null && salary.Components.Any();
        var salaryComponentsList = salary?.Components?.Select(c => new PayrollSalaryComponentDto
        {
            ComponentType = c.ComponentType,
            Amount = c.Amount,
            IsTaxable = c.IsTaxable,
            IsSocial = c.IsSocial,
            IsCIMR = c.IsCIMR
        }).ToList() ?? new();
        var packageItemsList = hasSalaryComponents
            ? new List<PayrollPackageItemDto>()
            : (packageAssignment?.SalaryPackage?.Items?.Select(i => new PayrollPackageItemDto
            {
                Label = i.Label,
                Type = i.Type,
                DefaultValue = i.DefaultValue,
                IsTaxable = i.IsTaxable,
                IsSocial = i.IsSocial,
                IsCIMR = i.IsCIMR,
                ExemptionLimit = i.ExemptionLimit
            }).ToList() ?? new());

        if (hasSalaryComponents && packageAssignment?.SalaryPackage?.Items != null && packageAssignment.SalaryPackage.Items.Any())
        {
            Console.WriteLine("📌 Primes utilisées pour le calcul : UNIQUEMENT EmployeeSalaryComponent (PackageItems ignorés pour éviter double comptage).");
        }

        var leaveDtos = await BuildLeavesForPayMonthAsync(leaves, employee.CompanyId, startDateMonth, endDateMonth);

        return new EmployeePayrollDto
        {
            PayMonth = month,
            PayYear = year,

            FullName = $"{employee.FirstName} {employee.LastName}",
            CinNumber = employee.CinNumber,
            CnssNumber = employee.CnssNumber,
            CimrNumber = employee.CimrNumber,
            CimrEmployeeRate = employee.CimrEmployeeRate,
            CimrCompanyRate = employee.CimrCompanyRate,
            HasPrivateInsurance = employee.HasPrivateInsurance,
            PrivateInsuranceRate = employee.PrivateInsuranceRate,
            DisableAmo = employee.DisableAmo,
            MaritalStatus = employee.MaritalStatus?.Code,
            NumberOfChildren = employee.Children?.Count ?? 0,
            HasSpouse = employee.Spouses?.Any() ?? false,

            ContractType = contract?.ContractType?.ContractTypeName,
            LegalContractType = contract?.ContractType?.LegalContractType?.Name,
            StateEmploymentProgram = contract?.ContractType?.StateEmploymentProgram?.Name,
            JobPosition = contract?.JobPosition?.Name,
            ContractStartDate = contract?.StartDate ?? startOfMonth,
            AncienneteYears = ancienneteYears,

            BaseSalary = salary?.BaseSalary ?? 0,
            SalaryComponents = salaryComponentsList,

            SalaryPackageName = packageAssignment?.SalaryPackage?.Name,
            PackageItems = packageItemsList,

            Absences = absences.Select(a => new PayrollAbsenceDto
            {
                AbsenceType = a.AbsenceType,
                AbsenceDate = a.AbsenceDate.ToDateTime(TimeOnly.MinValue),
                DurationType = a.DurationType.ToString(),
                Status = a.Status.ToString()
            }).ToList(),

            Overtimes = overtimes.Select(o => new PayrollOvertimeDto
            {
                OvertimeDate = o.OvertimeDate.ToDateTime(TimeOnly.MinValue),
                DurationInHours = o.DurationInHours,
                RateMultiplier = o.RateMultiplierApplied
            }).ToList(),

            Leaves = leaveDtos
        };
    }

    /// <summary>
    /// Construit la liste des congés pour le mois de paie : chaque congé contribue par le nombre de jours ouvrables qui tombent dans le mois.
    /// </summary>
    private async Task<List<PayrollLeaveDto>> BuildLeavesForPayMonthAsync(
        List<LeaveRequest> leaves,
        int companyId,
        DateOnly startDateMonth,
        DateOnly endDateMonth)
    {
        var list = new List<PayrollLeaveDto>();
        foreach (var l in leaves)
        {
            var overlapStart = l.StartDate > startDateMonth ? l.StartDate : startDateMonth;
            var overlapEnd = l.EndDate < endDateMonth ? l.EndDate : endDateMonth;
            var daysInMonth = await _workingDaysCalculator.CalculateWorkingDaysAsync(companyId, overlapStart, overlapEnd);
            if (daysInMonth <= 0) continue;
            list.Add(new PayrollLeaveDto
            {
                LeaveType = l.LeaveType?.LeaveCode,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                DaysCount = daysInMonth
            });
        }
        return list;
    }
    }
}
