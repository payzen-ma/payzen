using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Company.Dtos
{
    public class JobPositionCreateDto
    {
        [Required(ErrorMessage = "Le nom du poste est requis")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Le nom du poste doit contenir entre 2 et 200 caract�res")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "L'identifiant de la soci�t� est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'identifiant de la soci�t� doit �tre valide")]
        public int CompanyId { get; set; }
    }
}
