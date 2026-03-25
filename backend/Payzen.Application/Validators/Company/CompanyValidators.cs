using FluentValidation;
using Payzen.Application.DTOs.Company;
using Payzen.Application.Interfaces;

namespace Payzen.Application.Validators.Company;

public class CompanyCreateValidator : AbstractValidator<CompanyCreateDto>
{
    public CompanyCreateValidator(ICompanyService companyService)
    {
        RuleFor(x => x.CompanyName).NotEmpty().Length(2, 500);
        RuleFor(x => x.CompanyEmail).NotEmpty().EmailAddress().MaximumLength(500);
        RuleFor(x => x.CompanyPhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.CompanyAddress).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.CountryId).GreaterThan(0).WithMessage("Le pays est requis");
        RuleFor(x => x.CountryId)
            .MustAsync((id, ct) => companyService.CountryExistsAsync(id, ct))
            .When(x => x.CountryId > 0)
            .WithMessage("Pays non trouvé.");

        // Ville : soit CityId (existant), soit CityName (saisie libre), pas les deux
        RuleFor(x => x)
            .Must(x => !(x.CityId is > 0 && !string.IsNullOrWhiteSpace(x.CityName)))
            .WithMessage("Veuillez choisir entre une ville existante (CityId) ou une nouvelle ville (CityName), pas les deux.");
        RuleFor(x => x)
            .Must(x => (x.CityId ?? 0) > 0 || !string.IsNullOrWhiteSpace(x.CityName))
            .WithMessage("La ville est requise : sélectionnez une ville existante ou saisissez le nom de la ville.");
        When(x => x.CityId is > 0, () =>
            RuleFor(x => x)
                .MustAsync((dto, ct) => companyService.CityExistsForCountryAsync(dto.CityId!.Value, dto.CountryId, ct))
                .WithMessage("La ville sélectionnée n'existe pas ou n'appartient pas au pays choisi."));
        When(x => !string.IsNullOrWhiteSpace(x.CityName), () =>
            RuleFor(x => x.CityName!).MaximumLength(200));

        RuleFor(x => x.CnssNumber).NotEmpty().MaximumLength(100);

        RuleFor(x => x.AdminFirstName).NotEmpty().Length(2, 100);
        RuleFor(x => x.AdminLastName).NotEmpty().Length(2, 100);
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(500);
        RuleFor(x => x.AdminPhone).NotEmpty().MaximumLength(20);

        When(x => x.WebsiteUrl != null, () =>
            RuleFor(x => x.WebsiteUrl!)
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("Format d'URL invalide"));
    }
}

public class CompanyCreateByExpertValidator : AbstractValidator<CompanyCreateByExpertDto>
{
    public CompanyCreateByExpertValidator(ICompanyService companyService)
    {
        Include(new CompanyCreateValidator(companyService));
        RuleFor(x => x.ManagedByCompanyId).GreaterThan(0).WithMessage("L'identifiant du cabinet expert est requis");
    }
}

public class CompanyUpdateValidator : AbstractValidator<CompanyUpdateDto>
{
    public CompanyUpdateValidator()
    {
        When(x => x.Email != null, () =>
            RuleFor(x => x.Email!).EmailAddress().WithMessage("Format d'email invalide"));

        When(x => x.WebsiteUrl != null, () =>
            RuleFor(x => x.WebsiteUrl!)
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("Format d'URL invalide"));

        When(x => x.CompanyName != null, () =>
            RuleFor(x => x.CompanyName!).Length(2, 500));

        When(x => x.PayrollPeriodicity != null, () =>
            RuleFor(x => x.PayrollPeriodicity!)
                .Must(p => p == "Mensuelle" || p == "Bimensuelle")
                .WithMessage("La périodicité doit être 'Mensuelle' ou 'Bimensuelle'"));

        When(x => x.AuthType != null, () =>
            RuleFor(x => x.AuthType!)
                .Must(a => a == "JWT" || a == "C")
                .WithMessage("AuthType doit être 'JWT' ou 'C'"));
    }
}

public class DepartementCreateValidator : AbstractValidator<DepartementCreateDto>
{
    public DepartementCreateValidator()
    {
        RuleFor(x => x.DepartementName).NotEmpty().Length(2, 500);
        RuleFor(x => x.CompanyId).GreaterThan(0).WithMessage("L'ID de la société est requis");
    }
}

public class DepartementUpdateValidator : AbstractValidator<DepartementUpdateDto>
{
    public DepartementUpdateValidator()
    {
        When(x => x.DepartementName != null, () =>
            RuleFor(x => x.DepartementName!).Length(2, 500));

        When(x => x.CompanyId.HasValue, () =>
            RuleFor(x => x.CompanyId!.Value).GreaterThan(0));
    }
}

public class JobPositionCreateValidator : AbstractValidator<JobPositionCreateDto>
{
    public JobPositionCreateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Length(2, 200);
        RuleFor(x => x.CompanyId).GreaterThan(0).WithMessage("L'identifiant de la société est requis");
    }
}

public class JobPositionUpdateValidator : AbstractValidator<JobPositionUpdateDto>
{
    public JobPositionUpdateValidator()
    {
        When(x => x.Name != null, () =>
            RuleFor(x => x.Name!).Length(2, 200));
    }
}

public class ContractTypeCreateValidator : AbstractValidator<ContractTypeCreateDto>
{
    public ContractTypeCreateValidator()
    {
        RuleFor(x => x.ContractTypeName).NotEmpty().Length(2, 100);
        RuleFor(x => x.CompanyId).GreaterThan(0).WithMessage("L'identifiant de la société est requis");
    }
}

public class ContractTypeUpdateValidator : AbstractValidator<ContractTypeUpdateDto>
{
    public ContractTypeUpdateValidator()
    {
        When(x => x.ContractTypeName != null, () =>
            RuleFor(x => x.ContractTypeName!).Length(2, 100));
    }
}

public class HolidayCreateValidator : AbstractValidator<HolidayCreateDto>
{
    public HolidayCreateValidator()
    {
        RuleFor(x => x.NameFr).NotEmpty().Length(2, 200);
        RuleFor(x => x.NameAr).NotEmpty().Length(2, 200);
        RuleFor(x => x.NameEn).NotEmpty().Length(2, 200);
        RuleFor(x => x.HolidayDate).NotEmpty().WithMessage("La date du jour férié est requise");
        RuleFor(x => x.CountryId).GreaterThan(0).WithMessage("L'ID du pays est requis");
        RuleFor(x => x.HolidayType).NotEmpty().MaximumLength(50);

        When(x => x.Year.HasValue, () =>
            RuleFor(x => x.Year!.Value).InclusiveBetween(2020, 2100));

        When(x => x.Description != null, () =>
            RuleFor(x => x.Description!).MaximumLength(1000));
    }
}

public class HolidayUpdateValidator : AbstractValidator<HolidayUpdateDto>
{
    public HolidayUpdateValidator()
    {
        When(x => x.NameFr != null, () => RuleFor(x => x.NameFr!).Length(2, 200));
        When(x => x.NameAr != null, () => RuleFor(x => x.NameAr!).Length(2, 200));
        When(x => x.NameEn != null, () => RuleFor(x => x.NameEn!).Length(2, 200));
        When(x => x.HolidayType != null, () => RuleFor(x => x.HolidayType!).MaximumLength(50));
        When(x => x.Year.HasValue, () => RuleFor(x => x.Year!.Value).InclusiveBetween(2020, 2100));
    }
}

public class WorkingCalendarCreateValidator : AbstractValidator<WorkingCalendarCreateDto>
{
    public WorkingCalendarCreateValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0).WithMessage("L'ID de la société est requis");
        RuleFor(x => x.DayOfWeek).InclusiveBetween(0, 6).WithMessage("Le jour doit être entre 0 (Dimanche) et 6 (Samedi)");
    }
}

public class CompanyDocumentCreateValidator : AbstractValidator<CompanyDocumentCreateDto>
{
    public CompanyDocumentCreateValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0).WithMessage("L'ID de l'entreprise est requis");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.FilePath).NotEmpty().MaximumLength(1000);
        When(x => x.DocumentType != null, () =>
            RuleFor(x => x.DocumentType!).MaximumLength(100));
    }
}
