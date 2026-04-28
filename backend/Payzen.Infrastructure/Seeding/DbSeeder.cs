using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Payzen.Domain.Entities.Auth;
using Payzen.Domain.Entities.Company;
using Payzen.Domain.Entities.Employee;
using Payzen.Domain.Entities.Leave;
using Payzen.Domain.Entities.Referentiel;
using Payzen.Domain.Enums;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Seeding;

public static class DbSeeder
{
    /// <summary>
    /// Seeder idempotent pour initialiser les données référentielles minimales.
    /// Utiliser : await DbSeeder.SeedAsync(db, ct);
    /// </summary>
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        await SeedGendersAsync(db, ct);
        await SeedStatusesAsync(db, ct);
        await SeedEducationLevelsAsync(db, ct);
        await SeedMaritalStatusesAsync(db, ct);
        await SeedLegalContractTypesAsync(db, ct);
        await SeedCountriesAndCitiesAsync(db, ct);
        await SeedOvertimeRateRulesAsync(db, ct);
        await SeedRolesAndPermissionsAsync(db, ct);
        await SeedGlobalLeaveTypesAsync(db, ct);
        await db.SaveChangesAsync(ct);

        await SeedAdminRolePermissionsAsync(db, ct);
        await SeedCompanyAndAdminAsync(db, ct);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedCompanyAndAdminAsync(AppDbContext db, CancellationToken ct)
    {
        // 1) Créer la société Payzen si absente
        if (!await db.Companies.AnyAsync(c => c.CompanyName == "Payzen", ct))
        {
            var cityId = await db.Cities.OrderBy(c => c.Id).Select(c => c.Id).FirstOrDefaultAsync(ct);
            var countryId = await db.Countries.OrderBy(c => c.Id).Select(c => c.Id).FirstOrDefaultAsync(ct);
            if (cityId != 0 && countryId != 0)
            {
                var company = new Company
                {
                    CompanyName = "Payzen",
                    Email = "contact@payzen.ma",
                    PhoneNumber = "0522000000",
                    CountryPhoneCode = "+212",
                    CompanyAddress = "Casablanca, Maroc",
                    CityId = cityId,
                    CountryId = countryId,
                    CnssNumber = "J123456789",
                    IceNumber = "001234567000089",
                    Currency = "MAD",
                    PayrollPeriodicity = "Mensuelle",
                    isActive = true,
                    CreatedBy = 1,
                };
                db.Companies.Add(company);
            }
        }
    }

    private static async Task SeedGendersAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Genders.AnyAsync(ct))
            return;
        db.Genders.AddRange(
            new Gender
            {
                Code = "M",
                NameFr = "Homme",
                NameAr = "ذكر",
                NameEn = "Male",
                CreatedBy = 1,
            },
            new Gender
            {
                Code = "F",
                NameFr = "Femme",
                NameAr = "أنثى",
                NameEn = "Female",
                CreatedBy = 1,
            }
        );
    }

    private static async Task SeedStatusesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Statuses.AnyAsync(ct))
            return;
        db.Statuses.AddRange(
            new Status
            {
                Code = "ACTIVE",
                NameFr = "Actif",
                NameAr = "نشط",
                NameEn = "Active",
                IsActive = true,
                AffectsPayroll = true,
                AffectsAttendance = true,
                CreatedBy = 1,
            },
            new Status
            {
                Code = "INACTIVE",
                NameFr = "Inactif",
                NameAr = "غير نشط",
                NameEn = "Inactive",
                IsActive = false,
                CreatedBy = 1,
            },
            new Status
            {
                Code = "LEAVE",
                NameFr = "En congé",
                NameAr = "في إجازة",
                NameEn = "On leave",
                IsActive = true,
                AffectsAttendance = true,
                CreatedBy = 1,
            },
            new Status
            {
                Code = "RESIGNED",
                NameFr = "Démissionnaire",
                NameAr = "مستقيل",
                NameEn = "Resigned",
                IsActive = false,
                AffectsAccess = true,
                CreatedBy = 1,
            },
            new Status
            {
                Code = "RETIRED",
                NameFr = "Retraité",
                NameAr = "متقاعد",
                NameEn = "Retired",
                IsActive = false,
                AffectsAccess = true,
                CreatedBy = 1,
            }
        );
    }

    private static async Task SeedEducationLevelsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.EducationLevels.AnyAsync(ct))
            return;
        db.EducationLevels.AddRange(
            new EducationLevel
            {
                Code = "NONE",
                NameFr = "Sans diplôme",
                NameAr = "بدون شهادة",
                NameEn = "No degree",
                LevelOrder = 1,
                CreatedBy = 1,
            },
            new EducationLevel
            {
                Code = "BAC",
                NameFr = "Baccalauréat",
                NameAr = "البكالوريا",
                NameEn = "High school",
                LevelOrder = 2,
                CreatedBy = 1,
            },
            new EducationLevel
            {
                Code = "BAC+2",
                NameFr = "Bac+2 (BTS/DUT)",
                NameAr = "باك+2",
                NameEn = "Bac+2",
                LevelOrder = 3,
                CreatedBy = 1,
            },
            new EducationLevel
            {
                Code = "LICENCE",
                NameFr = "Licence (Bac+3)",
                NameAr = "الإجازة",
                NameEn = "Bachelor",
                LevelOrder = 4,
                CreatedBy = 1,
            },
            new EducationLevel
            {
                Code = "MASTER",
                NameFr = "Master (Bac+5)",
                NameAr = "الماستر",
                NameEn = "Master",
                LevelOrder = 5,
                CreatedBy = 1,
            },
            new EducationLevel
            {
                Code = "DOCTORAT",
                NameFr = "Doctorat",
                NameAr = "الدكتوراه",
                NameEn = "PhD",
                LevelOrder = 6,
                CreatedBy = 1,
            }
        );
    }

    private static async Task SeedMaritalStatusesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.MaritalStatuses.AnyAsync(ct))
            return;
        db.MaritalStatuses.AddRange(
            new MaritalStatus
            {
                Code = "SINGLE",
                NameFr = "Célibataire",
                NameAr = "أعزب",
                NameEn = "Single",
                CreatedBy = 1,
            },
            new MaritalStatus
            {
                Code = "MARRIED",
                NameFr = "Marié(e)",
                NameAr = "متزوج",
                NameEn = "Married",
                CreatedBy = 1,
            },
            new MaritalStatus
            {
                Code = "DIVORCED",
                NameFr = "Divorcé(e)",
                NameAr = "مطلق",
                NameEn = "Divorced",
                CreatedBy = 1,
            },
            new MaritalStatus
            {
                Code = "WIDOWED",
                NameFr = "Veuf/Veuve",
                NameAr = "أرمل",
                NameEn = "Widowed",
                CreatedBy = 1,
            }
        );
    }

    private static async Task SeedLegalContractTypesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.LegalContractTypes.AnyAsync(ct))
            return;
        db.LegalContractTypes.AddRange(
            new LegalContractType
            {
                Code = "CDI",
                Name = "Contrat à Durée Indéterminée",
                CreatedBy = 1,
            },
            new LegalContractType
            {
                Code = "CDD",
                Name = "Contrat à Durée Déterminée",
                CreatedBy = 1,
            },
            new LegalContractType
            {
                Code = "STAGE",
                Name = "Convention de Stage",
                CreatedBy = 1,
            },
            new LegalContractType
            {
                Code = "FREELANCE",
                Name = "Contrat de Prestation",
                CreatedBy = 1,
            },
            new LegalContractType
            {
                Code = "INTERIM",
                Name = "Contrat d'Intérim",
                CreatedBy = 1,
            }
        );
    }

    private static async Task SeedCountriesAndCitiesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Countries.AnyAsync(ct))
            return;

        var maroc = new Domain.Entities.Referentiel.Country
        {
            CountryName = "Maroc",
            CountryNameAr = "المغرب",
            CountryCode = "MA",
            CountryPhoneCode = "+212",
            CreatedBy = 1,
            Cities = new List<City>
            {
                new() { CityName = "Casablanca", CreatedBy = 1 },
                new() { CityName = "Rabat", CreatedBy = 1 },
                new() { CityName = "Marrakech", CreatedBy = 1 },
                new() { CityName = "Fès", CreatedBy = 1 },
                new() { CityName = "Tanger", CreatedBy = 1 },
                new() { CityName = "Agadir", CreatedBy = 1 },
                new() { CityName = "Meknès", CreatedBy = 1 },
                new() { CityName = "Oujda", CreatedBy = 1 },
                new() { CityName = "Kénitra", CreatedBy = 1 },
                new() { CityName = "Tétouan", CreatedBy = 1 },
                new() { CityName = "El Jadida", CreatedBy = 1 },
                new() { CityName = "Safi", CreatedBy = 1 },
                new() { CityName = "Mohammadia", CreatedBy = 1 },
                new() { CityName = "Laâyoune", CreatedBy = 1 },
                new() { CityName = "Béni Mellal", CreatedBy = 1 },
            },
        };
        db.Countries.Add(maroc);
    }

    private static async Task SeedOvertimeRateRulesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.OvertimeRateRules.AnyAsync(ct))
            return;

        var now = DateTimeOffset.UtcNow;

        db.OvertimeRateRules.AddRange(
            new OvertimeRateRule
            {
                Code = "STD_DAY",
                NameFr = "Heures supplémentaires jours normaux",
                NameEn = "Standard day overtime",
                NameAr = "ساعات إضافية أيام عمل عادية",
                Description = "Heures supplémentaires effectuées les jours ouvrables normaux",
                AppliesTo = OvertimeType.Standard,
                Multiplier = 1.25m,
                Priority = 10,
                Category = "Standard",
                TimeRangeType = TimeRangeType.AllDay,
                CumulationStrategy = MultiplierCumulationStrategy.TakeMaximum,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = 1,
            },
            new OvertimeRateRule
            {
                Code = "NIGHT_STD",
                NameFr = "Travail de nuit standard",
                NameEn = "Standard night work",
                NameAr = "عمل ليلي (أيام عادية)",
                Description = "Travail de nuit (21h-6h) les jours ouvrables",
                AppliesTo = OvertimeType.Standard | OvertimeType.Night,
                Multiplier = 1.50m,
                Priority = 5,
                Category = "Night",
                TimeRangeType = TimeRangeType.AllDay,
                CumulationStrategy = MultiplierCumulationStrategy.TakeMaximum,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = 1,
            },
            new OvertimeRateRule
            {
                Code = "WEEKLY_REST",
                NameFr = "Travail jour de repos",
                NameEn = "Weekly rest day work",
                NameAr = "عمل يوم الراحة الأسبوعية",
                Description = "Travail effectué pendant le jour de repos hebdomadaire",
                AppliesTo = OvertimeType.WeeklyRest,
                Multiplier = 1.50m,
                Priority = 5,
                Category = "WeeklyRest",
                TimeRangeType = TimeRangeType.AllDay,
                CumulationStrategy = MultiplierCumulationStrategy.TakeMaximum,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = 1,
            },
            new OvertimeRateRule
            {
                Code = "WEEKLY_REST_NIGHT",
                NameFr = "Travail de nuit jour de repos",
                NameEn = "Night work on rest day",
                NameAr = "عمل ليلي يوم الراحة الأسبوعية",
                Description = "Travail de nuit pendant le jour de repos hebdomadaire",
                AppliesTo = OvertimeType.WeeklyRest | OvertimeType.Night,
                Multiplier = 2.00m,
                Priority = 1,
                Category = "WeeklyRest+Night",
                TimeRangeType = TimeRangeType.AllDay,
                CumulationStrategy = MultiplierCumulationStrategy.TakeMaximum,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = 1,
            },
            new OvertimeRateRule
            {
                Code = "HOLIDAY",
                NameFr = "Travail jour férié",
                NameEn = "Public holiday work",
                NameAr = "عمل في يوم عطلة رسمية",
                Description = "Travail effectué pendant un jour férié",
                AppliesTo = OvertimeType.PublicHoliday,
                Multiplier = 2.00m,
                Priority = 5,
                Category = "Holiday",
                TimeRangeType = TimeRangeType.AllDay,
                CumulationStrategy = MultiplierCumulationStrategy.TakeMaximum,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = 1,
            },
            new OvertimeRateRule
            {
                Code = "HOLIDAY_NIGHT",
                NameFr = "Travail de nuit jour férié",
                NameEn = "Night work on holiday",
                NameAr = "عمل ليلي في يوم عطلة رسمية",
                Description = "Travail de nuit pendant un jour férié",
                AppliesTo = OvertimeType.PublicHoliday | OvertimeType.Night,
                Multiplier = 2.50m,
                Priority = 1,
                Category = "Holiday+Night",
                TimeRangeType = TimeRangeType.AllDay,
                CumulationStrategy = MultiplierCumulationStrategy.TakeMaximum,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = 1,
            }
        );
    }

    private static async Task SeedRolesAndPermissionsAsync(AppDbContext db, CancellationToken ct)
    {
        var permissions = new[]
        {
            // Users
            new Permissions
            {
                Name = "READ_USERS",
                Description = "Lister tous les utilisateurs",
                Resource = "users",
                Action = "read",
                CreatedBy = 1,
            },
            new Permissions
            {
                Name = "VIEW_USERS",
                Description = "Voir un utilisateur par ID",
                Resource = "users",
                Action = "view",
                CreatedBy = 1,
            },
            new Permissions
            {
                Name = "CREATE_USERS",
                Description = "Créer un utilisateur",
                Resource = "users",
                Action = "create",
                CreatedBy = 1,
            },
            new Permissions
            {
                Name = "EDIT_USERS",
                Description = "Modifier un utilisateur",
                Resource = "users",
                Action = "edit",
                CreatedBy = 1,
            },
            new Permissions
            {
                Name = "DELETE_USERS",
                Description = "Supprimer un utilisateur",
                Resource = "users",
                Action = "delete",
                CreatedBy = 1,
            },
            // Roles
            new Permissions
            {
                Name = "READ_ROLES",
                Description = "Lister tous les rôles",
                Resource = "roles",
                Action = "read",
                CreatedBy = 1,
            },
            new Permissions
            {
                Name = "VIEW_ROLES",
                Description = "Voir un rôle par ID",
                Resource = "roles",
                Action = "view",
                CreatedBy = 1,
            },
            new Permissions
            {
                Name = "CREATE_ROLES",
                Description = "Créer un rôle",
                Resource = "roles",
                Action = "create",
                CreatedBy = 1,
            },
            new Permissions
            {
                Name = "EDIT_ROLES",
                Description = "Modifier un rôle",
                Resource = "roles",
                Action = "edit",
                CreatedBy = 1,
            },
            new Permissions
            {
                Name = "DELETE_ROLES",
                Description = "Supprimer un rôle",
                Resource = "roles",
                Action = "delete",
                CreatedBy = 1,
            },
            new Permissions
            {
                Name = "VIEW_ROLES_USERS",
                Description = "Voir les utilisateurs d'un rôle",
                Resource = "roles",
                Action = "view",
                CreatedBy = 1,
            },
            // Permissions
            new Permissions
            {
                Name = "READ_PERMISSIONS",
                Description = "Lister toutes les permissions",
                Resource = "permissions",
                Action = "read",
                CreatedBy = 1,
            },
            new Permissions
            {
                Name = "CREATE_PERMISSIONS",
                Description = "Créer une permission",
                Resource = "permissions",
                Action = "create",
                CreatedBy = 1,
            },
            new Permissions
            {
                Name = "EDIT_PERMISSIONS",
                Description = "Modifier une permission",
                Resource = "permissions",
                Action = "edit",
                CreatedBy = 1,
            },
            new Permissions
            {
                Name = "DELETE_PERMISSIONS",
                Description = "Supprimer une permission",
                Resource = "permissions",
                Action = "delete",
                CreatedBy = 1,
            },
            // Roles <-> Permissions
            new Permissions
            {
                Name = "READ_ROLES_PERMISSIONS",
                Description = "Voir les permissions d'un rôle",
                Resource = "roles-permissions",
                Action = "read",
                CreatedBy = 1,
            },
            new Permissions
            {
                Name = "ASSIGN_ROLES_PERMISSIONS",
                Description = "Assigner une permission à un rôle",
                Resource = "roles-permissions",
                Action = "assign",
                CreatedBy = 1,
            },
            new Permissions
            {
                Name = "BULK_ASSIGN_ROLES_PERMISSION",
                Description = "Assigner en masse des permissions",
                Resource = "roles-permissions",
                Action = "bulk-assign",
                CreatedBy = 1,
            },
            new Permissions
            {
                Name = "DELETE_ROLES_PERMISSIONS",
                Description = "Révoquer une permission d'un rôle",
                Resource = "roles-permissions",
                Action = "delete",
                CreatedBy = 1,
            },
            // Users <-> Roles
            new Permissions
            {
                Name = "READ_USER_ROLES",
                Description = "Voir les rôles d'un employé",
                Resource = "users-roles",
                Action = "read",
                CreatedBy = 1,
            },
            new Permissions
            {
                Name = "ASSIGN_USERS_ROLES",
                Description = "Assigner des rôles à un utilisateur",
                Resource = "users-roles",
                Action = "assign",
                CreatedBy = 1,
            },
            new Permissions
            {
                Name = "BULK_ASSIGN_USERS_ROLES",
                Description = "Assigner en masse des rôles",
                Resource = "users-roles",
                Action = "bulk-assign",
                CreatedBy = 1,
            },
            new Permissions
            {
                Name = "UPDATE_USERS_ROLES",
                Description = "Remplacer les rôles d'un utilisateur",
                Resource = "users-roles",
                Action = "update",
                CreatedBy = 1,
            },
            new Permissions
            {
                Name = "DELETE_USER_ROLES",
                Description = "Révoquer un rôle d'un utilisateur",
                Resource = "users-roles",
                Action = "delete",
                CreatedBy = 1,
            },
        };

        if (!await db.Roles.AnyAsync(ct))
        {
            var roles = new[]
            {
                new Roles
                {
                    Name = "Admin Payzen",
                    Description = "Administrateur de Payzen.",
                    CreatedBy = 1,
                },
                new Roles
                {
                    Name = "Admin",
                    Description = "Administration de la société.",
                    CreatedBy = 1,
                },
                new Roles
                {
                    Name = "RH",
                    Description = "Responsable des Ressources Humaines.",
                    CreatedBy = 1,
                },
                new Roles
                {
                    Name = "Manager",
                    Description = "Manager d'équipe — accès lecture + approbations.",
                    CreatedBy = 1,
                },
                new Roles
                {
                    Name = "Employee",
                    Description = "Employé — accès à son propre profil et ses demandes de congé.",
                    CreatedBy = 1,
                },
                new Roles
                {
                    Name = "CEO",
                    Description = "CEO de la société.",
                    CreatedBy = 1,
                },
                new Roles
                {
                    Name = "CabinetExpert",
                    Description = "Cabinet d'expertise comptable — gestion multi-sociétés.",
                    CreatedBy = 1,
                },
            };
            db.Roles.AddRange(roles);
            db.Permissions.AddRange(permissions);
        }
        else
        {
            foreach (var p in permissions)
            {
                if (await db.Permissions.AnyAsync(x => x.Name == p.Name && x.DeletedAt == null, ct))
                    continue;
                db.Permissions.Add(p);
            }
        }
    }

    private static async Task SeedGlobalLeaveTypesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.LeaveTypes.AnyAsync(lt => lt.CompanyId == null, ct))
            return;

        // ── Congé annuel légal ────────────────────────────────────────────────
        var annual = new LeaveType
        {
            LeaveCode = "ANNUAL",
            LeaveNameFr = "Congé annuel",
            LeaveNameAr = "إجازة سنوية",
            LeaveNameEn = "Annual Leave",
            LeaveDescription = "Congé annuel légal — Code du Travail Art. 231",
            Scope = LeaveScope.Global,
            IsActive = true,
            CreatedBy = 1,
            Policies = new List<LeaveTypePolicy>
            {
                new()
                {
                    AccrualMethod = LeaveAccrualMethod.Monthly,
                    DaysPerMonthAdult = 1.5m,
                    DaysPerMonthMinor = 2.0m,
                    BonusDaysPerYearAfter5Years = 1.5m,
                    RequiresEligibility6Months = true,
                    RequiresBalance = true,
                    AnnualCapDays = 30,
                    AllowCarryover = true,
                    MaxCarryoverYears = 1,
                    UseWorkingCalendar = true,
                    IsPaid = true,
                    IsEnabled = true,
                    CreatedBy = 1,
                },
            },
            LegalRules = new List<LeaveTypeLegalRule>
            {
                new()
                {
                    EventCaseCode = "ANNUAL_STANDARD",
                    Description = "1,5 jour ouvrable par mois de service (2 j pour les moins de 18 ans)",
                    DaysGranted = 18,
                    LegalArticle = "Art. 231 CT",
                    CanBeDiscountinuous = false,
                    MustBeUsedWithinDays = 730,
                    CreatedBy = 1,
                },
            },
        };

        // ── Congés exceptionnels légaux ───────────────────────────────────────
        var mariage = new LeaveType
        {
            LeaveCode = "MARIAGE",
            LeaveNameFr = "Mariage de l'employé",
            LeaveNameAr = "زواج الموظف",
            LeaveNameEn = "Employee Marriage",
            LeaveDescription = "Congé exceptionnel pour mariage — Code du Travail Art. 274",
            Scope = LeaveScope.Global,
            IsActive = true,
            CreatedBy = 1,
            Policies = new List<LeaveTypePolicy>
            {
                new()
                {
                    AccrualMethod = LeaveAccrualMethod.None,
                    RequiresBalance = false,
                    IsPaid = true,
                    IsEnabled = true,
                    UseWorkingCalendar = true,
                    CreatedBy = 1,
                },
            },
            LegalRules = new List<LeaveTypeLegalRule>
            {
                new()
                {
                    EventCaseCode = "MARIAGE_EMPLOYE",
                    Description = "4 jours ouvrables accordés lors du mariage de l'employé",
                    DaysGranted = 4,
                    LegalArticle = "Art. 274 CT",
                    CanBeDiscountinuous = false,
                    MustBeUsedWithinDays = 30,
                    CreatedBy = 1,
                },
            },
        };

        var naissance = new LeaveType
        {
            LeaveCode = "NAISSANCE",
            LeaveNameFr = "Naissance / adoption",
            LeaveNameAr = "ولادة / تبني",
            LeaveNameEn = "Birth / Adoption",
            LeaveDescription = "Congé exceptionnel pour naissance ou adoption — Code du Travail Art. 274",
            Scope = LeaveScope.Global,
            IsActive = true,
            CreatedBy = 1,
            Policies = new List<LeaveTypePolicy>
            {
                new()
                {
                    AccrualMethod = LeaveAccrualMethod.None,
                    RequiresBalance = false,
                    IsPaid = true,
                    IsEnabled = true,
                    UseWorkingCalendar = true,
                    CreatedBy = 1,
                },
            },
            LegalRules = new List<LeaveTypeLegalRule>
            {
                new()
                {
                    EventCaseCode = "NAISSANCE_ENFANT",
                    Description = "3 jours ouvrables accordés lors de la naissance ou adoption d'un enfant",
                    DaysGranted = 3,
                    LegalArticle = "Art. 274 CT",
                    CanBeDiscountinuous = false,
                    MustBeUsedWithinDays = 15,
                    CreatedBy = 1,
                },
            },
        };

        var deces = new LeaveType
        {
            LeaveCode = "DECES",
            LeaveNameFr = "Décès conjoint / enfant",
            LeaveNameAr = "وفاة الزوج أو الطفل",
            LeaveNameEn = "Death of spouse / child",
            LeaveDescription = "Congé exceptionnel pour décès d'un proche — Code du Travail Art. 274",
            Scope = LeaveScope.Global,
            IsActive = true,
            CreatedBy = 1,
            Policies = new List<LeaveTypePolicy>
            {
                new()
                {
                    AccrualMethod = LeaveAccrualMethod.None,
                    RequiresBalance = false,
                    IsPaid = true,
                    IsEnabled = true,
                    UseWorkingCalendar = true,
                    CreatedBy = 1,
                },
            },
            LegalRules = new List<LeaveTypeLegalRule>
            {
                new()
                {
                    EventCaseCode = "DECES_CONJOINT_ENFANT",
                    Description = "3 jours ouvrables pour décès du conjoint ou d'un enfant",
                    DaysGranted = 3,
                    LegalArticle = "Art. 274 CT",
                    CanBeDiscountinuous = false,
                    MustBeUsedWithinDays = 7,
                    CreatedBy = 1,
                },
                new()
                {
                    EventCaseCode = "DECES_PARENT",
                    Description = "2 jours ouvrables pour décès d'un parent (père, mère, frère, sœur)",
                    DaysGranted = 2,
                    LegalArticle = "Art. 274 CT",
                    CanBeDiscountinuous = false,
                    MustBeUsedWithinDays = 7,
                    CreatedBy = 1,
                },
            },
        };

        var maladie = new LeaveType
        {
            LeaveCode = "MALADIE",
            LeaveNameFr = "Congé maladie",
            LeaveNameAr = "إجازة مرضية",
            LeaveNameEn = "Sick Leave",
            LeaveDescription = "Congé maladie sur justificatif médical",
            Scope = LeaveScope.Global,
            IsActive = true,
            CreatedBy = 1,
            Policies = new List<LeaveTypePolicy>
            {
                new()
                {
                    AccrualMethod = LeaveAccrualMethod.None,
                    RequiresBalance = false,
                    IsPaid = true,
                    IsEnabled = true,
                    UseWorkingCalendar = true,
                    CreatedBy = 1,
                },
            },
        };

        var maternite = new LeaveType
        {
            LeaveCode = "MATERNITE",
            LeaveNameFr = "Congé maternité",
            LeaveNameAr = "إجازة الأمومة",
            LeaveNameEn = "Maternity Leave",
            LeaveDescription = "Congé maternité légal — Code du Travail Art. 152 (14 semaines)",
            Scope = LeaveScope.Global,
            IsActive = true,
            CreatedBy = 1,
            Policies = new List<LeaveTypePolicy>
            {
                new()
                {
                    AccrualMethod = LeaveAccrualMethod.None,
                    RequiresBalance = false,
                    IsPaid = true,
                    IsEnabled = true,
                    UseWorkingCalendar = false,
                    CreatedBy = 1,
                },
            },
            LegalRules = new List<LeaveTypeLegalRule>
            {
                new()
                {
                    EventCaseCode = "MATERNITE_STANDARD",
                    Description = "98 jours calendaires (14 semaines) dont 7 semaines avant l'accouchement",
                    DaysGranted = 98,
                    LegalArticle = "Art. 152 CT",
                    CanBeDiscountinuous = false,
                    MustBeUsedWithinDays = null,
                    CreatedBy = 1,
                },
            },
        };

        db.LeaveTypes.AddRange(annual, mariage, naissance, deces, maladie, maternite);
    }

    private static async Task SeedAdminRolePermissionsAsync(AppDbContext db, CancellationToken ct = default)
    {
        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin Payzen" && r.DeletedAt == null, ct);
        if (adminRole == null)
            return;

        var permissionIds = await db.Permissions.Where(p => p.DeletedAt == null).Select(p => p.Id).ToListAsync(ct);
        if (permissionIds.Count == 0)
            return;

        var existingPermissionIds = await db
            .RolesPermissions.Where(rp => rp.RoleId == adminRole.Id && rp.DeletedAt == null)
            .Select(rp => rp.PermissionId)
            .ToListAsync(ct);
        var toAdd = permissionIds.Except(existingPermissionIds).ToList();
        foreach (var permissionId in toAdd)
            db.RolesPermissions.Add(
                new RolesPermissions
                {
                    RoleId = adminRole.Id,
                    PermissionId = permissionId,
                    CreatedBy = 1,
                }
            );
    }

    private static async Task SeedAdminCompanyRolePermissionsAsync(AppDbContext db, CancellationToken ct = default)
    {
        var adminCompanyRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin" && r.DeletedAt == null, ct);
        if (adminCompanyRole == null)
            return;

        var permissionIds = await db
            .Permissions.Where(p =>
                p.DeletedAt == null && (p.Resource == "users" ||
                p.Resource == "roles" || p.Resource == "users-roles" || p.Resource == "permissions" || p.Resource == "roles-permissions")
            )
            .Select(p => p.Id)
            .ToListAsync(ct);
        if (permissionIds.Count == 0)
            return;

        var existingPermissionIds = await db
            .RolesPermissions.Where(rp => rp.RoleId == adminCompanyRole.Id && rp.DeletedAt == null)
            .Select(rp => rp.PermissionId)
            .ToListAsync(ct);
        var toAdd = permissionIds.Except(existingPermissionIds).ToList();
        foreach (var permissionId in toAdd)
            db.RolesPermissions.Add(
                new RolesPermissions
                {
                    RoleId = adminCompanyRole.Id,
                    PermissionId = permissionId,
                    CreatedBy = 1,
                }
            );
    }
}
