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
        // Appliquer les migrations avant de seed (sécurise un drop + update)
        try
        {
            await db.Database.MigrateAsync(ct);
        }
        catch
        {
            // Ignore migration errors during unit tests or when DB not available
        }

        var now = DateTimeOffset.UtcNow;
        const int systemUserId = 0;

        // ===== Statuses =====
        if (!await db.Statuses.AnyAsync(s => s.Code.ToLower() == "active", ct))
        {
            db.Statuses.Add(
                new Status
                {
                    Code = "Active",
                    NameFr = "Actif",
                    NameAr = "",
                    NameEn = "Active",
                    IsActive = true,
                    AffectsAccess = true,
                    AffectsPayroll = false,
                    AffectsAttendance = false,
                    CreatedAt = now,
                    CreatedBy = systemUserId,
                }
            );
        }

        if (!await db.Statuses.AnyAsync(s => s.Code.ToLower() == "inactive", ct))
        {
            db.Statuses.Add(
                new Status
                {
                    Code = "Inactive",
                    NameFr = "Inactif",
                    NameAr = "",
                    NameEn = "Inactive",
                    IsActive = false,
                    CreatedAt = now,
                    CreatedBy = systemUserId,
                }
            );
        }

        if (!await db.Statuses.AnyAsync(s => s.Code.ToLower() == "resigned", ct))
        {
            db.Statuses.Add(
                new Status
                {
                    Code = "RESIGNED",
                    NameFr = "Démissionnaire",
                    NameAr = "",
                    NameEn = "Resigned",
                    IsActive = true,
                    AffectsAccess = true,
                    AffectsPayroll = true,
                    AffectsAttendance = true,
                    CreatedAt = now,
                    CreatedBy = systemUserId,
                }
            );
        }

        if (!await db.Statuses.AnyAsync(s => s.Code.ToLower() == "retired", ct))
        {
            db.Statuses.Add(
                new Status
                {
                    Code = "RETIRED",
                    NameFr = "Retraité",
                    NameAr = "",
                    NameEn = "Retired",
                    IsActive = true,
                    AffectsAccess = true,
                    AffectsPayroll = true,
                    AffectsAttendance = true,
                    CreatedAt = now,
                    CreatedBy = systemUserId,
                }
            );
        }

        // ===== Genders =====
        if (!await db.Genders.AnyAsync(g => g.Code.ToLower() == "m", ct))
        {
            db.Genders.Add(
                new Gender
                {
                    Code = "M",
                    NameFr = "Homme",
                    NameAr = "",
                    NameEn = "Male",
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = systemUserId,
                }
            );
        }

        if (!await db.Genders.AnyAsync(g => g.Code.ToLower() == "f", ct))
        {
            db.Genders.Add(
                new Gender
                {
                    Code = "F",
                    NameFr = "Femme",
                    NameAr = "",
                    NameEn = "Female",
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = systemUserId,
                }
            );
        }

        // ===== Marital Statuses =====
        var maritalStatuses = new[]
        {
            new
            {
                Code = "SINGLE",
                NameFr = "Célibataire",
                NameAr = "",
                NameEn = "Single",
            },
            new
            {
                Code = "MARRIED",
                NameFr = "Marié(e)",
                NameAr = "",
                NameEn = "Married",
            },
            new
            {
                Code = "DIVORCED",
                NameFr = "Divorcé(e)",
                NameAr = "",
                NameEn = "Divorced",
            },
            new
            {
                Code = "WIDOWED",
                NameFr = "Veuf / Veuve",
                NameAr = "",
                NameEn = "Widowed",
            },
            new
            {
                Code = "PARTNER",
                NameFr = "En union libre",
                NameAr = "",
                NameEn = "Partner",
            },
        };

        foreach (var ms in maritalStatuses)
        {
            if (!await db.MaritalStatuses.AnyAsync(m => m.Code.ToLower() == ms.Code.ToLower(), ct))
            {
                db.MaritalStatuses.Add(
                    new MaritalStatus
                    {
                        Code = ms.Code,
                        NameFr = ms.NameFr,
                        NameAr = ms.NameAr,
                        NameEn = ms.NameEn,
                        CreatedAt = now,
                        CreatedBy = systemUserId,
                    }
                );
            }
        }

        // ===== Education Levels =====
        var educationLevels = new[]
        {
            new
            {
                Code = "NONE",
                NameFr = "Sans diplôme",
                NameAr = "",
                NameEn = "No formal education",
                Order = 1,
            },
            new
            {
                Code = "PRIMARY",
                NameFr = "Primaire",
                NameAr = "",
                NameEn = "Primary",
                Order = 2,
            },
            new
            {
                Code = "SECONDARY",
                NameFr = "Secondaire",
                NameAr = "",
                NameEn = "Secondary",
                Order = 3,
            },
            new
            {
                Code = "BACC",
                NameFr = "Baccalauréat",
                NameAr = "",
                NameEn = "Baccalaureate",
                Order = 4,
            },
            new
            {
                Code = "LIC",
                NameFr = "Licence",
                NameAr = "",
                NameEn = "Bachelor",
                Order = 5,
            },
            new
            {
                Code = "MASTER",
                NameFr = "Master",
                NameAr = "",
                NameEn = "Master",
                Order = 6,
            },
            new
            {
                Code = "PHD",
                NameFr = "Doctorat",
                NameAr = "",
                NameEn = "Doctorate",
                Order = 7,
            },
        };

        foreach (var el in educationLevels)
        {
            if (!await db.EducationLevels.AnyAsync(e => e.Code.ToLower() == el.Code.ToLower(), ct))
            {
                db.EducationLevels.Add(
                    new EducationLevel
                    {
                        Code = el.Code,
                        NameFr = el.NameFr,
                        NameAr = el.NameAr,
                        NameEn = el.NameEn,
                        LevelOrder = el.Order,
                        IsActive = true,
                        CreatedAt = now,
                        CreatedBy = systemUserId,
                    }
                );
            }
        }

        // ===== Countries / Cities (minimal) =====
        if (!await db.Countries.AnyAsync(c => c.CountryCode.ToLower() == "mar", ct))
        {
            var morocco = new Country
            {
                CountryName = "Morocco",
                CountryNameAr = "",
                CountryCode = "MAR",
                CountryPhoneCode = "+212",
                CreatedAt = now,
                CreatedBy = systemUserId,
            };
            db.Countries.Add(morocco);
            await db.SaveChangesAsync(ct); // besoin de Id pour la ville

            if (!await db.Cities.AnyAsync(c => c.CountryId == morocco.Id && c.CityName.ToLower() == "casablanca", ct))
            {
                db.Cities.Add(
                    new City
                    {
                        CityName = "Casablanca",
                        CountryId = morocco.Id,
                        CreatedAt = now,
                        CreatedBy = systemUserId,
                    }
                );
            }
        }

        // ===== Roles minimaux =====
        if (!await db.Roles.AnyAsync(r => r.Name.ToLower() == "admin payzen", ct))
        {
            db.Roles.Add(
                new Roles
                {
                    Name = "Admin Payzen",
                    Description = "Admin Payzen",
                    CreatedAt = now,
                    CreatedBy = systemUserId,
                }
            );
        }
        if (!await db.Roles.AnyAsync(r => r.Name.ToLower() == "admin", ct))
        {
            db.Roles.Add(
                new Roles
                {
                    Name = "Admin",
                    Description = "Administrateur système",
                    CreatedAt = now,
                    CreatedBy = systemUserId,
                }
            );
        }
        if (!await db.Roles.AnyAsync(r => r.Name.ToLower() == "employee", ct))
        {
            db.Roles.Add(
                new Roles
                {
                    Name = "Employee",
                    Description = "Rôle employé par défaut",
                    CreatedAt = now,
                    CreatedBy = systemUserId,
                }
            );
        }
        if (!await db.Roles.AnyAsync(r => r.Name.ToLower() == "rh", ct))
        {
            db.Roles.Add(
                new Roles
                {
                    Name = "RH",
                    Description = "Ressources Humaines",
                    CreatedAt = now,
                    CreatedBy = systemUserId,
                }
            );
        }

        await db.SaveChangesAsync(ct);

        // ===== LeaveType for statutory/legal events (if absent) =====
        var legalLeaveType = await db.LeaveTypes.FirstOrDefaultAsync(
            lt => lt.LeaveCode == "LEGAL" && lt.DeletedAt == null,
            ct
        );
        if (legalLeaveType == null)
        {
            legalLeaveType = new LeaveType
            {
                LeaveCode = "LEGAL",
                LeaveNameFr = "Congés légaux",
                LeaveNameEn = "Statutory leaves",
                LeaveNameAr = "",
                LeaveDescription = "Congés prévus par la législation du travail (mariage, décès, naissance, etc.)",
                Scope = LeaveScope.Global,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = systemUserId,
            };
            db.LeaveTypes.Add(legalLeaveType);
            await db.SaveChangesAsync(ct);
        }

        // ===== Seed LeaveTypeLegalRule (idempotent) =====
        var legalRules = new[]
        {
            new
            {
                Code = "MARRIAGE_EMPLOYEE",
                Description = "Mariage du salarié",
                Days = 4,
                Article = "Article 274",
                CanBeDiscontinuous = false,
                MustWithinDays = (int?)null,
            },
            new
            {
                Code = "DEATH_CLOSE",
                Description = "Décès proche (conjoint/enfant)",
                Days = 3,
                Article = "Article 274",
                CanBeDiscontinuous = false,
                MustWithinDays = (int?)null,
            },
            new
            {
                Code = "DEATH_PARENT",
                Description = "Décès d'un parent",
                Days = 2,
                Article = "Article 274",
                CanBeDiscontinuous = false,
                MustWithinDays = (int?)null,
            },
            new
            {
                Code = "BIRTH",
                Description = "Naissance",
                Days = 2,
                Article = "Article 269",
                CanBeDiscontinuous = false,
                MustWithinDays = (int?)30,
            },
        };

        foreach (var rule in legalRules)
        {
            var exists = await db.LeaveTypeLegalRules.AnyAsync(
                r => r.EventCaseCode.ToLower() == rule.Code.ToLower() && r.LeaveTypeId == legalLeaveType.Id,
                ct
            );
            if (!exists)
            {
                db.LeaveTypeLegalRules.Add(
                    new LeaveTypeLegalRule
                    {
                        LeaveTypeId = legalLeaveType.Id,
                        EventCaseCode = rule.Code,
                        Description = rule.Description,
                        DaysGranted = rule.Days,
                        LegalArticle = rule.Article,
                        CanBeDiscountinuous = rule.CanBeDiscontinuous,
                        MustBeUsedWithinDays = rule.MustWithinDays,
                        CreatedAt = now,
                        CreatedBy = systemUserId,
                    }
                );
            }
        }

        await db.SaveChangesAsync(ct);

        // ===== Créer une company + employee + user admin (idempotent) =====
        var adminCompanyEmail = "admin@payzen.local";
        if (!await db.Companies.AnyAsync(c => c.Email.ToLower() == adminCompanyEmail.ToLower(), ct))
        {
            var country = await db.Countries.FirstOrDefaultAsync(
                c => c.CountryCode.ToUpper() == "MAR" || c.CountryName.ToLower() == "morocco",
                ct
            );
            if (country != null)
            {
                var city = await db.Cities.FirstOrDefaultAsync(
                    c => c.CountryId == country.Id && c.CityName.ToLower() == "casablanca",
                    ct
                );
                if (city == null)
                {
                    city = new City
                    {
                        CityName = "Casablanca",
                        CountryId = country.Id,
                        CreatedAt = now,
                        CreatedBy = systemUserId,
                    };
                    db.Cities.Add(city);
                    await db.SaveChangesAsync(ct);
                }

                var company = new Company
                {
                    CompanyName = "PayZen Demo Company",
                    Email = adminCompanyEmail,
                    PhoneNumber = "+212600000000",
                    CountryPhoneCode = country.CountryPhoneCode,
                    CompanyAddress = "1 Rue Exemple, Casablanca",
                    CityId = city.Id,
                    CountryId = country.Id,
                    CnssNumber = "CNSS-0001",
                    IsCabinetExpert = false,
                    Currency = "MAD",
                    PayrollPeriodicity = "Mensuelle",
                    FiscalYearStartMonth = 1,
                    CreatedAt = now,
                    CreatedBy = systemUserId,
                };
                db.Companies.Add(company);
                await db.SaveChangesAsync(ct);

                var activeStatus = await db.Statuses.FirstOrDefaultAsync(s => s.Code.ToLower() == "active", ct);
                var adminEmployee = new Employee
                {
                    FirstName = "Admin",
                    LastName = "PayZen",
                    CinNumber = "ADMIN-" + Guid.NewGuid().ToString()[..8].ToUpper(),
                    DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)),
                    Phone = "+212600000000",
                    Email = adminCompanyEmail,
                    CompanyId = company.Id,
                    StatusId = activeStatus?.Id ?? 0,
                    CreatedAt = now,
                    CreatedBy = systemUserId,
                };

                db.Employees.Add(adminEmployee);
                await db.SaveChangesAsync(ct);

                if (!await db.Users.AnyAsync(u => u.Email.ToLower() == adminCompanyEmail.ToLower(), ct))
                {
                    var baseUsername = "admin";
                    var username = baseUsername;
                    var suffix = 1;
                    while (await db.Users.AnyAsync(u => u.Username == username && u.DeletedAt == null, ct))
                    {
                        username = baseUsername + suffix;
                        suffix++;
                    }

                    var user = new Users
                    {
                        EmployeeId = adminEmployee.Id,
                        Username = username,
                        Email = adminCompanyEmail,
                        IsActive = true,
                        CreatedAt = now,
                        CreatedBy = systemUserId,
                    };

                    db.Users.Add(user);
                    await db.SaveChangesAsync(ct);

                    var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == "admin payzen", ct);
                    if (adminRole != null)
                    {
                        db.UsersRoles.Add(
                            new UsersRoles
                            {
                                UserId = user.Id,
                                RoleId = adminRole.Id,
                                CreatedAt = now,
                                CreatedBy = systemUserId,
                            }
                        );
                        await db.SaveChangesAsync(ct);
                    }
                }
            }
        }

        // ===== OvertimeRateRules =====
        if (!await db.OvertimeRateRules.AnyAsync(ct))
        {
            var overtimeRules = new[]
            {
                new
                {
                    Code = "STD_DAY",
                    NameFr = "Heures supplémentaires jours normaux",
                    NameEn = "Standard day overtime",
                    NameAr = "",
                    Description = "Heures supplémentaires effectuées les jours ouvrables normaux",
                    AppliesTo = OvertimeType.Standard,
                    Multiplier = 1.25m,
                    Priority = 10,
                    Category = "Standard",
                    TimeRangeType = TimeRangeType.AllDay,
                },
                new
                {
                    Code = "NIGHT_STD",
                    NameFr = "Travail de nuit standard",
                    NameEn = "Standard night work",
                    NameAr = "",
                    Description = "Travail de nuit (21h-6h) les jours ouvrables",
                    AppliesTo = OvertimeType.Standard | OvertimeType.Night,
                    Multiplier = 1.50m,
                    Priority = 5,
                    Category = "Night",
                    TimeRangeType = TimeRangeType.AllDay,
                },
                new
                {
                    Code = "WEEKLY_REST",
                    NameFr = "Travail jour de repos",
                    NameEn = "Weekly rest day work",
                    NameAr = "",
                    Description = "Travail effectué pendant le jour de repos hebdomadaire",
                    AppliesTo = OvertimeType.WeeklyRest,
                    Multiplier = 1.50m,
                    Priority = 5,
                    Category = "WeeklyRest",
                    TimeRangeType = TimeRangeType.AllDay,
                },
                new
                {
                    Code = "WEEKLY_REST_NIGHT",
                    NameFr = "Travail de nuit jour de repos",
                    NameEn = "Night work on rest day",
                    NameAr = "",
                    Description = "Travail de nuit pendant le jour de repos hebdomadaire",
                    AppliesTo = OvertimeType.WeeklyRest | OvertimeType.Night,
                    Multiplier = 2.00m,
                    Priority = 1,
                    Category = "WeeklyRest+Night",
                    TimeRangeType = TimeRangeType.AllDay,
                },
                new
                {
                    Code = "HOLIDAY",
                    NameFr = "Travail jour férié",
                    NameEn = "Public holiday work",
                    NameAr = "",
                    Description = "Travail effectué pendant un jour férié",
                    AppliesTo = OvertimeType.PublicHoliday,
                    Multiplier = 2.00m,
                    Priority = 5,
                    Category = "Holiday",
                    TimeRangeType = TimeRangeType.AllDay,
                },
                new
                {
                    Code = "HOLIDAY_NIGHT",
                    NameFr = "Travail de nuit jour férié",
                    NameEn = "Night work on holiday",
                    NameAr = "",
                    Description = "Travail de nuit pendant un jour férié",
                    AppliesTo = OvertimeType.PublicHoliday | OvertimeType.Night,
                    Multiplier = 2.50m,
                    Priority = 1,
                    Category = "Holiday+Night",
                    TimeRangeType = TimeRangeType.AllDay,
                },
            };

            foreach (var rule in overtimeRules)
            {
                db.OvertimeRateRules.Add(
                    new OvertimeRateRule
                    {
                        Code = rule.Code,
                        NameFr = rule.NameFr,
                        NameEn = rule.NameEn,
                        NameAr = rule.NameAr,
                        Description = rule.Description,
                        AppliesTo = rule.AppliesTo,
                        Multiplier = rule.Multiplier,
                        Priority = rule.Priority,
                        Category = rule.Category,
                        TimeRangeType = rule.TimeRangeType,
                        IsActive = true,
                        CreatedAt = now,
                        CreatedBy = systemUserId,
                    }
                );
            }

            await db.SaveChangesAsync(ct);
        }

        // ===== Nationalities =====
        if (!await db.Nationalities.AnyAsync(ct))
        {
            var nationalities = new[]
            {
                new Nationality
                {
                    Name = "Marocain",
                    CreatedAt = now,
                    CreatedBy = systemUserId,
                },
                new Nationality
                {
                    Name = "Algérien",
                    CreatedAt = now,
                    CreatedBy = systemUserId,
                },
                new Nationality
                {
                    Name = "Tunisien",
                    CreatedAt = now,
                    CreatedBy = systemUserId,
                },
                new Nationality
                {
                    Name = "Libyen",
                    CreatedAt = now,
                    CreatedBy = systemUserId,
                },
                new Nationality
                {
                    Name = "Libanais",
                    CreatedAt = now,
                    CreatedBy = systemUserId,
                },
            };
            db.Nationalities.AddRange(nationalities);
            await db.SaveChangesAsync(ct);
        }

        // ===== State Employment Programs =====
        var statePrograms = new[]
        {
            new StateEmploymentProgram
            {
                Code = "NONE",
                Name = "Régime normal",
                IsIrExempt = false,
                IsCnssEmployeeExempt = false,
                IsCnssEmployerExempt = false,
                CreatedAt = now,
                CreatedBy = systemUserId,
            },
            new StateEmploymentProgram
            {
                Code = "IDMAJ_M1",
                Name = "ANAPEC IDMAJ - Modèle 1",
                IsIrExempt = true,
                IsCnssEmployeeExempt = true,
                IsCnssEmployerExempt = true,
                MaxDurationMonths = 24,
                SalaryCeiling = 3125m,
                CreatedAt = now,
                CreatedBy = systemUserId,
            },
            new StateEmploymentProgram
            {
                Code = "TAHFIZ",
                Name = "Programme TAHFIZ",
                IsIrExempt = true,
                IsCnssEmployeeExempt = true,
                IsCnssEmployerExempt = true,
                MaxDurationMonths = 24,
                SalaryCeiling = 10000m,
                CreatedAt = now,
                CreatedBy = systemUserId,
            },
        };

        foreach (var prog in statePrograms)
        {
            if (!await db.StateEmploymentPrograms.AnyAsync(p => p.Code == prog.Code, ct))
                db.StateEmploymentPrograms.Add(prog);
        }
        await db.SaveChangesAsync(ct);

        // ===== DEMO : remplir les companies id 2..10 avec 10..200 employés aléatoires =====
        var companyIds = await db.Companies.Where(c => c.Id == 1016).Select(c => c.Id).ToListAsync(ct);
        if (companyIds.Count > 0)
        {
            var rng = new Random(42);

            // données de génération
            var firstNamesMale = new[]
            {
                "Mohamed",
                "Ahmed",
                "Youssef",
                "Omar",
                "Hamza",
                "Amine",
                "Karim",
                "Rachid",
                "Hassan",
                "Ali",
                "Mehdi",
                "Saad",
                "Zakaria",
                "Soufiane",
                "Ayoub",
                "Khalid",
            };
            var firstNamesFemale = new[]
            {
                "Fatima",
                "Khadija",
                "Aicha",
                "Meryem",
                "Zineb",
                "Nadia",
                "Hafsa",
                "Salma",
                "Houda",
                "Loubna",
                "Sanaa",
                "Laila",
                "Imane",
                "Sara",
                "Yasmine",
                "Dounia",
            };
            var lastNames = new[]
            {
                "Alami",
                "Benali",
                "Chraibi",
                "Daoudi",
                "ElMansouri",
                "Fakhouri",
                "Ghali",
                "Hammoudi",
                "Idrissi",
                "Jaouhari",
                "Kadiri",
                "Lahlou",
                "Mellouki",
                "Najimi",
                "Rachidi",
                "Sabiri",
                "Tazi",
            };
            var jobTitles = new[]
            {
                "Développeur",
                "Comptable",
                "Responsable RH",
                "Commercial",
                "Technicien",
                "Analyste",
                "Chef de projet",
                "Ingénieur",
                "Designer",
                "Consultant",
                "Juriste",
                "Marketeur",
                "Administrateur",
            };

            // récupérations globales
            var statuses = await db.Statuses.Where(s => s.DeletedAt == null).Select(s => s.Id).ToListAsync(ct);
            var genders = await db.Genders.Where(g => g.DeletedAt == null).Select(g => g.Id).ToListAsync(ct);
            var cities = await db.Cities.Where(c => c.DeletedAt == null).Select(c => c.Id).ToListAsync(ct);
            var nationalities = await db.Nationalities.Select(n => n.Id).ToListAsync(ct);
            var educationLevelIds = await db.EducationLevels.Select(e => e.Id).ToListAsync(ct);
            var legalContractTypeId = await db.LegalContractTypes.Select(l => l.Id).FirstOrDefaultAsync(ct);

            foreach (var companyId in companyIds)
            {
                // Nombre cible d'employés
                int target = rng.Next(10, 201); // 10..200
                var existing = await db.Employees.CountAsync(e => e.CompanyId == companyId && e.DeletedAt == null, ct);
                if (existing >= target)
                    continue;
                int toCreate = target - existing;

                // assure quelques départements
                var existingDepts = await db.Departements.Where(d => d.CompanyId == companyId).ToListAsync(ct);
                if (!existingDepts.Any())
                {
                    var deptNames = new[] { "Développement", "Comptabilité", "RH", "Commercial", "Support" };
                    foreach (var dn in deptNames)
                    {
                        db.Departements.Add(
                            new Departement
                            {
                                DepartementName = dn,
                                CompanyId = companyId,
                                CreatedAt = DateTimeOffset.UtcNow,
                                CreatedBy = systemUserId,
                            }
                        );
                    }
                    await db.SaveChangesAsync(ct);
                    existingDepts = await db.Departements.Where(d => d.CompanyId == companyId).ToListAsync(ct);
                }

                // assure postes
                var existingPositions = await db.JobPositions.Where(p => p.CompanyId == companyId).ToListAsync(ct);
                if (!existingPositions.Any())
                {
                    foreach (var jt in jobTitles)
                    {
                        db.JobPositions.Add(
                            new JobPosition
                            {
                                Name = jt,
                                CompanyId = companyId,
                                CreatedAt = DateTimeOffset.UtcNow,
                                CreatedBy = systemUserId,
                            }
                        );
                    }
                    await db.SaveChangesAsync(ct);
                    existingPositions = await db.JobPositions.Where(p => p.CompanyId == companyId).ToListAsync(ct);
                }

                // assure contract types
                var existingContractTypes = await db
                    .ContractTypes.Where(ctt => ctt.CompanyId == companyId)
                    .ToListAsync(ct);
                if (!existingContractTypes.Any())
                {
                    var ctNames = new[] { "Permanent", "CDD", "Stage", "Freelance" };
                    foreach (var name in ctNames)
                    {
                        db.ContractTypes.Add(
                            new ContractType
                            {
                                ContractTypeName = name,
                                CompanyId = companyId,
                                LegalContractTypeId = legalContractTypeId,
                                CreatedAt = DateTimeOffset.UtcNow,
                                CreatedBy = systemUserId,
                            }
                        );
                    }
                    await db.SaveChangesAsync(ct);
                    existingContractTypes = await db
                        .ContractTypes.Where(ctt => ctt.CompanyId == companyId)
                        .ToListAsync(ct);
                }

                // créer les employés en deux étapes : création Employee -> création Contract + Salary
                var newEmployees = new List<Employee>();
                for (int i = 0; i < toCreate; i++)
                {
                    bool isMale = rng.Next(2) == 0;
                    var first = isMale
                        ? firstNamesMale[rng.Next(firstNamesMale.Length)]
                        : firstNamesFemale[rng.Next(firstNamesFemale.Length)];
                    var last = lastNames[rng.Next(lastNames.Length)];

                    var age = rng.Next(22, 56); // 22..55
                    var birth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-age).AddDays(-rng.Next(0, 365)));

                    var emp = new Employee
                    {
                        FirstName = first,
                        LastName = last,
                        CinNumber = "CIN" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                        DateOfBirth = birth,
                        Phone = $"06{rng.Next(10000000, 99999999)}",
                        Email = BuildEmail(first, last, companyId, rng.Next(1, 9999)),
                        CompanyId = companyId,
                        StatusId = statuses.Count > 0 ? statuses[rng.Next(statuses.Count)] : 0,
                        GenderId = genders.Count > 0 ? genders[rng.Next(genders.Count)] : (int?)null,
                        NationalityId =
                            nationalities.Count > 0 ? nationalities[rng.Next(nationalities.Count)] : (int?)null,
                        EducationLevelId =
                            educationLevelIds.Count > 0
                                ? educationLevelIds[rng.Next(educationLevelIds.Count)]
                                : (int?)null,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = systemUserId,
                    };
                    newEmployees.Add(emp);
                }

                db.Employees.AddRange(newEmployees);
                await db.SaveChangesAsync(ct);

                // refresh lists
                var employeesAdded = await db
                    .Employees.Where(e => e.CompanyId == companyId && e.CreatedBy == systemUserId)
                    .OrderByDescending(e => e.Id)
                    .Take(toCreate)
                    .ToListAsync(ct);
                var positionIds = existingPositions.Select(p => p.Id).ToArray();
                var deptIds = existingDepts.Select(d => d.Id).ToArray();
                var contractTypeIds = existingContractTypes.Select(ctt => ctt.Id).ToArray();

                foreach (var e in employeesAdded)
                {
                    // contrat
                    var start = DateTime.UtcNow.AddDays(-rng.Next(0, 365 * 10));
                    DateTime? end = null;
                    if (rng.Next(100) < 15) // 15% chance de sortie
                        end = start.AddDays(rng.Next(30, 365 * 5));

                    var contract = new EmployeeContract
                    {
                        EmployeeId = e.Id,
                        CompanyId = companyId,
                        JobPositionId = positionIds[rng.Next(positionIds.Length)],
                        ContractTypeId = contractTypeIds[rng.Next(contractTypeIds.Length)],
                        StartDate = start,
                        EndDate = end,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = systemUserId,
                    };
                    db.EmployeeContracts.Add(contract);
                    await db.SaveChangesAsync(ct);

                    // salaire
                    var baseSalary = Math.Round((decimal)(rng.NextDouble() * (45000 - 3500) + 3500), 2);
                    var bonus = Math.Round(baseSalary * (decimal)(rng.NextDouble() * 0.30), 2);

                    var salary = new EmployeeSalary
                    {
                        EmployeeId = e.Id,
                        ContractId = contract.Id,
                        BaseSalary = baseSalary,
                        EffectiveDate = contract.StartDate,
                        EndDate = contract.EndDate,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = systemUserId,
                    };
                    db.EmployeeSalaries.Add(salary);
                    await db.SaveChangesAsync(ct);

                    // prime (composant)
                    if (bonus > 0)
                    {
                        var comp = new EmployeeSalaryComponent
                        {
                            EmployeeSalaryId = salary.Id,
                            ComponentType = "Prime",
                            IsTaxable = true,
                            IsSocial = true,
                            IsCIMR = false,
                            Amount = bonus,
                            EffectiveDate = salary.EffectiveDate,
                            CreatedAt = DateTimeOffset.UtcNow,
                            CreatedBy = systemUserId,
                        };
                        db.EmployeeSalaryComponents.Add(comp);
                    }

                    // adresse minimale
                    if (cities.Count > 0)
                    {
                        db.EmployeeAddresses.Add(
                            new EmployeeAddress
                            {
                                EmployeeId = e.Id,
                                AddressLine1 = "Adresse Demo",
                                ZipCode = "00000",
                                CityId = cities[rng.Next(cities.Count)],
                                CreatedAt = DateTimeOffset.UtcNow,
                                CreatedBy = systemUserId,
                            }
                        );
                    }

                    // assign department randomly
                    e.DepartementId = deptIds.Length > 0 ? deptIds[rng.Next(deptIds.Length)] : (int?)null;
                    // matricule
                    e.Matricule = rng.Next(1000, 999999);
                    // éventuellement date de sortie sur l'entité
                    if (contract.EndDate.HasValue)
                    {
                        e.AnnualLeaveOpeningEffectiveFrom = DateOnly.FromDateTime(contract.StartDate);
                    }

                    await db.SaveChangesAsync(ct);
                }
            }
        }
    }

    private static string BuildEmail(string first, string last, int companyId, int suffix)
    {
        var username = ($"{first}.{last}.{suffix}").Replace(" ", "").ToLowerInvariant();
        return $"{username}@company{companyId}.local";
    }
}
