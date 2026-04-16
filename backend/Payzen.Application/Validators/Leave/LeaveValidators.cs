using FluentValidation;
using Payzen.Application.DTOs.Leave;

namespace Payzen.Application.Validators.Leave;

public class LeaveTypeCreateValidator : AbstractValidator<LeaveTypeCreateDto>
{
    public LeaveTypeCreateValidator()
    {
        RuleFor(x => x.LeaveCode).NotEmpty().Length(3, 50);
        RuleFor(x => x.LeaveName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LeaveDescription).NotEmpty().MaximumLength(500);

        When(
            x => x.Scope == Domain.Enums.LeaveScope.Company,
            () =>
                RuleFor(x => x.CompanyId)
                    .NotNull()
                    .WithMessage("CompanyId est requis quand Scope = Company")
                    .GreaterThan(0)
        );
    }
}

public class LeaveTypePolicyCreateValidator : AbstractValidator<LeaveTypePolicyCreateDto>
{
    public LeaveTypePolicyCreateValidator()
    {
        RuleFor(x => x.LeaveTypeId).GreaterThan(0).WithMessage("L'ID du type de congé est requis");
        RuleFor(x => x.DaysPerMonthAdult).InclusiveBetween(0, 31);
        RuleFor(x => x.DaysPerMonthMinor).InclusiveBetween(0, 31);
        RuleFor(x => x.AnnualCapDays).InclusiveBetween(0, 365);
        RuleFor(x => x.MaxCarryoverYears).InclusiveBetween(0, 10);
        RuleFor(x => x.MinConsecutiveDays).InclusiveBetween(0, 365);
    }
}

public class LeaveTypeLegalRuleCreateValidator : AbstractValidator<LeaveTypeLegalRuleCreateDto>
{
    public LeaveTypeLegalRuleCreateValidator()
    {
        RuleFor(x => x.LeaveTypeId).GreaterThan(0).WithMessage("L'ID du type de congé est requis");
        RuleFor(x => x.EventCaseCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DaysGranted).InclusiveBetween(1, 3650);
        RuleFor(x => x.LegalArticle).NotEmpty().MaximumLength(50);

        When(
            x => x.MustBeUsedWithinDays.HasValue,
            () => RuleFor(x => x.MustBeUsedWithinDays!.Value).InclusiveBetween(1, 3650)
        );
    }
}

public class LeaveRequestCreateValidator : AbstractValidator<LeaveRequestCreateDto>
{
    public LeaveRequestCreateValidator()
    {
        RuleFor(x => x.LeaveTypeId).GreaterThan(0).WithMessage("L'ID du type de congé est requis");
        RuleFor(x => x.StartDate).NotEmpty().WithMessage("La date de début est requise");
        RuleFor(x => x.EndDate)
            .NotEmpty()
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("La date de fin doit être égale ou postérieure à la date de début");

        When(x => x.EmployeeNote != null, () => RuleFor(x => x.EmployeeNote!).MaximumLength(1000));
    }
}

public class LeaveBalanceCreateValidator : AbstractValidator<LeaveBalanceCreateDto>
{
    public LeaveBalanceCreateValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("L'ID de l'employé est requis");
        RuleFor(x => x.CompanyId).GreaterThan(0).WithMessage("L'ID de la société est requis");
        RuleFor(x => x.LeaveTypeId).GreaterThan(0).WithMessage("L'ID du type de congé est requis");
        RuleFor(x => x.Year).GreaterThan(2000).WithMessage("L'année doit être valide");
        RuleFor(x => x.Month).InclusiveBetween(1, 12).WithMessage("Le mois doit être entre 1 et 12");
    }
}

public class LeaveCarryOverAgreementCreateValidator : AbstractValidator<LeaveCarryOverAgreementCreateDto>
{
    public LeaveCarryOverAgreementCreateValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0).WithMessage("L'ID de l'employé est requis");
        RuleFor(x => x.CompanyId).GreaterThan(0).WithMessage("L'ID de la société est requis");
        RuleFor(x => x.LeaveTypeId).GreaterThan(0).WithMessage("L'ID du type de congé est requis");
        RuleFor(x => x.AgreementDate).NotEmpty().WithMessage("La date de l'accord est requise");
        RuleFor(x => x.ToYear).GreaterThan(x => x.FromYear).WithMessage("ToYear doit être supérieur à FromYear");

        When(x => x.AgreementDocRef != null, () => RuleFor(x => x.AgreementDocRef!).MaximumLength(500));
    }
}

public class LeaveRequestAttachmentCreateValidator : AbstractValidator<LeaveRequestAttachmentCreateDto>
{
    public LeaveRequestAttachmentCreateValidator()
    {
        RuleFor(x => x.LeaveRequestId).GreaterThan(0).WithMessage("L'ID de la demande de congé est requis");
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.FilePath).NotEmpty().MaximumLength(1000);

        When(x => x.FileType != null, () => RuleFor(x => x.FileType!).MaximumLength(100));
    }
}

public class LeaveRequestApprovalHistoryCreateValidator : AbstractValidator<LeaveRequestApprovalHistoryCreateDto>
{
    public LeaveRequestApprovalHistoryCreateValidator()
    {
        RuleFor(x => x.LeaveRequestId).GreaterThan(0).WithMessage("L'ID de la demande de congé est requis");

        When(x => x.Comment != null, () => RuleFor(x => x.Comment!).MaximumLength(1000));
    }
}

public class LeaveRequestExemptionCreateValidator : AbstractValidator<LeaveRequestExemptionCreateDto>
{
    public LeaveRequestExemptionCreateValidator()
    {
        RuleFor(x => x.LeaveRequestId).GreaterThan(0).WithMessage("L'ID de la demande de congé est requis");
        RuleFor(x => x.ExemptionDate).NotEmpty().WithMessage("La date d'exemption est requise");

        When(x => x.Note != null, () => RuleFor(x => x.Note!).MaximumLength(500));
    }
}

public class LeaveAuditLogCreateValidator : AbstractValidator<LeaveAuditLogCreateDto>
{
    public LeaveAuditLogCreateValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0).WithMessage("L'ID de la société est requis");
        RuleFor(x => x.EventName).NotEmpty().MaximumLength(200);

        When(x => x.OldValue != null, () => RuleFor(x => x.OldValue!).MaximumLength(2000));

        When(x => x.NewValue != null, () => RuleFor(x => x.NewValue!).MaximumLength(2000));
    }
}
