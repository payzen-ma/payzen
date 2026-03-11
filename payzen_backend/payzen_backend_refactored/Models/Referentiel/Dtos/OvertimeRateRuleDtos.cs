using System.ComponentModel.DataAnnotations;
using payzen_backend.Models.Common.OvertimeEnums;

namespace payzen_backend.Models.Referentiel.Dtos
{
    /// <summary>
    /// DTO pour cr�er une r�gle de majoration overtime
    /// </summary>
    public class OvertimeRateRuleCreateDto
    {
        [Required(ErrorMessage = "Le code est requis")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Le code doit contenir entre 2 et 50 caract�res")]
        public required string Code { get; set; }

        [Required(ErrorMessage = "Le nom en fran�ais est requis")]
        [StringLength(200, MinimumLength = 2)]
        public required string NameFr { get; set; }

        [Required(ErrorMessage = "Le nom en arabe est requis")]
        [StringLength(200, MinimumLength = 2)]
        public required string NameAr { get; set; }

        [Required(ErrorMessage = "Le nom en anglais est requis")]
        [StringLength(200, MinimumLength = 2)]
        public required string NameEn { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Le type d'overtime est requis")]
        public required OvertimeType AppliesTo { get; set; }

        [Required]
        public TimeRangeType TimeRangeType { get; set; } = TimeRangeType.AllDay;

        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }

        /// <summary>
        /// Bitmask des jours : 1=Lun, 2=Mar, 4=Mer, 8=Jeu, 16=Ven, 32=Sam, 64=Dim
        /// NULL = tous les jours
        /// </summary>
        [Range(1, 127, ErrorMessage = "Valeur invalide pour les jours de la semaine")]
        public int? ApplicableDaysOfWeek { get; set; }

        [Required]
        [Range(1.00, 10.00, ErrorMessage = "Le multiplicateur doit �tre entre 1.00 et 10.00")]
        public decimal Multiplier { get; set; }

        [Required]
        public MultiplierCumulationStrategy CumulationStrategy { get; set; } = MultiplierCumulationStrategy.TakeMaximum;

        [Required]
        [Range(1, 100, ErrorMessage = "La priorit� doit �tre entre 1 et 100")]
        public int Priority { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        public bool IsActive { get; set; } = true;

        public DateOnly? EffectiveFrom { get; set; }

        public DateOnly? EffectiveTo { get; set; }

        [Range(0.01, 24.00)]
        public decimal? MinimumDurationHours { get; set; }

        [Range(0.01, 24.00)]
        public decimal? MaximumDurationHours { get; set; }

        public bool RequiresSuperiorApproval { get; set; }

        [StringLength(200)]
        public string? LegalReference { get; set; }

        [StringLength(500)]
        [Url(ErrorMessage = "URL invalide")]
        public string? DocumentationUrl { get; set; }
    }

    /// <summary>
    /// DTO pour lire une r�gle de majoration overtime
    /// </summary>
    public class OvertimeRateRuleReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string NameFr { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string? Description { get; set; }
        public OvertimeType AppliesTo { get; set; }
        public TimeRangeType TimeRangeType { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public int? ApplicableDaysOfWeek { get; set; }
        public decimal Multiplier { get; set; }
        public MultiplierCumulationStrategy CumulationStrategy { get; set; }
        public int Priority { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public decimal? MinimumDurationHours { get; set; }
        public decimal? MaximumDurationHours { get; set; }
        public bool RequiresSuperiorApproval { get; set; }
        public string? LegalReference { get; set; }
        public string? DocumentationUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO pour mettre � jour une r�gle de majoration overtime
    /// </summary>
    public class OvertimeRateRuleUpdateDto
    {
        [StringLength(200, MinimumLength = 2)]
        public string? NameFr { get; set; }

        [StringLength(200, MinimumLength = 2)]
        public string? NameAr { get; set; }

        [StringLength(200, MinimumLength = 2)]
        public string? NameEn { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public OvertimeType? AppliesTo { get; set; }
        public TimeRangeType? TimeRangeType { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }

        [Range(1, 127)]
        public int? ApplicableDaysOfWeek { get; set; }

        [Range(1.00, 10.00)]
        public decimal? Multiplier { get; set; }

        public MultiplierCumulationStrategy? CumulationStrategy { get; set; }

        [Range(1, 100)]
        public int? Priority { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        public bool? IsActive { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }

        [Range(0.01, 24.00)]
        public decimal? MinimumDurationHours { get; set; }

        [Range(0.01, 24.00)]
        public decimal? MaximumDurationHours { get; set; }

        public bool? RequiresSuperiorApproval { get; set; }

        [StringLength(200)]
        public string? LegalReference { get; set; }

        [StringLength(500)]
        [Url]
        public string? DocumentationUrl { get; set; }
    }
}