using FluentValidation;
using Payzen.Application.DTOs.Payroll;

namespace Payzen.Application.Validators.Payroll;

public class PayrollSimulateRequestValidator : AbstractValidator<PayrollSimulateRequestDto>
{
    public PayrollSimulateRequestValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("L'ID de l'employé est requis");
        RuleFor(x => x.PayMonth).InclusiveBetween(1, 12).WithMessage("Le mois doit être entre 1 et 12");
        RuleFor(x => x.PayYear).InclusiveBetween(2020, 2100).WithMessage("L'année doit être entre 2020 et 2100");
    }
}

public class PayrollBatchRequestValidator : AbstractValidator<PayrollBatchRequestDto>
{
    public PayrollBatchRequestValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0).WithMessage("L'ID de la société est requis");
        RuleFor(x => x.PayMonth).InclusiveBetween(1, 12).WithMessage("Le mois doit être entre 1 et 12");
        RuleFor(x => x.PayYear).InclusiveBetween(2020, 2100).WithMessage("L'année doit être entre 2020 et 2100");

        When(x => x.EmployeeIds != null, () =>
            RuleFor(x => x.EmployeeIds!)
                .Must(ids => ids.Count > 0 && ids.All(id => id > 0))
                .WithMessage("Les IDs d'employés doivent être valides"));
    }
}

public class SalaryPackageCreateValidator : AbstractValidator<SalaryPackageCreateDto>
{
    private static readonly string[] ValidStatuses = ["draft", "published", "deprecated"];
    private static readonly string[] ValidTemplateTypes = ["OFFICIAL", "COMPANY"];

    public SalaryPackageCreateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Length(2, 200);
        RuleFor(x => x.Category).NotEmpty().Length(2, 100);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage("Le statut doit être : draft, published ou deprecated");

        When(x => x.TemplateType != null, () =>
            RuleFor(x => x.TemplateType!)
                .Must(t => ValidTemplateTypes.Contains(t))
                .WithMessage("TemplateType doit être OFFICIAL ou COMPANY"));

        When(x => x.CimrRate.HasValue, () =>
            RuleFor(x => x.CimrRate!.Value)
                .InclusiveBetween(0, 0.12m)
                .WithMessage("Le taux CIMR doit être entre 0 et 12%"));

        When(x => x.Description != null, () =>
            RuleFor(x => x.Description!).MaximumLength(1000));

        RuleFor(x => x.BaseSalary).GreaterThanOrEqualTo(0);

        RuleForEach(x => x.Items).SetValidator(new SalaryPackageItemWriteValidator());
    }
}

public class SalaryPackageUpdateValidator : AbstractValidator<SalaryPackageUpdateDto>
{
    private static readonly string[] ValidStatuses = ["draft", "published", "deprecated"];
    private static readonly string[] ValidTemplateTypes = ["OFFICIAL", "COMPANY"];

    public SalaryPackageUpdateValidator()
    {
        When(x => x.Name != null, () => RuleFor(x => x.Name!).Length(2, 200));
        When(x => x.Category != null, () => RuleFor(x => x.Category!).Length(2, 100));

        When(x => x.Status != null, () =>
            RuleFor(x => x.Status!)
                .Must(s => ValidStatuses.Contains(s))
                .WithMessage("Le statut doit être : draft, published ou deprecated"));

        When(x => x.TemplateType != null, () =>
            RuleFor(x => x.TemplateType!)
                .Must(t => ValidTemplateTypes.Contains(t))
                .WithMessage("TemplateType doit être OFFICIAL ou COMPANY"));

        When(x => x.CimrRate.HasValue, () =>
            RuleFor(x => x.CimrRate!.Value)
                .InclusiveBetween(0, 0.12m)
                .WithMessage("Le taux CIMR doit être entre 0 et 12%"));

        When(x => x.Items != null, () =>
            RuleForEach(x => x.Items!).SetValidator(new SalaryPackageItemWriteValidator()));
    }
}

public class SalaryPackageItemWriteValidator : AbstractValidator<SalaryPackageItemWriteDto>
{
    private static readonly string[] ValidTypes = ["base_salary", "allowance", "bonus", "benefit_in_kind", "social_charge"];

    public SalaryPackageItemWriteValidator()
    {
        RuleFor(x => x.Label).NotEmpty().Length(1, 200);

        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(t => ValidTypes.Contains(t))
            .WithMessage("Le type doit être : base_salary, allowance, bonus, benefit_in_kind ou social_charge");

        RuleFor(x => x.DefaultValue).GreaterThanOrEqualTo(0);

        When(x => x.ExemptionLimit.HasValue, () =>
            RuleFor(x => x.ExemptionLimit!.Value).GreaterThanOrEqualTo(0));
    }
}

public class SalaryPackageAssignmentCreateValidator : AbstractValidator<SalaryPackageAssignmentCreateDto>
{
    public SalaryPackageAssignmentCreateValidator()
    {
        RuleFor(x => x.SalaryPackageId).GreaterThan(0).WithMessage("L'ID du package salarial est requis");
        RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("L'ID de l'employé est requis");
        RuleFor(x => x.ContractId).GreaterThan(0).WithMessage("L'ID du contrat est requis");
        RuleFor(x => x.EffectiveDate).NotEmpty().WithMessage("La date d'effet est requise");
    }
}

public class SalaryPreviewRequestValidator : AbstractValidator<SalaryPreviewRequestDto>
{
    public SalaryPreviewRequestValidator()
    {
        RuleFor(x => x.BaseSalary).GreaterThanOrEqualTo(0).WithMessage("Le salaire de base doit être positif");
        RuleFor(x => x.Dependents).GreaterThanOrEqualTo(0).WithMessage("Le nombre de personnes à charge doit être positif");

        When(x => x.YearsOfService.HasValue, () =>
            RuleFor(x => x.YearsOfService!.Value).GreaterThanOrEqualTo(0));

        RuleForEach(x => x.Items).SetValidator(new SalaryPackageItemWriteValidator());
    }
}

public class PayComponentWriteValidator : AbstractValidator<PayComponentWriteDto>
{
    private static readonly string[] ValidTypes = ["base_salary", "allowance", "bonus", "benefit_in_kind", "social_charge"];

    public PayComponentWriteValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(2, 50)
            .Matches("^[A-Z0-9_]+$").WithMessage("Le code doit contenir uniquement des lettres majuscules, chiffres et underscores");

        RuleFor(x => x.NameFr).NotEmpty().Length(2, 200);

        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(t => ValidTypes.Contains(t))
            .WithMessage("Le type doit être : base_salary, allowance, bonus, benefit_in_kind ou social_charge");

        When(x => x.NameAr != null, () => RuleFor(x => x.NameAr!).MaximumLength(200));
        When(x => x.NameEn != null, () => RuleFor(x => x.NameEn!).MaximumLength(200));

        When(x => x.ExemptionLimit.HasValue, () =>
            RuleFor(x => x.ExemptionLimit!.Value).GreaterThanOrEqualTo(0));

        When(x => x.DefaultAmount.HasValue, () =>
            RuleFor(x => x.DefaultAmount!.Value).GreaterThanOrEqualTo(0));
    }
}

public class SalaryPackageCloneValidator : AbstractValidator<SalaryPackageCloneDto>
{
    public SalaryPackageCloneValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0).WithMessage("L'ID de la société est requis");

        When(x => x.Name != null, () =>
            RuleFor(x => x.Name!).Length(2, 200));
    }
}
