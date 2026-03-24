using FluentValidation;
using Payzen.Application.DTOs.Employee;

namespace Payzen.Application.Validators.Employee;

public class EmployeeCreateValidator : AbstractValidator<EmployeeCreateDto>
{
    public EmployeeCreateValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().Length(2, 500);
        RuleFor(x => x.LastName).NotEmpty().Length(2, 500);
        RuleFor(x => x.CinNumber).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(10).WithMessage("Le numéro de téléphone doit contenir 10 chiffres");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(500);
        RuleFor(x => x.StatusId).GreaterThan(0).WithMessage("L'ID du statut est requis");

        RuleFor(x => x.DateOfBirth)
            .Must(dob => DateOnly.FromDateTime(DateTime.Today).Year - dob.Year >= 16)
            .WithMessage("L'employé doit avoir au moins 16 ans");

        When(x => x.CompanyId.HasValue, () =>
            RuleFor(x => x.CompanyId!.Value).GreaterThan(0));

        When(x => x.Salary.HasValue, () =>
            RuleFor(x => x.Salary!.Value).GreaterThanOrEqualTo(0));

        When(x => x.SalaryHourly.HasValue, () =>
            RuleFor(x => x.SalaryHourly!.Value).GreaterThanOrEqualTo(0));

        When(x => x.Password != null, () =>
            RuleFor(x => x.Password!).MinimumLength(8));
    }
}

public class EmployeeUpdateValidator : AbstractValidator<EmployeeUpdateDto>
{
    public EmployeeUpdateValidator()
    {
        When(x => x.Email != null, () =>
            RuleFor(x => x.Email!).EmailAddress().MaximumLength(500));
        
        When(x => x.Phone != null, () =>
            RuleFor(x => x.Phone!).MaximumLength(10).WithMessage("Le numéro de téléphone doit contenir 10 chiffres"));

        When(x => x.FirstName != null, () =>
            RuleFor(x => x.FirstName!).Length(2, 500));

        When(x => x.LastName != null, () =>
            RuleFor(x => x.LastName!).Length(2, 500));

        When(x => x.Salary.HasValue, () =>
            RuleFor(x => x.Salary!.Value).GreaterThan(0));

        When(x => x.SalaryHourly.HasValue, () =>
            RuleFor(x => x.SalaryHourly!.Value).GreaterThanOrEqualTo(0));

        When(x => x.CnssNumber != null, () =>
            RuleFor(x => x.CnssNumber!).MaximumLength(100));

        When(x => x.CimrNumber != null, () =>
            RuleFor(x => x.CimrNumber!).MaximumLength(100));
    }
}

public class EmployeeContractCreateValidator : AbstractValidator<EmployeeContractCreateDto>
{
    public EmployeeContractCreateValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("L'ID de l'employé est requis");
        RuleFor(x => x.CompanyId).GreaterThan(0).WithMessage("L'ID de la société est requis");
        RuleFor(x => x.JobPositionId).GreaterThan(0).WithMessage("L'ID du poste est requis");
        RuleFor(x => x.ContractTypeId).GreaterThan(0).WithMessage("L'ID du type de contrat est requis");
        RuleFor(x => x.StartDate).NotEmpty().WithMessage("La date de début est requise");

        When(x => x.EndDate.HasValue, () =>
            RuleFor(x => x.EndDate!.Value)
                .GreaterThan(x => x.StartDate)
                .WithMessage("La date de fin doit être postérieure à la date de début"));
    }
}

public class EmployeeSalaryCreateValidator : AbstractValidator<EmployeeSalaryCreateDto>
{
    public EmployeeSalaryCreateValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("L'ID de l'employé est requis");
        RuleFor(x => x.ContractId).GreaterThan(0).WithMessage("L'ID du contrat est requis");
        RuleFor(x => x.EffectiveDate).NotEmpty().WithMessage("La date d'effet est requise");

        RuleFor(x => x)
            .Must(x => x.BaseSalary.HasValue || x.BaseSalaryHourly.HasValue)
            .WithMessage("Le salaire de base (BaseSalary) ou le salaire horaire (BaseSalaryHourly) est requis");

        When(x => x.BaseSalary.HasValue, () =>
            RuleFor(x => x.BaseSalary!.Value).GreaterThan(0));

        When(x => x.BaseSalaryHourly.HasValue, () =>
            RuleFor(x => x.BaseSalaryHourly!.Value).GreaterThan(0));

        When(x => x.EndDate.HasValue, () =>
            RuleFor(x => x.EndDate!.Value)
                .GreaterThan(x => x.EffectiveDate)
                .WithMessage("La date de fin doit être postérieure à la date d'effet"));
    }
}

public class EmployeeSalaryComponentCreateValidator : AbstractValidator<EmployeeSalaryComponentCreateDto>
{
    public EmployeeSalaryComponentCreateValidator()
    {
        RuleFor(x => x.EmployeeSalaryId).GreaterThan(0).WithMessage("L'ID du salaire est requis");
        RuleFor(x => x.ComponentType).NotEmpty().Length(2, 100);
        RuleFor(x => x.Amount).NotEqual(0).WithMessage("Le montant est requis");
        RuleFor(x => x.EffectiveDate).NotEmpty().WithMessage("La date d'effet est requise");

        When(x => x.EndDate.HasValue, () =>
            RuleFor(x => x.EndDate!.Value)
                .GreaterThan(x => x.EffectiveDate)
                .WithMessage("La date de fin doit être postérieure à la date d'effet"));
    }
}

public class EmployeeAddressCreateValidator : AbstractValidator<EmployeeAddressCreateDto>
{
    public EmployeeAddressCreateValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("L'ID de l'employé est requis");
        RuleFor(x => x.AddressLine1).NotEmpty().Length(5, 500);
        RuleFor(x => x.ZipCode).NotEmpty().Length(4, 20);
        RuleFor(x => x.CityId).GreaterThan(0).WithMessage("L'ID de la ville est requis");
        RuleFor(x => x.CountryId).GreaterThan(0).WithMessage("L'ID du pays est requis");

        When(x => x.AddressLine2 != null, () =>
            RuleFor(x => x.AddressLine2!).MaximumLength(500));
    }
}

public class EmployeeDocumentCreateValidator : AbstractValidator<EmployeeDocumentCreateDto>
{
    public EmployeeDocumentCreateValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("L'ID de l'employé est requis");
        RuleFor(x => x.Name).NotEmpty().Length(2, 500);
        RuleFor(x => x.FilePath).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.DocumentType).NotEmpty().MaximumLength(100);
    }
}

public class EmployeeChildCreateValidator : AbstractValidator<EmployeeChildCreateDto>
{
    public EmployeeChildCreateValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("L'ID de l'employé est requis");
        RuleFor(x => x.FirstName).NotEmpty().Length(2, 100);
        RuleFor(x => x.LastName).NotEmpty().Length(2, 100);
        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .LessThan(DateTime.Today).WithMessage("La date de naissance doit être dans le passé");
    }
}

public class EmployeeSpouseCreateValidator : AbstractValidator<EmployeeSpouseCreateDto>
{
    public EmployeeSpouseCreateValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("L'ID de l'employé est requis");
        RuleFor(x => x.FirstName).NotEmpty().Length(2, 100);
        RuleFor(x => x.LastName).NotEmpty().Length(2, 100);
        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .LessThan(DateTime.Today).WithMessage("La date de naissance doit être dans le passé");

        When(x => x.CinNumber != null, () =>
            RuleFor(x => x.CinNumber!).MaximumLength(50));
    }
}

public class EmployeeAbsenceCreateValidator : AbstractValidator<EmployeeAbsenceCreateDto>
{
    public EmployeeAbsenceCreateValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("L'ID de l'employé est requis");
        RuleFor(x => x.AbsenceDate).NotEmpty().WithMessage("La date d'absence est requise");
        RuleFor(x => x.AbsenceType).NotEmpty().MaximumLength(50);

        When(x => x.Reason != null, () =>
            RuleFor(x => x.Reason!).MaximumLength(500));
    }
}

public class EmployeeOvertimeCreateValidator : AbstractValidator<EmployeeOvertimeCreateDto>
{
    public EmployeeOvertimeCreateValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("L'ID de l'employé est requis");
        RuleFor(x => x.OvertimeDate).NotEmpty().WithMessage("La date est requise");

        When(x => x.DurationInHours.HasValue, () =>
            RuleFor(x => x.DurationInHours!.Value)
                .InclusiveBetween(0.01m, 24m)
                .WithMessage("La durée doit être entre 0.01 et 24 heures"));

        When(x => x.EmployeeComment != null, () =>
            RuleFor(x => x.EmployeeComment!).MaximumLength(500));
    }
}

public class EmployeeCategoryCreateValidator : AbstractValidator<EmployeeCategoryCreateDto>
{
    public EmployeeCategoryCreateValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0).WithMessage("L'ID de la société est requis");
        RuleFor(x => x.Name).NotEmpty().Length(2, 500);

        When(x => x.PayrollPeriodicity != null, () =>
            RuleFor(x => x.PayrollPeriodicity!)
                .Must(p => p == "Mensuelle" || p == "Bimensuelle")
                .WithMessage("La périodicité doit être 'Mensuelle' ou 'Bimensuelle'"));
    }
}
