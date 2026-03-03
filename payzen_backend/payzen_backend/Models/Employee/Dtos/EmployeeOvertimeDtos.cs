using System.ComponentModel.DataAnnotations;
using payzen_backend.Models.Common.OvertimeEnums;

namespace payzen_backend.Models.Employee.Dtos
{
    /// <summary>
    /// DTO pour créer un overtime (soumission par employé)
    /// </summary>
    public class EmployeeOvertimeCreateDto
    {
        [Required(ErrorMessage = "L'ID de l'employé est requis")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "La date est requise")]
        public required DateOnly OvertimeDate { get; set; }

        [Required(ErrorMessage = "Le mode de saisie est requis")]
        public OvertimeEntryMode EntryMode { get; set; }

        // ========== HoursRange Mode ==========
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }

        // ========== DurationOnly Mode ==========
        [Range(0.01, 24.00, ErrorMessage = "La durée doit être entre 0.01 et 24 heures")]
        public decimal? DurationInHours { get; set; }

        // ========== FullDay Mode ==========
        [Range(1.00, 24.00)]
        public decimal? StandardDayHours { get; set; }

        [StringLength(500)]
        public string? EmployeeComment { get; set; }
    }

    /// <summary>
    /// DTO pour lire un overtime
    /// </summary>
    public class EmployeeOvertimeReadDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeFullName { get; set; } = string.Empty;
        public OvertimeType OvertimeType { get; set; }
        public string OvertimeTypeDescription { get; set; } = string.Empty;
        public OvertimeEntryMode EntryMode { get; set; }
        public int? HolidayId { get; set; }
        public string? HolidayName { get; set; }
        public DateOnly OvertimeDate { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public bool CrossesMidnight { get; set; }
        public decimal DurationInHours { get; set; }
        public decimal? StandardDayHours { get; set; }

        // Règle appliquée (snapshot)
        public int? RateRuleId { get; set; }
        public string? RateRuleCodeApplied { get; set; }
        public string? RateRuleNameApplied { get; set; }
        public decimal RateMultiplierApplied { get; set; }
        public string? MultiplierCalculationDetails { get; set; }

        // Split automatique
        public Guid? SplitBatchId { get; set; }
        public int? SplitSequence { get; set; }
        public int? SplitTotalSegments { get; set; }

        // Workflow
        public OvertimeStatus Status { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
        public string? EmployeeComment { get; set; }
        public string? ManagerComment { get; set; }
        public int? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // Paie
        public bool IsProcessedInPayroll { get; set; }
        public int? PayrollBatchId { get; set; }
        public DateTime? ProcessedInPayrollAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO pour soumettre un overtime (passer de Draft à Submitted)
    /// </summary>
    public class EmployeeOvertimeSubmitDto
    {
        [StringLength(500)]
        public string? EmployeeComment { get; set; }
    }

    /// <summary>
    /// DTO pour approuver/rejeter un overtime (Manager)
    /// </summary>
    public class EmployeeOvertimeApprovalDto
    {
        [Required(ErrorMessage = "La décision est requise")]
        public OvertimeStatus Status { get; set; } // Approved ou Rejected

        [StringLength(500)]
        public string? ManagerComment { get; set; }
    }

    /// <summary>
    /// DTO pour mettre à jour un overtime (avant soumission)
    /// </summary>
    public class EmployeeOvertimeUpdateDto
    {
        public DateOnly? OvertimeDate { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }

        [Range(0.01, 24.00)]
        public decimal? DurationInHours { get; set; }

        [StringLength(500)]
        public string? EmployeeComment { get; set; }
    }

    /// <summary>
    /// DTO pour liste paginée avec filtres
    /// </summary>
    public class EmployeeOvertimeListDto
    {
        public int Id { get; set; }
        public string EmployeeFullName { get; set; } = string.Empty;
        public DateOnly OvertimeDate { get; set; }
        public OvertimeType OvertimeType { get; set; }
        public string OvertimeTypeDescription { get; set; } = string.Empty;
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }            
        public string? HolidayName { get; set; }          
        public string? RateRuleNameApplied { get; set; }  
        public string? EmployeeComment { get; set; }     
        public decimal DurationInHours { get; set; }
        public decimal RateMultiplierApplied { get; set; }
        public OvertimeStatus Status { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
        public bool IsProcessedInPayroll { get; set; }
        public DateTime CreatedAt { get; set; }
    }

/// <summary>
/// Helper static pour décrire les types d'overtime (flags combinés)
/// </summary>
public static class OvertimeTypeHelper
    {
        /// <summary>
        /// Retourne une description lisible d'un OvertimeType (gère les flags combinés)
        /// </summary>
        public static string GetDescription(OvertimeType type)
        {
            if (type == OvertimeType.None)
                return "Aucun";

            var parts = new List<string>();

            // Vérifier chaque flag individuellement
            if ((type & OvertimeType.PublicHoliday) != 0)
                parts.Add("Jour férié");
            else if ((type & OvertimeType.WeeklyRest) != 0)
                parts.Add("Repos hebdomadaire");
            else if ((type & OvertimeType.Standard) != 0)
                parts.Add("Standard");

            if ((type & OvertimeType.Night) != 0)
                parts.Add("Nuit");

            return parts.Count > 0 ? string.Join(" + ", parts) : type.ToString();
        }

        /// <summary>
        /// Retourne une description en anglais
        /// </summary>
        public static string GetDescriptionEn(OvertimeType type)
        {
            if (type == OvertimeType.None)
                return "None";

            var parts = new List<string>();

            if ((type & OvertimeType.PublicHoliday) != 0)
                parts.Add("Public Holiday");
            else if ((type & OvertimeType.WeeklyRest) != 0)
                parts.Add("Weekly Rest");
            else if ((type & OvertimeType.Standard) != 0)
                parts.Add("Standard");

            if ((type & OvertimeType.Night) != 0)
                parts.Add("Night");

            return parts.Count > 0 ? string.Join(" + ", parts) : type.ToString();
        }
    }
}
