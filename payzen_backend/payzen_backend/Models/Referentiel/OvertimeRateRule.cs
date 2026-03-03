using System.ComponentModel.DataAnnotations;
using payzen_backend.Models.Common.OvertimeEnums;
using payzen_backend.Models.Employee;

namespace payzen_backend.Models.Referentiel
{
    /// <summary>
    /// Règle de majoration pour heures supplémentaires
    /// </summary>
    public class OvertimeRateRule
    {
        public int Id { get; set; }

        // ========== Identification ==========

        /// <summary>
        /// Code unique de la règle (ex: HS_JOUR_25, HS_NUIT_50, FERIE_JOUR_100)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Nom en arabe
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string NameAr { get; set; } = string.Empty;

        /// <summary>
        /// Nom en français
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string NameFr { get; set; } = string.Empty;

        /// <summary>
        /// Nom en anglais
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string NameEn { get; set; } = string.Empty;

        /// <summary>
        /// Description détaillée
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        // ========== Application ==========

        /// <summary>
        /// Type d'overtime concerné
        /// </summary>
        [Required]
        public OvertimeType AppliesTo { get; set; }
        // Exemple de règles :
        // Règle 1: AppliesTo = Standard, TimeRange = 08:00-18:00, Multiplier = 1.25
        // Règle 2: AppliesTo = WeeklyRest, TimeRange = AllDay, Multiplier = 1.50
        // Règle 3: AppliesTo = PublicHoliday, TimeRange = AllDay, Multiplier = 2.00
        // Règle 4: AppliesTo = Night, TimeRange = 21:00-06:00, Multiplier = 1.50


        /// <summary>
        /// Type de plage horaire
        /// </summary>
        [Required]
        public TimeRangeType TimeRangeType { get; set; } = TimeRangeType.AllDay;

        /// <summary>
        /// Heure de début de la plage (NULL si AllDay)
        /// </summary>
        public TimeOnly? StartTime { get; set; }

        /// <summary>
        /// Heure de fin de la plage (NULL si AllDay)
        /// </summary>
        public TimeOnly? EndTime { get; set; }

        /// <summary>
        /// Jours de la semaine concernés (bitmask)
        /// 1=Lundi, 2=Mardi, 4=Mercredi, 8=Jeudi, 16=Vendredi, 32=Samedi, 64=Dimanche
        /// NULL = tous les jours
        /// Exemple: 96 (64+32) = Samedi + Dimanche
        /// </summary>
        public int? ApplicableDaysOfWeek { get; set; }

        // ========== Majoration ==========

        /// <summary>
        /// Multiplicateur (ex: 1.25 pour +25%, 2.00 pour +100%)
        /// </summary>
        [Required]
        [Range(1.00, 10.00)]
        public decimal Multiplier { get; set; }

        /// <summary>
        /// Stratégie de cumul si plusieurs règles s'appliquent
        /// </summary>
        [Required]
        public MultiplierCumulationStrategy CumulationStrategy { get; set; } = MultiplierCumulationStrategy.TakeMaximum;

        // ========== Priorité ==========

        /// <summary>
        /// Niveau de priorité (1 = plus haute priorité)
        /// Utilisé pour déterminer quelle règle appliquer en cas de chevauchement
        /// </summary>
        [Required]
        [Range(1, 100)]
        public int Priority { get; set; }

        /// <summary>
        /// Catégorie de règle (pour faciliter le tri)
        /// Exemples: "BASE", "NUIT", "WEEKEND", "FERIE"
        /// </summary>
        [MaxLength(50)]
        public string? Category { get; set; }

        // ========== Validité ==========

        /// <summary>
        /// Indique si la règle est active
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Date de début de validité
        /// </summary>
        public DateOnly? EffectiveFrom { get; set; }

        /// <summary>
        /// Date de fin de validité (NULL = indéfinie)
        /// </summary>
        public DateOnly? EffectiveTo { get; set; }

        // ========== Contraintes métier ==========

        /// <summary>
        /// Durée minimum pour que la règle s'applique (en heures)
        /// NULL = pas de minimum
        /// </summary>
        [Range(0.01, 24.00)]
        public decimal? MinimumDurationHours { get; set; }

        /// <summary>
        /// Durée maximum pour que la règle s'applique (en heures)
        /// NULL = pas de maximum
        /// </summary>
        [Range(0.01, 24.00)]
        public decimal? MaximumDurationHours { get; set; }

        /// <summary>
        /// Nécessite approbation niveau supérieur (ex: DRH pour taux > 200%)
        /// </summary>
        public bool RequiresSuperiorApproval { get; set; }

        // ========== Référence légale ==========

        /// <summary>
        /// Référence de la loi/convention (ex: "Code du Travail Art. 184")
        /// </summary>
        [MaxLength(200)]
        public string? LegalReference { get; set; }

        /// <summary>
        /// Lien vers documentation externe
        /// </summary>
        [MaxLength(500)]
        public string? DocumentationUrl { get; set; }

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

        public ICollection<EmployeeOvertime> OvertimeRecords { get; set; } = new List<EmployeeOvertime>();

        // ========== Méthodes helper ==========

        /// <summary>
        /// Vérifie si la règle est valide à une date donnée
        /// </summary>
        public bool IsValidOn(DateOnly date)
        {
            return IsActive
                && date >= EffectiveFrom
                && (EffectiveTo == null || date <= EffectiveTo);
        }

        /// <summary>
        /// Vérifie si une plage horaire chevauche cette règle
        /// </summary>
        public bool OverlapsTimeRange(TimeOnly start, TimeOnly end)
        {
            if (TimeRangeType == TimeRangeType.AllDay) return true;
            if (StartTime == null || EndTime == null) return false;

            // Logique de chevauchement selon TimeRangeType
            return TimeRangeType switch
            {
                TimeRangeType.SameDay => start < EndTime && end > StartTime,
                TimeRangeType.CrossesMidnight => true, // Logique plus complexe nécessaire
                _ => false
            };
        }
    }
}