using System.ComponentModel.DataAnnotations;

namespace payzen_backend.DTOs.Payroll.Referentiel
{
    public class BusinessSectorDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsStandard { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateBusinessSectorDto
    {
        [Required(ErrorMessage = "Le code est requis")]
        [MaxLength(50, ErrorMessage = "Le code ne peut pas dépasser 50 caractères")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est requis")]
        [MaxLength(200, ErrorMessage = "Le nom ne peut pas dépasser 200 caractères")]
        public string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; } = 0;
    }

    public class UpdateBusinessSectorDto
    {
        [Required(ErrorMessage = "Le code est requis")]
        [MaxLength(50, ErrorMessage = "Le code ne peut pas dépasser 50 caractères")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est requis")]
        [MaxLength(200, ErrorMessage = "Le nom ne peut pas dépasser 200 caractères")]
        public string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; } = 0;
    }
}
