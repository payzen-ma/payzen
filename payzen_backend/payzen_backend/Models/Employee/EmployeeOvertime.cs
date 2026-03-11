using System.ComponentModel.DataAnnotations;
using payzen_backend.Models.Common.OvertimeEnums;
using payzen_backend.Models.Company;
using payzen_backend.Models.Referentiel;

namespace payzen_backend.Models.Employee
{
    /// <summary>
    /// Enregistrement d'heures supplémentaires pour un employé
    /// </summary>
    public class EmployeeOvertime
    {
        public int Id { get; set; }

        // ========== Informations de base ==========

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public OvertimeType OverTimeType { get; set; }

        [Required]
        public OvertimeEntryMode EntryMode { get; set; }
        public int? HolidayId { get; set; }
        public Holiday? Holiday { get; set; }

        [Required]
        public DateOnly OvertimeDate { get; set; }

        // ========== Plage horaire (conditionnel) ==========

        /// <summary>
        /// Heure de début (obligatoire si EntryMode = HoursRange)
        /// </summary>
        public TimeOnly? StartTime { get; set; }

        /// <summary>
        /// Heure de fin (obligatoire si EntryMode = HoursRange)
        /// </summary>
        public TimeOnly? EndTime { get; set; }

        /// <summary>
        /// Indique si la plage traverse minuit (calculé automatiquement)
        /// </summary>
        public bool CrossesMidnight { get; set; }

        // ========== Durée et calcul ==========

        /// <summary>
        /// Durée totale en heures décimales
        /// - HoursRange: calculée automatiquement
        /// - DurationOnly: saisie manuellement
        /// - FullDay: valeur standard entreprise (ex: 8.00)
        /// </summary>
        [Required]
        [Range(0.01, 24.00)]
        public decimal DurationInHours { get; set; }

        /// <summary>
        /// Durée standard d'une journée complète (snapshot au moment de création)
        /// Utilisé uniquement si EntryMode = FullDay
        /// </summary>
        public decimal? StandardDayHours { get; set; }

        // ========== Règle de majoration ==========

        /// <summary>
        /// ID de la règle principale appliquée
        /// NULL si aucune règle trouvée (overtime à 100%)
        /// </summary>
        public int? RateRuleId { get; set; }

        /// <summary>
        /// Snapshot du code de la règle (pour traçabilité)
        /// </summary>
        [MaxLength(50)]
        public string? RateRuleCodeApplied { get; set; }

        /// <summary>
        /// Snapshot du nom de la règle (multilingue selon contexte)
        /// </summary>
        [MaxLength(200)]
        public string? RateRuleNameApplied { get; set; }

        /// <summary>
        /// Multiplicateur final appliqué (snapshot au moment de l'approbation)
        /// Peut être le résultat d'un cumul de plusieurs règles
        /// </summary>
        [Required]
        [Range(1.00, 10.00)]
        public decimal RateMultiplierApplied { get; set; } = 1.00m;

        /// <summary>
        /// Détail du calcul de majoration (JSON)
        /// Exemple: {"baseRule": "HS_JOUR_25", "nightRule": "HS_NUIT_50", "strategy": "Multiply", "result": 1.875}
        /// </summary>
        [MaxLength(1000)]
        public string? MultiplierCalculationDetails { get; set; }

        // ========== Split batch ==========

        /// <summary>
        /// GUID de batch pour grouper les segments issus du même split automatique
        /// NULL = pas de split (overtime simple)
        /// </summary>
        public Guid? SplitBatchId { get; set; }

        /// <summary>
        /// Position dans le split (1, 2, 3...)
        /// NULL si pas de split
        /// </summary>
        public int? SplitSequence { get; set; }

        /// <summary>
        /// Nombre total de segments dans le batch
        /// NULL si pas de split
        /// </summary>
        public int? SplitTotalSegments { get; set; }

        // ========== Workflow ==========

        [Required]
        public OvertimeStatus Status { get; set; } = OvertimeStatus.Draft;

        /// <summary>
        /// Commentaire de l'employé (lors de soumission)
        /// </summary>
        [MaxLength(500)]
        public string? EmployeeComment { get; set; }

        /// <summary>
        /// Commentaire du manager (lors de approbation/rejet)
        /// </summary>
        [MaxLength(500)]
        public string? ManagerComment { get; set; }

        /// <summary>
        /// ID du manager ayant approuvé/rejeté
        /// </summary>
        public int? ApprovedBy { get; set; }

        /// <summary>
        /// Date d'approbation/rejet
        /// </summary>
        public DateTimeOffset? ApprovedAt { get; set; }

        // ========== Intégration paie ==========

        /// <summary>
        /// Indique si cet overtime a été inclus dans une paie
        /// </summary>
        public bool IsProcessedInPayroll { get; set; }

        /// <summary>
        /// ID du batch de paie qui a traité cet overtime
        /// </summary>
        public int? PayrollBatchId { get; set; }

        /// <summary>
        /// Date de traitement dans la paie
        /// </summary>
        public DateTimeOffset? ProcessedInPayrollAt { get; set; }

        // ========== Audit ==========

        [Required]
        public int CreatedBy { get; set; }

        [Required]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public int? ModifiedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }

        public int? DeletedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }

        /// <summary>
        /// Version pour optimistic locking
        /// </summary>
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // ========== Navigation ==========

        public Employee Employee { get; set; } = null!;
        public OvertimeRateRule? RateRule { get; set; }

        // ========== Méthodes helper ==========

        /// <summary>
        /// Vérifie si cet overtime fait partie d'un split
        /// </summary>
        public bool IsSplit() => SplitBatchId.HasValue;

        /// <summary>
        /// Vérifie si l'overtime peut être modifié
        /// </summary>
        public bool CanBeModified() => Status is OvertimeStatus.Draft or OvertimeStatus.Rejected;

        /// <summary>
        /// Vérifie si l'overtime peut être supprimé
        /// </summary>
        public bool CanBeDeleted() => !IsProcessedInPayroll && Status != OvertimeStatus.Approved;
    }
}
