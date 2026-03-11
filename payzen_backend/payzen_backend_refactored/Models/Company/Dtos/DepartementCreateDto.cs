using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Company.Dtos
{
    public class DepartementCreateDto
    {
        [Required(ErrorMessage = "Le nom du département est requis")]
        [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom du département doit contenir entre 2 et 500 caractères")]
        public required string DepartementName { get; set; }

        [Required(ErrorMessage = "L'ID de la société est requis")]
        public required int CompanyId { get; set; }
    }
}
