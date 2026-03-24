using Payzen.Application.DTOs.Payroll;
using Payzen.Domain.Enums;

namespace Payzen.Tests.Helpers;

/// <summary>
/// Builder fluent pour construire des EmployeePayrollDto dans les tests.
/// Usage : PayrollDtoBuilder.Create().WithBaseSalary(10000).WithChildren(2).Build()
/// </summary>
public class PayrollDtoBuilder
{
    private readonly EmployeePayrollDto _dto = new()
    {
        FullName            = "Test Employé",
        BaseSalary          = 5000m,
        // IMPORTANT: le moteur utilise (AnneePaie,MoisPaie) pour calculer la fin de mois
        // dans Module01_Anciennete. Doit être non-zéro sinon exception => Success=false.
        PayMonth            = DateTime.Today.Month,
        PayYear             = DateTime.Today.Year,
        // Par défaut, ancienneté < 2 ans pour coller aux hypothèses de nombreux tests
        // (CNSS/NI/CIMR attendent un PrimeAnciennete = 0).
        ContractStartDate   = DateTime.Today.AddYears(-1),
        AncienneteYears     = 4,
        MaritalStatus       = "CELIBATAIRE",
        NumberOfChildren    = 0,
        HasSpouse           = false,
        DisableAmo          = false,
        HasPrivateInsurance = false,
        Absences            = new(),
        Overtimes           = new(),
        Leaves              = new(),
        PackageItems        = new(),
        SalaryComponents    = new(),
    };

    public static PayrollDtoBuilder Create() => new();

    public PayrollDtoBuilder WithBaseSalary(decimal amount)
    {
        _dto.BaseSalary = amount;
        return this;
    }

    public PayrollDtoBuilder WithChildren(int count)
    {
        _dto.NumberOfChildren = count;
        return this;
    }

    public PayrollDtoBuilder WithSpouse(bool hasSpouse = true)
    {
        _dto.HasSpouse = hasSpouse;
        return this;
    }

    public PayrollDtoBuilder WithAnciennete(int years)
    {
        _dto.AncienneteYears    = years;
        _dto.ContractStartDate  = DateTime.Today.AddYears(-years);
        return this;
    }

    public PayrollDtoBuilder WithOvertimeHours(decimal h25 = 0, decimal h50 = 0, decimal h100 = 0)
    {
        if (h25  > 0) _dto.Overtimes.Add(new PayrollOvertimeDto { DurationInHours = h25,  RateMultiplier = 1.25m });
        if (h50  > 0) _dto.Overtimes.Add(new PayrollOvertimeDto { DurationInHours = h50,  RateMultiplier = 1.50m });
        if (h100 > 0) _dto.Overtimes.Add(new PayrollOvertimeDto { DurationInHours = h100, RateMultiplier = 2.00m });
        return this;
    }

    public PayrollDtoBuilder WithCimr(decimal employeeRate, decimal companyRate)
    {
        // Le moteur active le régime CIMR uniquement si CimrNumber != null
        _dto.CimrNumber = "CIMR_TEST";
        _dto.CimrEmployeeRate = employeeRate;
        _dto.CimrCompanyRate  = companyRate;
        return this;
    }

    public PayrollDtoBuilder WithPrivateInsurance(decimal rate)
    {
        _dto.HasPrivateInsurance  = true;
        _dto.PrivateInsuranceRate = rate;
        return this;
    }

    public PayrollDtoBuilder WithAbsenceDays(int days)
    {
        for (int i = 0; i < days; i++)
            _dto.Absences.Add(new PayrollAbsenceDto
            {
                // PayrollCalculationEngine attend :
                // - Status = "Approved"
                // - AbsenceType != "MATERNITE"
                // - DurationType ∈ {"FullDay","HalfDay"}
                AbsenceDate  = DateTime.Today.AddDays(-i),
                AbsenceType  = "ABSENCE",
                DurationType = "FullDay",
                Status       = "Approved"
            });
        return this;
    }

    public PayrollDtoBuilder WithPackageItem(string label, decimal amount, bool isTaxable = true)
    {
        _dto.PackageItems.Add(new PayrollPackageItemDto
        {
            Label      = label,
            DefaultValue = amount,
            IsTaxable  = isTaxable,
            // Flags non utilisés dans les tests actuels
            IsSocial   = false,
            IsCIMR      = false
        });
        return this;
    }

    public PayrollDtoBuilder WithSalaryComponent(string componentType, decimal amount, bool isTaxable)
    {
        _dto.SalaryComponents.Add(new PayrollSalaryComponentDto
        {
            ComponentType = componentType,
            Amount        = amount,
            IsTaxable     = isTaxable,
            IsSocial      = true,
            IsCIMR        = false
        });
        return this;
    }

    public PayrollDtoBuilder WithNoAmo()
    {
        _dto.DisableAmo = true;
        return this;
    }

    public EmployeePayrollDto Build() => _dto;
}
