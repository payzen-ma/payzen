using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Referentiel;
using payzen_backend.Models.Permissions;
using payzen_backend.Models.Company;
using payzen_backend.Models.Users;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Leave;
using payzen_backend.Models.Common.LeaveStatus;
using payzen_backend.Models.Common.OvertimeEnums;

namespace payzen_backend.Seeding
{
    /// <summary>
    /// Seeder idempotent pour initialiser les données référentielles minimales
    /// Utiliser : await DbSeeder.SeedAsync(db);
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            // Appliquer les migrations avant de seed (sécurise un drop + update)
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            const int systemUserId = 0;

            // ===== Statuses =====
            if (!await db.Statuses.AnyAsync(s => s.Code.ToLower() == "active"))
            {
                db.Statuses.Add(new Models.Referentiel.Status
                {
                    Code = "Active",
                    NameFr = "Actif",
                    NameAr = "???",
                    NameEn = "Active",
                    IsActive = true,
                    AffectsAccess = true,
                    AffectsPayroll = false,
                    AffectsAttendance = false,
                    CreatedAt = now,
                    CreatedBy = systemUserId
                });

            }

            if (!await db.Statuses.AnyAsync(s => s.Code.ToLower() == "inactive"))
            {
                db.Statuses.Add(new Models.Referentiel.Status
                {
                    Code = "Inactive",
                    NameFr = "Inactif",
                    NameAr = "??? ???",
                    NameEn = "Inactive",
                    IsActive = false,
                    CreatedAt = now,
                    CreatedBy = systemUserId
                });
            }

            if (!await db.Statuses.AnyAsync(s => s.Code.ToLower() == "fired"))
            {
                db.Statuses.Add(new Models.Referentiel.Status
                {
                    Code = "FIRED",
                    NameFr = "Licencié",
                    NameAr = "????",
                    NameEn = "Fired",
                    IsActive = true,
                    AffectsAccess = true,
                    AffectsPayroll = true,
                    AffectsAttendance = true,
                    CreatedAt = now,
                    CreatedBy = systemUserId
                });
            }
            
            
            if (!await db.Statuses.AnyAsync(s => s.Code.ToLower() == "retired"))
            {
                db.Statuses.Add(new Models.Referentiel.Status
                {
                    Code = "RETIRED",
                    NameFr = "Retraité",
                    NameAr = "??????",
                    NameEn = "Retired",
                    IsActive = true,
                    AffectsAccess = true,
                    AffectsPayroll = true,
                    AffectsAttendance = true,
                    CreatedAt = now,
                    CreatedBy = systemUserId
                });
            }

            if (!await db.Statuses.AnyAsync(s => s.Code.ToLower() == "resigned"))
            {
                db.Statuses.Add(new Models.Referentiel.Status
                {
                    Code = "RESIGNED",
                    NameFr = "Démissionnaire",
                    NameAr = "?????",
                    NameEn = "Resigned",
                    IsActive = true,
                    AffectsAccess = true,
                    AffectsPayroll = true,
                    AffectsAttendance = true,
                    CreatedAt = now,
                    CreatedBy = systemUserId
                });
            }
            // ===== Genders =====
            if (!await db.Genders.AnyAsync(g => g.Code.ToLower() == "male"))
            {
                db.Genders.Add(new Models.Referentiel.Gender
                {
                    Code = "Male",
                    NameFr = "Homme",
                    NameAr = "???",
                    NameEn = "Male",
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = systemUserId
                });
            }

            if (!await db.Genders.AnyAsync(g => g.Code.ToLower() == "female"))
            {
                db.Genders.Add(new Models.Referentiel.Gender
                {
                    Code = "Female",
                    NameFr = "Femme",
                    NameAr = "????",
                    NameEn = "Female",
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = systemUserId
                });
            }

            // ===== Marital Statuses =====
            var maritalStatuses = new[]
            {
                new { Code = "SINGLE", NameFr = "Célibataire", NameAr = "????", NameEn = "Single" },
                new { Code = "MARRIED", NameFr = "Marié(e)", NameAr = "?????", NameEn = "Married" },
                new { Code = "DIVORCED", NameFr = "Divorcé(e)", NameAr = "????", NameEn = "Divorced" },
                new { Code = "WIDOWED", NameFr = "Veuf / Veuve", NameAr = "????", NameEn = "Widowed" },
                new { Code = "PARTNER", NameFr = "En union libre", NameAr = "???? ????", NameEn = "Partner" }
            };

            foreach (var ms in maritalStatuses)
            {
                if (!await db.MaritalStatuses.AnyAsync(m => m.Code.ToLower() == ms.Code.ToLower()))
                {
                    db.MaritalStatuses.Add(new Models.Referentiel.MaritalStatus
                    {
                        Code = ms.Code,
                        NameFr = ms.NameFr,
                        NameAr = ms.NameAr,
                        NameEn = ms.NameEn,
                        IsActive = true,
                        CreatedAt = now,
                        CreatedBy = systemUserId
                    });
                }
            }

            // ===== Education Levels =====
            var educationLevels = new[]
            {
                new { Code = "NONE", NameFr = "Sans diplôme", NameAr = "??? ?????", NameEn = "No formal education", Order = 1 },
                new { Code = "PRIMARY", NameFr = "Primaire", NameAr = "???????", NameEn = "Primary", Order = 2 },
                new { Code = "SECONDARY", NameFr = "Secondaire", NameAr = "?????", NameEn = "Secondary", Order = 3 },
                new { Code = "BACC", NameFr = "Baccalauréat", NameAr = "????????", NameEn = "Baccalaureate", Order = 4 },
                new { Code = "LIC", NameFr = "Licence", NameAr = "??????", NameEn = "Bachelor", Order = 5 },
                new { Code = "MASTER", NameFr = "Master", NameAr = "???????", NameEn = "Master", Order = 6 },
                new { Code = "PHD", NameFr = "Doctorat", NameAr = "???????", NameEn = "Doctorate", Order = 7 }
            };

            foreach (var el in educationLevels)
            {
                if (!await db.EducationLevels.AnyAsync(e => e.Code.ToLower() == el.Code.ToLower()))
                {
                    db.EducationLevels.Add(new Models.Referentiel.EducationLevel
                    {
                        Code = el.Code,
                        NameFr = el.NameFr,
                        NameAr = el.NameAr,
                        NameEn = el.NameEn,
                        LevelOrder = el.Order,
                        IsActive = true,
                        CreatedAt = now,
                        CreatedBy = systemUserId
                    });
                }
            }

            // ===== Countries / Cities (minimal) =====
            if (!await db.Countries.AnyAsync(c => c.CountryCode.ToLower() == "mar"))
            {
                var morocco = new Models.Referentiel.Country
                {
                    CountryName = "Morocco",
                    CountryNameAr = "??????",
                    CountryCode = "MAR",
                    CountryPhoneCode = "+212",
                    CreatedAt = now,
                    CreatedBy = systemUserId
                };
                db.Countries.Add(morocco);
                await db.SaveChangesAsync(); // besoin de Id pour la ville

                if (!await db.Cities.AnyAsync(c => c.CountryId == morocco.Id && c.CityName.ToLower() == "casablanca"))
                {
                    db.Cities.Add(new Models.Referentiel.City
                    {
                        CityName = "Casablanca",
                        CountryId = morocco.Id,
                        CreatedAt = now,
                        CreatedBy = systemUserId
                    });
                }
            }

            // ===== Roles minimaux =====
            if (!await db.Roles.AnyAsync(r => r.Name.ToLower() == "Admin Payzen"))
            {
                db.Roles.Add(new Models.Permissions.Roles
                {
                    Name = "Admin Payzen",
                    Description = "Admin Payzen",
                    CreatedAt = now,
                    CreatedBy = systemUserId
                });
            }
            if (!await db.Roles.AnyAsync(r => r.Name.ToLower() == "admin"))
            {
                db.Roles.Add(new Models.Permissions.Roles
                {
                    Name = "Admin",
                    Description = "Administrateur système",
                    CreatedAt = now,
                    CreatedBy = systemUserId
                });
            }

            if (!await db.Roles.AnyAsync(r => r.Name.ToLower() == "employee"))
            {
                db.Roles.Add(new Models.Permissions.Roles
                {
                    Name = "employee",
                    Description = "Rôle employé par défaut",
                    CreatedAt = now,
                    CreatedBy = systemUserId
                });
            }

            if (!await db.Roles.AnyAsync(r => r.Name.ToLower() == "rh"))
            {
                db.Roles.Add(new Models.Permissions.Roles
                {
                    Name = "RH",
                    Description = "Ressources Humaines",
                    CreatedAt = now,
                    CreatedBy = systemUserId
                });
            }

            await db.SaveChangesAsync();


            // ===== LeaveType for statutory/legal events (if absent) =====
            var legalLeaveType = await db.LeaveTypes
                .FirstOrDefaultAsync(lt => lt.LeaveCode == "LEGAL" && lt.DeletedAt == null);

            if (legalLeaveType == null)
            {
                legalLeaveType = new LeaveType
                {
                    LeaveCode = "LEGAL",
                    LeaveNameFr = "Congés légaux",
                    LeaveNameEn = "Statutory leaves",
                    LeaveNameAr = "?????? ???????",
                    LeaveDescription = "Congés prévus par la législation du travail (mariage, décès, naissance, etc.)",
                    Scope = LeaveScope.Global,
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = systemUserId
                };
                db.LeaveTypes.Add(legalLeaveType);
                await db.SaveChangesAsync();
            }

            // ===== Seed LeaveTypeLegalRule (idempotent) =====
            var legalRules = new[]
            {
                new {
                    Code = "MARRIAGE_EMPLOYEE",
                    Description = "Mariage du salarié",
                    Days = 4,
                    Article = "Article 274",
                    CanBeDiscontinuous = false,
                    MustWithinDays = (int?)null
                },
                new {
                    Code = "MARRIAGE_CHILD_OR_SPOUSE_CHILD",
                    Description = "Mariage d'un enfant ou d'un enfant du conjoint",
                    Days = 2,
                    Article = "Article 274",
                    CanBeDiscontinuous = false,
                    MustWithinDays = (int?)null
                },
                new {
                    Code = "DEATH_CLOSE",
                    Description = "Décès d'un conjoint, d'un enfant, d'un petit-enfant, d'un ascendant du salarié ou d'un enfant issu d'un précédent mariage du conjoint",
                    Days = 3,
                    Article = "Article 274",
                    CanBeDiscontinuous = false,
                    MustWithinDays = (int?)null
                },
                new {
                    Code = "DEATH_SIBLING",
                    Description = "Décès d'un frère, d'une sœur du salarié, d'un frère ou d'une sœur du conjoint ou d'un ascendant du conjoint",
                    Days = 2,
                    Article = "Article 274",
                    CanBeDiscontinuous = false,
                    MustWithinDays = (int?)null
                },
                new {
                    Code = "CIRCUMCISION",
                    Description = "Circoncision (du salarié)",
                    Days = 2,
                    Article = "Article 274",
                    CanBeDiscontinuous = false,
                    MustWithinDays = (int?)null
                },
                new {
                    Code = "SURGICAL_OPERATION",
                    Description = "Opération chirurgicale du conjoint ou d'un enfant à charge",
                    Days = 2,
                    Article = "Article 274",
                    CanBeDiscontinuous = false,
                    MustWithinDays = (int?)null
                },
                new {
                    Code = "BIRTH",
                    Description = "Naissance (inclus dans la période d'un mois à compter de la date de la naissance)",
                    Days = 2,
                    Article = "Article 269",
                    CanBeDiscontinuous = false,
                    MustWithinDays = (int?)30
                }
            };

            foreach (var rule in legalRules)
            {
                var exists = await db.LeaveTypeLegalRules
                    .AnyAsync(r => r.EventCaseCode.ToLower() == rule.Code.ToLower() && r.LeaveTypeId == legalLeaveType.Id);

                if (!exists)
                {
                    db.LeaveTypeLegalRules.Add(new LeaveTypeLegalRule
                    {
                        LeaveTypeId = legalLeaveType.Id,
                        EventCaseCode = rule.Code,
                        Description = rule.Description,
                        DaysGranted = rule.Days,
                        LegalArticle = rule.Article,
                        CanBeDiscountinuous = rule.CanBeDiscontinuous,
                        MustBeUsedWithinDays = rule.MustWithinDays,
                        CreatedAt = now,
                        CreatedBy = systemUserId
                    });
                }
            }

            await db.SaveChangesAsync();

            // ===== Création d'une company + employee + user admin (idempotent) =====
            var adminCompanyEmail = "admin@payzen.local";
            if (!await db.Companies.AnyAsync(c => c.Email.ToLower() == adminCompanyEmail.ToLower()))
            {
                // récupérer country / city existants (créés plus haut)
                var country = await db.Countries.FirstOrDefaultAsync(c => c.CountryCode.ToUpper() == "MAR" || c.CountryName.ToLower() == "morocco");
                if (country == null)
                {
                    // si absent (très improbable ici), quitter la création d'admin
                    return;
                }

                var city = await db.Cities.FirstOrDefaultAsync(c => c.CountryId == country.Id && c.CityName.ToLower() == "casablanca");
                if (city == null)
                {
                    city = new Models.Referentiel.City
                    {
                        CityName = "Casablanca",
                        CountryId = country.Id,
                        CreatedAt = now,
                        CreatedBy = systemUserId
                    };
                    db.Cities.Add(city);
                    await db.SaveChangesAsync();
                }

                // Créer company
                var company = new Models.Company.Company
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
                    CreatedBy = systemUserId
                };

                db.Companies.Add(company);
                await db.SaveChangesAsync();

                // Créer employé admin
                var activeStatus = await db.Statuses.FirstOrDefaultAsync(s => s.Code.ToLower() == "active");
                var adminEmployee = new Models.Employee.Employee
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
                    CreatedBy = systemUserId
                };

                db.Employees.Add(adminEmployee);
                await db.SaveChangesAsync();

                // Créer user admin (si inexistant)
                if (!await db.Users.AnyAsync(u => u.Email.ToLower() == adminCompanyEmail.ToLower()))
                {
                    // générer username simple et unique
                    var baseUsername = "admin";
                    var username = baseUsername;
                    var suffix = 1;
                    while (await db.Users.AnyAsync(u => u.Username == username && u.DeletedAt == null))
                    {
                        username = baseUsername + suffix;
                        suffix++;
                    }

                    // mot de passe temporaire : changez-le après premier démarrage en prod
                    var tempPassword = "Admin@123"; // remplacer en production par un flow sécurisé

                    var user = new Models.Users.Users
                    {
                        EmployeeId = adminEmployee.Id,
                        Username = username,
                        Email = adminCompanyEmail,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                        IsActive = true,
                        CreatedAt = now,
                        CreatedBy = systemUserId
                    };

                    db.Users.Add(user);
                    await db.SaveChangesAsync();

                    // Assigner rôle Admin
                    var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == "Admin Payzen");
                    if (adminRole != null)
                    {
                        db.UsersRoles.Add(new Models.Permissions.UsersRoles
                        {
                            UserId = user.Id,
                            RoleId = adminRole.Id,
                            CreatedAt = now,
                            CreatedBy = systemUserId
                        });
                        await db.SaveChangesAsync();
                    }
                }
            }

            // ===== Règles de majoration des heures supplémentaires =====
            if (!await db.OvertimeRateRules.AnyAsync())
            {
                var overtimeRules = new[]
                {
                    // Règles pour jours normaux (Standard)
                    new {
                        Code = "STD_DAY",
                        NameFr = "Heures supplémentaires jours normaux",
                        NameEn = "Standard day overtime",
                        NameAr = "????? ?????? ???? ?????",
                        Description = "Heures supplémentaires effectuées les jours ouvrables normaux",
                        AppliesTo = OvertimeType.Standard,
                        Multiplier = 1.25m,
                        Priority = 10,
                        Category = "Standard",
                        TimeRangeType = TimeRangeType.AllDay
                    },
                    
                    // Règles pour travail de nuit
                    new {
                        Code = "NIGHT_STD",
                        NameFr = "Travail de nuit standard",
                        NameEn = "Standard night work",
                        NameAr = "??? ???? ????",
                        Description = "Travail de nuit (21h-6h) les jours ouvrables",
                        AppliesTo = OvertimeType.Standard | OvertimeType.Night,
                        Multiplier = 1.50m,
                        Priority = 5,
                        Category = "Night",
                        TimeRangeType = TimeRangeType.AllDay
                    },
                    
                    // Règles pour repos hebdomadaire
                    new {
                        Code = "WEEKLY_REST",
                        NameFr = "Travail jour de repos",
                        NameEn = "Weekly rest day work",
                        NameAr = "??? ??? ???? ???????",
                        Description = "Travail effectué pendant le jour de repos hebdomadaire",
                        AppliesTo = OvertimeType.WeeklyRest,
                        Multiplier = 1.50m,
                        Priority = 5,
                        Category = "WeeklyRest",
                        TimeRangeType = TimeRangeType.AllDay
                    },
                    
                    // Règles pour repos hebdomadaire + nuit
                    new {
                        Code = "WEEKLY_REST_NIGHT",
                        NameFr = "Travail de nuit jour de repos",
                        NameEn = "Night work on rest day",
                        NameAr = "??? ???? ??? ????",
                        Description = "Travail de nuit pendant le jour de repos hebdomadaire",
                        AppliesTo = OvertimeType.WeeklyRest | OvertimeType.Night,
                        Multiplier = 2.00m,
                        Priority = 1,
                        Category = "WeeklyRest+Night",
                        TimeRangeType = TimeRangeType.AllDay
                    },
                    
                    // Règles pour jours fériés
                    new {
                        Code = "HOLIDAY",
                        NameFr = "Travail jour férié",
                        NameEn = "Public holiday work",
                        NameAr = "??? ??? ????",
                        Description = "Travail effectué pendant un jour férié",
                        AppliesTo = OvertimeType.PublicHoliday,
                        Multiplier = 2.00m,
                        Priority = 5,
                        Category = "Holiday",
                        TimeRangeType = TimeRangeType.AllDay
                    },
                    
                    // Règles pour jours fériés + nuit
                    new {
                        Code = "HOLIDAY_NIGHT",
                        NameFr = "Travail de nuit jour férié",
                        NameEn = "Night work on holiday",
                        NameAr = "??? ???? ??? ????",
                        Description = "Travail de nuit pendant un jour férié",
                        AppliesTo = OvertimeType.PublicHoliday | OvertimeType.Night,
                        Multiplier = 2.50m,
                        Priority = 1,
                        Category = "Holiday+Night",
                        TimeRangeType = TimeRangeType.AllDay
                    }
                };

                foreach (var rule in overtimeRules)
                {
                    db.OvertimeRateRules.Add(new Models.Referentiel.OvertimeRateRule
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
                        EffectiveFrom = null,
                        EffectiveTo = null,
                        CreatedAt = now,
                        CreatedBy = systemUserId
                    });
                }

                await db.SaveChangesAsync();
                Console.WriteLine("? Règles d'overtime créées avec succès");
            }

            // Seed Nationalities
            if (!await db.Nationalities.AnyAsync())
            {
                var nationalities = new[]
                {
                    new Nationality { Name = "Marocain" },
                    new Nationality { Name = "Algérien" },
                    new Nationality { Name = "Tunisien" },
                    new Nationality { Name = "Libyen" },
                    new Nationality { Name = "Libanais" },
                    new Nationality { Name = "Syrien" },
                    new Nationality { Name = "Palestinien" },
                    new Nationality { Name = "Jordanien" },
                    new Nationality { Name = "Iraqien" },
                    new Nationality { Name = "Saudi" },
                    new Nationality { Name = "Yémeni" },
                };

                foreach (var nationality in nationalities)
                {
                    db.Nationalities.Add(nationality);
                }
                await db.SaveChangesAsync();
                Console.WriteLine("? Nationalités créées avec succès");
            }
            // ===== Seed Default State Legal Contract Types and State Employment Programs =====
            var stateEmploymentPrograms = new[]
            {
                // Régime normal
                new StateEmploymentProgram
                {
                    Code = "NONE",
                    Name = "Régime normal",
                    IsIrExempt = false,
                    IsCnssEmployeeExempt = false,
                    IsCnssEmployerExempt = false,
                    MaxDurationMonths = null,
                    SalaryCeiling = null,
                    CreatedBy = 1,
                    CreatedAt = DateTimeOffset.UtcNow
                },

                // ANAPEC IDMAJ - Modèle 1
                new StateEmploymentProgram
                {
                    Code = "IDMAJ_M1",
                    Name = "ANAPEC IDMAJ - Modèle 1",
                    IsIrExempt = true,
                    IsCnssEmployeeExempt = true,
                    IsCnssEmployerExempt = true,
                    MaxDurationMonths = 24,
                    SalaryCeiling = 3125m,
                    CreatedBy = 0,
                    CreatedAt = DateTimeOffset.UtcNow
                },

                // ANAPEC IDMAJ - Modèle 2
                new StateEmploymentProgram
                {
                    Code = "IDMAJ_M2",
                    Name = "ANAPEC IDMAJ - Modèle 2",
                    IsIrExempt = true,
                    IsCnssEmployeeExempt = true,
                    IsCnssEmployerExempt = true,
                    MaxDurationMonths = 24,
                    SalaryCeiling = 6000m,
                    CreatedBy = 0,
                    CreatedAt = DateTimeOffset.UtcNow
                },

                // ANAPEC IDMAJ - Modèle 3
                new StateEmploymentProgram
                {
                    Code = "IDMAJ_M3",
                    Name = "ANAPEC IDMAJ - Modèle 3",
                    IsIrExempt = false,
                    IsCnssEmployeeExempt = true,
                    IsCnssEmployerExempt = true,
                    MaxDurationMonths = 24,
                    SalaryCeiling = 6000m,
                    CreatedBy = 0,
                    CreatedAt = DateTimeOffset.UtcNow
                },

                // TAHFIZ
                new StateEmploymentProgram
                {
                    Code = "TAHFIZ",
                    Name = "Programme TAHFIZ",
                    IsIrExempt = true,
                    IsCnssEmployeeExempt = true,
                    IsCnssEmployerExempt = true,
                    MaxDurationMonths = 24,
                    SalaryCeiling = 10000m,
                    CreatedBy = 1,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            };

            // Ajoutez ensuite le code pour insérer ces programmes dans la base si besoin
            foreach (var prog in stateEmploymentPrograms)
            {
                if (!await db.StateEmploymentPrograms.AnyAsync(p => p.Code == prog.Code))
                {
                    db.StateEmploymentPrograms.Add(prog);
                }
            }
            await db.SaveChangesAsync();
        }
    }
}
