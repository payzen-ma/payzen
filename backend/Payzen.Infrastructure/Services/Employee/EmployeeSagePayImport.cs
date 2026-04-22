using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Employee;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Auth;
using Payzen.Domain.Entities.Company;
using Payzen.Domain.Entities.Employee;
using Payzen.Domain.Entities.Referentiel;
using Payzen.Infrastructure.Persistence;
using DomainEmployee = Payzen.Domain.Entities.Employee.Employee;

namespace Payzen.Infrastructure.Services.Employee;

internal static class EmployeeSagePayImport
{
    public static async Task<ServiceResult<SageImportResultDto>> ExecuteAsync(
        AppDbContext db,
        IEmployeeEventLogService eventLog,
        Stream csvStream,
        int targetCompanyId,
        int userId,
        int? month,
        int? year,
        bool preview,
        CancellationToken ct
    )
    {
        if (!csvStream.CanSeek)
        {
            var ms = new MemoryStream();
            await csvStream.CopyToAsync(ms, ct);
            ms.Position = 0;
            csvStream = ms;
        }

        // Ancien contrôleur : Code == "Active" ; seed monolithe : souvent "ACTIVE"
        var defaultStatus = await db
            .Statuses.AsNoTracking()
            .FirstOrDefaultAsync(
                s =>
                    s.IsActive
                    && s.DeletedAt == null
                    && (s.Code == "Active" || s.Code == "ACTIVE" || s.Code == "active"),
                ct
            );
        if (defaultStatus == null)
            return ServiceResult<SageImportResultDto>.Fail(
                "Statut actif introuvable dans la base (codes Active / ACTIVE)."
            );

        var genders = await db.Genders.AsNoTracking().Where(g => g.IsActive && g.DeletedAt == null).ToListAsync(ct);

        var rows = ParseCsvRows(csvStream);
        var result = new SageImportResultDto { TotalProcessed = rows.Count };

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNum = i + 2;
            var fullName = $"{row.Prenom?.Trim()} {row.Nom?.Trim()}".Trim();

            try
            {
                var prenom = row.Prenom?.Trim();
                var nom = row.Nom?.Trim();
                var matriculeRaw = row.Matricule?.Trim();
                var dateNaissanceRaw = row.DateNaissance?.Trim();
                var dateEntreeRaw = row.DateEntree?.Trim();
                var situationFamilialeRaw = row.SituationFamiliale?.Trim();
                var posteRaw = row.EmploiOccupe?.Trim();
                var tauxHoraireRaw = row.TauxHoraire?.Trim();

                if (string.IsNullOrWhiteSpace(prenom))
                    throw new InvalidOperationException("Le prénom est requis");
                if (string.IsNullOrWhiteSpace(nom))
                    throw new InvalidOperationException("Le nom est requis");
                if (string.IsNullOrWhiteSpace(dateNaissanceRaw))
                    throw new InvalidOperationException("La date de naissance est requise");
                if (string.IsNullOrWhiteSpace(dateEntreeRaw))
                    throw new InvalidOperationException("La date d'embauche est requise");
                if (string.IsNullOrWhiteSpace(situationFamilialeRaw))
                    throw new InvalidOperationException("La situation familiale est requise");
                if (string.IsNullOrWhiteSpace(posteRaw))
                    throw new InvalidOperationException("Le poste (emploi occupé) est requis");
                if (string.IsNullOrWhiteSpace(tauxHoraireRaw))
                    throw new InvalidOperationException("Le taux horaire est requis");

                if (!TryParseDateOnlyFlexible(dateNaissanceRaw, out var dateOfBirth))
                    throw new InvalidOperationException(
                        $"Format de date de naissance invalide : '{row.DateNaissance}'. Format attendu : JJ/MM/AAAA"
                    );

                if (!TryParseDateTimeFlexible(dateEntreeRaw, out var parsedStart))
                    throw new InvalidOperationException(
                        $"Format de date d'entrée invalide : '{row.DateEntree}'. Format attendu : JJ/MM/AAAA"
                    );
                var startDate = (DateTime?)parsedStart;

                var tauxClean = tauxHoraireRaw.Replace(" ", "").Replace(",", ".");
                if (!decimal.TryParse(tauxClean, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedHourly))
                    throw new InvalidOperationException($"Format de taux horaire invalide : '{row.TauxHoraire}'");
                var hourlyRate = (decimal?)parsedHourly;

                var allMaritals = await db.MaritalStatuses.AsNoTracking().ToListAsync(ct);
                var resolvedMarital = ResolveMaritalStatus(allMaritals, situationFamilialeRaw);
                if (resolvedMarital == null)
                    throw new InvalidOperationException("Situation familiale non trouvée");

                var maritalStatusId = resolvedMarital.Id;

                int? genderId = null;
                if (!string.IsNullOrWhiteSpace(row.Genre))
                {
                    var genderCode = row.Genre.Trim().ToUpperInvariant();
                    var genderMatch = genders.FirstOrDefault(g =>
                        g.Code.Equals(genderCode, StringComparison.OrdinalIgnoreCase)
                        || g.NameFr.StartsWith(genderCode, StringComparison.OrdinalIgnoreCase)
                        || g.NameEn.StartsWith(genderCode, StringComparison.OrdinalIgnoreCase)
                    );
                    genderId = genderMatch?.Id;
                }

                JobPosition? jobPosition = null;
                if (!string.IsNullOrWhiteSpace(posteRaw))
                {
                    var posteKey = posteRaw.Trim();
                    var jpList = await db
                        .JobPositions.AsNoTracking()
                        .Where(jp => jp.CompanyId == targetCompanyId && jp.DeletedAt == null)
                        .ToListAsync(ct);
                    jobPosition = jpList.FirstOrDefault(j =>
                        j.Name.Equals(posteKey, StringComparison.OrdinalIgnoreCase)
                    );

                    if (jobPosition == null && !preview)
                    {
                        var newJp = new JobPosition
                        {
                            Name = posteKey,
                            CompanyId = targetCompanyId,
                            CreatedBy = userId,
                        };
                        db.JobPositions.Add(newJp);
                        await db.SaveChangesAsync(ct);
                        jobPosition = newJp;
                    }
                }

                int? matriculeParsed = null;
                if (!string.IsNullOrWhiteSpace(matriculeRaw) && int.TryParse(matriculeRaw, out var mval))
                    matriculeParsed = mval;

                var cinTrimmed = string.IsNullOrWhiteSpace(row.CIN) ? null : row.CIN.Trim();
                var emailRaw = string.IsNullOrWhiteSpace(row.Email) ? null : row.Email.Trim();
                var phoneRaw = string.IsNullOrWhiteSpace(row.Telephone) ? null : row.Telephone.Trim();

                DomainEmployee? existingEmployee = null;
                if (matriculeParsed.HasValue)
                {
                    existingEmployee = await db
                        .Employees.Include(e => e.Salaries)
                        .Include(e => e.Contracts)
                        .Include(e => e.MaritalStatus)
                        .FirstOrDefaultAsync(
                            e =>
                                e.Matricule == matriculeParsed && e.CompanyId == targetCompanyId && e.DeletedAt == null,
                            ct
                        );
                }

                if (existingEmployee == null && !string.IsNullOrWhiteSpace(cinTrimmed))
                {
                    existingEmployee = await db
                        .Employees.Include(e => e.Salaries)
                        .Include(e => e.Contracts)
                        .Include(e => e.MaritalStatus)
                        .FirstOrDefaultAsync(
                            e => e.CinNumber == cinTrimmed && e.CompanyId == targetCompanyId && e.DeletedAt == null,
                            ct
                        );
                }

                if (existingEmployee == null)
                {
                    existingEmployee = await db
                        .Employees.Include(e => e.Salaries)
                        .Include(e => e.Contracts)
                        .Include(e => e.MaritalStatus)
                        .FirstOrDefaultAsync(
                            e =>
                                e.FirstName == prenom
                                && e.LastName == nom
                                && e.DateOfBirth == dateOfBirth
                                && e.CompanyId == targetCompanyId
                                && e.DeletedAt == null,
                            ct
                        );
                }
                else if (string.IsNullOrWhiteSpace(existingEmployee.CnssNumber) && existingEmployee.Matricule.HasValue)
                    existingEmployee.CnssNumber = $"CNSS{existingEmployee.Matricule.Value}";

                if (existingEmployee == null)
                {
                    var cinToUse = !string.IsNullOrWhiteSpace(cinTrimmed)
                        ? cinTrimmed
                        : $"IMPORT-{targetCompanyId}-{DateTime.UtcNow:yyyyMMddHHmmss}-{i}";
                    var phoneToUse = !string.IsNullOrWhiteSpace(phoneRaw) ? phoneRaw : "0000000000";
                    var emailToUse = !string.IsNullOrWhiteSpace(emailRaw)
                        ? emailRaw
                        : $"import.{targetCompanyId}.{DateTime.UtcNow:yyyyMMddHHmmss}.{i}@import.local";

                    var assignedMatricule = matriculeParsed ?? await GenerateUniqueMatricule(db, targetCompanyId, ct);

                    var employee = new DomainEmployee
                    {
                        FirstName = prenom,
                        LastName = nom,
                        CinNumber = cinToUse,
                        DateOfBirth = dateOfBirth,
                        Phone = phoneToUse,
                        Email = emailToUse,
                        CompanyId = targetCompanyId,
                        StatusId = defaultStatus.Id,
                        GenderId = genderId,
                        CnssNumber = assignedMatricule.HasValue ? $"CNSS{assignedMatricule.Value}" : null,
                        Matricule = assignedMatricule,
                        MaritalStatusId = maritalStatusId,
                        CreatedBy = userId,
                    };

                    if (!preview)
                    {
                        db.Employees.Add(employee);
                        await db.SaveChangesAsync(ct);

                        await eventLog.LogSimpleEventAsync(
                            employee.Id,
                            EmployeeEventLogNames.EmployeeCreated,
                            null,
                            $"{employee.FirstName} {employee.LastName} (Import Sage - Matricule: {employee.Matricule})",
                            userId,
                            ct
                        );

                        EmployeeContract? createdContract = null;
                        if (jobPosition != null)
                        {
                            var defaultCt = await db
                                .ContractTypes.AsNoTracking()
                                .FirstOrDefaultAsync(c => c.CompanyId == targetCompanyId && c.DeletedAt == null, ct);
                            if (defaultCt != null)
                            {
                                var employeeContract = new EmployeeContract
                                {
                                    EmployeeId = employee.Id,
                                    CompanyId = targetCompanyId,
                                    JobPositionId = jobPosition.Id,
                                    ContractTypeId = defaultCt.Id,
                                    StartDate = startDate ?? DateTime.UtcNow,
                                    EndDate = null,
                                    CreatedBy = userId,
                                };
                                db.EmployeeContracts.Add(employeeContract);
                                await db.SaveChangesAsync(ct);
                                createdContract = employeeContract;

                                var contractInfo = $"{defaultCt.ContractTypeName} — {jobPosition.Name}";
                                await eventLog.LogSimpleEventAsync(
                                    employee.Id,
                                    EmployeeEventLogNames.ContractCreated,
                                    null,
                                    jobPosition.Name,
                                    userId,
                                    ct
                                );
                            }
                        }

                        if (hourlyRate.HasValue && hourlyRate.Value > 0 && createdContract != null)
                        {
                            var effectiveDate =
                                month.HasValue && year.HasValue
                                    ? new DateTime(year.Value, month.Value, 1)
                                    : startDate ?? DateTime.UtcNow;
                            var employeeSalary = new EmployeeSalary
                            {
                                EmployeeId = employee.Id,
                                ContractId = createdContract.Id,
                                BaseSalary = 0m,
                                BaseSalaryHourly = hourlyRate.Value,
                                EffectiveDate = effectiveDate,
                                EndDate = null,
                                CreatedBy = userId,
                            };
                            db.EmployeeSalaries.Add(employeeSalary);
                            await db.SaveChangesAsync(ct);
                            await eventLog.LogSimpleEventAsync(
                                employee.Id,
                                EmployeeEventLogNames.SalaryCreated,
                                null,
                                employeeSalary.BaseSalaryHourly?.ToString(CultureInfo.InvariantCulture),
                                userId,
                                ct
                            );
                        }

                        try
                        {
                            var casablancaCity = await db
                                .Cities.AsNoTracking()
                                .Where(c => c.DeletedAt == null)
                                .FirstOrDefaultAsync(
                                    c =>
                                        c.CityName == "Casablanca"
                                        || c.CityName == "CASABLANCA"
                                        || c.CityName.Trim().ToLowerInvariant() == "casablanca",
                                    ct
                                );

                            if (casablancaCity != null)
                            {
                                var employeeAddress = new EmployeeAddress
                                {
                                    EmployeeId = employee.Id,
                                    CityId = casablancaCity.Id,
                                    AddressLine1 = string.IsNullOrWhiteSpace(row.Adresse)
                                        ? "Adresse inconnue"
                                        : row.Adresse.Trim(),
                                    AddressLine2 = null,
                                    ZipCode = "ZIP-CASA",
                                    CreatedBy = userId,
                                };
                                db.EmployeeAddresses.Add(employeeAddress);
                                await db.SaveChangesAsync(ct);
                                await eventLog.LogSimpleEventAsync(
                                    employee.Id,
                                    EmployeeEventLogNames.AddressCreated,
                                    null,
                                    $"{employeeAddress.AddressLine1}, Casablanca",
                                    userId,
                                    ct
                                );
                            }
                        }
                        catch
                        {
                            /* adresse par défaut optionnelle */
                        }

                        if (!string.IsNullOrWhiteSpace(emailRaw))
                        {
                            var baseUsername = GenerateUsername(employee.FirstName, employee.LastName);
                            var username = baseUsername;
                            var suffix = 1;
                            while (await db.Users.AnyAsync(u => u.Username == username && u.DeletedAt == null, ct))
                            {
                                username = GenerateUsername(employee.FirstName, employee.LastName, suffix);
                                suffix++;
                            }

                            var createdUser = new Users
                            {
                                EmployeeId = employee.Id,
                                Username = username,
                                Email = employee.Email,
                                IsActive = true,
                                CreatedBy = userId,
                            };
                            db.Users.Add(createdUser);
                            await db.SaveChangesAsync(ct);

                            var defaultRole = await db
                                .Roles.AsNoTracking()
                                .FirstOrDefaultAsync(
                                    r =>
                                        (
                                            r.Name.Equals("employee", StringComparison.OrdinalIgnoreCase)
                                            || r.Name.Equals("Employee", StringComparison.OrdinalIgnoreCase)
                                        )
                                        && r.DeletedAt == null,
                                    ct
                                );
                            if (defaultRole != null)
                            {
                                db.UsersRoles.Add(
                                    new UsersRoles
                                    {
                                        UserId = createdUser.Id,
                                        RoleId = defaultRole.Id,
                                        CreatedBy = userId,
                                    }
                                );
                                await db.SaveChangesAsync(ct);
                            }
                        }
                    }

                    result.SuccessCount++;
                    result.Created.Add(
                        new SageImportCreatedItemDto
                        {
                            Id = preview ? 0 : employee.Id,
                            FullName = $"{prenom} {nom}",
                            Matricule = preview ? assignedMatricule : employee.Matricule,
                            Email = emailRaw ?? string.Empty,
                        }
                    );
                }
                else
                {
                    var changes = new List<string>();
                    if (!string.Equals(existingEmployee.FirstName?.Trim(), prenom, StringComparison.Ordinal))
                        changes.Add("FirstName");
                    if (!string.Equals(existingEmployee.LastName?.Trim(), nom, StringComparison.Ordinal))
                        changes.Add("LastName");
                    if (existingEmployee.DateOfBirth != dateOfBirth)
                        changes.Add("DateOfBirth");
                    if (
                        !string.IsNullOrWhiteSpace(emailRaw)
                        && !string.Equals(existingEmployee.Email?.Trim(), emailRaw, StringComparison.OrdinalIgnoreCase)
                    )
                        changes.Add("Email");
                    if (existingEmployee.MaritalStatusId != maritalStatusId)
                        changes.Add("MaritalStatus");

                    var activeContract = existingEmployee
                        .Contracts?.Where(c => c.DeletedAt == null && c.EndDate == null)
                        .OrderByDescending(c => c.StartDate)
                        .FirstOrDefault();
                    if (jobPosition != null && activeContract != null && activeContract.JobPositionId != jobPosition.Id)
                        changes.Add("JobPosition");

                    var activeSalary = existingEmployee
                        .Salaries?.Where(s => s.DeletedAt == null && s.EndDate == null)
                        .OrderByDescending(s => s.EffectiveDate)
                        .FirstOrDefault();
                    if (
                        hourlyRate.HasValue
                        && (activeSalary == null || activeSalary.BaseSalaryHourly != hourlyRate.Value)
                    )
                        changes.Add("HourlyRate");

                    if (changes.Count == 0)
                        continue;

                    if (preview)
                    {
                        result.SuccessCount++;
                        result.Updated.Add(
                            new SageImportUpdatedItemDto
                            {
                                Id = existingEmployee.Id,
                                FullName = $"{existingEmployee.FirstName} {existingEmployee.LastName}",
                                Matricule = existingEmployee.Matricule,
                                Email = existingEmployee.Email,
                            }
                        );
                    }
                    else
                    {
                        if (changes.Contains("FirstName"))
                        {
                            await eventLog.LogSimpleEventAsync(
                                existingEmployee.Id,
                                EmployeeEventLogNames.FirstNameChanged,
                                existingEmployee.FirstName,
                                prenom,
                                userId,
                                ct
                            );
                            existingEmployee.FirstName = prenom;
                        }

                        if (changes.Contains("LastName"))
                        {
                            await eventLog.LogSimpleEventAsync(
                                existingEmployee.Id,
                                EmployeeEventLogNames.LastNameChanged,
                                existingEmployee.LastName,
                                nom,
                                userId,
                                ct
                            );
                            existingEmployee.LastName = nom;
                        }

                        if (changes.Contains("DateOfBirth"))
                        {
                            await eventLog.LogSimpleEventAsync(
                                existingEmployee.Id,
                                EmployeeEventLogNames.DateOfBirthChanged,
                                existingEmployee.DateOfBirth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                                dateOfBirth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                                userId,
                                ct
                            );
                            existingEmployee.DateOfBirth = dateOfBirth;
                        }

                        if (changes.Contains("Email"))
                        {
                            var previousEmail = existingEmployee.Email;
                            await eventLog.LogSimpleEventAsync(
                                existingEmployee.Id,
                                EmployeeEventLogNames.EmailChanged,
                                previousEmail,
                                emailRaw,
                                userId,
                                ct
                            );
                            existingEmployee.Email = emailRaw;

                            var linkedUser = await db.Users
                                .FirstOrDefaultAsync(u => u.EmployeeId == existingEmployee.Id && u.DeletedAt == null, ct);
                            if (linkedUser != null)
                            {
                                var emailAlreadyUsed = await db.Users.AnyAsync(
                                    u => u.Id != linkedUser.Id && u.DeletedAt == null && u.Email == emailRaw,
                                    ct
                                );
                                if (!emailAlreadyUsed)
                                    linkedUser.Email = emailRaw;
                            }
                        }

                        if (changes.Contains("MaritalStatus"))
                        {
                            var newMarital = await db
                                .MaritalStatuses.AsNoTracking()
                                .FirstOrDefaultAsync(m => m.Id == maritalStatusId, ct);
                            var oldMs = await db
                                .MaritalStatuses.AsNoTracking()
                                .FirstOrDefaultAsync(m => m.Id == existingEmployee.MaritalStatusId, ct);
                            await eventLog.LogSimpleEventAsync(
                                existingEmployee.Id,
                                EmployeeEventLogNames.MaritalStatusChanged,
                                oldMs?.Code,
                                newMarital?.Code,
                                userId,
                                ct
                            );
                            existingEmployee.MaritalStatusId = maritalStatusId;
                        }

                        EmployeeContract? contractForSalary = activeContract;

                        if (changes.Contains("JobPosition") && jobPosition != null)
                        {
                            var defaultCt = await db
                                .ContractTypes.AsNoTracking()
                                .FirstOrDefaultAsync(c => c.CompanyId == targetCompanyId && c.DeletedAt == null, ct);
                            if (defaultCt != null)
                            {
                                var employeeContract = new EmployeeContract
                                {
                                    EmployeeId = existingEmployee.Id,
                                    CompanyId = targetCompanyId,
                                    JobPositionId = jobPosition.Id,
                                    ContractTypeId = defaultCt.Id,
                                    StartDate = startDate ?? DateTime.UtcNow,
                                    EndDate = null,
                                    CreatedBy = userId,
                                };
                                db.EmployeeContracts.Add(employeeContract);
                                await db.SaveChangesAsync(ct);
                                contractForSalary = employeeContract;
                                await eventLog.LogSimpleEventAsync(
                                    existingEmployee.Id,
                                    EmployeeEventLogNames.JobPositionChanged,
                                    null,
                                    jobPosition.Name,
                                    userId,
                                    ct
                                );
                            }
                        }

                        if (
                            changes.Contains("HourlyRate")
                            && hourlyRate.HasValue
                            && hourlyRate.Value > 0
                            && contractForSalary != null
                        )
                        {
                            var oldHourly = activeSalary?.BaseSalaryHourly ?? 0m;
                            var effectiveDate =
                                month.HasValue && year.HasValue
                                    ? new DateTime(year.Value, month.Value, 1)
                                    : startDate ?? DateTime.UtcNow;
                            var empSalary = new EmployeeSalary
                            {
                                EmployeeId = existingEmployee.Id,
                                ContractId = contractForSalary.Id,
                                BaseSalary = 0m,
                                BaseSalaryHourly = hourlyRate.Value,
                                EffectiveDate = effectiveDate,
                                EndDate = null,
                                CreatedBy = userId,
                            };
                            db.EmployeeSalaries.Add(empSalary);
                            await eventLog.LogSimpleEventAsync(
                                existingEmployee.Id,
                                EmployeeEventLogNames.SalaryUpdated,
                                oldHourly.ToString(CultureInfo.InvariantCulture),
                                hourlyRate.Value.ToString(CultureInfo.InvariantCulture),
                                userId,
                                ct
                            );
                        }

                        await db.SaveChangesAsync(ct);

                        result.SuccessCount++;
                        result.Updated.Add(
                            new SageImportUpdatedItemDto
                            {
                                Id = existingEmployee.Id,
                                FullName = $"{existingEmployee.FirstName} {existingEmployee.LastName}",
                                Matricule = existingEmployee.Matricule,
                                Email = existingEmployee.Email,
                            }
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add(
                    new SageImportErrorDto
                    {
                        Row = rowNum,
                        FullName = string.IsNullOrWhiteSpace(fullName) ? $"Ligne {rowNum}" : fullName,
                        Message = ex.Message,
                    }
                );
            }
        }

        return ServiceResult<SageImportResultDto>.Ok(result);
    }

    /// <summary>Résolution situation familiale — logique de l’ancien EmployeeController.ImportFromSage.</summary>
    private static MaritalStatus? ResolveMaritalStatus(List<MaritalStatus> allMaritals, string? incomingMarital)
    {
        if (string.IsNullOrWhiteSpace(incomingMarital))
            return null;

        MaritalStatus? resolvedMarital = null;

        if (int.TryParse(incomingMarital, out var idm))
            resolvedMarital = allMaritals.FirstOrDefault(m => m.Id == idm);

        if (resolvedMarital == null)
            resolvedMarital = allMaritals.FirstOrDefault(m =>
                (
                    !string.IsNullOrWhiteSpace(m.Code)
                    && m.Code.Equals(incomingMarital, StringComparison.OrdinalIgnoreCase)
                )
                || (
                    !string.IsNullOrWhiteSpace(m.NameFr)
                    && m.NameFr.Equals(incomingMarital, StringComparison.OrdinalIgnoreCase)
                )
                || (
                    !string.IsNullOrWhiteSpace(m.NameEn)
                    && m.NameEn.Equals(incomingMarital, StringComparison.OrdinalIgnoreCase)
                )
            );

        if (resolvedMarital == null)
        {
            var normIncoming = NormalizeHeader(incomingMarital);
            foreach (var m in allMaritals)
            {
                var codeNorm = NormalizeHeader(m.Code);
                var nameFrNorm = NormalizeHeader(m.NameFr);
                var nameEnNorm = NormalizeHeader(m.NameEn);
                if (
                    !string.IsNullOrWhiteSpace(normIncoming)
                    && (codeNorm == normIncoming || nameFrNorm == normIncoming || nameEnNorm == normIncoming)
                )
                {
                    resolvedMarital = m;
                    break;
                }
            }
        }

        if (resolvedMarital == null)
        {
            var normIncoming = NormalizeHeader(incomingMarital);
            foreach (var m in allMaritals)
            {
                var codeNorm = NormalizeHeader(m.Code);
                var nameFrNorm = NormalizeHeader(m.NameFr);
                var nameEnNorm = NormalizeHeader(m.NameEn);
                if (
                    !string.IsNullOrWhiteSpace(normIncoming)
                    && (
                        codeNorm.Contains(normIncoming, StringComparison.Ordinal)
                        || nameFrNorm.Contains(normIncoming, StringComparison.Ordinal)
                        || nameEnNorm.Contains(normIncoming, StringComparison.Ordinal)
                        || normIncoming.Contains(codeNorm, StringComparison.Ordinal)
                        || normIncoming.Contains(nameFrNorm, StringComparison.Ordinal)
                        || normIncoming.Contains(nameEnNorm, StringComparison.Ordinal)
                    )
                )
                {
                    resolvedMarital = m;
                    break;
                }
            }
        }

        if (resolvedMarital == null)
        {
            var normIncoming = NormalizeHeader(incomingMarital);
            if (!string.IsNullOrWhiteSpace(normIncoming) && normIncoming.Length == 1)
            {
                resolvedMarital = allMaritals.FirstOrDefault(m =>
                    NormalizeHeader(m.Code).StartsWith(normIncoming, StringComparison.Ordinal)
                );
            }
        }

        if (resolvedMarital == null)
        {
            var normIncoming = NormalizeHeader(incomingMarital);
            var bestDist = int.MaxValue;
            MaritalStatus? best = null;
            foreach (var m in allMaritals)
            {
                var codeNorm = NormalizeHeader(m.Code);
                var nameFrNorm = NormalizeHeader(m.NameFr);
                var nameEnNorm = NormalizeHeader(m.NameEn);
                var d1 = LevenshteinDistance(normIncoming, codeNorm);
                var d2 = LevenshteinDistance(normIncoming, nameFrNorm);
                var d3 = LevenshteinDistance(normIncoming, nameEnNorm);
                var d = Math.Min(d1, Math.Min(d2, d3));
                if (d < bestDist)
                {
                    bestDist = d;
                    best = m;
                }
            }

            if (best != null && (bestDist <= 1 || (normIncoming.Length > 4 && bestDist <= 2)))
                resolvedMarital = best;
        }

        return resolvedMarital;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a))
            return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b))
            return a.Length;

        var n = a.Length;
        var m = b.Length;
        var d = new int[n + 1, m + 1];
        for (var i = 0; i <= n; i++)
            d[i, 0] = i;
        for (var j = 0; j <= m; j++)
            d[0, j] = j;
        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    private static async Task<int?> GenerateUniqueMatricule(AppDbContext db, int companyId, CancellationToken ct)
    {
        var has = await db.Employees.AnyAsync(
            e => e.CompanyId == companyId && e.DeletedAt == null && e.Matricule.HasValue,
            ct
        );
        if (!has)
            return 1;
        var maxMatricule = await db
            .Employees.Where(e => e.CompanyId == companyId && e.DeletedAt == null && e.Matricule.HasValue)
            .MaxAsync(e => e.Matricule!.Value, ct);
        return maxMatricule + 1;
    }

    private static string GenerateUsername(string firstName, string lastName, int suffix = 0)
    {
        var baseName = $"{firstName}.{lastName}"
            .ToLowerInvariant()
            .Replace(" ", "", StringComparison.Ordinal)
            .Replace("'", "", StringComparison.Ordinal);
        if (suffix > 0)
            baseName += suffix;
        return baseName;
    }

    private static List<SageImportRowDto> ParseCsvRows(Stream csvStream)
    {
        using var peekReader = new StreamReader(csvStream, leaveOpen: true);
        var firstLine = peekReader.ReadLine() ?? string.Empty;
        var delimiter = firstLine.Contains(';') ? ";" : ",";
        csvStream.Position = 0;

        using var reader = new StreamReader(csvStream, leaveOpen: true);
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null,
            Delimiter = delimiter,
            TrimOptions = TrimOptions.Trim,
        };

        using var csv = new CsvReader(reader, csvConfig);
        if (!csv.Read())
            return new List<SageImportRowDto>();

        csv.ReadHeader();
        var headerRecord = csv.HeaderRecord ?? Array.Empty<string>();
        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var h = 0; h < headerRecord.Length; h++)
        {
            var raw = headerRecord[h] ?? string.Empty;
            var key = NormalizeHeader(raw);
            if (!string.IsNullOrWhiteSpace(key) && !headerMap.ContainsKey(key))
                headerMap[key] = h;
        }

        string? CleanCell(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            var s = raw.Trim();
            if (s.StartsWith("=\"", StringComparison.Ordinal) && s.EndsWith('\"'))
                s = s.Substring(2, s.Length - 3);
            s = s.Trim('"', '\'', '=');
            s = s.Replace('\u00A0', ' ').Trim();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        string? GetByCandidates(CsvReader cr, params string[] candidates)
        {
            foreach (var c in candidates)
            {
                var candKey = NormalizeHeader(c ?? string.Empty);
                if (string.IsNullOrWhiteSpace(candKey))
                    continue;
                if (headerMap.TryGetValue(candKey, out var ix))
                {
                    var v = CleanCell(cr.GetField(ix));
                    if (!string.IsNullOrWhiteSpace(v))
                        return v;
                }
            }

            foreach (var kv in headerMap.OrderBy(x => x.Value))
            {
                foreach (var c in candidates)
                {
                    var candKey = NormalizeHeader(c ?? string.Empty);
                    if (string.IsNullOrWhiteSpace(candKey))
                        continue;
                    if (kv.Key.Contains(candKey, StringComparison.OrdinalIgnoreCase))
                    {
                        var v = CleanCell(cr.GetField(kv.Value));
                        if (!string.IsNullOrWhiteSpace(v))
                            return v;
                    }
                }
            }

            foreach (var kv in headerMap.OrderBy(x => x.Value))
            {
                foreach (var c in candidates)
                {
                    var candKey = NormalizeHeader(c ?? string.Empty);
                    if (string.IsNullOrWhiteSpace(candKey))
                        continue;
                    try
                    {
                        if (LevenshteinDistance(kv.Key, candKey) <= 1)
                        {
                            var v = CleanCell(cr.GetField(kv.Value));
                            if (!string.IsNullOrWhiteSpace(v))
                                return v;
                        }
                    }
                    catch
                    {
                        // ignore (ancien contrôleur)
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Évite que la recherche floue sur « nom » ne prenne la colonne « prenom » (sous-chaîne « nom »).
        /// </summary>
        string? GetNomFromRow(CsvReader cr)
        {
            foreach (
                var c in new[]
                {
                    "nom",
                    "nomdefamille",
                    "nomfamille",
                    "nomusage",
                    "nomnaissance",
                    "nompatronymique",
                    "nomjeunefille",
                }
            )
            {
                var candKey = NormalizeHeader(c);
                if (string.IsNullOrWhiteSpace(candKey))
                    continue;
                if (headerMap.TryGetValue(candKey, out var ix))
                {
                    var v = CleanCell(cr.GetField(ix));
                    if (!string.IsNullOrWhiteSpace(v))
                        return v;
                }
            }

            foreach (var kv in headerMap.OrderBy(x => x.Value))
            {
                if (kv.Key.StartsWith("prenom", StringComparison.OrdinalIgnoreCase))
                    continue;
                foreach (var c in new[] { "nom", "nomdefamille", "nomfamille", "nomusage" })
                {
                    var candKey = NormalizeHeader(c);
                    if (string.IsNullOrWhiteSpace(candKey))
                        continue;
                    if (!kv.Key.Contains(candKey, StringComparison.OrdinalIgnoreCase))
                        continue;
                    var v = CleanCell(cr.GetField(kv.Value));
                    if (!string.IsNullOrWhiteSpace(v))
                        return v;
                }
            }

            foreach (var kv in headerMap.OrderBy(x => x.Value))
            {
                if (kv.Key.StartsWith("prenom", StringComparison.OrdinalIgnoreCase))
                    continue;
                foreach (var c in new[] { "nom", "nomdefamille", "nomfamille", "nomusage" })
                {
                    var candKey = NormalizeHeader(c);
                    if (string.IsNullOrWhiteSpace(candKey))
                        continue;
                    try
                    {
                        if (LevenshteinDistance(kv.Key, candKey) <= 1)
                        {
                            var v = CleanCell(cr.GetField(kv.Value));
                            if (!string.IsNullOrWhiteSpace(v))
                                return v;
                        }
                    }
                    catch
                    { /* ignore */
                    }
                }
            }

            return null;
        }

        string? GetPrenomFromRow(CsvReader cr)
        {
            foreach (
                var c in new[]
                {
                    "prenom",
                    "preno",
                    "prenomusuel",
                    "prenomsocial",
                    "prenom1",
                    "prenom2",
                    "prenom3",
                    "firstname",
                    "givenname",
                    "prenomjeunefille",
                    "prnom",
                    "pren",
                }
            )
            {
                var candKey = NormalizeHeader(c);
                if (string.IsNullOrWhiteSpace(candKey))
                    continue;
                if (headerMap.TryGetValue(candKey, out var ix))
                {
                    var v = CleanCell(cr.GetField(ix));
                    if (!string.IsNullOrWhiteSpace(v))
                        return v;
                }
            }

            foreach (var kv in headerMap.OrderBy(x => x.Value))
            {
                if (kv.Key.Equals("nom", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (kv.Key.Contains("prenom", StringComparison.OrdinalIgnoreCase))
                {
                    var v = CleanCell(cr.GetField(kv.Value));
                    if (!string.IsNullOrWhiteSpace(v))
                        return v;
                }
            }

            foreach (var kv in headerMap.OrderBy(x => x.Value))
            {
                if (kv.Key.Equals("nom", StringComparison.OrdinalIgnoreCase))
                    continue;
                foreach (var c in new[] { "prenom", "preno", "prnom" })
                {
                    var candKey = NormalizeHeader(c);
                    if (string.IsNullOrWhiteSpace(candKey))
                        continue;
                    try
                    {
                        if (LevenshteinDistance(kv.Key, candKey) <= 1)
                        {
                            var v = CleanCell(cr.GetField(kv.Value));
                            if (!string.IsNullOrWhiteSpace(v))
                                return v;
                        }
                    }
                    catch
                    { /* ignore */
                    }
                }
            }

            return null;
        }

        static void TrySplitCombinedNameIntoDto(SageImportRowDto dto, string? combined)
        {
            if (string.IsNullOrWhiteSpace(combined))
                return;
            var parts = combined.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return;
            var nom = parts[^1].Trim();
            var prenom = string.Join(' ', parts.Take(parts.Length - 1)).Trim();
            if (string.IsNullOrWhiteSpace(dto.Nom))
                dto.Nom = nom;
            if (string.IsNullOrWhiteSpace(dto.Prenom))
                dto.Prenom = prenom;
        }

        static void TrySplitNomColumnIfMultipart(SageImportRowDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Prenom) || string.IsNullOrWhiteSpace(dto.Nom))
                return;
            var parts = dto.Nom!.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return;
            dto.Prenom = string.Join(' ', parts.Take(parts.Length - 1)).Trim();
            dto.Nom = parts[^1].Trim();
        }

        var rows = new List<SageImportRowDto>();
        while (csv.Read())
        {
            var dto = new SageImportRowDto
            {
                Matricule = GetByCandidates(csv, "matricule"),
                Nom = GetNomFromRow(csv),
                Prenom = GetPrenomFromRow(csv),
                CIN = GetByCandidates(csv, "cin"),
                Telephone = GetByCandidates(csv, "telephone", "tel", "phone", "mobile"),
                Email = GetByCandidates(csv, "email", "mail"),
                CNSS = GetByCandidates(csv, "cnss"),
                Salaire = GetByCandidates(csv, "salaire", "salbasemn", "salbase", "brut", "net"),
                DateEntree = GetByCandidates(csv, "dateentree", "dateembauche", "datedembauche", "dateembauch"),
                DateNaissance = GetByCandidates(csv, "datedenaissance", "datenais", "datenissance"),
                Genre = GetByCandidates(csv, "genre", "sexe"),
                Adresse = GetByCandidates(csv, "adresse", "address"),
                SituationFamiliale = GetByCandidates(
                    csv,
                    "situationfamiliale",
                    "situation",
                    "situationfam",
                    "situationfamille"
                ),
                EmploiOccupe = GetByCandidates(csv, "emploi", "emploioccupe", "emploi_occupe", "emploiocc"),
                TauxAnc = GetByCandidates(csv, "tauxanc", "tauxanciennete", "tauxancientete"),
                Anct = GetByCandidates(csv, "anct", "anciennete", "anciennetee", "anc"),
                TauxHoraire = GetByCandidates(csv, "tauxh", "tauth", "tauxhoraire", "taux_h"),
            };

            var combinedName = GetByCandidates(
                csv,
                "nomcomplet",
                "nomprenom",
                "nometprenom",
                "employe",
                "salarie",
                "libelle",
                "liblong",
                "intitule",
                "designation",
                "collaborateur",
                "identite",
                "name",
                "raisonsociale",
                "nomsalarie",
                "nomsalrie",
                "lib",
                "libellelong",
                "champsalarie"
            );
            TrySplitCombinedNameIntoDto(dto, combinedName);

            TrySplitNomColumnIfMultipart(dto);

            rows.Add(dto);
        }

        return rows;
    }

    private static string NormalizeHeader(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        var s = input.Trim();
        if (s.StartsWith("=\"", StringComparison.Ordinal) && s.EndsWith('\"'))
            s = s.Substring(2, s.Length - 3);
        s = s.Trim('"', '\'', '=');
        var normalized = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(ch))
                sb.Append(char.ToLowerInvariant(ch));
        }

        return sb.ToString();
    }

    private static bool TryParseDateOnlyFlexible(string? input, out DateOnly parsedDate)
    {
        parsedDate = default;
        if (string.IsNullOrWhiteSpace(input))
            return false;
        var s = input.Trim().Replace('\u00A0', ' ').Trim('"', '\'');
        var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yy", "d/M/yy" };
        if (DateOnly.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
            return true;

        var sep =
            s.Contains('/') ? '/'
            : s.Contains('-') ? '-'
            : (char?)null;
        if (sep != null)
        {
            var parts = s.Split((char)sep);
            if (
                parts.Length >= 3
                && int.TryParse(parts[0], out var d)
                && int.TryParse(parts[1], out var m)
                && int.TryParse(parts[2], out var y)
            )
            {
                if (parts[2].Length == 2)
                    y = y <= 50 ? 2000 + y : 1900 + y;
                try
                {
                    parsedDate = new DateOnly(y, m, d);
                    return true;
                }
                catch
                { /* ignore */
                }
            }
        }

        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            parsedDate = DateOnly.FromDateTime(dt);
            return true;
        }

        return false;
    }

    private static bool TryParseDateTimeFlexible(string? input, out DateTime parsedDateTime)
    {
        parsedDateTime = default;
        if (string.IsNullOrWhiteSpace(input))
            return false;
        var s = input.Trim().Replace('\u00A0', ' ').Trim('"', '\'');
        var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd/MM/yy", "d/M/yy" };
        if (DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime))
            return true;

        var sep =
            s.Contains('/') ? '/'
            : s.Contains('-') ? '-'
            : (char?)null;
        if (sep != null)
        {
            var parts = s.Split((char)sep);
            if (
                parts.Length >= 3
                && int.TryParse(parts[0], out var d)
                && int.TryParse(parts[1], out var m)
                && int.TryParse(parts[2], out var y)
            )
            {
                if (parts[2].Length == 2)
                    y = y <= 50 ? 2000 + y : 1900 + y;
                try
                {
                    parsedDateTime = new DateTime(y, m, d);
                    return true;
                }
                catch
                { /* ignore */
                }
            }
        }

        return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime);
    }
}
