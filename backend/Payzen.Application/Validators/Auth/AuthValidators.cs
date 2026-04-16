using FluentValidation;
using Payzen.Application.DTOs.Auth;

namespace Payzen.Application.Validators.Auth;

public class UserCreateValidator : AbstractValidator<UserCreateDto>
{
    public UserCreateValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Le nom d'utilisateur est requis")
            .Length(3, 50)
            .WithMessage("Le nom d'utilisateur doit contenir entre 3 et 50 caractères");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("L'email est requis")
            .EmailAddress()
            .WithMessage("Format d'email invalide")
            .MaximumLength(100);
    }
}

public class UserUpdateValidator : AbstractValidator<UserUpdateDto>
{
    public UserUpdateValidator()
    {
        When(
            x => x.Email != null,
            () => RuleFor(x => x.Email!).EmailAddress().WithMessage("Format d'email invalide").MaximumLength(100)
        );

        When(x => x.Username != null, () => RuleFor(x => x.Username!).Length(3, 50));
    }
}

public class RoleCreateValidator : AbstractValidator<RoleCreateDto>
{
    public RoleCreateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Le nom du rôle est requis")
            .Length(2, 50)
            .WithMessage("Le nom du rôle doit contenir entre 2 et 50 caractères");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("La description est requise")
            .Length(10, 500)
            .WithMessage("La description doit contenir entre 10 et 500 caractères");
    }
}

public class RoleUpdateValidator : AbstractValidator<RoleUpdateDto>
{
    public RoleUpdateValidator()
    {
        When(x => x.Name != null, () => RuleFor(x => x.Name!).Length(2, 50));

        When(x => x.Description != null, () => RuleFor(x => x.Description!).Length(10, 500));
    }
}

public class PermissionCreateValidator : AbstractValidator<PermissionCreateDto>
{
    public PermissionCreateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Le nom de la permission est requis")
            .Length(3, 100)
            .WithMessage("Le nom doit contenir entre 3 et 100 caractères");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("La description est requise")
            .Length(10, 500)
            .WithMessage("La description doit contenir entre 10 et 500 caractères");
    }
}

public class RolePermissionCreateValidator : AbstractValidator<RolePermissionCreateDto>
{
    public RolePermissionCreateValidator()
    {
        RuleFor(x => x.RoleId).GreaterThan(0).WithMessage("L'ID du rôle doit être supérieur à 0");
        RuleFor(x => x.PermissionId).GreaterThan(0).WithMessage("L'ID de la permission doit être supérieur à 0");
    }
}

public class RolePermissionsBulkAssignValidator : AbstractValidator<RolePermissionsBulkAssignDto>
{
    public RolePermissionsBulkAssignValidator()
    {
        RuleFor(x => x.RoleId).GreaterThan(0).WithMessage("L'ID du rôle doit être valide");
        RuleFor(x => x.PermissionIds)
            .NotNull()
            .Must(ids => ids.Any(id => id.HasValue && id.Value > 0))
            .WithMessage("Au moins une permission valide doit être spécifiée");

        // Si la liste contient des null (payload front), on les ignore mais on rejette les valeurs <= 0.
        RuleForEach(x => x.PermissionIds)
            .Must(id => id == null || id.Value > 0)
            .WithMessage("Les ID de permissions doivent être supérieurs à 0");
    }
}

public class UserRoleCreateValidator : AbstractValidator<UserRoleCreateDto>
{
    public UserRoleCreateValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("L'ID de l'utilisateur doit être supérieur à 0");
        RuleFor(x => x.RoleId).GreaterThan(0).WithMessage("L'ID du rôle doit être supérieur à 0");
    }
}

public class UserRolesBulkAssignValidator : AbstractValidator<UserRolesBulkAssignDto>
{
    public UserRolesBulkAssignValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("L'ID de l'utilisateur doit être valide");
        RuleFor(x => x.RoleIds).NotNull().Must(ids => ids.Count > 0).WithMessage("Au moins un rôle doit être spécifié");
    }
}
