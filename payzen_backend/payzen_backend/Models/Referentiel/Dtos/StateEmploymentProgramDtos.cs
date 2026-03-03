using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Referentiel.Dtos
{
    public class StateEmploymentProgramCreateDto
    {
        [Required(ErrorMessage = "Le code est requis")]
        [StringLength(30, MinimumLength = 2, ErrorMessage = "Le code doit contenir entre 2 et 30 caract�res")]
        public required string Code { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 200 caract�res")]
        public required string Name { get; set; }

        public bool IsCnssEmployeeExempt { get; set; } = false;
        public bool IsCnssEmployerExempt { get; set; } = false;
        public bool IsIrExempt { get; set; } = false;

        [Range(1, 120, ErrorMessage = "La dur�e maximale doit �tre entre 1 et 120 mois")]
        public int? MaxDurationMonths { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Le plafond salarial doit �tre positif")]
        public decimal? SalaryCeiling { get; set; }
    }

    public class StateEmploymentProgramReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsCnssEmployeeExempt { get; set; }
        public bool IsCnssEmployerExempt { get; set; }
        public bool IsIrExempt { get; set; }
        public int? MaxDurationMonths { get; set; }
        public decimal? SalaryCeiling { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class StateEmploymentProgramUpdateDto
    {
        [StringLength(30, MinimumLength = 2, ErrorMessage = "Le code doit contenir entre 2 et 30 caract�res")]
        public string? Code { get; set; }

        [StringLength(200, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 200 caract�res")]
        public string? Name { get; set; }

        public bool? IsCnssEmployeeExempt { get; set; }
        public bool? IsCnssEmployerExempt { get; set; }
        public bool? IsIrExempt { get; set; }

        [Range(1, 120, ErrorMessage = "La dur�e maximale doit �tre entre 1 et 120 mois")]
        public int? MaxDurationMonths { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Le plafond salarial doit �tre positif")]
        public decimal? SalaryCeiling { get; set; }
    }
}