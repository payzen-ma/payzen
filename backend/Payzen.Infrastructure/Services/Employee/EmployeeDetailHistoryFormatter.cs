using Payzen.Application.Common;
using Payzen.Application.DTOs.Employee;
using Payzen.Domain.Entities.Events;

namespace Payzen.Infrastructure.Services.Employee;

internal static class EmployeeDetailHistoryFormatter
{
    public static List<EmployeeDetailHistoryEventDto> Build(
        IReadOnlyList<EmployeeEventLog> logs,
        bool canViewSensitiveDetails,
        IReadOnlyDictionary<int, (string Name, string Role)> modifiersByUserId
    )
    {
        var list = new List<EmployeeDetailHistoryEventDto>(logs.Count);
        foreach (var log in logs)
        {
            if (!modifiersByUserId.TryGetValue(log.CreatedBy, out var mod))
                mod = ("Système", "Système");

            var dto = MapLog(log, canViewSensitiveDetails, mod.Name, mod.Role);
            if (dto != null)
                list.Add(dto);
        }

        return list;
    }

    private static EmployeeDetailHistoryEventDto? MapLog(
        EmployeeEventLog log,
        bool rh,
        string modifierName,
        string modifierRole
    )
    {
        var mb = new EmployeeDetailHistoryModifierDto { Name = modifierName, Role = modifierRole };
        var dateStr = log.CreatedAt.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);

        return log.eventName switch
        {
            EmployeeEventLogNames.SalaryUpdated => new EmployeeDetailHistoryEventDto
            {
                Type = "salary_increase",
                Title = "Augmentation de salaire",
                Date = dateStr,
                Description = rh
                    ? $"Salaire de base: {log.oldValue} MAD → {log.newValue} MAD"
                    : "Modification du salaire de base",
                Details = rh
                    ? new EmployeeHistoryEventDetailsDto
                    {
                        OldSalary = log.oldValue,
                        NewSalary = log.newValue,
                        Currency = "MAD",
                    }
                    : null,
                ModifiedBy = mb,
                Timestamp = log.CreatedAt,
            },
            EmployeeEventLogNames.JobPositionChanged => new EmployeeDetailHistoryEventDto
            {
                Type = "promotion",
                Title = "Promotion",
                Date = dateStr,
                Description = rh ? $"{log.oldValue} → {log.newValue}" : "Changement de poste",
                Details = rh
                    ? new EmployeeHistoryEventDetailsDto { OldPosition = log.oldValue, NewPosition = log.newValue }
                    : null,
                ModifiedBy = mb,
                Timestamp = log.CreatedAt,
            },
            EmployeeEventLogNames.ContractCreated => new EmployeeDetailHistoryEventDto
            {
                Type = "contract_created",
                Title = "Nouveau contrat",
                Date = dateStr,
                Description = rh ? $"Contrat créé: {log.newValue}" : "Nouveau contrat créé",
                Details = rh ? new EmployeeHistoryEventDetailsDto { ContractInfo = log.newValue } : null,
                ModifiedBy = mb,
                Timestamp = log.CreatedAt,
            },
            EmployeeEventLogNames.ContractTerminated => new EmployeeDetailHistoryEventDto
            {
                Type = "contract_terminated",
                Title = "Fin de contrat",
                Date = dateStr,
                Description = rh ? $"Contrat {log.oldValue} terminé le {log.newValue}" : "Fin de contrat",
                Details = rh
                    ? new EmployeeHistoryEventDetailsDto { ContractInfo = log.oldValue, EndDate = log.newValue }
                    : null,
                ModifiedBy = mb,
                Timestamp = log.CreatedAt,
            },
            EmployeeEventLogNames.StatusChanged => MapStatusChanged(log, rh, mb, dateStr),
            EmployeeEventLogNames.DepartmentChanged => new EmployeeDetailHistoryEventDto
            {
                Type = "department_change",
                Title = "Changement de département",
                Date = dateStr,
                Description = rh ? $"{log.oldValue} → {log.newValue}" : "Département modifié",
                Details = rh
                    ? new EmployeeHistoryEventDetailsDto { OldDepartment = log.oldValue, NewDepartment = log.newValue }
                    : null,
                ModifiedBy = mb,
                Timestamp = log.CreatedAt,
            },
            EmployeeEventLogNames.ManagerChanged => new EmployeeDetailHistoryEventDto
            {
                Type = "manager_change",
                Title = "Changement de manager",
                Date = dateStr,
                Description = rh ? $"{log.oldValue ?? "Aucun"} → {log.newValue}" : "Manager modifié",
                Details = rh
                    ? new EmployeeHistoryEventDetailsDto { OldManager = log.oldValue, NewManager = log.newValue }
                    : null,
                ModifiedBy = mb,
                Timestamp = log.CreatedAt,
            },
            EmployeeEventLogNames.AddressUpdated or EmployeeEventLogNames.AddressCreated =>
                new EmployeeDetailHistoryEventDto
                {
                    Type =
                        log.eventName == EmployeeEventLogNames.AddressCreated ? "address_created" : "address_updated",
                    Title =
                        log.eventName == EmployeeEventLogNames.AddressCreated
                            ? "Ajout d'adresse"
                            : "Modification d'adresse",
                    Date = dateStr,
                    Description = rh
                        ? (log.oldValue != null ? $"{log.oldValue} → {log.newValue}" : log.newValue ?? string.Empty)
                        : "Adresse modifiée",
                    Details = rh
                        ? new EmployeeHistoryEventDetailsDto { OldAddress = log.oldValue, NewAddress = log.newValue }
                        : null,
                    ModifiedBy = mb,
                    Timestamp = log.CreatedAt,
                },
            _ => MapDefaultEvent(log, rh, mb, dateStr),
        };
    }

    private static EmployeeDetailHistoryEventDto MapStatusChanged(
        EmployeeEventLog log,
        bool rh,
        EmployeeDetailHistoryModifierDto mb,
        string dateStr
    )
    {
        var statusTitle = "Changement de statut";
        var statusDescription = "Statut modifié";

        if (
            log.newValue != null
            && (
                log.newValue.Contains("Actif", StringComparison.OrdinalIgnoreCase)
                || log.newValue.Contains("Active", StringComparison.OrdinalIgnoreCase)
            )
            && log.oldValue != null
            && (
                log.oldValue.Contains("Essai", StringComparison.OrdinalIgnoreCase)
                || log.oldValue.Contains("Trial", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            statusTitle = "Fin de période d'essai";
            statusDescription = "Période d'essai validée avec succès";
        }
        else if (rh)
        {
            statusDescription = $"{log.oldValue} → {log.newValue}";
        }

        return new EmployeeDetailHistoryEventDto
        {
            Type = "status_change",
            Title = statusTitle,
            Date = dateStr,
            Description = statusDescription,
            Details = rh
                ? new EmployeeHistoryEventDetailsDto { OldStatus = log.oldValue, NewStatus = log.newValue }
                : null,
            ModifiedBy = mb,
            Timestamp = log.CreatedAt,
        };
    }

    private static EmployeeDetailHistoryEventDto MapDefaultEvent(
        EmployeeEventLog log,
        bool rh,
        EmployeeDetailHistoryModifierDto mb,
        string dateStr
    )
    {
        var eventTitle = log.eventName switch
        {
            EmployeeEventLogNames.FirstNameChanged => "Modification du prénom",
            EmployeeEventLogNames.LastNameChanged => "Modification du nom",
            EmployeeEventLogNames.EmailChanged => "Modification de l'email",
            EmployeeEventLogNames.PhoneChanged => "Modification du téléphone",
            EmployeeEventLogNames.CinNumberChanged => "Modification du CIN",
            EmployeeEventLogNames.DateOfBirthChanged => "Modification de la date de naissance",
            EmployeeEventLogNames.GenderChanged => "Modification du genre",
            EmployeeEventLogNames.MaritalStatusChanged => "Modification du statut marital",
            EmployeeEventLogNames.NationalityChanged => "Modification de la nationalité",
            EmployeeEventLogNames.EducationLevelChanged => "Modification du niveau d'éducation",
            EmployeeEventLogNames.CnssNumberChanged => "Modification du numéro CNSS",
            EmployeeEventLogNames.CimrNumberChanged => "Modification du numéro CIMR",
            EmployeeEventLogNames.ContractTypeChanged => "Modification du type de contrat",
            _ => "Modification",
        };

        return new EmployeeDetailHistoryEventDto
        {
            Type = "general_update",
            Title = eventTitle,
            Date = dateStr,
            Description = rh
                ? $"{log.oldValue ?? "N/A"} → {log.newValue ?? "N/A"}"
                : "Une modification a été effectuée.",
            Details = rh
                ? new EmployeeHistoryEventDetailsDto { OldValue = log.oldValue, NewValue = log.newValue }
                : null,
            ModifiedBy = mb,
            Timestamp = log.CreatedAt,
        };
    }
}
